using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Mark
{
    public Vector2[] points;           // world-space snapshot
    public float born;
    public float lifetime;             // how long THIS mark lives
    public LineRenderer lr;
}

public class PlayerAttack : MonoBehaviour
{
    public static PlayerAttack Instance;

    // ---- shared state (PlayerManager reads / calls these) ----
    static float attackEndTime;
    public static bool IsAttacking => Time.time < attackEndTime;
    public static void CancelAttack() => attackEndTime = 0f;

    [Header("Aim")]
    public Transform attackPivot;
    public float attackDuration = 0.25f;     // how long the player counts as "attacking"
    public float attackCooldown = 0.4f;      // minimum time between slashes (from the start of a slash)
    float nextAttackTime;

    [Header("Attack Spin")]
    public Transform spinVisual;             // the player's sprite child (a diamond looks the same every 90 degrees)
    public float spinDegrees = 90f;          // per attack (negative = other direction)
    public float spinTime = 0.15f;

    [Header("Slash Arc")]
    public float arcDistance = 5f;           // how far from the player the arc sits
    public float arcLength = 3f;             // length measured ALONG the curve
    public int arcPointCount = 4;

    [Header("Marks")]
    public Material lineMaterial;            // slash lines (Sprites/Default)
    public Material dashMaterial;            // dash lines
    public float lineWidth = 0.12f;
    public Color lineColor = new Color(0.4f, 0.9f, 1f, 1f);
    public float slashLifetime = 3f;         // how long slash marks last
    public float dashLifetime = 1.5f;        // how long dash marks last
    public int maxMarks = 8;
    public float endTolerance = 0.4f;        // leniency for near-miss crossings, in world units

    [Header("Loop")]
    public LayerMask enemyLayers;
    public float loopDuration = 2f;          // how long a loop is visible AND deals damage
    public float loopTickRate = 0.3f;        // seconds between damage ticks
    public float loopTickDamage = 1f;        // damage per tick
    public Material loopmateria;             // outline + fill material (Sprites/Default)
    public Color outlineColor = new Color(1f, 0.9f, 0.3f, 1f);
    public float fillOpacity = 1f;           // 1 = same look as the outline, lower = see-through
    public bool fadeLoop = true;             // false = stays fully bright until it ends

    public readonly List<Mark> marks = new List<Mark>();

    void Awake() => Instance = this;

    void Update() => AimPivot();

    void AimPivot()
    {
        attackPivot.right = Aim.DirFrom(transform.position, attackPivot.right);
    }

    // ================= INPUT =================
    public void OnAttack(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        if (PlayerManager.IsDashing || IsAttacking) return;
        if (Time.time < nextAttackTime) return;

        nextAttackTime = Time.time + attackCooldown;
        attackEndTime = Time.time + attackDuration;
        AimPivot();                          // make sure the arc uses the latest mouse position
        SpinVisual();
        AudioManager.Play(SFX.Slash);        // SFX
        CameraShake.Shake(0.06f, 0.08f);     // small slash kick
        Mark newest = SpawnMark();
        TryFormLoop(newest);
    }

    // ================= ATTACK SPIN =================
    float spinTarget;                        // total angle the sprite is heading to
    float spinCurrent;                       // angle it is at right now
    Coroutine spinRoutine;

    void SpinVisual()
    {
        if (!spinVisual) return;
        spinTarget += spinDegrees;
        if (spinRoutine != null) StopCoroutine(spinRoutine);
        spinRoutine = StartCoroutine(SpinTo(spinTarget));
    }

    IEnumerator SpinTo(float target)
    {
        float start = spinCurrent;               // continues smoothly even if the last spin was cut short
        float t = 0f;
        while (t < spinTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / spinTime);
            float eased = 1f - Mathf.Pow(1f - p, 3f);            // fast start, soft landing
            spinCurrent = Mathf.Lerp(start, target, eased);
            spinVisual.localRotation = Quaternion.Euler(0f, 0f, spinCurrent);
            yield return null;
        }
        spinCurrent = target;
        spinVisual.localRotation = Quaternion.Euler(0f, 0f, target);
        spinRoutine = null;
    }

    // called by PlayerManager when a dash finishes
    public void AddDashMark(Vector2 start, Vector2 end)
    {
        if ((end - start).sqrMagnitude < 0.25f) return;   // ignore tiny dashes
        Mark m = SpawnMark(new[] { start, end }, dashLifetime, dashMaterial);
        TryFormLoop(m);
    }

    // ================= MARKS =================
    // slash: points on an arc around the player, facing the mouse
    public Mark SpawnMark()
    {
        Vector2 origin = transform.position;
        float baseAngle = Mathf.Atan2(attackPivot.right.y, attackPivot.right.x);
        float span = Mathf.Min(arcLength / arcDistance, Mathf.PI * 2f);   // angle = length / radius

        var pts = new Vector2[arcPointCount];
        for (int i = 0; i < pts.Length; i++)
        {
            float a = baseAngle + Mathf.Lerp(-span / 2f, span / 2f, i / (float)(pts.Length - 1));
            pts[i] = origin + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * arcDistance;
        }
        return SpawnMark(pts, slashLifetime);
    }

    // any list of world points becomes a mark
    public Mark SpawnMark(Vector2[] pts, float life, Material mat = null)
    {
        var go = new GameObject("SlashMark");     // no parent, stays in the world
        var lr = go.AddComponent<LineRenderer>();
        lr.material = mat ? mat : lineMaterial;
        lr.useWorldSpace = true;
        lr.positionCount = pts.Length;
        for (int i = 0; i < pts.Length; i++)
            lr.SetPosition(i, pts[i]);
        lr.startWidth = lr.endWidth = lineWidth;
        lr.startColor = lr.endColor = lineColor;
        lr.sortingOrder = 5;

        var mark = new Mark { points = pts, born = Time.time, lifetime = life, lr = lr };
        marks.Add(mark);
        if (marks.Count > maxMarks) RemoveMark(0);
        return mark;
    }

    void RemoveMark(int i)
    {
        if (marks[i].lr) Destroy(marks[i].lr.gameObject);
        marks.RemoveAt(i);
    }

    // fade + expire
    void FixedUpdate()
    {
        for (int i = marks.Count - 1; i >= 0; i--)
        {
            float age = Time.time - marks[i].born;
            if (age >= marks[i].lifetime) { RemoveMark(i); continue; }

            Color c = lineColor;
            c.a = 1f - age / marks[i].lifetime;
            marks[i].lr.startColor = marks[i].lr.endColor = c;
        }
    }

    // ================= LOOP DETECTION =================
    class Crossing { public Mark a, b; public Vector2 p; public float posA, posB; }

    readonly List<Crossing> crossings = new List<Crossing>();
    readonly List<Mark> pathMarks = new List<Mark>();
    readonly List<Crossing> pathCross = new List<Crossing>();

    static float Side(Vector2 A, Vector2 B, Vector2 P)
        => (B.x - A.x) * (P.y - A.y) - (B.y - A.y) * (P.x - A.x);

    // do A-B and C-D cross? p = where, t = how far along A-B, u = how far along C-D (0..1)
    static bool SegCross(Vector2 A, Vector2 B, Vector2 C, Vector2 D,
                         out Vector2 p, out float t, out float u)
    {
        p = default; t = 0; u = 0;
        float s1 = Side(A, B, C), s2 = Side(A, B, D);
        float s3 = Side(C, D, A), s4 = Side(C, D, B);
        if (s1 * s2 < 0 && s3 * s4 < 0)
        {
            t = s3 / (s3 - s4);
            u = s1 / (s1 - s2);
            p = A + (B - A) * t;
            return true;
        }
        return false;
    }

    // slightly lengthen both ends of a mark, for the crossing test only
    Vector2[] Extended(Vector2[] pts)
    {
        var e = (Vector2[])pts.Clone();
        int n = e.Length;
        e[0] = pts[0] + (pts[0] - pts[1]).normalized * endTolerance;
        e[n - 1] = pts[n - 1] + (pts[n - 1] - pts[n - 2]).normalized * endTolerance;
        return e;
    }

    // every crossing between every pair of marks
    void FindCrossings()
    {
        crossings.Clear();
        for (int i = 0; i < marks.Count; i++)
            for (int j = i + 1; j < marks.Count; j++)
            {
                Vector2[] A = Extended(marks[i].points), B = Extended(marks[j].points);
                for (int s = 0; s < A.Length - 1; s++)
                    for (int q = 0; q < B.Length - 1; q++)
                        if (SegCross(A[s], A[s + 1], B[q], B[q + 1], out Vector2 p, out float t, out float u))
                            crossings.Add(new Crossing
                            {
                                a = marks[i],
                                b = marks[j],
                                p = p,
                                posA = s + t,   // segment index + fraction along mark a
                                posB = q + u    // same, along mark b
                            });
            }
    }

    static float PosOn(Crossing c, Mark m) => c.a == m ? c.posA : c.posB;

    // search for a ring: newest -> mark -> mark -> ... -> back to newest
    bool Dfs(Mark cur, Crossing entry, Mark start)
    {
        foreach (var c in crossings)
        {
            if (c == entry) continue;
            if (c.a != cur && c.b != cur) continue;
            Mark other = c.a == cur ? c.b : c.a;

            if (other == start)
            {
                // closing the ring: needs 2+ marks and a different crossing than the first one
                if (pathMarks.Count >= 2 && c != pathCross[0])
                {
                    pathCross.Add(c);
                    return true;
                }
            }
            else if (!pathMarks.Contains(other))
            {
                pathMarks.Add(other);
                pathCross.Add(c);
                if (Dfs(other, c, start)) return true;
                pathMarks.RemoveAt(pathMarks.Count - 1);
                pathCross.RemoveAt(pathCross.Count - 1);
            }
        }
        return false;
    }

    // on each mark in the ring, keep only the piece between its entry and exit crossings
    List<Vector2> BuildPolygon()
    {
        var poly = new List<Vector2>();
        int k = pathMarks.Count;
        for (int i = 0; i < k; i++)
        {
            Mark m = pathMarks[i];
            Crossing cin = pathCross[(i - 1 + k) % k];
            Crossing cout = pathCross[i];
            float pin = PosOn(cin, m), pout = PosOn(cout, m);

            poly.Add(cin.p);
            if (pin < pout)
            {
                for (int v = 0; v < m.points.Length; v++)
                    if (v > pin && v < pout) poly.Add(m.points[v]);
            }
            else
            {
                for (int v = m.points.Length - 1; v >= 0; v--)
                    if (v < pin && v > pout) poly.Add(m.points[v]);
            }
        }
        return poly;
    }

    void TryFormLoop(Mark newest)
    {
        FindCrossings();
        pathMarks.Clear();
        pathCross.Clear();
        pathMarks.Add(newest);

        if (!Dfs(newest, null, newest)) return;

        List<Vector2> poly = BuildPolygon();

        foreach (var m in pathMarks)             // consume the marks used in the ring
        {
            int idx = marks.IndexOf(m);
            if (idx >= 0) RemoveMark(idx);
        }
        OnLoopFormed(poly);
    }

    // ================= WHAT HAPPENS WHEN A LOOP FORMS =================
    void OnLoopFormed(List<Vector2> poly)
    {
        AudioManager.Play(SFX.LoopFormed);   // SFX
        CameraShake.Shake(0.4f, 0.3f);
        CameraShake.HitStop(0.06f);
        StartCoroutine(ShowLoop(poly));
        StartCoroutine(DamageLoop(poly));
    }

    IEnumerator DamageLoop(List<Vector2> poly)
    {
        Vector2 min = poly[0], max = poly[0];
        foreach (var p in poly) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
        min -= Vector2.one * 0.5f;
        max += Vector2.one * 0.5f;

        float end = Time.time + loopDuration;

        while (Time.time < end)
        {
            int hits = 0;
            foreach (var col in Physics2D.OverlapAreaAll(min, max, enemyLayers))
            {
                if (!TouchesPoly(col, poly)) continue;
                var d = col.GetComponentInParent<IDamageable>();
                if (d != null) { d.TakeDamage(loopTickDamage); hits++; }
            }
            if (hits > 0)                    // shake only when the tick actually hit something
            {
                AudioManager.Play(SFX.LoopTick);   // SFX
                CameraShake.Shake(Mathf.Min(0.05f + 0.02f * hits, 0.16f), 0.12f);
            }
            yield return new WaitForSeconds(loopTickRate);
        }
    }

    static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
        return Vector2.Distance(p, a + ab * t);
    }

    // inside the polygon OR touching its edge
    static bool TouchesPoly(Collider2D col, List<Vector2> poly)
    {
        Vector2 c = col.bounds.center;
        if (InPoly(c, poly)) return true;

        float r = Mathf.Max(col.bounds.extents.x, col.bounds.extents.y);
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            if (DistToSegment(c, poly[j], poly[i]) <= r) return true;
        return false;
    }

    public static bool InPoly(Vector2 p, List<Vector2> poly)
    {
        bool inside = false;
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                inside = !inside;
        return inside;
    }

    // ================= LOOP VISUALS (outline + filled area) =================
    IEnumerator ShowLoop(List<Vector2> poly)
    {
        // outline
        var go = new GameObject("LoopOutline");
        var lr = go.AddComponent<LineRenderer>();
        lr.material = loopmateria;
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.positionCount = poly.Count;
        for (int i = 0; i < poly.Count; i++) lr.SetPosition(i, poly[i]);
        lr.startWidth = lr.endWidth = lineWidth * 2.5f;
        lr.sortingOrder = 10;

        // fill: SAME material as the outline, just on a mesh
        var fillGo = new GameObject("LoopFill");
        var mf = fillGo.AddComponent<MeshFilter>();
        var mr = fillGo.AddComponent<MeshRenderer>();
        Mesh mesh = BuildFillMesh(poly);
        mf.mesh = mesh;
        var fillMat = new Material(loopmateria);
        mr.material = fillMat;
        mr.sortingOrder = 9;                      // just under the outline

        // find the color property this shader uses, so fading works without changing the look
        string prop = fillMat.HasProperty("_Color") ? "_Color"
                    : fillMat.HasProperty("_BaseColor") ? "_BaseColor" : null;
        Color baseCol = prop != null ? fillMat.GetColor(prop) : Color.white;

        float t = 0f;
        while (t < loopDuration)
        {
            t += Time.deltaTime;
            float f = fadeLoop ? 1f - t / loopDuration : 1f;

            Color oc = outlineColor; oc.a *= f;
            lr.startColor = lr.endColor = oc;

            if (prop != null)
            {
                Color c = baseCol;
                c.a = baseCol.a * f * fillOpacity;
                fillMat.SetColor(prop, c);
            }
            yield return null;
        }

        Destroy(go);
        Destroy(fillGo);
        Destroy(mesh);
        Destroy(fillMat);
    }

    // ---------- triangulation (ear clipping) ----------
    static float SignedArea(List<Vector2> p)
    {
        float a = 0f;
        for (int i = 0, j = p.Count - 1; i < p.Count; j = i++)
            a += (p[j].x * p[i].y) - (p[i].x * p[j].y);
        return a * 0.5f;
    }

    static bool PointInTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Side(a, b, p), d2 = Side(b, c, p), d3 = Side(c, a, p);
        bool neg = d1 < 0 || d2 < 0 || d3 < 0;
        bool pos = d1 > 0 || d2 > 0 || d3 > 0;
        return !(neg && pos);
    }

    static List<int> Triangulate(List<Vector2> p)
    {
        var tris = new List<int>();
        var V = new List<int>();
        for (int i = 0; i < p.Count; i++) V.Add(i);
        if (SignedArea(p) < 0f) V.Reverse();      // make it counter-clockwise

        while (V.Count > 3)
        {
            bool clipped = false;
            for (int i = 0; i < V.Count; i++)
            {
                int a = V[(i + V.Count - 1) % V.Count], b = V[i], c = V[(i + 1) % V.Count];

                // skip reflex (inward) corners
                Vector2 ab = p[b] - p[a], bc = p[c] - p[b];
                if (ab.x * bc.y - ab.y * bc.x <= 0f) continue;

                // an ear must not contain any other vertex
                bool ear = true;
                foreach (int v in V)
                {
                    if (v == a || v == b || v == c) continue;
                    if (PointInTri(p[v], p[a], p[b], p[c])) { ear = false; break; }
                }
                if (!ear) continue;

                tris.Add(a); tris.Add(b); tris.Add(c);
                V.RemoveAt(i);
                clipped = true;
                break;
            }

            if (!clipped)                          // odd shape: fall back to a fan
            {
                for (int i = 1; i < V.Count - 1; i++) { tris.Add(V[0]); tris.Add(V[i]); tris.Add(V[i + 1]); }
                return tris;
            }
        }

        tris.Add(V[0]); tris.Add(V[1]); tris.Add(V[2]);
        return tris;
    }

    Mesh BuildFillMesh(List<Vector2> poly)
    {
        var verts = new Vector3[poly.Count];
        var cols = new Color[poly.Count];
        var uvs = new Vector2[poly.Count];
        for (int i = 0; i < poly.Count; i++)
        {
            verts[i] = poly[i];
            cols[i] = Color.white;
            uvs[i] = poly[i];
        }

        List<int> tris = Triangulate(poly);
        var both = new List<int>(tris);
        for (int i = 0; i < tris.Count; i += 3)      // reversed copy, so it shows from either side
        {
            both.Add(tris[i]);
            both.Add(tris[i + 2]);
            both.Add(tris[i + 1]);
        }

        var mesh = new Mesh();
        mesh.vertices = verts;
        mesh.colors = cols;
        mesh.uv = uvs;
        mesh.triangles = both.ToArray();
        return mesh;
    }
}
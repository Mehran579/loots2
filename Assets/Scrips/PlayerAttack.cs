using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Mark
{
    public Vector2[] points;           // world-space snapshot
    public float born;
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
    public float arcSize = 1f;
    public float attackDuration = 0.25f;     // how long the player counts as "attacking"

    [Header("Setup")]
    public Transform[] slashPoints;          // your 4 empties under attackPivot
    public Material lineMaterial;            // Sprites/Default material
    public float lineWidth = 0.12f;
    public Color lineColor = new Color(0.4f, 0.9f, 1f, 1f);
    public float lifetime = 3f;
    public int maxMarks = 8;

    Camera cam;
    public readonly List<Mark> marks = new List<Mark>();

    void Awake()
    {
        cam = Camera.main;
        Instance = this;
    }

    void Update() => AimPivot();

    void AimPivot()
    {
        attackPivot.localScale = new Vector3(arcSize, arcSize, 1f);
        attackPivot.right = Aim.DirFrom(transform.position, attackPivot.right);
    }

    // ================= INPUT =================
    public void OnAttack(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        if (PlayerManager.IsDashing || IsAttacking) return;

        attackEndTime = Time.time + attackDuration;
        AimPivot();                          // make sure arc uses the latest mouse position
        Mark newest = SpawnMark();
        TryFormLoop(newest);
    }

    // called by PlayerManager when a dash finishes
    public void AddDashMark(Vector2 start, Vector2 end)
    {
        if ((end - start).sqrMagnitude < 0.25f) return;   // ignore tiny dashes
        Mark m = SpawnMark(new[] { start, end });
        TryFormLoop(m);
    }

    // ================= MARKS =================
    // slash: snapshot the arc points
    public Mark SpawnMark()
    {
        var pts = new Vector2[slashPoints.Length];
        for (int i = 0; i < pts.Length; i++)
            pts[i] = slashPoints[i].position;
        return SpawnMark(pts);
    }

    // any list of world points becomes a mark
    public Mark SpawnMark(Vector2[] pts)
    {
        var go = new GameObject("SlashMark");     // no parent, stays in the world
        var lr = go.AddComponent<LineRenderer>();
        lr.material = lineMaterial;
        lr.useWorldSpace = true;
        lr.positionCount = pts.Length;
        for (int i = 0; i < pts.Length; i++)
            lr.SetPosition(i, pts[i]);
        lr.startWidth = lr.endWidth = lineWidth;
        lr.startColor = lr.endColor = lineColor;
        lr.sortingOrder = 5;

        var mark = new Mark { points = pts, born = Time.time, lr = lr };
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
            if (age >= lifetime) { RemoveMark(i); continue; }

            Color c = lineColor;
            c.a = 1f - age / lifetime;
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
        Debug.Log("LOOP formed from " + pathMarks.Count + " marks");

        foreach (var m in pathMarks)             // consume the marks used in the ring
        {
            int idx = marks.IndexOf(m);
            if (idx >= 0) RemoveMark(idx);
        }
        OnLoopFormed(poly);
    }

    // ================= WHAT HAPPENS WHEN A LOOP FORMS =================
    public LayerMask enemyLayers;     // set to your Enemy layer
    public int loopDamage = 3;

    void OnLoopFormed(List<Vector2> poly)
    {
        StartCoroutine(ShowLoop(poly));

        Vector2 min = poly[0], max = poly[0];
        foreach (var p in poly) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
        min -= Vector2.one * 0.5f;
        max += Vector2.one * 0.5f;

        foreach (var col in Physics2D.OverlapAreaAll(min, max, enemyLayers))
        {
            if (!TouchesPoly(col, poly)) continue;
            var e = col.GetComponentInParent<EnemyManager>();
            if (e) e.TakeDamage(loopDamage);
        }
    }

    static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
        return Vector2.Distance(p, a + ab * t);
    }

    static bool TouchesPoly(Collider2D col, List<Vector2> poly)
    {
        Vector2 c = col.bounds.center;
        if (InPoly(c, poly)) return true;

        float r = Mathf.Max(col.bounds.extents.x, col.bounds.extents.y);
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            if (DistToSegment(c, poly[j], poly[i]) <= r) return true;
        return false;
    }
    public float loopShowTime = 5f;       // how long the yellow outline stays (seconds)
    public bool fadeLoop = true;          // false = stays fully bright until it disappears

    IEnumerator ShowLoop(List<Vector2> poly)
    {
        var go = new GameObject("LoopFlash");
        var lr = go.AddComponent<LineRenderer>();
        lr.material = lineMaterial;
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.positionCount = poly.Count;
        for (int i = 0; i < poly.Count; i++) lr.SetPosition(i, poly[i]);
        lr.startWidth = lr.endWidth = lineWidth * 2.5f;
        lr.sortingOrder = 10;

        float t = 0f;
        while (t < loopShowTime)
        {
            t += Time.deltaTime;
            float a = fadeLoop ? 1f - t / loopShowTime : 1f;
            lr.startColor = lr.endColor = new Color(1f, 0.9f, 0.3f, a);
            yield return null;
        }
        Destroy(go);
    }
    public float endTolerance = 0.4f;     // leniency, in world units

    Vector2[] Extended(Vector2[] pts)
    {
        var e = (Vector2[])pts.Clone();
        int n = e.Length;
        e[0] = pts[0] + (pts[0] - pts[1]).normalized * endTolerance;
        e[n - 1] = pts[n - 1] + (pts[n - 1] - pts[n - 2]).normalized * endTolerance;
        return e;
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
}
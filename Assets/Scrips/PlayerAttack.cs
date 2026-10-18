using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Mark
{
    public Vector2[] points;           // world-space snapshot
    public float born;
    public LineRenderer lr;
}
[RequireComponent(typeof(LineRenderer))]
public class PlayerAttack : MonoBehaviour
{
    public Transform attackPivot;
    Camera cam;

    void Awake() => cam = Camera.main;

    public void Update()
    {
        //Vector2 j = Mouse.current.position.ReadValue();
        Vector3 m = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        m.z = 0f;

        Vector2 dir = (m - transform.position).normalized;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        attackPivot.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    [Header("Setup")]
    public Transform[] slashPoints;        // your 4 empties under AttackRadius
    public Material lineMaterial;          // any unlit/Sprites-Default material
    public float lineWidth = 0.12f;
    public Color lineColor = new Color(0.4f, 0.9f, 1f, 1f);
    public float lifetime = 3f;
    public int maxMarks = 8;


    public readonly List<Mark> marks = new List<Mark>();

    // Call this from your attack, AFTER you've rotated AttackRadius toward the mouse
    public Mark SpawnMark()
    {
        // 1. snapshot world positions, all in the same frame
        var pts = new Vector2[slashPoints.Length];
        for (int i = 0; i < pts.Length; i++)
            pts[i] = slashPoints[i].position;

        // 2. create a standalone line object (no parent, so it stays in the world)
        var go = new GameObject("SlashMark");
        var lr = go.AddComponent<LineRenderer>();
        lr.material = lineMaterial;
        lr.useWorldSpace = true;
        lr.positionCount = pts.Length;
        for (int i = 0; i < pts.Length; i++)
            lr.SetPosition(i, pts[i]);
        lr.startWidth = lr.endWidth = lineWidth;
        lr.startColor = lr.endColor = lineColor;
        lr.sortingOrder = 5;

        // 3. store it
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

    // fade + expire (otherwise marks pile up forever)
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
    public void OnAttack()
    {
        SpawnMark();
    }
}

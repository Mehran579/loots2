using UnityEngine;
using UnityEngine.InputSystem;

public static class Aim
{
    static Camera cam;

    public static Vector2 MouseWorld
    {
        get
        {
            if (!cam) cam = Camera.main;     // re-finds the camera after a scene reload
            Vector3 m = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            return new Vector2(m.x, m.y);
        }
    }

    public static Vector2 DirFrom(Vector2 origin, Vector2 fallback)
    {
        Vector2 d = MouseWorld - origin;
        return d.sqrMagnitude > 0.01f ? d.normalized : fallback;
    }
}
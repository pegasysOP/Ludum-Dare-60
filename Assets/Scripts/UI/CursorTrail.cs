using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// soft glowing streak behind the mouse, a ribbon mesh through the last fraction of a second of positions
public class CursorTrail : MaskableGraphic
{
    public Texture glow;
    public float lifetime = 0.2f;
    public float width = 27f;
    // from the cursor's tip to the middle of the AeroCursor art, in screen pixels
    public Vector2 cursorCentre = new Vector2(6f, -12f);
    public Color headColour = new Color(0.85f, 0.97f, 1f, 0.9f);
    public Color tailColour = new Color(0.35f, 0.8f, 1f, 0f);

    // each recorded point is split into this many smooth steps so fast flicks still curve
    private const int Smoothing = 4;

    private struct Point
    {
        public Vector2 position;
        public float time;
    }

    private readonly List<Point> points = new List<Point>();
    private readonly List<Point> smoothed = new List<Point>();

    public override Texture mainTexture => glow != null ? glow : s_WhiteTexture;

    // one trail for the whole game, made on startup so no scene needs it
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        GameObject prefab = Resources.Load<GameObject>("CursorTrailCanvas");
        if (prefab != null)
            DontDestroyOnLoad(Instantiate(prefab));
    }

    private void Update()
    {
        float now = Time.unscaledTime;
        points.RemoveAll(point => now - point.time > lifetime);

        // nothing new while the cursor is hidden, the trail just fades out
        if (Mouse.current != null && Cursor.visible && Application.isFocused)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Mouse.current.position.ReadValue() + cursorCentre, null, out Vector2 local);
            if (points.Count == 0 || (local - points[points.Count - 1].position).sqrMagnitude > 1f)
                points.Add(new Point { position = local, time = now });
        }

        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Smooth();
        int count = smoothed.Count;
        if (count < 2)
            return;

        float now = Time.unscaledTime;
        for (int i = 0; i < count; i++)
        {
            Point point = smoothed[i];
            Vector2 forward = smoothed[Mathf.Min(i + 1, count - 1)].position - smoothed[Mathf.Max(i - 1, 0)].position;
            Vector2 side = new Vector2(-forward.y, forward.x).normalized;

            // newest end is full width and bright, tapering and fading to nothing at the tail
            float along = (float)i / (count - 1);
            float strength = along * (1f - Mathf.Clamp01((now - point.time) / lifetime));
            Color colour = Color.Lerp(tailColour, headColour, strength) * color;
            Vector2 offset = side * (width * strength * 0.5f);

            vh.AddVert(point.position - offset, colour, new Vector2(along, 0f));
            vh.AddVert(point.position + offset, colour, new Vector2(along, 1f));

            if (i > 0)
            {
                int v = i * 2;
                vh.AddTriangle(v - 2, v - 1, v + 1);
                vh.AddTriangle(v - 2, v + 1, v);
            }
        }
    }

    // catmull-rom through the recorded points
    private void Smooth()
    {
        smoothed.Clear();
        for (int i = 0; i < points.Count - 1; i++)
        {
            Point p0 = points[Mathf.Max(i - 1, 0)];
            Point p1 = points[i];
            Point p2 = points[i + 1];
            Point p3 = points[Mathf.Min(i + 2, points.Count - 1)];

            for (int step = 0; step < Smoothing; step++)
            {
                float t = (float)step / Smoothing;
                float t2 = t * t;
                float t3 = t2 * t;
                Vector2 position = 0.5f * (2f * p1.position + (p2.position - p0.position) * t
                    + (2f * p0.position - 5f * p1.position + 4f * p2.position - p3.position) * t2
                    + (3f * p1.position - p0.position - 3f * p2.position + p3.position) * t3);
                smoothed.Add(new Point { position = position, time = Mathf.Lerp(p1.time, p2.time, t) });
            }
        }

        if (points.Count > 0)
            smoothed.Add(points[points.Count - 1]);
    }
}

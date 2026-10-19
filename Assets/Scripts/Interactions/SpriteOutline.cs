using DG.Tweening;
using UnityEngine;

// line around the sprite drawn by the SpriteOutline shader, thin and blue at rest (grey once used), thicker and red on hover
public class SpriteOutline : MonoBehaviour
{
    // sizes are in world units so the line scales with the screen
    [SerializeField] private Color idleColour = new Color(0.55f, 0.8f, 1f, 0.85f);
    [SerializeField] private Color hoverColour = new Color(0.92f, 0.16f, 0.12f, 1f);
    [SerializeField] private Color usedColour = new Color(0.8f, 0.82f, 0.85f, 0.85f);
    [SerializeField] private float idleWidth = 0.02f;
    [SerializeField] private float hoverWidth = 0.05f;
    [SerializeField] private float gapWidth = 0.02f;

    private static readonly int ColourId = Shader.PropertyToID("_Color");
    private static readonly int ThicknessId = Shader.PropertyToID("_Thickness");
    private static Material material;

    private MeshRenderer outline;
    private MaterialPropertyBlock block;
    private Color colour;
    private float thickness;
    private Tween hoverTween;
    private bool used = false;

    private void Awake()
    {
        SpriteRenderer original = GetComponent<SpriteRenderer>();
        if (original == null || original.sprite == null)
            return;

        // shader lives in Resources so it makes it into builds
        if (material == null)
            material = new Material(Resources.Load<Shader>("SpriteOutline"));

        GameObject outlineObject = new GameObject("Sprite Outline");
        outlineObject.transform.SetParent(transform, false);
        outlineObject.transform.localScale = new Vector3(original.flipX ? -1f : 1f, original.flipY ? -1f : 1f, 1f);
        outlineObject.AddComponent<MeshFilter>().mesh = BuildQuad(original.sprite, out Vector4 spriteRect, out Vector2 uvPerUnit);

        outline = outlineObject.AddComponent<MeshRenderer>();
        outline.sharedMaterial = material;
        outline.sortingLayerID = original.sortingLayerID;
        outline.sortingOrder = original.sortingOrder - 1;

        block = new MaterialPropertyBlock();
        block.SetTexture("_MainTex", original.sprite.texture);
        block.SetVector("_SpriteRect", spriteRect);
        block.SetVector("_UVPerUnit", uvPerUnit);
        block.SetFloat("_Gap", gapWidth);
        Apply(idleColour, idleWidth);
    }

    // plain rectangle over the whole sprite plus room for the line, so tight sprite meshes can't clip it
    private Mesh BuildQuad(Sprite sprite, out Vector4 spriteRect, out Vector2 uvPerUnit)
    {
        Vector2 textureSize = new Vector2(sprite.texture.width, sprite.texture.height);
        Vector2 uvMin = sprite.rect.position / textureSize;
        Vector2 uvSize = sprite.rect.size / textureSize;
        spriteRect = new Vector4(uvMin.x, uvMin.y, uvSize.x, uvSize.y);

        Vector3 scale = transform.lossyScale;
        Vector2 localPerWorld = new Vector2(1f / Mathf.Abs(scale.x), 1f / Mathf.Abs(scale.y));
        Vector2 size = sprite.rect.size / sprite.pixelsPerUnit;
        uvPerUnit = uvSize / size * localPerWorld;

        Vector2 room = (gapWidth + hoverWidth) * 1.25f * localPerWorld;
        Vector2 min = -sprite.pivot / sprite.pixelsPerUnit - room;
        Vector2 max = min + size + 2f * room;
        Vector2 uvRoom = room / size * uvSize;
        Vector2 uvLow = uvMin - uvRoom;
        Vector2 uvHigh = uvMin + uvSize + uvRoom;

        Mesh mesh = new Mesh { name = "Sprite Outline" };
        mesh.vertices = new[] { new Vector3(min.x, min.y), new Vector3(max.x, min.y), new Vector3(min.x, max.y), new Vector3(max.x, max.y) };
        mesh.uv = new[] { uvLow, new Vector2(uvHigh.x, uvLow.y), new Vector2(uvLow.x, uvHigh.y), uvHigh };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateBounds();
        return mesh;
    }

    public void ShowOutline()
    {
        Animate(hoverColour, hoverWidth);
    }

    public void HideOutline()
    {
        Animate(used ? usedColour : idleColour, idleWidth);
    }

    // already looked at, so it settles to grey instead of blue once the mouse leaves
    public void MarkUsed()
    {
        used = true;
    }

    private void Animate(Color targetColour, float targetThickness)
    {
        if (outline == null)
            return;

        hoverTween?.Kill();
        Color startColour = colour;
        float startThickness = thickness;
        hoverTween = DOVirtual.Float(0f, 1f, 0.15f, t => Apply(Color.Lerp(startColour, targetColour, t), Mathf.Lerp(startThickness, targetThickness, t)))
            .SetLink(outline.gameObject);
    }

    private void Apply(Color newColour, float newThickness)
    {
        colour = newColour;
        thickness = newThickness;
        block.SetColor(ColourId, colour);
        block.SetFloat(ThicknessId, thickness);
        outline.SetPropertyBlock(block);
    }
}

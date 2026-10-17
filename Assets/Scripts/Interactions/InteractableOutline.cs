
using UnityEngine;

public class SpriteOutline : MonoBehaviour
{
    [SerializeField] private Color outlineColor = Color.red;
    [SerializeField, Range(1.01f, 1.2f)]
    private float outlineScale = 1.05f;

    private GameObject outlineObject;

    public void ShowOutline()
    {
        if (outlineObject != null)
        {
            Debug.LogError("Outline object already exists. Cannot create another one.", this);
            return;
        }

        SpriteRenderer original = GetComponent<SpriteRenderer>();

        if (original == null || original.sprite == null)
        {
            Debug.LogWarning("No SpriteRenderer or sprite found.", this);
            return;
        }

        outlineObject = new GameObject("Sprite Outline");

        outlineObject.transform.SetParent(transform, false);
        outlineObject.transform.localPosition = Vector3.zero;
        outlineObject.transform.localRotation = Quaternion.identity;
        outlineObject.transform.localScale = Vector3.one * outlineScale;

        SpriteRenderer outlineRenderer =
            outlineObject.AddComponent<SpriteRenderer>();

        outlineRenderer.sprite = original.sprite;
        outlineRenderer.color = outlineColor;

        outlineRenderer.sortingLayerID = original.sortingLayerID;
        outlineRenderer.sortingOrder = original.sortingOrder - 1;

        outlineRenderer.sharedMaterial = original.sharedMaterial;
    }

    public void HideOutline()
    {
        if (outlineObject == null)
            return;

        Destroy(outlineObject);
        outlineObject = null;
    }
}

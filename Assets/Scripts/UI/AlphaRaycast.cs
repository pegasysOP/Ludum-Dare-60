using UnityEngine;
using UnityEngine.UI;

// clicks only land on the visible parts of the sprite, the texture needs read/write enabled
[RequireComponent(typeof(Image))]
public class AlphaRaycast : MonoBehaviour
{
    [Range(0f, 1f)] public float threshold = 0.5f;

    private void Awake()
    {
        GetComponent<Image>().alphaHitTestMinimumThreshold = threshold;
    }
}

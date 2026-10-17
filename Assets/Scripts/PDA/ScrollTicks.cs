using UnityEngine;
using UnityEngine.UI;

// plays the phone scroll tick every so often while a list scrolls
[RequireComponent(typeof(ScrollRect))]
public class ScrollTicks : MonoBehaviour
{
    public float tickSpacing = 70f;

    private ScrollRect scroll;
    private PhoneSfxLibrary sounds;
    private float lastY;
    private float travelled;

    private void Awake()
    {
        scroll = GetComponent<ScrollRect>();
        sounds = PhoneSfxLibrary.Load();
        lastY = scroll.content.anchoredPosition.y;
        scroll.onValueChanged.AddListener(OnScrolled);
    }

    private void OnScrolled(Vector2 position)
    {
        float y = scroll.content.anchoredPosition.y;
        travelled += Mathf.Abs(y - lastY);
        lastY = y;

        if (travelled < tickSpacing)
            return;

        travelled %= tickSpacing;
        sounds.scrollTick.Play();
    }
}

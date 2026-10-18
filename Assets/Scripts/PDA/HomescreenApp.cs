using UnityEngine;
using UnityEngine.UI;

public class HomescreenApp : MonoBehaviour
{
    public Button button;
    public RectTransform rect;
    public FullscreenApp fullscreenApp;
    public GameObject notificationDot;

    public void SetNotification(bool show)
    {
        notificationDot.SetActive(show);
    }
}

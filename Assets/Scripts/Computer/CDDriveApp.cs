using UnityEngine;
using UnityEngine.UI;

// cd drive on a desktop: empty until the cd goes in, then the icon opens straight to what's on it
public class CDDriveApp : MonoBehaviour
{
    public ComputerDesktop desktop;
    public Button icon;
    public GameObject driveWindow;
    public GameObject contentsWindow;
    // only clickable once there's a cd to load, so a click always means it went in
    public Button loadButton;

    private void Awake()
    {
        icon.onClick.AddListener(() => desktop.Open(GameManager.Instance.UsedCD() ? contentsWindow : driveWindow));
        loadButton.onClick.AddListener(() => driveWindow.SetActive(false));
    }
}

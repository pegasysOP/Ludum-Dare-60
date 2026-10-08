using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public Button startButton;
    public Button soundtrackButton; 
    public Button quitButton;
    public Button creditsButton;

    private void Awake()
    {
        startButton.onClick.AddListener(OnStartClicked);
        soundtrackButton.onClick.AddListener(OnSoundtrackClicked);
        creditsButton.onClick.AddListener(OnCreditsClicked);
        quitButton.onClick.AddListener(OnQuitClicked);

#if UNITY_WEBGL
        quitButton.gameObject.SetActive(false);
#endif
    }

    private void OnStartClicked()
    {
        SceneUtils.LoadGameScene();
    }

    private void OnQuitClicked()
    {
        SceneUtils.QuitApplication();
    }

    private void OnSoundtrackClicked()
    {
        SceneUtils.LoadSoundtrackScene();
    }

    private void OnCreditsClicked()
    {
        SceneUtils.LoadCreditScene();
    }
}

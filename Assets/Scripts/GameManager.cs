using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public PlayerController playerController;
    public HudController hudController;

    public bool LOCKED = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        DontDestroyOnLoad(this);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        UpdateUI();

        SetLocked(false);
    }

    private void UpdateUI()
    {
        //TODO: Implement UI 
    }

    public void TogglePauseMenu()
    {
        if (hudController == null || hudController.pauseMenu == null)
        {
            Debug.LogWarning("HudController or PauseMenu is not assigned!");
            return;
        }

        bool isPausing = !hudController.pauseMenu.isActiveAndEnabled;

        SetLocked(isPausing);
        Time.timeScale = isPausing ? 0f : 1f;

        hudController.pauseMenu.Toggle();
    }

    public void DestroySelf()
    {
        Time.timeScale = 1;

        Destroy(gameObject);
    }

    public void SetLocked(bool locked)
    {
        LOCKED = locked;
        Cursor.visible = locked;
        Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
    }
}

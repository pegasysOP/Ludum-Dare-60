using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private TextMeshProUGUI  flavourText;

    SusPXApp suspectApp;

    bool hasCD = false;

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
        suspectApp = FindAnyObjectByType<SusPXApp>(FindObjectsInactive.Include);
        suspectApp.onReport.AddListener(TriggerEnding);  
    }

    public void DestroySelf()
    {
        Time.timeScale = 1;

        Destroy(gameObject);
    }

    public void ResetFlavourText()
    {
        flavourText.text = "";
        flavourText.maxVisibleCharacters = 0;
    }

    public void ActivateGotCDFlag()
    {
        hasCD = true;
    }

    public bool HasCD()
    {
        return hasCD;
    }

    public void TriggerEnding(Character character)
    {
         //TODO: Based on flags we should trigger the correct ending.
         Debug.Log("TriggerEnding called, but no ending is implemented yet.");
    }
}

using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private TextMeshProUGUI  flavourText;

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
}

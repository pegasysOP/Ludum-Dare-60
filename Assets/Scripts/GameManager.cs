using System.Reflection.Metadata.Ecma335;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private TextMeshProUGUI  flavourText;

    SusPXApp suspectApp;

    bool hasCD = false;

    bool usedCD = false;

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

    public void ActivateUsedCDFlag()
    {
        usedCD = true;
    }

    public bool UsedCD()
    {
        return usedCD;
    }

    public void TriggerEnding(Character character)
    {
        EndingText endingText = FindAnyObjectByType<EndingText>();
        AmbiencePlayer.FadeOut();
        // the lazy ending is a lost case too, so it shares the bad ending music
        if (!usedCD ||  !hasCD)
        {
            GameMusicGuy.PlayEnding(GameEnding.Bad);
            endingText.StartEnding(ENDINGS.LAZY);
        }
        else if (character.characterName == "Greg Lawson" && usedCD)
        {
            GameMusicGuy.PlayEnding(GameEnding.Good);
            endingText.StartEnding(ENDINGS.GOOD);
        }
        else
        {
            GameMusicGuy.PlayEnding(GameEnding.Bad);
            endingText.StartEnding(ENDINGS.BAD);
        }
        
    }
}

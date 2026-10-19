using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private TextMeshProUGUI  flavourText;

    SusPXApp suspectApp;


    bool hasCD = false;
    bool usedCD = false;

    // everything that has to have been looked at, in any order, before an accusation is taken seriously
    private static readonly Evidence[] RequiredEvidence =
    {
        Evidence.SecurityFootage, Evidence.StaffComputer
    };
    private readonly HashSet<Evidence> foundEvidence = new HashSet<Evidence>();

    // reporting this fast without the evidence gets the speedrunner ending
    private const float SpeedrunTime = 60f;
    private float startTime;

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
        startTime = Time.time;
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

    public void FoundEvidence(Evidence evidence)
    {
        foundEvidence.Add(evidence);
    }

    public bool HasFound(Evidence evidence)
    {
        return foundEvidence.Contains(evidence);
    }

    public void ActivateCheckedCameraFlag()
    {
        FoundEvidence(Evidence.SecurityFootage);
        GameMusicGuy.SetMood(GameMood.Orange);
    }

    public bool CheckedCamera()
    {
        return HasFound(Evidence.SecurityFootage);
    }

    public void ActivateCheckedReviewFlag()
    {
        FoundEvidence(Evidence.PerformanceReview);
    }

    public bool CheckedReview()
    {
        return HasFound(Evidence.PerformanceReview);
    }

    public void TriggerEnding(Character character)
    {
        bool foundAll = Array.TrueForAll(RequiredEvidence, HasFound);
        bool isLazy = !foundAll && Time.time - startTime < SpeedrunTime;
        EndingText endingText = FindAnyObjectByType<EndingText>();
        AmbiencePlayer.FadeOut();
        // the lazy ending is a lost case too, so it shares the bad ending music
        if (foundAll && character.characterName == "Greg Lawson")
        {
            GameMusicGuy.PlayEnding(GameEnding.Good);
            endingText.StartEnding(ENDINGS.GOOD);
        }
        else if (isLazy)
        {
            GameMusicGuy.PlayEnding(GameEnding.Bad);
            endingText.StartEnding(ENDINGS.LAZY);
        }
        else
        {
            GameMusicGuy.PlayEnding(GameEnding.Bad);
            endingText.StartEnding(ENDINGS.BAD);
        }
        
    }
}

// things the player can look into, the required ones are listed in GameManager
public enum Evidence
{
    SoftwareUpdates,
    Inbox,
    Calendar,
    CollegeTimetable,
    SecurityFootage,
    PerformanceReview,
    // either the pm's or finance's pc
    StaffComputer
}

using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The four in-game music moods, in story order. Each maps to one
/// <see cref="MusicTrack"/> (see <c>_silas_design/music/tracklist-v2.md</c>).
/// </summary>
public enum GameMood
{
    Blue = 0,       // early investigation, happy & calm
    Yellow = 1,     // mid investigation, puzzled & worried
    Orange = 2,     // late investigation, drama & tense
    Red = 3         // finale ramp, mad & determined
}

/// <summary>How the case ends. Each maps to one play-once <see cref="MusicTrack"/>.</summary>
public enum GameEnding
{
    Good = 0,       // 07_ending_good: the case is solved
    Bad = 1         // 08_ending_bad: the case is lost
}

/// <summary>
/// Drives music selection from a single place.
///
/// <list type="bullet">
///   <item><description>Menu / credits scenes: snaps to that scene's track using the
///   non-envelope playback path and stops steering after that.</description></item>
///   <item><description>Game scene: plays the current <see cref="GameMood"/> through
///   <see cref="MusicManager.PlayGameMusic"/>, so every mood change crossfades in, swells and
///   settles into the background. Every Game scene load starts on <see cref="GameMood.Blue"/>.</description></item>
/// </list>
///
/// To change the mood from gameplay code, call:
/// <code>GameMusicGuy.SetMood(GameMood.Orange);</code>
/// It is static, so it is safe to call from anywhere at any time (even before this component
/// has started); setting the mood that is already playing does nothing.
///
/// To end the game's music, call:
/// <code>GameMusicGuy.PlayEnding(GameEnding.Good);</code>
/// The ending plays once and stops; mood changes are ignored after it until the Game scene reloads.
///
/// Everything here is hardcoded on purpose - there are no inspector fields.
/// Track mapping and tuning values are project-wide creative decisions; we
/// do NOT want them drifting across scenes / prefabs. The single shared
/// <see cref="MusicLibrary"/> is loaded from <c>Resources/MusicLibrary</c>.
/// </summary>
[DisallowMultipleComponent]
public class GameMusicGuy : MonoBehaviour
{
    // ---------- Track mapping (hardcoded, see class-level summary) ----------

    private const MusicTrack MenuTrack = MusicTrack.MainMenu;
    private const MusicTrack CreditsTrack = MusicTrack.Credits;

    private const GameMood StartingMood = GameMood.Blue;

    // Name of the MusicLibrary ScriptableObject under any Resources folder.
    private const string MusicLibraryResourcePath = "MusicLibrary";

    // Seconds after the game start stinger begins before the music comes in.
    // The bloom's tail rings under the theme's first bars.
    private const float GameStartMusicDelay = 3f;

    // OST plan: the good ending should feel like an arrival (the mood crossfades into its swell),
    // the bad ending like a stop (the mood cuts dead; the file opens on a beat of silence).
    private const float GoodEndingCrossfade = 2f;
    private const float BadEndingCrossfade = 0f;

    // ---------- Public API ----------

    /// <summary>The mood the Game scene is playing (or will play once music starts).</summary>
    public static GameMood CurrentMood { get; private set; } = StartingMood;

    /// <summary>
    /// Switch the in-game music to <paramref name="mood"/>. Crossfades over
    /// <see cref="MusicManager.crossfadeDuration"/>; no-op if that mood is already playing.
    /// </summary>
    public static void SetMood(GameMood mood)
    {
        CurrentMood = mood;
    }

    /// <summary>The ending playing, if any. While set, mood changes no longer steer the music.</summary>
    public static GameEnding? CurrentEnding { get; private set; }

    /// <summary>
    /// Play the music for <paramref name="ending"/> once, out of whatever is playing. Static and safe to call
    /// from anywhere; needs a <see cref="MusicManager"/> in the scene.
    /// </summary>
    public static void PlayEnding(GameEnding ending)
    {
        MusicLibrary lib = Resources.Load<MusicLibrary>(MusicLibraryResourcePath);
        if (lib == null || MusicManager.Instance == null)
        {
            Debug.LogError($"{nameof(GameMusicGuy)}: cannot play the {ending} ending: no MusicLibrary or MusicManager.");
            return;
        }
        CurrentEnding = ending;
        bool good = ending == GameEnding.Good;
        MusicManager.Instance.PlayEnding(lib.Get(good ? MusicTrack.EndingGood : MusicTrack.EndingBad),
                                         good ? GoodEndingCrossfade : BadEndingCrossfade);
    }

    // ---------- Runtime state ----------

    private MusicLibrary library;
    private AudioClip lastRequestedClip;
    private bool menuModeActive;
    private bool gameModeActive;

    // The game start stinger plays once per session, in whichever scene loads first.
    private static bool gameStartPlayed;
    private static float gameStartMusicTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        gameStartPlayed = false;
        gameStartMusicTime = 0f;
        CurrentMood = StartingMood;
        CurrentEnding = null;
    }

    private void Awake()
    {
        library = Resources.Load<MusicLibrary>(MusicLibraryResourcePath);
        if (library == null)
        {
            Debug.LogError($"{nameof(GameMusicGuy)} on '{name}' could not load " +
                           $"'{MusicLibraryResourcePath}' from any Resources folder.");
        }
    }

    private void Start()
    {
        if (library == null)
            return;

        if (MusicManager.Instance == null)
        {
            Debug.LogError($"{nameof(GameMusicGuy)} on '{name}' cannot start music: no {nameof(MusicManager)} in scene.");
            return;
        }

        // The first sound of the game.
        if (!gameStartPlayed && library.gameStartStinger != null)
        {
            gameStartPlayed = true;
            gameStartMusicTime = Time.realtimeSinceStartup + GameStartMusicDelay;
            MusicManager.Instance.PlayStinger(library.gameStartStinger, library.gameStartStingerVolume);
        }

        if (IsMenuScene())
        {
            menuModeActive = true;
            AudioClip menuClip = library.Get(MenuTrack);
            if (menuClip != null)
            {
                lastRequestedClip = menuClip;
                StartCoroutine(PlayMusicAfterGameStart(menuClip));
            }
            return;
        }

        if (IsCreditsScene())
        {
            menuModeActive = true;
            AudioClip creditsClip = library.Get(CreditsTrack);
            if (creditsClip != null)
            {
                lastRequestedClip = creditsClip;
                StartCoroutine(PlayMusicAfterGameStart(creditsClip));
            }
            return;
        }

        // Other scenes (soundtrack player, test scenes) drive the music themselves.
        gameModeActive = IsGameScene();
    }

    /// <summary>Starts a menu/credits track, held back while the game start stinger has its moment.</summary>
    private System.Collections.IEnumerator PlayMusicAfterGameStart(AudioClip clip)
    {
        float wait = gameStartMusicTime - Time.realtimeSinceStartup;
        if (wait > 0f)
            yield return new WaitForSecondsRealtime(wait);
        if (MusicManager.Instance != null)
            MusicManager.Instance.PlayMusic(clip);
    }

    private void OnDestroy()
    {
        // Leaving the Game scene: the next run starts back on the first mood.
        if (gameModeActive)
        {
            CurrentMood = StartingMood;
            CurrentEnding = null;
        }
    }

    private void Update()
    {
        if (!gameModeActive || menuModeActive || CurrentEnding != null) return;
        if (library == null || MusicManager.Instance == null)
            return;

        // While the PowerDownSequence (or anything else) has game music suspended,
        // stay out of the way entirely. Clear the cached clip so when music resumes
        // we crossfade the current mood back in, rather than short-circuiting because
        // lastRequestedClip still matches the track that was playing before the fade-out.
        if (MusicManager.Instance.IsGameMusicSuspended)
        {
            lastRequestedClip = null;
            return;
        }

        // Hold the music back while the game start stinger has its moment.
        if (Time.realtimeSinceStartup < gameStartMusicTime)
            return;

        RequestClip(library.Get(TrackFor(CurrentMood)));
    }

    private void RequestClip(AudioClip clip, float crossfadeOverride = -1f)
    {
        if (clip == null) return;
        if (clip == lastRequestedClip) return;
        lastRequestedClip = clip;
        MusicManager.Instance.PlayGameMusic(clip, crossfadeOverride);
    }

    private static MusicTrack TrackFor(GameMood mood)
    {
        switch (mood)
        {
            case GameMood.Yellow: return MusicTrack.GameYellow;
            case GameMood.Orange: return MusicTrack.GameOrange;
            case GameMood.Red: return MusicTrack.GameRed;
            default: return MusicTrack.GameBlue;
        }
    }

    private static bool IsGameScene()
    {
        return SceneManager.GetActiveScene().name == SceneUtils.GAME_SCENE;
    }

    private static bool IsMenuScene()
    {
        return SceneManager.GetActiveScene().name == SceneUtils.MENU_SCENE;
    }

    private static bool IsCreditsScene()
    {
        return SceneManager.GetActiveScene().name == SceneUtils.CREDIT_SCENE;
    }
}

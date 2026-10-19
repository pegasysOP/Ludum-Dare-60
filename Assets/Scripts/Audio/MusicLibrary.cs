using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Canonical ids for the project's music tracks. Keep in sync with
/// <see cref="MusicLibrary"/> and <c>_silas_design/music/tracklist-v2.md</c>.
///
/// Numeric values are intentionally stable (do not renumber) so Unity
/// serialization of <see cref="MusicTrack"/> fields in scenes / prefabs
/// does not silently remap. Declaration order below follows the creative
/// order in the tracklist; numeric values reflect historical insertion.
/// </summary>
public enum MusicTrack
{
    MainMenu = 0,
    Credits = 1,
    GameBlue = 2,       // 03_game_blue: early investigation, happy & calm
    GameYellow = 3,     // 04_game_yellow: mid investigation, puzzled & worried
    GameOrange = 4,     // 05_game_orange: late investigation, drama & tense
    GameRed = 5,        // 06_game_red: finale ramp, mad & determined
    EndingGood = 6,     // 07_ending_good: the case is solved; a one-shot, then the credits
    EndingBad = 7       // 08_ending_bad: the case is lost; a one-shot
}

/// <summary>
/// Serializable container pairing a MusicTrack id with its clip, display text
/// and optional artwork. Exposed in the MusicLibrary inspector as an array.
/// </summary>
[System.Serializable]
public class MusicTrackEntry
{
    public MusicTrack id;
    public AudioClip clip;
    public string displayText;
    public Sprite sprite;

    [Tooltip("Loop region in samples (the studio render's loop.json). Both 0 = loop the whole clip. " +
             "The intro before loopStartSample plays once; the body loops; the outro after loopEndSample " +
             "is only for the OST.")]
    public int loopStartSample;
    public int loopEndSample;

    [Tooltip("Play the clip once and stop instead of looping it (the endings). Ignored when the entry has a loop region.")]
    public bool playOnce;

    public bool HasLoopRegion => loopEndSample > loopStartSample && loopStartSample >= 0;
}

/// <summary>
/// Central ScriptableObject holding every music track in the project. Prefer
/// using the <see cref="entries"/> array; legacy per-field clip slots are
/// preserved for backward compatibility and will be consulted when building
/// the lookup map.
/// </summary>
[CreateAssetMenu(menuName = "Audio/Music Library", fileName = "MusicLibrary")]
public class MusicLibrary : ScriptableObject
{
    [Header("Tracks")]
    public MusicTrackEntry[] entries;

    // Stingers are separate from MusicTrack: SoundtrackMenu lists the MusicTrack values (in its AlbumOrder).
    [Header("Stingers")]
    [Tooltip("11_stinger_game_start: the first sound of the game, played once at launch. The menu theme comes in on its tail.")]
    public AudioClip gameStartStinger;
    [Tooltip("Perceived loudness of the game start stinger (1 = as rendered), on top of the music volume setting.")]
    [Range(0f, 1f)] public float gameStartStingerVolume = 1f;

    private Dictionary<MusicTrack, MusicTrackEntry> trackToEntryMap;

    private void OnEnable()
    {
        CreateDictionary();
    }

    private void OnValidate()
    {
        CreateDictionary();
    }

    private void CreateDictionary()
    {
        if (trackToEntryMap == null)
            trackToEntryMap = new Dictionary<MusicTrack, MusicTrackEntry>();
        else
            trackToEntryMap.Clear();

        if (entries != null)
        {
            foreach (MusicTrackEntry e in entries)
            {
                if (e == null) continue;
                
                if (!trackToEntryMap.ContainsKey(e.id))
                    trackToEntryMap[e.id] = e;
            }
        }
    }

    /// <summary>Returns the clip assigned to <paramref name="track"/>, or null if unassigned.</summary>
    public AudioClip Get(MusicTrack track)
    {
        if (trackToEntryMap != null && trackToEntryMap.TryGetValue(track, out MusicTrackEntry e))
            return e.clip;

        Debug.LogError($"MusicLibrary: clip for track {track} is not assigned.");
        return null;
    }

    /// <summary>The loop region for <paramref name="clip"/>, if its entry has one.</summary>
    public bool TryGetLoopRegion(AudioClip clip, out int startSample, out int endSample)
    {
        startSample = endSample = 0;
        if (clip == null || entries == null) return false;
        foreach (MusicTrackEntry e in entries)
        {
            if (e != null && e.clip == clip && e.HasLoopRegion)
            {
                startSample = e.loopStartSample;
                endSample = e.loopEndSample;
                return true;
            }
        }
        return false;
    }

    /// <summary>True if <paramref name="clip"/>'s entry plays once instead of looping.</summary>
    public bool IsPlayOnce(AudioClip clip)
    {
        if (clip == null || entries == null) return false;
        foreach (MusicTrackEntry e in entries)
        {
            if (e != null && e.clip == clip && e.playOnce && !e.HasLoopRegion)
                return true;
        }
        return false;
    }

    /// <summary>Returns display text for the track, or the enum name if none assigned.</summary>
    public string GetDisplayText(MusicTrack track)
    {
        if (trackToEntryMap != null && trackToEntryMap.TryGetValue(track, out MusicTrackEntry e) && !string.IsNullOrEmpty(e.displayText))
            return e.displayText;
        
        Debug.LogWarning($"MusicLibrary: display text for track {track} is not assigned; using enum name.");
        return track.ToString();
    }

    /// <summary>Returns the sprite assigned to the track, or null if none assigned.</summary>
    public Sprite GetSprite(MusicTrack track)
    {
        if (trackToEntryMap != null && trackToEntryMap.TryGetValue(track, out MusicTrackEntry e))
            return e.sprite;
        //TODO: Consider returning a default sprite instead of null. Perhaps a "no artwork" placeholder. 
        return null;
    }
}

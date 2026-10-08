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
    Credits = 1
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

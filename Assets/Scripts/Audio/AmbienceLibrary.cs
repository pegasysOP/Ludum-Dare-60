using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The life of each room: small sounds of people and things (typing behind a cubicle wall, a CCTV monitor flipping
/// channel), each lane at its own random interval and from its own part of the screen. A room can also have a soft
/// recorded bed of people at work (the open office), but never fans, air or hum.
/// Played by <see cref="AmbiencePlayer"/>, which <see cref="RoomNavigator"/> tells which room you're in.
/// Files live in <c>Assets/Audio/sfx/ambience</c>, exported by Silas's SoundMapper from the cuts that
/// <c>_silas/tools/office_ambience.py</c> builds.
/// </summary>
[CreateAssetMenu(menuName = "Audio/Ambience Library", fileName = "AmbienceLibrary")]
public class AmbienceLibrary : ScriptableObject
{
    private const string ResourcePath = "AmbienceLibrary";

    [Header("Mix")]
    [Tooltip("Off: the rooms are silent apart from the music and interaction sounds.")]
    public bool playAmbience = true;
    [Tooltip("Perceived loudness of all ambience, on top of the SFX volume setting. Keep it very soft.")]
    [Range(0f, 1f)] public float masterVolume = 0.45f;

    [Header("Timing")]
    [Tooltip("Seconds a sound still playing takes to fade out when you click to leave the room, and a bed to hand over.")]
    [Min(0f)] public float roomExitFade = 0.25f;
    [Tooltip("Seconds the first bed takes to fade in when the game starts.")]
    [Min(0f)] public float startFade = 2.5f;
    [Tooltip("Seconds each bed crossfades into its own start when it loops, so the seam never shows.")]
    [Min(0.1f)] public float loopCrossfade = 4f;
    [Tooltip("Seconds the ambience takes to fade out when an ending starts.")]
    [Min(0f)] public float endingFade = 2f;

    [Header("Mood")]
    [Tooltip("How much the gaps between sounds stretch at each mood. The music gets busier as the case heats up, " +
             "so the room steps back. Blue is 1.")]
    [Min(1f)] public float yellowSpacing = 1.2f;
    [Min(1f)] public float orangeSpacing = 1.5f;
    [Min(1f)] public float redSpacing = 2f;

    [Header("Rooms")]
    public List<RoomAmbience> rooms = new List<RoomAmbience>();

    /// <summary>Loads the shared library from Resources, or null if it is missing.</summary>
    public static AmbienceLibrary Load()
    {
        AmbienceLibrary library = Resources.Load<AmbienceLibrary>(ResourcePath);
        if (library == null)
            Debug.LogError($"AmbienceLibrary: no asset found at Resources/{ResourcePath}.");
        return library;
    }

    /// <summary>The entry for a room by its GameObject name under RoomNavigator.rooms, or null.</summary>
    public RoomAmbience Get(string roomName)
    {
        foreach (RoomAmbience room in rooms)
        {
            if (room != null && room.roomName == roomName)
                return room;
        }
        return null;
    }

    /// <summary>How much to stretch every lane's gaps at <paramref name="mood"/>.</summary>
    public float Spacing(GameMood mood) => mood switch
    {
        GameMood.Yellow => yellowSpacing,
        GameMood.Orange => orangeSpacing,
        GameMood.Red => redSpacing,
        _ => 1f,
    };
}

[System.Serializable]
public class RoomAmbience
{
    [Tooltip("Name of the room's GameObject under RoomNavigator.rooms, e.g. \"Office\".")]
    public string roomName;

    [Tooltip("Optional long loop of people at work under the lanes. Never fans, air or hum. Leave empty for none.")]
    public AudioClip bed;
    [Tooltip("Perceived loudness of the bed, before the master volume.")]
    [Range(0f, 1f)] public float bedVolume = 0.6f;

    [Tooltip("Each lane plays on its own clock. Every sound should come from something the player can see, or " +
             "assume (someone behind a cubicle wall). Leave empty for a silent room.")]
    public List<AmbienceLane> lanes = new List<AmbienceLane>();
}

[System.Serializable]
public class AmbienceLane
{
    [Tooltip("What this lane is, for the inspector, e.g. \"Typing\".")]
    public string name;

    [Tooltip("One-shots picked at random, never the same one twice in a row.")]
    public SfxBank sounds = new SfxBank();

    [Tooltip("Seconds between sounds at the Blue mood. Later moods stretch them (the library's Mood spacing).")]
    [Min(0.5f)] public float minInterval = 6f;
    [Min(0.5f)] public float maxInterval = 14f;

    [Tooltip("Where on screen the sound comes from: -1 is the left edge, 1 the right. Each play picks a spot in this range.")]
    [Range(-1f, 1f)] public float panMin;
    [Range(-1f, 1f)] public float panMax;
}

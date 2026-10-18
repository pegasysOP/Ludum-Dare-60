using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The quiet sound of each room: a looping bed plus small details scattered on top (distant typing, a radio).
/// Played by <see cref="AmbiencePlayer"/>, which <see cref="RoomNavigator"/> tells which room you're in.
/// Files live in <c>Assets/Audio/ambience</c> and are built by <c>_silas/tools/office_ambience.py</c>.
/// </summary>
[CreateAssetMenu(menuName = "Audio/Ambience Library", fileName = "AmbienceLibrary")]
public class AmbienceLibrary : ScriptableObject
{
    private const string ResourcePath = "AmbienceLibrary";

    [Header("Mix")]
    [Tooltip("Perceived loudness of all ambience (beds and details), on top of the SFX volume setting. Keep it very soft.")]
    [Range(0f, 1f)] public float masterVolume = 0.45f;

    [Header("Timing")]
    [Tooltip("Seconds the old room's bed takes to hand over to the new one.")]
    [Min(0f)] public float roomCrossfade = 1.2f;
    [Tooltip("Seconds each bed crossfades into its own start when it loops, so the seam never shows.")]
    [Min(0.1f)] public float loopCrossfade = 4f;
    [Tooltip("Seconds the ambience takes to fade in when the game starts.")]
    [Min(0f)] public float startFade = 2.5f;
    [Tooltip("Seconds the ambience takes to fade out when an ending starts.")]
    [Min(0f)] public float endingFade = 2f;

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
}

[System.Serializable]
public class RoomAmbience
{
    [Tooltip("Name of the room's GameObject under RoomNavigator.rooms, e.g. \"Office\".")]
    public string roomName;

    [Tooltip("Seamless loop that plays the whole time you're in the room.")]
    public AudioClip bed;
    [Tooltip("Perceived loudness of the bed, before the master volume.")]
    [Range(0f, 1f)] public float bedVolume = 1f;

    [Tooltip("One-shots scattered over the bed at random intervals. Leave empty for a still room.")]
    public SfxBank details = new SfxBank();
    [Min(0.5f)] public float detailMinInterval = 6f;
    [Min(0.5f)] public float detailMaxInterval = 14f;
}

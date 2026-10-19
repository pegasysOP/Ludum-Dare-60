using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central ScriptableObject holding the sounds for clicking things in the office (the world, not the phone).
/// Slot names follow <c>_silas/SoundList.md</c>. Add new slots here as more interact sounds land in
/// <c>Assets/Audio/sfx/interact</c>.
/// </summary>
[CreateAssetMenu(menuName = "Audio/Interact Sfx Library", fileName = "InteractSfxLibrary")]
public class InteractSfxLibrary : ScriptableObject
{
    private const string ResourcePath = "InteractSfxLibrary";

    [Header("Clicks")]
    [Tooltip("Plays when an Interactable in the world is clicked, unless it has its own click sound.")]
    public SfxBank select = new SfxBank();

    [Header("Items")]
    [Tooltip("Plays when the player is given something (the secretary's CD).")]
    public SfxBank itemGet = new SfxBank();

    [Header("Paper")]
    public SfxBank paperOpen = new SfxBank();
    public SfxBank paperClose = new SfxBank();

    [Header("Objects")]
    [Tooltip("Tapping the fish tank glass in Security. Falls back to Select while empty.")]
    public SfxBank fishTap = new SfxBank();

    [Header("Moving")]
    [Tooltip("Three footsteps on carpet while the camera slides to the next room.")]
    public SfxBank roomMove = new SfxBank();

    /// <summary>Loads the shared library from Resources, or null if it is missing.</summary>
    public static InteractSfxLibrary Load()
    {
        InteractSfxLibrary library = Resources.Load<InteractSfxLibrary>(ResourcePath);
        if (library == null)
            Debug.LogError($"InteractSfxLibrary: no asset found at Resources/{ResourcePath}.");
        return library;
    }

    /// <summary>Every slot with its display name, in inspector order.</summary>
    public IEnumerable<KeyValuePair<string, SfxBank>> AllBanks()
    {
        yield return new KeyValuePair<string, SfxBank>("Select", select);
        yield return new KeyValuePair<string, SfxBank>("Item Get", itemGet);
        yield return new KeyValuePair<string, SfxBank>("Paper Open", paperOpen);
        yield return new KeyValuePair<string, SfxBank>("Paper Close", paperClose);
        yield return new KeyValuePair<string, SfxBank>("Fish Tap", fishTap);
        yield return new KeyValuePair<string, SfxBank>("Room Move", roomMove);
    }
}

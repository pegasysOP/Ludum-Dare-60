using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central ScriptableObject holding the generic UI sounds (menus, computer screen, phone buttons).
/// Loaded from Resources by <see cref="UiHoverSfx"/>, so no scene or prefab needs a reference to it.
/// Slot names follow <c>_silas/SoundList.md</c>. Add new slots here as more UI sounds land in
/// <c>Assets/Audio/sfx/ui</c>.
/// </summary>
[CreateAssetMenu(menuName = "Audio/UI Sfx Library", fileName = "UiSfxLibrary")]
public class UiSfxLibrary : ScriptableObject
{
    private const string ResourcePath = "UiSfxLibrary";

    [Header("Buttons")]
    [Tooltip("Plays when the pointer moves onto any interactable Button or Toggle, in every scene.")]
    public SfxBank buttonHover = new SfxBank();
    [Tooltip("Seconds between hover sounds so sweeping across a row of buttons doesn't machine-gun.")]
    [Min(0f)] public float hoverMinInterval = 0.05f;
    [Tooltip("Plays when a menu button is clicked (main menu, credits, soundtrack).")]
    public SfxBank buttonPress = new SfxBank();

    private void OnEnable()
    {
        // UI sounds go to the UI mixer group, so the phone snapshot doesn't muffle them with the world.
        foreach (KeyValuePair<string, SfxBank> bank in AllBanks())
            bank.Value.routeToUi = true;
    }

    /// <summary>Loads the shared library from Resources, or null if it is missing.</summary>
    public static UiSfxLibrary Load()
    {
        UiSfxLibrary library = Resources.Load<UiSfxLibrary>(ResourcePath);
        if (library == null)
            Debug.LogError($"UiSfxLibrary: no asset found at Resources/{ResourcePath}.");
        return library;
    }

    /// <summary>Every slot with its display name, in inspector order.</summary>
    public IEnumerable<KeyValuePair<string, SfxBank>> AllBanks()
    {
        yield return new KeyValuePair<string, SfxBank>("Button Hover", buttonHover);
        yield return new KeyValuePair<string, SfxBank>("Button Press", buttonPress);
    }
}

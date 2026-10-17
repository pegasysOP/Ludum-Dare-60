using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central ScriptableObject holding every phone OS / notification sound, so the
/// real phone UI and the PhoneSoundTest bench play from the same tuned banks.
/// Slot names follow <c>_silas/sound_slots.txt</c>. Add new slots here as more
/// phone sounds land in <c>Assets/Audio/sfx/Phone</c>.
/// </summary>
[CreateAssetMenu(menuName = "Audio/Phone Sfx Library", fileName = "PhoneSfxLibrary")]
public class PhoneSfxLibrary : ScriptableObject
{
    private const string ResourcePath = "PhoneSfxLibrary";

    [Header("OS")]
    public SfxBank tap = new SfxBank();
    public SfxBank back = new SfxBank();
    public SfxBank appOpen = new SfxBank();
    public SfxBank appClose = new SfxBank();
    public SfxBank swipe = new SfxBank();
    public SfxBank scrollTick = new SfxBank();
    public SfxBank toggleOn = new SfxBank();
    public SfxBank toggleOff = new SfxBank();
    public SfxBank error = new SfxBank();

    [Header("Notifications")]
    public SfxBank notifMessage = new SfxBank();

    /// <summary>Loads the shared library from Resources, or null if it is missing.</summary>
    public static PhoneSfxLibrary Load()
    {
        PhoneSfxLibrary library = Resources.Load<PhoneSfxLibrary>(ResourcePath);
        if (library == null)
            Debug.LogError($"PhoneSfxLibrary: no asset found at Resources/{ResourcePath}.");
        return library;
    }

    /// <summary>Every slot with its display name, in inspector order. Used by the test soundboard.</summary>
    public IEnumerable<KeyValuePair<string, SfxBank>> AllBanks()
    {
        yield return new KeyValuePair<string, SfxBank>("Tap", tap);
        yield return new KeyValuePair<string, SfxBank>("Back", back);
        yield return new KeyValuePair<string, SfxBank>("App Open", appOpen);
        yield return new KeyValuePair<string, SfxBank>("App Close", appClose);
        yield return new KeyValuePair<string, SfxBank>("Swipe", swipe);
        yield return new KeyValuePair<string, SfxBank>("Scroll Tick", scrollTick);
        yield return new KeyValuePair<string, SfxBank>("Toggle On", toggleOn);
        yield return new KeyValuePair<string, SfxBank>("Toggle Off", toggleOff);
        yield return new KeyValuePair<string, SfxBank>("Error", error);
        yield return new KeyValuePair<string, SfxBank>("Message", notifMessage);
    }
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central ScriptableObject holding the dialogue box sounds and the typing feel that goes with them.
/// Loaded from Resources by <see cref="DialogSfx"/>, so no scene or prefab needs a reference to it.
/// Slot names follow <c>_silas/SoundList.md</c> (Area: Dialog); clips live in <c>Assets/Audio/sfx/dialog</c>.
/// </summary>
[CreateAssetMenu(menuName = "Audio/Dialog Sfx Library", fileName = "DialogSfxLibrary")]
public class DialogSfxLibrary : ScriptableObject
{
    private const string ResourcePath = "DialogSfxLibrary";

    [Header("Text blips")]
    [Tooltip("Plays while a person's line types out. Rooted on Bb4; each Character's voiceSemitones pitches it. " +
             "The bank's own pitch range is ignored (pitch comes from the speaker).")]
    public SfxBank blipPerson = new SfxBank();
    [Tooltip("Plays while narration, objects and signs type out (lines with no Character). Rooted on Bb5, never wobbles.")]
    public SfxBank blipObject = new SfxBank();
    [Tooltip("Blip on every Nth letter or digit of a line, counting from its first letter. Spaces and punctuation never blip.")]
    [Range(1, 4)] public int blipEveryNLetters = 2;
    [Tooltip("Random pitch wobble per person blip, +/- semitones, so a voice doesn't sound like a machine.")]
    [Range(0f, 1.5f)] public float personWobbleSemitones = 0.5f;
    [Tooltip("Seconds to fade out the previous blip when the next one starts, so blips never stack up.")]
    [Range(0f, 0.05f)] public float cutFadeSeconds = 0.012f;

    [Header("Box")]
    [Tooltip("The first line of a conversation appears.")]
    public SfxBank open = new SfxBank();
    [Tooltip("The player clicks on to the next line.")]
    public SfxBank advance = new SfxBank();
    [Tooltip("The player clicks past the last line.")]
    public SfxBank close = new SfxBank();

    [Header("Typing pauses")]
    [Tooltip("Hold briefly on punctuation so lines read like speech.")]
    public bool punctuationPause = true;
    [Tooltip("Delay multiplier after . ! ? when a space follows (end of a sentence).")]
    [Min(1f)] public float sentencePause = 6f;
    [Tooltip("Delay multiplier after , ; :")]
    [Min(1f)] public float clausePause = 3f;
    [Tooltip("Delay multiplier for a dot inside an ellipsis (...).")]
    [Min(1f)] public float ellipsisPause = 2f;

    private void OnEnable()
    {
        // Dialogue is UI, so the phone snapshot doesn't muffle it with the world.
        foreach (KeyValuePair<string, SfxBank> bank in AllBanks())
            bank.Value.routeToUi = true;
    }

    /// <summary>Loads the shared library from Resources, or null if it is missing.</summary>
    public static DialogSfxLibrary Load()
    {
        DialogSfxLibrary library = Resources.Load<DialogSfxLibrary>(ResourcePath);
        if (library == null)
            Debug.LogError($"DialogSfxLibrary: no asset found at Resources/{ResourcePath}.");
        return library;
    }

    /// <summary>How much longer than usual to wait after showing <paramref name="c"/>.</summary>
    public float PauseFactor(char c, char next)
    {
        if (!punctuationPause)
            return 1f;
        if ((c == '.' || c == '!' || c == '?') && next == ' ')
            return sentencePause;
        if (c == ',' || c == ';' || c == ':')
            return clausePause;
        if (c == '.')
            return ellipsisPause;
        return 1f;
    }

    /// <summary>Every slot with its display name, in inspector order.</summary>
    public IEnumerable<KeyValuePair<string, SfxBank>> AllBanks()
    {
        yield return new KeyValuePair<string, SfxBank>("Blip Person", blipPerson);
        yield return new KeyValuePair<string, SfxBank>("Blip Object", blipObject);
        yield return new KeyValuePair<string, SfxBank>("Open", open);
        yield return new KeyValuePair<string, SfxBank>("Advance", advance);
        yield return new KeyValuePair<string, SfxBank>("Close", close);
    }
}

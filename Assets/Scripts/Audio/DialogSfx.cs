using UnityEngine;

/// <summary>
/// Dialogue box sounds: open / advance / close, and the text blips while a line types out.
/// <see cref="DialogManager"/> adds this to itself at runtime, so no scene wiring is needed.
///
/// Blips: every Nth letter of a line plays one short note. People share one clip, pitched by their
/// <see cref="Character.voiceSemitones"/> plus a small random wobble; narration (no Character) uses
/// its own steady tick. Two sources alternate so each new blip can fade the previous one out.
/// </summary>
public class DialogSfx : MonoBehaviour
{
    private DialogSfxLibrary library;
    private readonly AudioSource[] voices = new AudioSource[2];
    private readonly float[] fadeRate = new float[2];
    private int nextVoice;
    private int lastPersonClip = -1;
    private int lastObjectClip = -1;
    private Character speaker;
    private int lettersThisLine;

    public static DialogSfx Attach(GameObject host)
    {
        DialogSfx sfx = host.GetComponent<DialogSfx>();
        return sfx != null ? sfx : host.AddComponent<DialogSfx>();
    }

    private void Awake()
    {
        library = DialogSfxLibrary.Load();
        for (int i = 0; i < voices.Length; i++)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            voices[i] = src;
        }
    }

    private void Update()
    {
        for (int i = 0; i < voices.Length; i++)
        {
            if (fadeRate[i] <= 0f)
                continue;
            AudioSource src = voices[i];
            src.volume = Mathf.Max(0f, src.volume - fadeRate[i] * Time.unscaledDeltaTime);
            if (src.volume <= 0f)
            {
                src.Stop();
                fadeRate[i] = 0f;
            }
        }
    }

    public void Open() => library?.open.Play();
    public void Advance() => library?.advance.Play();
    public void Close() => library?.close.Play();

    /// <summary>Call when a new line starts typing. Null speaker = narration / object.</summary>
    public void StartLine(Character lineSpeaker)
    {
        speaker = lineSpeaker;
        lettersThisLine = 0;
    }

    /// <summary>Call for each character as it becomes visible.</summary>
    public void Letter(char c)
    {
        if (library == null || !char.IsLetterOrDigit(c))
            return;
        if (lettersThisLine++ % library.blipEveryNLetters == 0)
            Blip();
    }

    /// <summary>Delay multiplier after showing <paramref name="c"/> (punctuation pauses).</summary>
    public float PauseFactor(char c, char next) => library != null ? library.PauseFactor(c, next) : 1f;

    private void Blip()
    {
        bool person = speaker != null;
        SfxBank bank = person ? library.blipPerson : library.blipObject;
        int clip = PickClip(bank, person ? lastPersonClip : lastObjectClip);
        if (clip < 0)
            return;
        if (person) lastPersonClip = clip; else lastObjectClip = clip;

        // Fade out whatever is still ringing so blips never stack.
        for (int i = 0; i < voices.Length; i++)
        {
            if (voices[i].isPlaying && fadeRate[i] <= 0f)
                fadeRate[i] = library.cutFadeSeconds > 0f ? voices[i].volume / library.cutFadeSeconds : float.MaxValue;
        }

        float semitones = 0f;
        if (person)
            semitones = speaker.voiceSemitones + Random.Range(-library.personWobbleSemitones, library.personWobbleSemitones);

        AudioClipVolume entry = bank.clips[clip];
        float perceived = entry.Volume * bank.gain;
        if (bank.volumeJitter > 0f)
            perceived *= 1f + Random.Range(-bank.volumeJitter, bank.volumeJitter);

        AudioSource src = voices[nextVoice];
        fadeRate[nextVoice] = 0f;
        nextVoice = (nextVoice + 1) % voices.Length;
        if (AudioManager.Instance != null && AudioManager.Instance.uiSfxSource != null)
            src.outputAudioMixerGroup = AudioManager.Instance.uiSfxSource.outputAudioMixerGroup;
        src.clip = entry.Clip;
        src.pitch = Mathf.Pow(2f, semitones / 12f);
        src.volume = AudioVolume.ToLinear(perceived);
        src.Play();
    }

    /// <summary>A random clip index, never the same one twice in a row (when there's a choice).</summary>
    private static int PickClip(SfxBank bank, int last)
    {
        if (bank == null || !bank.HasAnyClip)
            return -1;
        int count = bank.clips.Count;
        for (int tries = 0; tries < 8; tries++)
        {
            int i = Random.Range(0, count);
            if ((i != last || count == 1) && bank.clips[i] != null && bank.clips[i].Clip != null)
                return i;
        }
        for (int i = 0; i < count; i++)
            if (bank.clips[i] != null && bank.clips[i].Clip != null)
                return i;
        return -1;
    }
}

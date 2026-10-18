using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Music bench for the four in-game moods. One button per mood crossfades to it through the real
/// <see cref="MusicManager"/> (PlayGameMusic), so what you hear is what the game will do. The readout shows
/// where the track is (intro, or which pass of the looping body) and "Hear the loop seam" jumps to just
/// before the loop end so the wrap can be checked by ear. Keys 1-4 pick a mood, S hears the seam, Space stops.
/// The UI is built in code so the scene stays tiny.
/// </summary>
public class SilasMusicTest : MonoBehaviour
{
    public MusicLibrary library;

    [Tooltip("Crossfade length between moods.")]
    public float crossfadeSeconds = 3f;
    [Tooltip("Keep the music at full level instead of letting the in-game envelope settle it into the background.")]
    public bool holdFullVolume = true;

    private struct Mood
    {
        public MusicTrack track;
        public string name, detail;
        public Color color;
    }

    private static readonly Mood[] Moods =
    {
        new Mood { track = MusicTrack.GameBlue, name = "Blue", detail = "Early investigation\nE♭ major · 96", color = new Color(.36f, .69f, .95f) },
        new Mood { track = MusicTrack.GameYellow, name = "Yellow", detail = "Mid investigation\nC Dorian · 104", color = new Color(.98f, .82f, .3f) },
        new Mood { track = MusicTrack.GameOrange, name = "Orange", detail = "Late investigation\nC minor · 112", color = new Color(.98f, .58f, .25f) },
        new Mood { track = MusicTrack.GameRed, name = "Red", detail = "Finale ramp\nC minor · 128", color = new Color(.93f, .33f, .33f) },
    };

    private readonly Outline[] outlines = new Outline[4];
    private TextMeshProUGUI status;
    private int current = -1;

    private void Start()
    {
        if (library == null)
            library = Resources.Load<MusicLibrary>("MusicLibrary");
        MusicManager mm = MusicManager.Instance;
        if (mm != null && holdFullVolume)
        {
            mm.peakMultiplier = 1f;
            mm.backgroundMultiplier = 1f;
        }
        BuildUI();
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.digit1Key.wasPressedThisFrame) Play(0);
            if (kb.digit2Key.wasPressedThisFrame) Play(1);
            if (kb.digit3Key.wasPressedThisFrame) Play(2);
            if (kb.digit4Key.wasPressedThisFrame) Play(3);
            if (kb.sKey.wasPressedThisFrame) HearSeam();
            if (kb.spaceKey.wasPressedThisFrame) Stop();
        }
        UpdateStatus();
    }

    private void Play(int i)
    {
        MusicManager mm = MusicManager.Instance;
        AudioClip clip = library != null ? library.Get(Moods[i].track) : null;
        if (mm == null || clip == null)
        {
            Debug.LogWarning($"SilasMusicTest: no MusicManager or no clip for {Moods[i].track}.");
            return;
        }
        mm.PlayGameMusic(clip, crossfadeSeconds);
        current = i;
        for (int k = 0; k < outlines.Length; k++)
            outlines[k].enabled = k == i;
    }

    private void HearSeam()
    {
        if (MusicManager.Instance != null)
            MusicManager.Instance.JumpBeforeLoopEnd(4f);
    }

    private void Stop()
    {
        if (MusicManager.Instance != null)
            MusicManager.Instance.StopMusic(1.5f);
        current = -1;
        foreach (Outline o in outlines)
            o.enabled = false;
    }

    private void UpdateStatus()
    {
        MusicManager mm = MusicManager.Instance;
        if (status == null) return;
        if (mm == null || current < 0 || !mm.IsMusicPlaying || mm.CurrentClip == null)
        {
            status.text = "Pick a mood (1-4). Crossfades take " + crossfadeSeconds.ToString("0.#") + " s.";
            return;
        }
        AudioClip clip = mm.CurrentClip;
        int pos = mm.CurrentTimeSamples;
        string where;
        if (mm.TryGetActiveLoopRegion(out int ls, out int le))
            where = pos < ls ? "intro (plays once)" : $"loop body {Time(pos - ls, clip.frequency)} / {Time(le - ls, clip.frequency)}";
        else
            where = "whole clip loops";
        status.text = $"{Moods[current].name}  ·  {Time(pos, clip.frequency)}  ·  {where}";
    }

    private static string Time(int samples, int freq)
    {
        float s = samples / (float)freq;
        return $"{(int)(s / 60)}:{s % 60:00.0}";
    }

    //==================== UI ====================

    private void BuildUI()
    {
        RectTransform root = Rect("SilasMusicTestCanvas", transform);
        Canvas canvas = root.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = root.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        root.gameObject.AddComponent<GraphicRaycaster>();

        Image bg = Rect("Background", root).gameObject.AddComponent<Image>();
        Stretch(bg.rectTransform);
        bg.color = new Color(.85f, .93f, .98f);

        Label(root, "Silas Music Test", 64, FontStyles.Bold, new Vector2(0, 380), new Vector2(1600, 100));
        Label(root, "The four in-game moods through the real MusicManager: crossfade between them, and check each loop seam.",
              28, FontStyles.Normal, new Vector2(0, 300), new Vector2(1600, 60));

        for (int i = 0; i < Moods.Length; i++)
        {
            int index = i;
            Button b = MoodButton(root, Moods[i], new Vector2(-630 + i * 420, 40));
            b.onClick.AddListener(() => Play(index));
            outlines[i] = b.gameObject.AddComponent<Outline>();
            outlines[i].effectColor = new Color(.1f, .2f, .35f);
            outlines[i].effectDistance = new Vector2(8, -8);
            outlines[i].enabled = false;
        }

        status = Label(root, "", 34, FontStyles.Normal, new Vector2(0, -210), new Vector2(1600, 60));
        SmallButton(root, "Hear the loop seam (S)", new Vector2(-220, -330)).onClick.AddListener(HearSeam);
        SmallButton(root, "Stop (Space)", new Vector2(220, -330)).onClick.AddListener(Stop);
    }

    private Button MoodButton(RectTransform parent, Mood mood, Vector2 pos)
    {
        RectTransform r = Rect(mood.name + "Button", parent);
        r.anchoredPosition = pos;
        r.sizeDelta = new Vector2(380, 300);
        Image img = r.gameObject.AddComponent<Image>();
        img.color = mood.color;
        Button b = r.gameObject.AddComponent<Button>();
        Label(r, mood.name, 56, FontStyles.Bold, new Vector2(0, 60), new Vector2(360, 80));
        Label(r, mood.detail, 26, FontStyles.Normal, new Vector2(0, -50), new Vector2(360, 120));
        return b;
    }

    private Button SmallButton(RectTransform parent, string text, Vector2 pos)
    {
        RectTransform r = Rect(text, parent);
        r.anchoredPosition = pos;
        r.sizeDelta = new Vector2(400, 80);
        r.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, .85f);
        Button b = r.gameObject.AddComponent<Button>();
        Label(r, text, 28, FontStyles.Normal, Vector2.zero, r.sizeDelta);
        return b;
    }

    private static TextMeshProUGUI Label(RectTransform parent, string text, float size, FontStyles style, Vector2 pos, Vector2 box)
    {
        RectTransform r = Rect("Label", parent);
        r.anchoredPosition = pos;
        r.sizeDelta = box;
        TextMeshProUGUI t = r.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
        t.color = new Color(.08f, .16f, .26f);
        t.raycastTarget = false;
        return t;
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }
}

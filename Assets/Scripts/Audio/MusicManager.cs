using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Single-track music player with two playback modes:
///
/// <list type="number">
///   <item>
///     <description>
///     <see cref="PlayGameMusic(AudioClip)"/> - in-game flow. Crossfades from
///     the currently playing track to the new clip over
///     <see cref="crossfadeDuration"/> (fading IN to the peak multiplier), holds
///     at peak for <see cref="peakHoldDuration"/>, then decays down to
///     <see cref="backgroundMultiplier"/> over <see cref="decayDuration"/>.
///     The three phases together are tuned so the music lands in the background
///     roughly 10 seconds after a change, mirroring the swell/decay behaviour
///     of <see cref="HeartbeatSoundPlayer"/>.
///     </description>
///   </item>
///   <item>
///     <description>
///     <see cref="PlayMusic(AudioClip)"/> - non-in-game flow (main menu, win,
///     credits). Snaps to the new clip at full volume with no envelope or
///     ongoing volume manipulation.
///     </description>
///   </item>
/// </list>
///
/// Tracks whose <see cref="MusicLibrary"/> entry has a loop region (the studio renders: intro, body,
/// outro) play the intro once and then loop the body sample-accurately: each music source gets a twin,
/// and the two are scheduled on the DSP clock to hand over exactly at the loop points. Tracks without a
/// loop region loop the whole clip as before, unless their entry is marked play-once (the endings), which
/// play through once and stop. <see cref="PlayEnding"/> crossfades out of the current track into one.
///
/// Re-requesting the clip that is already playing is a no-op in both modes.
/// All interpolation happens in perceived-loudness space via
/// <see cref="AudioVolume"/>, matching the rest of the audio stack.
/// </summary>
[DisallowMultipleComponent]
public class MusicManager : MonoBehaviour
{
    /// <summary>Most recently enabled instance.</summary>
    public static MusicManager Instance { get; private set; }

    private enum Mode { None, Game, Menu }

    [Header("Sources")]
    [Tooltip("Primary music source. Auto-created as a child if left null.")]
    public AudioSource sourceA;
    [Tooltip("Secondary music source used for crossfading. Auto-created as a child if left null.")]
    public AudioSource sourceB;
    [Tooltip("Optional mixer group output applied to both sources on Awake.")]
    public AudioMixerGroup mixerGroup;
    [Tooltip("Stingers play here. Its Send feeds the Duck Volume on the Music group, so a stinger ducks the music.")]
    public AudioMixerGroup stingerMixerGroup;

    [Header("In-Game Envelope")]
    [Tooltip("Crossfade length when switching tracks in-game. Also acts as the ramp-up " +
             "from silence to peak the first time a track starts.")]
    [Min(0.01f)] public float crossfadeDuration = 3f;
    [Tooltip("Seconds the new track stays at peak before decaying to the background.")]
    [Min(0f)] public float peakHoldDuration = 3f;
    [Tooltip("Decay length from peak down to backgroundMultiplier. " +
             "crossfade + peakHold + decay should land around ~10s total.")]
    [Min(0.01f)] public float decayDuration = 4f;
    [Tooltip("Perceived-loudness multiplier at the peak of the envelope (0-1). " +
             "1.0 = full music loudness.")]
    [Range(0f, 1f)] public float peakMultiplier = 1f;
    [Tooltip("Perceived-loudness multiplier during the background / resting phase (0-1).")]
    [Range(0f, 1f)] public float backgroundMultiplier = 0.4f;
    [Tooltip("Ease ramp-in and decay with SmoothStep instead of a straight linear lerp.")]
    public bool smoothEnvelope = true;

    [Header("Non-Game Playback")]
    [Tooltip("Perceived-loudness multiplier for menu/victory/credits music. 1.0 = full loudness.")]
    [Range(0f, 1f)] public float fullVolumeMultiplier = 1f;

    [Header("Pre-warm")]
    [Tooltip("Optional library whose clips are pre-loaded on Start so the first real Play call does not hitch. " +
             "If left null, the manager falls back to Resources.Load at 'prewarmLibraryResourcePath'.")]
    public MusicLibrary prewarmLibrary;
    [Tooltip("Resources path used to auto-load the pre-warm library when 'prewarmLibrary' is not assigned.")]
    public string prewarmLibraryResourcePath = "MusicLibrary";
    [Tooltip("If true, every pre-warmed clip is briefly played on a dedicated muted source to force the audio decoder " +
             "to initialise. Turn off if you only want the compressed data loaded.")]
    public bool prewarmPrimeDecoder = true;

    [Header("Debug")]
    [SerializeField] private AudioClip currentClipDebug;
    [SerializeField, Range(0f, 1f)] private float currentPerceivedMultiplierDebug;
    [SerializeField] private Mode currentModeDebug = Mode.None;
    // inspector readouts only, nothing in code reads them back
#pragma warning disable 0414
    [SerializeField] private string phaseDebug = "Idle";
    [SerializeField] private bool prewarmCompleteDebug;
#pragma warning restore 0414

    private AudioSource activeSource;
    private AudioSource stingerSource;

    // ---------- Loop regions ----------
    private class LoopState
    {
        public AudioSource primary, twin, playing;
        public AudioClip clip;
        public int start, end;
        public double nextSwitch;
        // DSP time the pair falls silent on its own (a fade-out's end), so a stalled frame loop can't leave it playing
        public double stopAt = double.MaxValue;
        public double Body => (end - start) / (double)clip.frequency;
        public AudioSource Waiting => playing == primary ? twin : primary;
    }

    private const double ScheduleLead = 0.05;      // seconds of DSP lead time when starting a scheduled track
    private const double FadeStopMargin = 0.25;    // a faded-out track stops on the audio clock this long after its fade ends
    private readonly Dictionary<AudioSource, LoopState> loops = new Dictionary<AudioSource, LoopState>();
    private AudioSource twinA, twinB;
    private MusicLibrary loopLibrary;
    private int pausedLoopSample = -1;
    // a paused source can report its position as 0, so a plain (non-loop) track remembers where it stopped
    private int pausedSample = -1;
    private Coroutine routine;
    private Coroutine prewarmRoutine;

    /// <summary>
    /// True while game music has been suspended (typically by the PowerDownSequence).
    /// While this is set, <see cref="PlayGameMusic"/> is ignored so external drivers
    /// (e.g. <see cref="GameMusicGuy"/>) can keep requesting clips without fighting the
    /// suspension. Clear with <see cref="ResumeGameMusic"/>.
    /// </summary>
    public bool IsGameMusicSuspended { get; private set; }

    private void Awake()
    {
        if (Instance == null || !Instance.isActiveAndEnabled)
            Instance = this;

        EnsureSource(ref sourceA, "MusicSourceA");
        EnsureSource(ref sourceB, "MusicSourceB");
        activeSource = sourceA;
    }

    private void Start()
    {
        MusicLibrary lib = prewarmLibrary != null
            ? prewarmLibrary
            : (string.IsNullOrEmpty(prewarmLibraryResourcePath)
                ? null
                : Resources.Load<MusicLibrary>(prewarmLibraryResourcePath));

        loopLibrary = lib;
        if (lib == null)
        {
            prewarmCompleteDebug = true;
            return;
        }

        prewarmRoutine = StartCoroutine(PrewarmRoutine(lib));
    }

    private void Update()
    {
        if (loops.Count == 0) return;
        double now = AudioSettings.dspTime;
        foreach (LoopState st in loops.Values)
        {
            // The primary carries the envelope (crossfades, ducking for settings); the twin follows it.
            st.twin.volume = st.primary.volume;
            if (pausedLoopSample < 0)
                AdvanceLoop(st, now);
        }
    }

    // Once a handover has happened, re-arm the source that finished for the next pass.
    private void AdvanceLoop(LoopState st, double now)
    {
        if (now < st.nextSwitch)
            return;
        double body = st.Body;
        if (now >= st.nextSwitch + body)
        {
            // frames stalled for a whole pass (a hidden browser tab) and both sources have run out: pick the body back up
            st.playing = st.primary;
            ScheduleBody(st, st.primary, now + ScheduleLead);
            st.nextSwitch = now + ScheduleLead + body;
            ScheduleBody(st, st.twin, st.nextSwitch);
            return;
        }
        AudioSource ended = st.playing;
        st.playing = st.Waiting;
        st.nextSwitch += body;
        ScheduleBody(st, ended, st.nextSwitch);
    }

    private void ScheduleBody(LoopState st, AudioSource src, double at)
    {
        src.Stop();
        if (at >= st.stopAt)
            return;
        src.timeSamples = st.start;
        src.PlayScheduled(at);
        src.SetScheduledEndTime(System.Math.Min(st.stopAt, at + st.Body));
    }

    /// <summary>
    /// Has a looping pair fall silent at DSP time <paramref name="at"/> on the audio clock (double.MaxValue to
    /// cancel), so a faded-out track stops even if frames stall mid-fade and the fade never finishes.
    /// </summary>
    private void SetStopTime(AudioSource src, double at)
    {
        if (src == null || !loops.TryGetValue(src, out LoopState st))
            return;
        double now = AudioSettings.dspTime;
        if (pausedLoopSample < 0)
            AdvanceLoop(st, now);
        st.stopAt = at;
        st.playing.SetScheduledEndTime(System.Math.Min(at, st.nextSwitch));
        // Re-arm the waiting source (a cancelled stop may have skipped arming it), unless its start is too close to touch.
        if (st.nextSwitch - now > ScheduleLead)
            ScheduleBody(st, st.Waiting, st.nextSwitch);
        else
            st.Waiting.SetScheduledEndTime(System.Math.Min(at, st.nextSwitch + st.Body));
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void EnsureSource(ref AudioSource src, string sourceName)
    {
        if (src == null)
        {
            GameObject go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            src = go.AddComponent<AudioSource>();
        }

        src.loop = true;
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.volume = 0f;
        if (mixerGroup != null)
            src.outputAudioMixerGroup = mixerGroup;
    }

    //==================== Public API ====================

    /// <summary>
    /// Play <paramref name="clip"/> using the in-game envelope (crossfade in,
    /// hold at peak, then fade to the background multiplier). No-op when the
    /// clip is already the actively-playing track. Pass a positive
    /// <paramref name="crossfadeOverride"/> to force a specific crossfade
    /// length for this call (useful for panic stingers that need to swap
    /// tracks near-instantly); leave it negative to use
    /// <see cref="crossfadeDuration"/>.
    /// </summary>
    public void PlayGameMusic(AudioClip clip, float crossfadeOverride = -1f)
    {
        if (clip == null) return;
        if (IsGameMusicSuspended) return;
        if (IsAlreadyPlaying(clip)) return;

        StopRoutine();
        currentClipDebug = clip;
        currentModeDebug = Mode.Game;
        float fade = crossfadeOverride >= 0f ? crossfadeOverride : crossfadeDuration;
        routine = StartCoroutine(CrossfadeRoutine(clip, fade, true));
    }

    /// <summary>
    /// Play <paramref name="clip"/> at full volume with no envelope or ongoing
    /// manipulation. Intended for menu / victory / credits screens. No-op when
    /// the clip is already the actively-playing track.
    /// </summary>
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        if (IsAlreadyPlaying(clip)) return;

        PlayFullVolume(clip, 0, false);
    }

    /// <summary>
    /// Like <see cref="PlayMusic"/>, but plays the whole clip once from <paramref name="startSeconds"/>, intro to
    /// outro, ignoring its loop region. For the soundtrack player, where songs play like an album.
    /// </summary>
    public void PlayThrough(AudioClip clip, float startSeconds = 0f)
    {
        if (clip == null) return;

        PlayFullVolume(clip, Mathf.FloorToInt(startSeconds * clip.frequency), true);
    }

    private void PlayFullVolume(AudioClip clip, int fromSample, bool playThrough)
    {
        StopRoutine();

        AudioSource other = OtherSource(activeSource);
        if (other != null)
        {
            StopSource(other);
            other.volume = 0f;
            other.clip = null;
        }

        activeSource.volume = TargetLinear(fullVolumeMultiplier);
        StartSource(activeSource, clip, fromSample, playThrough);

        currentClipDebug = clip;
        currentModeDebug = Mode.Menu;
        currentPerceivedMultiplierDebug = fullVolumeMultiplier;
        phaseDebug = "FullVolume";
    }

    /// <summary>
    /// Play an ending (or any one-shot track) at full volume, like <see cref="PlayMusic"/>, but crossfading
    /// out of whatever is playing over <paramref name="crossfadeSeconds"/>; 0 cuts the current track dead.
    /// Ignores <see cref="IsGameMusicSuspended"/>: an ending isn't game music. Whether the clip plays once
    /// comes from its <see cref="MusicLibrary"/> entry (<see cref="MusicTrackEntry.playOnce"/>).
    /// </summary>
    public void PlayEnding(AudioClip clip, float crossfadeSeconds)
    {
        if (clip == null) return;
        if (IsAlreadyPlaying(clip)) return;
        if (crossfadeSeconds <= 0f || !(IsPlayingPair(sourceA) || IsPlayingPair(sourceB)))
        {
            PlayMusic(clip);
            return;
        }

        StopRoutine();
        currentClipDebug = clip;
        currentModeDebug = Mode.Menu;
        routine = StartCoroutine(CrossfadeRoutine(clip, crossfadeSeconds, false));
    }

    /// <summary>
    /// Pause the active music source (preserves playback position).
    /// </summary>
    public void PauseMusic()
    {
        if (activeSource == null)
        {
            Debug.LogWarning("MusicManager: no active source to pause.");
            return;
        }
        if (loops.TryGetValue(activeSource, out LoopState st))
        {
            // Scheduled hand-overs live on the DSP clock, which keeps running while paused:
            // remember where we are and restart from there on UnPause.
            if (pausedLoopSample < 0)
            {
                pausedLoopSample = st.playing.timeSamples;
                st.primary.Pause();
                st.twin.Pause();
            }
            return;
        }
        if (activeSource.isPlaying)
        {
            pausedSample = activeSource.timeSamples;
            activeSource.Pause();
        }
    }

    /// <summary>
    /// Un-pause the active music source if a clip is assigned.
    /// </summary>
    public void UnPauseMusic()
    {
        if (activeSource == null)
        {
            Debug.LogWarning("MusicManager: no active source to un-pause.");
            return;
        }
        if (pausedLoopSample >= 0 && loops.TryGetValue(activeSource, out LoopState st))
        {
            int from = pausedLoopSample;
            pausedLoopSample = -1;
            StartSource(activeSource, st.clip, from);
            return;
        }
        if (pausedSample >= 0 && activeSource.clip != null)
        {
            activeSource.timeSamples = pausedSample;
            pausedSample = -1;
            activeSource.Play();
            return;
        }
        if (activeSource.clip != null && !activeSource.isPlaying)
            activeSource.UnPause();
    }

    /// <summary>
    /// True if the active music source is currently playing.
    /// </summary>
    public bool IsMusicPlaying => activeSource != null && IsPlayingPair(activeSource);

    /// <summary>Playback position of the active track in samples (whichever source of a loop pair is sounding).</summary>
    public int CurrentTimeSamples
    {
        get
        {
            if (activeSource == null) return 0;
            if (loops.TryGetValue(activeSource, out LoopState st)) return pausedLoopSample >= 0 ? pausedLoopSample : st.playing.timeSamples;
            if (pausedSample >= 0) return pausedSample;
            return activeSource.clip != null ? activeSource.timeSamples : 0;
        }
    }

    /// <summary>The active track's loop region in samples, if it has one.</summary>
    public bool TryGetActiveLoopRegion(out int startSample, out int endSample)
    {
        startSample = endSample = 0;
        if (activeSource == null || !loops.TryGetValue(activeSource, out LoopState st)) return false;
        startSample = st.start;
        endSample = st.end;
        return true;
    }

    /// <summary>
    /// Restart the active looped track a few seconds before its loop end, to hear the seam.
    /// Returns false if the active track has no loop region.
    /// </summary>
    public bool JumpBeforeLoopEnd(float seconds = 4f)
    {
        if (activeSource == null || !loops.TryGetValue(activeSource, out LoopState st)) return false;
        int at = Mathf.Max(0, st.end - Mathf.RoundToInt(seconds * st.clip.frequency));
        StartSource(activeSource, st.clip, at);
        return true;
    }

    /// <summary>
    /// Returns the clip currently assigned to the active source, or null.
    /// </summary>
    public AudioClip CurrentClip => activeSource != null ? activeSource.clip : null;

    /// <summary>
    /// Set the playback time on the active source if possible.
    /// </summary>
    public void SeekActive(float timeSeconds)
    {
        if (activeSource == null) return;
        if (activeSource.clip == null) return;
        // the clip's full length is one past the last sample, which Unity rejects as a seek
        AudioClip clip = activeSource.clip;
        int sample = Mathf.Clamp(Mathf.FloorToInt(timeSeconds * clip.frequency), 0, clip.samples - 1);
        if (loops.TryGetValue(activeSource, out LoopState st))
        {
            // the loop hand-overs are scheduled ahead, so restart the pair from the new spot rather than nudge one source
            if (pausedLoopSample >= 0)
                pausedLoopSample = Mathf.Min(sample, st.end - 1);
            else
                StartSource(activeSource, clip, sample);
            return;
        }
        if (pausedSample >= 0)
            pausedSample = sample;
        activeSource.timeSamples = sample;
    }

    /// <summary>
    /// Returns the current playback time (seconds) of the active source, or 0.
    /// </summary>
    public float CurrentTime => activeSource != null && activeSource.clip != null ? activeSource.time : 0f;

    /// <summary>
    /// Restart active clip from the beginning when possible.
    /// </summary>
    public void RestartActive()
    {
        SeekActive(0f);
    }

    /// <summary>
    /// Stop all music. When <paramref name="fadeOutDuration"/> is greater than
    /// zero, ramps the active source down in perceived space first.
    /// </summary>
    public void StopMusic(float fadeOutDuration = 0f)
    {
        StopRoutine();

        if (fadeOutDuration <= 0f)
        {
            SilenceAll();
            ResetDebug();
            return;
        }

        routine = StartCoroutine(FadeOutRoutine(fadeOutDuration));
    }

    /// <summary>
    /// Fade game music out (same ramp as <see cref="StopMusic"/>) and latch a suspension
    /// flag so subsequent <see cref="PlayGameMusic"/> calls are ignored until
    /// <see cref="ResumeGameMusic"/> is called. Intended for narrative moments where
    /// music must drop out and stay silent (e.g. the power-down sequence).
    /// </summary>
    public void SuspendGameMusic(float fadeOutDuration)
    {
        IsGameMusicSuspended = true;
        StopMusic(Mathf.Max(0f, fadeOutDuration));
    }

    /// <summary>
    /// Clears the <see cref="IsGameMusicSuspended"/> latch. Does NOT auto-restart any
    /// track; the next <see cref="PlayGameMusic"/> call (typically from
    /// <see cref="GameMusicGuy"/>) will crossfade the appropriate clip back in.
    /// </summary>
    public void ResumeGameMusic()
    {
        IsGameMusicSuspended = false;
    }

    /// <summary>
    /// Plays a one-shot stinger over the music, at the music volume setting. Routed to
    /// <see cref="stingerMixerGroup"/>, whose Send drives the Music group's Duck Volume, so
    /// the music ducks under the stinger and comes back on its own.
    /// </summary>
    public void PlayStinger(AudioClip clip, float perceivedVolume = 1f)
    {
        if (clip == null)
            return;
        if (stingerSource == null)
        {
            GameObject go = new GameObject("StingerSource");
            go.transform.SetParent(transform, false);
            stingerSource = go.AddComponent<AudioSource>();
            stingerSource.playOnAwake = false;
            stingerSource.spatialBlend = 0f;
            stingerSource.outputAudioMixerGroup = stingerMixerGroup != null ? stingerMixerGroup : mixerGroup;
        }
        stingerSource.PlayOneShot(clip, TargetLinear(perceivedVolume));
    }

    /// <summary>
    /// Rescale the active music source to match the current master-volume setting.
    /// Call after <see cref="SettingsUtils.SetMasterVolume"/> so the slider applies
    /// live to music that would otherwise hold its pre-change volume (menu music
    /// and game music that has settled into the Background phase).
    /// </summary>
    public void RefreshMasterVolume()
    {
        if (activeSource == null) return;
        if (currentModeDebug == Mode.None) return;
        activeSource.volume = TargetLinear(currentPerceivedMultiplierDebug);
    }

    //==================== Internals ====================

    private bool IsAlreadyPlaying(AudioClip clip)
    {
        return activeSource != null
            && activeSource.clip == clip
            && IsPlayingPair(activeSource);
    }

    private bool IsPlayingPair(AudioSource src)
    {
        if (src == null) return false;
        if (pausedLoopSample >= 0 && loops.ContainsKey(src)) return false;
        return src.isPlaying || (loops.TryGetValue(src, out LoopState st) && st.twin.isPlaying);
    }

    private AudioSource TwinOf(AudioSource src)
    {
        if (src == sourceA)
        {
            EnsureSource(ref twinA, "MusicSourceA Loop");
            return twinA;
        }
        EnsureSource(ref twinB, "MusicSourceB Loop");
        return twinB;
    }

    /// <summary>
    /// Start <paramref name="clip"/> on <paramref name="src"/> (volume already set by the caller). With a loop
    /// region: plays from <paramref name="fromSample"/> to the loop end, then hands over sample-accurately to the
    /// twin playing the loop body, and so on. Without: loops the whole clip. <paramref name="playThrough"/>
    /// ignores both and plays the clip once to its end.
    /// </summary>
    private void StartSource(AudioSource src, AudioClip clip, int fromSample = 0, bool playThrough = false)
    {
        StopSource(src);
        pausedSample = -1;
        src.clip = clip;
        if (!playThrough && loopLibrary != null && loopLibrary.TryGetLoopRegion(clip, out int ls, out int le) && le <= clip.samples)
        {
            AudioSource twin = TwinOf(src);
            int from = Mathf.Clamp(fromSample, 0, le - 1);
            double t0 = AudioSettings.dspTime + ScheduleLead;
            double switchAt = t0 + (le - from) / (double)clip.frequency;
            double body = (le - ls) / (double)clip.frequency;

            src.loop = false;
            src.timeSamples = from;
            src.PlayScheduled(t0);
            src.SetScheduledEndTime(switchAt);

            twin.loop = false;
            twin.clip = clip;
            twin.volume = src.volume;
            twin.timeSamples = ls;
            twin.PlayScheduled(switchAt);
            twin.SetScheduledEndTime(switchAt + body);

            loops[src] = new LoopState { primary = src, twin = twin, playing = src, clip = clip, start = ls, end = le,
                                         nextSwitch = switchAt };
            pausedLoopSample = -1;
            return;
        }
        src.loop = !playThrough && (loopLibrary == null || !loopLibrary.IsPlayOnce(clip));
        src.timeSamples = Mathf.Clamp(fromSample, 0, Mathf.Max(0, clip.samples - 1));
        src.Play();
    }

    private void StopSource(AudioSource src)
    {
        if (src == null) return;
        if (loops.TryGetValue(src, out LoopState st))
        {
            st.twin.Stop();
            st.twin.clip = null;
            st.twin.volume = 0f;
            loops.Remove(src);
        }
        src.Stop();
    }

    private AudioSource OtherSource(AudioSource src)
    {
        return src == sourceA ? sourceB : sourceA;
    }

    private void StopRoutine()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }

    private void SilenceAll()
    {
        if (sourceA != null) { StopSource(sourceA); sourceA.volume = 0f; sourceA.clip = null; }
        if (sourceB != null) { StopSource(sourceB); sourceB.volume = 0f; sourceB.clip = null; }
        pausedLoopSample = -1;
        pausedSample = -1;
    }

    private void ResetDebug()
    {
        currentClipDebug = null;
        currentModeDebug = Mode.None;
        currentPerceivedMultiplierDebug = 0f;
        phaseDebug = "Idle";
    }

    /// <summary>
    /// The source to fade <paramref name="clip"/> in on. A fade cut short by a new request leaves both sources
    /// sounding, so: if one is still playing the clip (fading back to the track you just left), it carries on
    /// from where it is; otherwise the quieter one is reused, so the cut lands on the track you hear least.
    /// </summary>
    private AudioSource PickFadeTarget(AudioClip clip, out bool resume)
    {
        resume = true;
        if (sourceA.clip == clip && IsPlayingPair(sourceA)) return sourceA;
        if (sourceB.clip == clip && IsPlayingPair(sourceB)) return sourceB;
        resume = false;
        float a = IsPlayingPair(sourceA) ? sourceA.volume : -1f;
        float b = IsPlayingPair(sourceB) ? sourceB.volume : -1f;
        return a <= b ? sourceA : sourceB;
    }

    // Fades run on the real clock, not frame deltas: a long or frozen frame finishes a fade, it never holds it halfway.
    private static double Now => Time.realtimeSinceStartupAsDouble;

    private static float Elapsed(double since, float limit)
    {
        return Mathf.Min(limit, (float)(Now - since));
    }

    // Crossfade from whatever is playing into the clip. With the envelope (in-game): rise to peak, hold, then
    // decay to background. Without (the endings): rise to full volume and stay there.
    private IEnumerator CrossfadeRoutine(AudioClip clip, float crossfadeSeconds, bool envelope)
    {
        AudioSource to = PickFadeTarget(clip, out bool resume);
        AudioSource from = OtherSource(to);
        // the new track counts as the current one from the start, so a repeat request mid-fade is a no-op
        activeSource = to;

        float fromStartLinear = IsPlayingPair(from) ? from.volume : 0f;
        float toStartLinear = resume ? to.volume : 0f;
        float topMultiplier = envelope ? peakMultiplier : fullVolumeMultiplier;
        float fade = Mathf.Max(0.01f, crossfadeSeconds);

        if (resume)
            SetStopTime(to, double.MaxValue);
        else
        {
            to.volume = 0f;
            StartSource(to, clip);
        }
        // If frames stall mid-fade (a hidden browser tab), the old track still stops on the audio clock.
        if (IsPlayingPair(from))
            SetStopTime(from, AudioSettings.dspTime + fade + FadeStopMargin);

        // Phase 1: crossfade (previous track fades out as new track rises to peak)
        phaseDebug = "Crossfade";
        double since = Now;
        float t = 0f;
        while (t < fade)
        {
            t = Elapsed(since, fade);
            float n = Ease(t / fade);
            // Re-evaluate the target each frame so master-volume changes mid-fade still track.
            to.volume = AudioVolume.LerpAmplitudePerceived(toStartLinear, TargetLinear(topMultiplier), n);
            from.volume = AudioVolume.LerpAmplitudePerceived(fromStartLinear, 0f, n);
            currentPerceivedMultiplierDebug = Mathf.Lerp(0f, topMultiplier, n);
            yield return null;
        }

        StopSource(from);
        from.volume = 0f;
        from.clip = null;
        to.volume = TargetLinear(topMultiplier);
        currentPerceivedMultiplierDebug = topMultiplier;

        if (!envelope)
        {
            phaseDebug = "FullVolume";
            routine = null;
            yield break;
        }

        // Phase 2: hold at peak
        phaseDebug = "PeakHold";
        since = Now;
        while (Now - since < peakHoldDuration)
        {
            // Re-evaluate each frame so master-volume changes mid-hold still track.
            to.volume = TargetLinear(peakMultiplier);
            yield return null;
        }

        // Phase 3: decay to background
        phaseDebug = "Decay";
        float decay = Mathf.Max(0.01f, decayDuration);
        float decayStart = to.volume;
        since = Now;
        t = 0f;
        while (t < decay)
        {
            t = Elapsed(since, decay);
            float n = Ease(t / decay);
            // Recompute end each frame so master-volume changes still track.
            to.volume = AudioVolume.LerpAmplitudePerceived(decayStart, TargetLinear(backgroundMultiplier), n);
            currentPerceivedMultiplierDebug = Mathf.Lerp(peakMultiplier, backgroundMultiplier, n);
            yield return null;
        }

        to.volume = TargetLinear(backgroundMultiplier);
        currentPerceivedMultiplierDebug = backgroundMultiplier;
        phaseDebug = "Background";
        routine = null;
    }

    // Load (and optionally decoder-prime) every clip in the library so the first
    // real Play call does not stall waiting on disk I/O or decoder allocation.
    private IEnumerator PrewarmRoutine(MusicLibrary lib)
    {
        AudioSource primeSource = null;
        if (prewarmPrimeDecoder)
        {
            GameObject go = new GameObject("MusicPrewarmSource");
            go.transform.SetParent(transform, false);
            primeSource = go.AddComponent<AudioSource>();
            primeSource.playOnAwake = false;
            primeSource.loop = false;
            primeSource.spatialBlend = 0f;
            primeSource.volume = 0f;
            primeSource.mute = true;
            if (mixerGroup != null)
                primeSource.outputAudioMixerGroup = mixerGroup;
        }

        foreach (MusicTrack track in System.Enum.GetValues(typeof(MusicTrack)))
        {
            AudioClip clip = lib.Get(track);
            if (clip == null) continue;

            if (clip.loadState != AudioDataLoadState.Loaded)
            {
                clip.LoadAudioData();
                while (clip.loadState == AudioDataLoadState.Loading)
                    yield return null;
            }

            if (primeSource != null && clip.loadState == AudioDataLoadState.Loaded)
            {
                primeSource.clip = clip;
                primeSource.Play();
                yield return null;
                primeSource.Stop();
                primeSource.clip = null;
            }

            yield return null;
        }

        if (primeSource != null)
            Destroy(primeSource.gameObject);

        prewarmCompleteDebug = true;
        prewarmRoutine = null;
    }

    private IEnumerator FadeOutRoutine(float duration)
    {
        if (!IsPlayingPair(sourceA) && !IsPlayingPair(sourceB))
        {
            SilenceAll();
            ResetDebug();
            routine = null;
            yield break;
        }

        // both sources, in case this cuts into a crossfade
        phaseDebug = "FadeOut";
        float startA = IsPlayingPair(sourceA) ? sourceA.volume : 0f;
        float startB = IsPlayingPair(sourceB) ? sourceB.volume : 0f;
        double stopAt = AudioSettings.dspTime + duration + FadeStopMargin;
        SetStopTime(sourceA, stopAt);
        SetStopTime(sourceB, stopAt);
        double since = Now;
        float t = 0f;
        while (t < duration)
        {
            t = Elapsed(since, duration);
            sourceA.volume = AudioVolume.LerpAmplitudePerceived(startA, 0f, t / duration);
            sourceB.volume = AudioVolume.LerpAmplitudePerceived(startB, 0f, t / duration);
            yield return null;
        }

        SilenceAll();
        ResetDebug();
        routine = null;
    }

    // Convert a perceived 0-1 multiplier into a linear AudioSource.volume, scaled
    // to the project's "full music loudness" baseline (master * music / 3).
    private float TargetLinear(float perceivedMultiplier)
    {
        float masterLinear = Mathf.Clamp01(SettingsUtils.GetMasterVolume() * SettingsUtils.GetMusicVolume()) / 3f;
        float masterPerceived = AudioVolume.ToPerceived(masterLinear);
        float perceived = masterPerceived * Mathf.Clamp01(perceivedMultiplier);
        return AudioVolume.ToLinear(perceived);
    }

    private float Ease(float t)
    {
        t = Mathf.Clamp01(t);
        return smoothEnvelope ? Mathf.SmoothStep(0f, 1f, t) : t;
    }
}

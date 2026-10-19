using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays the current room's ambience from <see cref="AmbienceLibrary"/>: each lane's one-shots at random intervals,
/// panned to where their source is on screen, over the room's bed if it has one (a soft recording of people at work).
/// As the mood climbs (<see cref="GameMusicGuy.CurrentMood"/>), the gaps grow so the busier music gets the room.
///
/// Beds loop by crossfading into their own start on a second AudioSource rather than with
/// <see cref="AudioSource.loop"/>, so a codec gap at the seam (WebGL transcodes to AAC) never shows.
///
/// Everything goes to the SFX mixer group, so the SFX volume setting applies and the world muffles when the phone
/// is up. Sounds still playing when you leave a room fade out behind the move, and the beds crossfade.
///
/// Created on the first <see cref="SetRoom"/> in the active scene, so it goes away with the Game scene.
/// </summary>
public class AmbiencePlayer : MonoBehaviour
{
    // more than any room ever overlaps; if they're all busy, that sound is skipped
    private const int MaxVoices = 6;

    private static AmbiencePlayer instance;

    private AmbienceLibrary library;
    private readonly List<Voice> voices = new List<Voice>();
    private readonly List<BedVoice> beds = new List<BedVoice>();
    private RoomAmbience currentRoom;
    private float[] nextTimes = new float[0];
    private int[] lastClips = new int[0];
    private bool stopped;

    /// <summary>Switch to the ambience of the room with this GameObject name.</summary>
    public static void SetRoom(string roomName)
    {
        if (instance == null)
        {
            AmbienceLibrary library = AmbienceLibrary.Load();
            if (library == null || !library.playAmbience)
                return;
            instance = new GameObject(nameof(AmbiencePlayer)).AddComponent<AmbiencePlayer>();
            instance.library = library;
        }
        instance.ChangeRoom(roomName);
    }

    /// <summary>Fade all ambience out for good (the ending). Safe to call when nothing is playing.</summary>
    public static void FadeOut()
    {
        if (instance == null)
            return;
        instance.stopped = true;
        foreach (Voice voice in instance.voices)
            voice.FadeOut(instance.library.endingFade);
        foreach (BedVoice bed in instance.beds)
            bed.FadeTo(0f, instance.library.endingFade);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void ChangeRoom(string roomName)
    {
        if (stopped)
            return;

        RoomAmbience room = library.Get(roomName);
        if (room == currentRoom)
            return;

        foreach (Voice voice in voices)
            voice.FadeOut(library.roomExitFade);
        ChangeBed(room);

        currentRoom = room;
        int count = room != null ? room.lanes.Count : 0;
        nextTimes = new float[count];
        lastClips = new int[count];
        for (int i = 0; i < count; i++)
        {
            // first sound lands early in the lane's gap, so the room comes alive soon after the fade
            AmbienceLane lane = room.lanes[i];
            float firstGap = Mathf.Max(0.5f, Mathf.Min(lane.minInterval, lane.maxInterval));
            nextTimes[i] = Time.unscaledTime + Random.Range(0.5f, firstGap) * Spacing();
            lastClips[i] = -1;
        }
    }

    private void ChangeBed(RoomAmbience room)
    {
        bool first = currentRoom == null && beds.Count == 0;
        float fadeTime = first ? library.startFade : library.roomExitFade;
        foreach (BedVoice bed in beds)
            bed.FadeTo(0f, fadeTime);
        if (room == null || room.bed == null)
            return;

        // coming straight back picks the old bed up where it was instead of restarting it
        BedVoice target = beds.Find(bed => bed.room == room);
        if (target == null)
        {
            target = new BedVoice(gameObject, room, MixerGroup());
            beds.Add(target);
        }
        target.FadeTo(1f, fadeTime);
    }

    private void Update()
    {
        float sfxVolume = AudioManager.Instance != null && AudioManager.Instance.sfxSource != null
            ? AudioManager.Instance.sfxSource.volume
            : 1f;
        foreach (Voice voice in voices)
            voice.Tick(Time.unscaledDeltaTime, sfxVolume);
        for (int i = beds.Count - 1; i >= 0; i--)
        {
            BedVoice bed = beds[i];
            float bedVolume = sfxVolume * AudioVolume.ToLinear(library.masterVolume * bed.room.bedVolume);
            if (!bed.Tick(Time.unscaledDeltaTime, bedVolume, library.loopCrossfade))
            {
                bed.Dispose();
                beds.RemoveAt(i);
            }
        }

        if (stopped || currentRoom == null)
            return;

        for (int i = 0; i < nextTimes.Length; i++)
        {
            if (Time.unscaledTime < nextTimes[i])
                continue;
            AmbienceLane lane = currentRoom.lanes[i];
            nextTimes[i] = Time.unscaledTime + NextGap(lane);
            Play(lane, i, sfxVolume);
        }
    }

    private void Play(AmbienceLane lane, int laneIndex, float sfxVolume)
    {
        SfxBank bank = lane.sounds;
        int clipIndex = PickClip(bank, lastClips[laneIndex]);
        if (clipIndex < 0)
            return;
        Voice voice = FreeVoice();
        if (voice == null)
            return;
        lastClips[laneIndex] = clipIndex;

        // same perceived-loudness shaping as SfxBank.Play, with the ambience master on top
        AudioClipVolume entry = bank.clips[clipIndex];
        float perceived = entry.Volume * bank.gain * library.masterVolume;
        if (bank.volumeJitter > 0f)
            perceived *= 1f + Random.Range(-bank.volumeJitter, bank.volumeJitter);

        float pitch = Random.Range(Mathf.Min(bank.pitchMin, bank.pitchMax), Mathf.Max(bank.pitchMin, bank.pitchMax));
        float pan = Random.Range(Mathf.Min(lane.panMin, lane.panMax), Mathf.Max(lane.panMin, lane.panMax));
        voice.Play(entry.Clip, AudioVolume.ToLinear(perceived), pitch, pan, Mathf.Max(0f, entry.Delay), sfxVolume);
    }

    /// <summary>A random clip index from the bank, never <paramref name="last"/> when there's another; -1 if none.</summary>
    private static int PickClip(SfxBank bank, int last)
    {
        if (bank == null || bank.clips == null)
            return -1;
        int count = bank.clips.Count;
        int start = Random.Range(0, count);
        int fallback = -1;
        for (int i = 0; i < count; i++)
        {
            int idx = (start + i) % count;
            if (bank.clips[idx] == null || bank.clips[idx].Clip == null)
                continue;
            if (idx != last)
                return idx;
            fallback = idx;
        }
        return fallback;
    }

    private Voice FreeVoice()
    {
        foreach (Voice voice in voices)
        {
            if (voice.IsFree)
                return voice;
        }
        if (voices.Count >= MaxVoices)
            return null;
        Voice created = new Voice(gameObject, MixerGroup());
        voices.Add(created);
        return created;
    }

    private float NextGap(AmbienceLane lane)
    {
        float min = Mathf.Min(lane.minInterval, lane.maxInterval);
        float max = Mathf.Max(lane.minInterval, lane.maxInterval);
        return Random.Range(min, max) * Spacing();
    }

    private float Spacing()
    {
        return library.Spacing(GameMusicGuy.CurrentMood);
    }

    private static UnityEngine.Audio.AudioMixerGroup MixerGroup()
    {
        return AudioManager.Instance != null && AudioManager.Instance.sfxSource != null
            ? AudioManager.Instance.sfxSource.outputAudioMixerGroup
            : null;
    }

    /// <summary>One AudioSource the lanes take turns on, so each sound can have its own pan and fade.</summary>
    private class Voice
    {
        private readonly AudioSource source;
        private float gain;
        private float level;
        private float fadeSpeed;

        public Voice(GameObject host, UnityEngine.Audio.AudioMixerGroup group)
        {
            source = host.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = group;
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
        }

        public bool IsFree => !source.isPlaying;

        public void Play(AudioClip clip, float gain, float pitch, float pan, float delay, float sfxVolume)
        {
            this.gain = gain;
            level = 1f;
            fadeSpeed = 0f;
            source.clip = clip;
            source.pitch = pitch;
            source.panStereo = pan;
            source.volume = sfxVolume * gain;
            if (delay > 0f)
                source.PlayDelayed(delay);
            else
                source.Play();
        }

        public void FadeOut(float seconds)
        {
            if (source.isPlaying)
                fadeSpeed = seconds > 0f ? 1f / seconds : float.MaxValue;
        }

        public void Tick(float dt, float sfxVolume)
        {
            if (!source.isPlaying)
                return;
            if (fadeSpeed > 0f)
            {
                level = Mathf.MoveTowards(level, 0f, fadeSpeed * dt);
                if (level <= 0f)
                {
                    source.Stop();
                    fadeSpeed = 0f;
                    return;
                }
            }
            // equal power, matching how the old beds faded
            source.volume = sfxVolume * gain * Mathf.Sin(level * Mathf.PI * 0.5f);
        }
    }

    /// <summary>One room's bed: two sources taking turns so each pass crossfades into the next.</summary>
    private class BedVoice
    {
        public readonly RoomAmbience room;
        private readonly AudioSource[] sources = new AudioSource[2];
        private int newest;
        private float level;
        private float target;
        private float speed;

        public BedVoice(GameObject host, RoomAmbience room, UnityEngine.Audio.AudioMixerGroup group)
        {
            this.room = room;
            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource src = host.AddComponent<AudioSource>();
                src.clip = room.bed;
                src.outputAudioMixerGroup = group;
                src.playOnAwake = false;
                src.loop = false;
                src.spatialBlend = 0f;
                src.volume = 0f;
                sources[i] = src;
            }
            // start somewhere in the loop so each visit doesn't open on the same second
            sources[0].time = Random.Range(0f, room.bed.length * 0.5f);
            sources[0].Play();
        }

        public void FadeTo(float value, float seconds)
        {
            target = value;
            speed = seconds > 0f ? 1f / seconds : float.MaxValue;
        }

        /// <summary>Advances fades and the loop. Returns false once it has faded out and can go.</summary>
        public bool Tick(float dt, float baseVolume, float loopCrossfade)
        {
            level = Mathf.MoveTowards(level, target, speed * dt);
            if (level <= 0f && target <= 0f)
                return false;

            AudioSource current = sources[newest];
            AudioSource previous = sources[1 - newest];
            float length = room.bed.length;
            float overlap = Mathf.Min(loopCrossfade, length * 0.25f);

            // near the end (or stopped early): start the other source from the top and crossfade into it
            if (!previous.isPlaying && (!current.isPlaying || current.time >= length - overlap))
            {
                newest = 1 - newest;
                previous = current;
                current = sources[newest];
                current.time = 0f;
                current.Play();
            }

            // equal power, so the level holds steady through both kinds of crossfade
            float roomGain = Mathf.Sin(level * Mathf.PI * 0.5f);
            if (previous.isPlaying)
            {
                float t = Mathf.Clamp01(current.time / overlap);
                current.volume = baseVolume * roomGain * Mathf.Sin(t * Mathf.PI * 0.5f);
                previous.volume = baseVolume * roomGain * Mathf.Cos(t * Mathf.PI * 0.5f);
                if (t >= 1f)
                    previous.Stop();
            }
            else
            {
                current.volume = baseVolume * roomGain;
            }
            return true;
        }

        public void Dispose()
        {
            foreach (AudioSource src in sources)
            {
                if (src != null)
                    Object.Destroy(src);
            }
        }
    }
}

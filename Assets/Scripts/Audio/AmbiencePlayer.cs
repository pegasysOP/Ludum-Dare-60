using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays the current room's ambience from <see cref="AmbienceLibrary"/>: its bed on a loop, and its details at
/// random intervals. Moving rooms crossfades the beds. Everything goes to the SFX mixer group, so the SFX
/// volume setting applies and the world muffles when the phone is up.
///
/// Beds loop by crossfading into their own start on a second AudioSource rather than with
/// <see cref="AudioSource.loop"/>, so a codec gap at the seam (WebGL transcodes to AAC) never shows.
///
/// Created on the first <see cref="SetRoom"/> in the active scene, so it goes away with the Game scene.
/// </summary>
public class AmbiencePlayer : MonoBehaviour
{
    private static AmbiencePlayer instance;

    private AmbienceLibrary library;
    private readonly List<BedVoice> voices = new List<BedVoice>();
    private RoomAmbience currentRoom;
    private float nextDetailTime;
    private bool stopped;

    /// <summary>Switch to the ambience of the room with this GameObject name. The first call fades the ambience in.</summary>
    public static void SetRoom(string roomName)
    {
        if (instance == null)
        {
            AmbienceLibrary library = AmbienceLibrary.Load();
            if (library == null)
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
        foreach (BedVoice voice in instance.voices)
            voice.FadeTo(0f, instance.library.endingFade);
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

        bool first = currentRoom == null && voices.Count == 0;
        float fadeTime = first ? library.startFade : library.roomCrossfade;
        foreach (BedVoice voice in voices)
            voice.FadeTo(0f, fadeTime);

        currentRoom = room;
        nextDetailTime = Time.unscaledTime + NextDetailDelay();
        if (room == null || room.bed == null)
            return;

        // coming straight back picks the old bed up where it was instead of restarting it
        BedVoice target = voices.Find(voice => voice.room == room);
        if (target == null)
        {
            target = new BedVoice(gameObject, room, MixerGroup());
            voices.Add(target);
        }
        target.FadeTo(1f, fadeTime);
    }

    private void Update()
    {
        float sfxVolume = AudioManager.Instance != null && AudioManager.Instance.sfxSource != null
            ? AudioManager.Instance.sfxSource.volume
            : 1f;

        for (int i = voices.Count - 1; i >= 0; i--)
        {
            BedVoice voice = voices[i];
            float baseVolume = sfxVolume * AudioVolume.ToLinear(library.masterVolume * voice.room.bedVolume);
            if (!voice.Tick(Time.unscaledDeltaTime, baseVolume, library.loopCrossfade))
            {
                voice.Dispose();
                voices.RemoveAt(i);
            }
        }

        if (stopped || currentRoom == null || Time.unscaledTime < nextDetailTime)
            return;
        nextDetailTime = Time.unscaledTime + NextDetailDelay();
        if (currentRoom.details.HasAnyClip)
            currentRoom.details.Play(library.masterVolume);
    }

    private float NextDetailDelay()
    {
        if (currentRoom == null)
            return 0f;
        float min = Mathf.Min(currentRoom.detailMinInterval, currentRoom.detailMaxInterval);
        float max = Mathf.Max(currentRoom.detailMinInterval, currentRoom.detailMaxInterval);
        return Random.Range(min, max);
    }

    private static UnityEngine.Audio.AudioMixerGroup MixerGroup()
    {
        return AudioManager.Instance != null && AudioManager.Instance.sfxSource != null
            ? AudioManager.Instance.sfxSource.outputAudioMixerGroup
            : null;
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

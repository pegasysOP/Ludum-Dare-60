using System;
using System.Collections.Generic;
using UnityEngine;

// which way a note points, shown as the colour of its bullet in SusPX
public enum NoteLean
{
    Neutral,
    Against,
    For
}

// one line of evidence about a suspect, picked up while investigating
[Serializable]
public class SuspectNote
{
    public Character suspect;
    [TextArea(2, 4)] public string text;
    public NoteLean lean;
}

// everything the player has noted so far, read by the SusPX app
public static class SuspectNotes
{
    private static readonly List<SuspectNote> notes = new List<SuspectNote>();
    private static readonly HashSet<SuspectNote> unseen = new HashSet<SuspectNote>();

    public static event Action Changed;

    public static bool HasUnseen => unseen.Count > 0;

    public static void Add(SuspectNote note)
    {
        if (note == null || note.suspect == null || string.IsNullOrEmpty(note.text) || notes.Contains(note))
            return;

        notes.Add(note);
        unseen.Add(note);
        Changed?.Invoke();
    }

    public static List<SuspectNote> For(Character suspect)
    {
        return notes.FindAll(note => note.suspect == suspect);
    }

    public static bool HasUnseenFor(Character suspect)
    {
        foreach (SuspectNote note in unseen)
            if (note.suspect == suspect)
                return true;
        return false;
    }

    public static void MarkSeen(Character suspect)
    {
        if (unseen.RemoveWhere(note => note.suspect == suspect) > 0)
            Changed?.Invoke();
    }

    // a new game starts with a clean notebook, and the old phone's listeners go with the old scene
    public static void Reset()
    {
        notes.Clear();
        unseen.Clear();
        Changed = null;
    }
}

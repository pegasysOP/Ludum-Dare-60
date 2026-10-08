using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutsceneSystem : MonoBehaviour
{
    public static CutsceneSystem Instance { get; private set; }

    public event Action<string> OnCutsceneStarted;
    public event Action<string> OnCutsceneEnded;
    public event Action<string> OnCutsceneSkipped;

    private readonly Dictionary<string, ICutscene> registeredCutscenes = new();
   
    private Coroutine activeCutscene;
    private string currentCutsceneId;
    private ICutscene currentCutscene;
    private bool isPlaying;

    public bool IsPlaying => isPlaying;
    public string CurrentCutsceneId => currentCutsceneId;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void RegisterCutscene(string cutsceneId, ICutscene cutscene)
    {
        if (string.IsNullOrWhiteSpace(cutsceneId) || cutscene == null)
            return;

        registeredCutscenes[cutsceneId] = cutscene;
    }

    public void PlayCutscene(string cutsceneId)
    {
        if (isPlaying)
            return;

        if (!registeredCutscenes.TryGetValue(cutsceneId, out ICutscene cutscene))
            return;

        activeCutscene = StartCoroutine(RunCutscene(cutsceneId, cutscene));
    }

    public void SkipCurrentCutscene()
    {
        if (!isPlaying || currentCutscene == null)
            return;

        if (activeCutscene != null)
            StopCoroutine(activeCutscene);

        string skippedId = currentCutsceneId;
        ICutscene skippedCutscene = currentCutscene;

        StartCoroutine(RunSkip(skippedId, skippedCutscene));
    }

    private IEnumerator RunCutscene(string cutsceneId, ICutscene cutscene)
    {
        isPlaying = true;
        currentCutsceneId = cutsceneId;
        currentCutscene = cutscene;

        OnCutsceneStarted?.Invoke(cutsceneId);

        if (cutscene.PreCutscene() != null)
            yield return StartCoroutine(cutscene.PreCutscene());

        if (cutscene.PlayCutscene() != null)
            yield return StartCoroutine(cutscene.PlayCutscene());

        if (cutscene.PostCutscene() != null)
            yield return StartCoroutine(cutscene.PostCutscene());

        string endedId = currentCutsceneId;

        CleanupCutsceneState();

        OnCutsceneEnded?.Invoke(endedId);
    }

    private IEnumerator RunSkip(string cutsceneId, ICutscene cutscene)
    {
        if (cutscene.SkipCutscene() != null)
            yield return StartCoroutine(cutscene.SkipCutscene());

        CleanupCutsceneState();

        OnCutsceneSkipped?.Invoke(cutsceneId);
    }

    private void CleanupCutsceneState()
    {
        activeCutscene = null;
        currentCutsceneId = null;
        currentCutscene = null;
        isPlaying = false;
    }
}
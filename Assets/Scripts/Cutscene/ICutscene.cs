using System.Collections;
using UnityEngine;

public interface ICutscene
{
    string cutsceneId { get; }
    IEnumerator PreCutscene();
    IEnumerator PlayCutscene();
    IEnumerator PostCutscene();
    IEnumerator SkipCutscene();
    void PauseCutscene() {
        Time.timeScale = 0f;
    }
    void ResumeCutscene() {
        Time.timeScale = 1f;
    }
}
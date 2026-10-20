using UnityEngine;

// renders at the biggest 16:9 size the monitor fits, unity letterboxes whatever's left
public static class NativeResolution
{
    private const float Aspect = 16f / 9f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
#if UNITY_STANDALONE && !UNITY_EDITOR
        int width = Mathf.Min(Display.main.systemWidth, Mathf.FloorToInt(Display.main.systemHeight * Aspect));
        int height = Mathf.FloorToInt(width / Aspect);
        Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow);
#endif
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Plays <see cref="UiSfxLibrary.buttonHover"/> whenever the mouse moves onto an interactable
/// Button or Toggle, in any scene. It spawns itself before the first scene loads and raycasts the
/// UI under the mouse each frame, so buttons need no per-button component and scenes need no
/// wiring: a new button made from any prefab, or straight in a scene, gets the sound for free.
/// </summary>
public class UiHoverSfx : MonoBehaviour
{
    private UiSfxLibrary library;
    private PointerEventData pointer;
    private EventSystem pointerOwner;
    private readonly List<RaycastResult> hits = new List<RaycastResult>();
    private Selectable hovered;
    private float lastPlayTime = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Spawn()
    {
        UiSfxLibrary library = UiSfxLibrary.Load();
        if (library == null)
            return;

        GameObject go = new GameObject(nameof(UiHoverSfx));
        DontDestroyOnLoad(go);
        go.AddComponent<UiHoverSfx>().library = library;
    }

    private void Update()
    {
        Selectable current = FindHovered();
        if (current == hovered)
            return;

        hovered = current;
        if (current == null || Time.unscaledTime - lastPlayTime < library.hoverMinInterval)
            return;

        lastPlayTime = Time.unscaledTime;
        library.buttonHover.Play();
    }

    private Selectable FindHovered()
    {
        // hidden cursor = camera viewfinder or similar, nothing is really being pointed at
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null || Mouse.current == null || !Cursor.visible || !Application.isFocused)
            return null;

        if (pointer == null || pointerOwner != eventSystem)
        {
            pointer = new PointerEventData(eventSystem);
            pointerOwner = eventSystem;
        }
        pointer.position = Mouse.current.position.ReadValue();

        hits.Clear();
        eventSystem.RaycastAll(pointer, hits);
        if (hits.Count == 0)
            return null;

        // only the topmost hit counts, so a button under a panel stays silent
        Selectable selectable = hits[0].gameObject.GetComponentInParent<Selectable>();
        if (selectable == null || !(selectable is Button || selectable is Toggle) || !selectable.IsInteractable())
            return null;
        return selectable;
    }
}

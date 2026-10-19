using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public Button startButton;
    public Button soundtrackButton; 
    public Button quitButton;

    // swing in from the left one after another, the phone comes in from the right
    public RectTransform[] swingIn;
    public RectTransform phone;
    public float swingDistance = 700f;
    public float stagger = 0.07f;
    // sky wash over the whole menu before the next scene loads
    public CanvasGroup fade;

    private readonly Dictionary<RectTransform, (Vector2 position, Quaternion rotation)> rest = new Dictionary<RectTransform, (Vector2, Quaternion)>();
    private bool leaving = false;

    private void Awake()
    {
        startButton.onClick.AddListener(OnStartClicked);
        soundtrackButton.onClick.AddListener(OnSoundtrackClicked);
        quitButton.onClick.AddListener(OnQuitClicked);

#if UNITY_WEBGL
        quitButton.gameObject.SetActive(false);
#endif

        fade.alpha = 0f;
        fade.blocksRaycasts = false;
    }

    private void Start()
    {
        // buttons sit in a layout group, so place them once and let the tweens move them freely
        LayoutGroup layout = startButton.GetComponentInParent<LayoutGroup>();
        if (layout != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)layout.transform);
            layout.enabled = false;
        }

        for (int i = 0; i < swingIn.Length; i++)
            SwingIn(swingIn[i], -1f, 0.15f + i * stagger);
        SwingIn(phone, 1f, 0.3f);
    }

    // slides in from the side tilted, settling flat as it lands
    private void SwingIn(RectTransform rect, float side, float delay)
    {
        if (!rect.gameObject.activeInHierarchy)
            return;

        CanvasGroup group = rect.GetComponent<CanvasGroup>();
        if (group == null)
            group = rect.gameObject.AddComponent<CanvasGroup>();

        Vector2 target = rect.anchoredPosition;
        Quaternion rotation = rect.localRotation;
        rest[rect] = (target, rotation);
        rect.anchoredPosition = target + new Vector2(side * swingDistance, 0f);
        rect.localRotation = rotation * Quaternion.Euler(0f, 0f, -side * 12f);
        group.alpha = 0f;

        rect.DOAnchorPos(target, 0.7f).SetDelay(delay).SetEase(Ease.OutBack).SetLink(rect.gameObject);
        rect.DOLocalRotateQuaternion(rotation, 0.8f).SetDelay(delay).SetEase(Ease.OutBack).SetLink(rect.gameObject);
        group.DOFade(1f, 0.35f).SetDelay(delay).SetLink(rect.gameObject);
    }

    private void OnStartClicked()
    {
        Leave(SceneUtils.LoadGameScene);
    }

    private void OnQuitClicked()
    {
        SceneUtils.QuitApplication();
    }

    private void OnSoundtrackClicked()
    {
        Leave(SceneUtils.LoadSoundtrackScene);
    }

    // everything flies back off the way it came, then the screen washes to sky before the next scene
    private void Leave(TweenCallback load)
    {
        if (leaving)
            return;

        leaving = true;
        fade.blocksRaycasts = true;

        Sequence exit = DOTween.Sequence().SetLink(gameObject);
        for (int i = 0; i < swingIn.Length; i++)
            SwingOut(exit, swingIn[i], -1f, i * 0.04f);
        SwingOut(exit, phone, 1f, 0f);
        exit.Insert(0.3f, fade.DOFade(1f, 0.25f));
        exit.AppendCallback(load);
    }

    private void SwingOut(Sequence exit, RectTransform rect, float side, float at)
    {
        if (!rest.TryGetValue(rect, out var from))
            return;

        CanvasGroup group = rect.GetComponent<CanvasGroup>();
        // from wherever it is, so a click mid intro doesn't jump
        rect.DOKill();
        group.DOKill();

        exit.Insert(at, rect.DOAnchorPos(from.position + new Vector2(side * swingDistance, 0f), 0.4f).SetEase(Ease.InBack));
        exit.Insert(at, rect.DOLocalRotateQuaternion(from.rotation * Quaternion.Euler(0f, 0f, -side * 12f), 0.4f).SetEase(Ease.InQuad));
        exit.Insert(at + 0.1f, group.DOFade(0f, 0.3f));
    }
}

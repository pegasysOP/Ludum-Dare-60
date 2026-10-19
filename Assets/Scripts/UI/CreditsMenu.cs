using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CreditsMenu : MonoBehaviour
{
    public Button menuButton;
    public CanvasGroup blackOverlay;
    // faded and slid up one after another as the black lifts
    public CanvasGroup[] entrance;

    public float blackFadeDuration = 0.6f;
    public float stagger = 0.1f;

    private void Awake()
    {
        menuButton.onClick.AddListener(OnMenuClicked);

        blackOverlay.alpha = 1f;
        blackOverlay.blocksRaycasts = true;
        foreach (CanvasGroup group in entrance)
            group.alpha = 0f;
    }

    private void Start()
    {
        Sequence intro = DOTween.Sequence().SetLink(gameObject);
        intro.Append(blackOverlay.DOFade(0f, blackFadeDuration).SetEase(Ease.InOutSine));
        intro.AppendCallback(() => blackOverlay.blocksRaycasts = false);

        for (int i = 0; i < entrance.Length; i++)
        {
            RectTransform rect = (RectTransform)entrance[i].transform;
            Vector2 target = rect.anchoredPosition;
            rect.anchoredPosition = target + Vector2.down * 24f;

            float at = blackFadeDuration * 0.4f + i * stagger;
            intro.Insert(at, entrance[i].DOFade(1f, 0.4f));
            intro.Insert(at, rect.DOAnchorPos(target, 0.5f).SetEase(Ease.OutCubic));
        }
    }

    private void OnMenuClicked()
    {
        SceneUtils.LoadMenuScene();
    }
}

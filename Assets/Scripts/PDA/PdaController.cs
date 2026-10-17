using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PdaController : MonoBehaviour
{
    public Button sideButton;
    public TextMeshProUGUI sideText;
    public RectTransform rect;

    private bool open = false;

    private void Awake()
    {
        sideButton.onClick.AddListener(ToggleOpen);
    }

    private void ToggleOpen()
    {
        rect.DOAnchorPosX(open ? -550 : 50, 0.5f).SetEase(Ease.OutExpo);
        sideText.text = open ? ">" : "<";
        open = !open;
    }
}

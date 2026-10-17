using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CameraApp : FullscreenApp
{
    public PdaController pda;
    public RectTransform viewfinder;
    public Image flash;
    public RawImage photo;
    public Button retakeButton;

    public float flashDuration = 0.5f;

    private bool aiming = false;
    private bool cursorWasVisible;
    private Texture2D photoTexture;

    private void Awake()
    {
        // these live in the app prefab but have to sit outside the pda mask
        Transform canvas = GetComponentInParent<Canvas>().rootCanvas.transform;
        viewfinder.SetParent(canvas, false);
        flash.rectTransform.SetParent(canvas, false);

        retakeButton.onClick.AddListener(StartAiming);
    }

    private void Update()
    {
        if (!aiming)
            return;

        // overlay canvas, so world position is screen pixels
        Vector2 halfSize = viewfinder.rect.size * viewfinder.lossyScale / 2f;
        Vector2 mousePos = Mouse.current.position.ReadValue();
        mousePos.x = Mathf.Clamp(mousePos.x, halfSize.x, Screen.width - halfSize.x);
        mousePos.y = Mathf.Clamp(mousePos.y, halfSize.y, Screen.height - halfSize.y);
        viewfinder.position = mousePos;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            StartCoroutine(TakePhoto());
    }

    protected override void OnOpened()
    {
        StartAiming();
    }

    protected override void OnClosed()
    {
        StopAiming();
    }

    private void StartAiming()
    {
        aiming = true;
        cursorWasVisible = Cursor.visible;
        Cursor.visible = false;

        pda.SetVisible(false);
        viewfinder.gameObject.SetActive(true);

        flash.DOKill();
        flash.color = new Color(1f, 1f, 1f, 0f);
        flash.gameObject.SetActive(true);
    }

    private void StopAiming()
    {
        if (!aiming)
            return;

        aiming = false;
        Cursor.visible = cursorWasVisible;

        viewfinder.gameObject.SetActive(false);
        flash.gameObject.SetActive(false);
        pda.SetVisible(true);
    }

    private IEnumerator TakePhoto()
    {
        aiming = false;
        viewfinder.gameObject.SetActive(false);

        yield return new WaitForEndOfFrame();

        Vector3[] corners = new Vector3[4];
        viewfinder.GetWorldCorners(corners);
        Rect area = new Rect(corners[0].x, corners[0].y, corners[2].x - corners[0].x, corners[2].y - corners[0].y);

        if (photoTexture != null)
            Destroy(photoTexture);

        photoTexture = new Texture2D((int)area.width, (int)area.height, TextureFormat.RGB24, false);
        photoTexture.ReadPixels(area, 0, 0);
        photoTexture.Apply();

        photo.texture = photoTexture;
        photo.color = Color.white;

        Cursor.visible = cursorWasVisible;

        flash.color = Color.white;
        flash.DOFade(0f, flashDuration).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            flash.gameObject.SetActive(false);
            pda.SetVisible(true);
        });
    }
}

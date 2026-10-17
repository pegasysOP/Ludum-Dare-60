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

    public int photoResolution = 180;

    // cheap early 2000s sensor look
    private static readonly Color PhotoTint = new Color(0.96f, 1.02f, 1.05f);
    private const float PhotoSaturation = 1.15f;
    private const float PhotoContrast = 1.12f;
    private const float PhotoBlackLift = 0.04f;
    private const int PhotoColourLevels = 32;

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

        int width = (int)area.width;
        int height = (int)area.height;

        Texture2D capture = new Texture2D(width, height, TextureFormat.RGB24, false);
        capture.ReadPixels(area, 0, 0);
        Color[] pixels = Downscale(capture.GetPixels(), width, height, photoResolution);
        ColourGrade(pixels);
        Destroy(capture);

        if (photoTexture != null)
            Destroy(photoTexture);

        photoTexture = new Texture2D(photoResolution, photoResolution, TextureFormat.RGB24, false);
        photoTexture.filterMode = FilterMode.Bilinear;
        photoTexture.wrapMode = TextureWrapMode.Clamp;
        photoTexture.SetPixels(pixels);
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

    // averages each block of source pixels into one output pixel
    private Color[] Downscale(Color[] source, int width, int height, int size)
    {
        Color[] result = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            int yMin = y * height / size;
            int yMax = Mathf.Max(yMin + 1, (y + 1) * height / size);

            for (int x = 0; x < size; x++)
            {
                int xMin = x * width / size;
                int xMax = Mathf.Max(xMin + 1, (x + 1) * width / size);

                Color sum = Color.clear;
                for (int sy = yMin; sy < yMax; sy++)
                    for (int sx = xMin; sx < xMax; sx++)
                        sum += source[sy * width + sx];

                result[y * size + x] = sum / ((xMax - xMin) * (yMax - yMin));
            }
        }

        return result;
    }

    private void ColourGrade(Color[] pixels)
    {
        for (int i = 0; i < pixels.Length; i++)
        {
            Color colour = pixels[i] * PhotoTint;

            float luma = colour.r * 0.299f + colour.g * 0.587f + colour.b * 0.114f;
            colour = Color.LerpUnclamped(new Color(luma, luma, luma), colour, PhotoSaturation);

            colour.r = GradeChannel(colour.r);
            colour.g = GradeChannel(colour.g);
            colour.b = GradeChannel(colour.b);

            pixels[i] = colour;
        }
    }

    // contrast that clips highlights, lifted blacks, then a little banding
    private float GradeChannel(float value)
    {
        value = Mathf.Clamp01((value - 0.5f) * PhotoContrast + 0.5f);
        value = PhotoBlackLift + value * (1f - PhotoBlackLift);
        return Mathf.Round(value * (PhotoColourLevels - 1)) / (PhotoColourLevels - 1);
    }
}

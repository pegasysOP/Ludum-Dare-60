using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PicturesApp : FullscreenApp
{
    public const int MaxPhotos = 20;

    public Button thumbnailTemplate;
    public RectTransform grid;
    public GameObject emptyText;
    public GameObject photoView;
    public RawImage fullPhoto;
    public Button deleteButton;

    // kept in memory only, so they clear when the game stops
    private List<Texture2D> photos = new List<Texture2D>();
    private Button viewing;
    private Vector2 fullPhotoPos;
    private Sequence deleteSequence;

    public bool IsFull => photos.Count >= MaxPhotos;

    private void Awake()
    {
        fullPhotoPos = fullPhoto.rectTransform.anchoredPosition;
        deleteButton.onClick.AddListener(DeletePhoto);
    }

    public void AddPhoto(Texture2D photo)
    {
        photos.Add(photo);

        Button thumbnail = Instantiate(thumbnailTemplate, grid);
        thumbnail.GetComponent<RawImage>().texture = photo;
        thumbnail.onClick.AddListener(() => ShowPhoto(thumbnail));
        thumbnail.gameObject.SetActive(true);
        thumbnail.transform.SetAsFirstSibling();

        emptyText.SetActive(false);
    }

    public override bool Back()
    {
        if (!photoView.activeSelf)
            return false;

        HidePhoto();
        return true;
    }

    protected override void OnClosed()
    {
        HidePhoto();
    }

    private void ShowPhoto(Button thumbnail)
    {
        // finish off a delete that's still flying away
        if (deleteSequence != null && deleteSequence.IsActive())
            deleteSequence.Complete();

        viewing = thumbnail;
        fullPhoto.texture = thumbnail.GetComponent<RawImage>().texture;
        fullPhoto.color = Color.white;
        deleteButton.interactable = true;
        photoView.SetActive(true);

        // grow out of the thumbnail
        RectTransform photoRect = fullPhoto.rectTransform;
        RectTransform thumbnailRect = (RectTransform)thumbnail.transform;
        photoRect.DOKill();
        photoRect.localRotation = Quaternion.identity;
        photoRect.position = thumbnailRect.position;
        photoRect.localScale = Vector3.one * (thumbnailRect.rect.width / photoRect.rect.width);
        photoRect.DOAnchorPos(fullPhotoPos, 0.3f).SetEase(Ease.OutExpo);
        photoRect.DOScale(1f, 0.3f).SetEase(Ease.OutExpo);
    }

    private void HidePhoto()
    {
        viewing = null;
        fullPhoto.texture = null;
        photoView.SetActive(false);
    }

    private void DeletePhoto()
    {
        Texture2D photo = (Texture2D)fullPhoto.texture;
        photos.Remove(photo);

        Destroy(viewing.gameObject);
        emptyText.SetActive(photos.Count == 0);
        deleteButton.interactable = false;

        // little hop, then fling it off the bottom, over the delete button
        RectTransform photoRect = fullPhoto.rectTransform;
        photoRect.DOKill();
        photoRect.SetAsLastSibling();
        deleteSequence = DOTween.Sequence()
            .Append(photoRect.DOAnchorPosY(fullPhotoPos.y + 30f, 0.12f).SetEase(Ease.OutQuad))
            .Append(photoRect.DOAnchorPosY(fullPhotoPos.y - 900f, 0.4f).SetEase(Ease.InQuad))
            .Join(photoRect.DORotate(new Vector3(0f, 0f, Random.Range(-20f, 20f)), 0.4f).SetEase(Ease.InQuad))
            .Join(fullPhoto.DOFade(0f, 0.4f).SetEase(Ease.InQuad))
            .OnComplete(() =>
            {
                // back may have already closed the view
                if (fullPhoto.texture == photo)
                    HidePhoto();

                Destroy(photo);
            });
    }

    private void OnDestroy()
    {
        foreach (Texture2D photo in photos)
            Destroy(photo);
    }
}

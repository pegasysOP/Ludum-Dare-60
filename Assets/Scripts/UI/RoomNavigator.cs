using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class RoomNavigator : MonoBehaviour
{
    // each child is a room, left to right, and the camera centres on whichever one you're in
    public Transform rooms;
    public int startRoom = 1;
    public Button leftButton;
    public Button rightButton;
    public CanvasGroup fade;
    public float fadeDuration = 0.5f;
    // the menu leaves on a sky wash, so the game picks up from it and clears
    public Color arrivalColour = new Color(0.42f, 0.74f, 1f);
    public float arrivalDuration = 0.35f;

    private Camera mainCamera;
    private InteractSfxLibrary sounds;
    private int currentRoom;
    private Sequence moveSequence;

    private void Awake()
    {
        mainCamera = Camera.main;
        sounds = InteractSfxLibrary.Load();

        leftButton.onClick.AddListener(() => Move(-1));
        rightButton.onClick.AddListener(() => Move(1));

        ShowRoom(startRoom);
        Arrive();
    }

    // reverse of the menu's exit: start washed out in sky blue, then clear, before the fade goes back to black for room changes
    private void Arrive()
    {
        Image fadeImage = fade.GetComponent<Image>();
        Color roomColour = fadeImage.color;
        fadeImage.color = arrivalColour;
        fade.alpha = 1f;
        fade.blocksRaycasts = true;

        moveSequence = DOTween.Sequence()
            .AppendInterval(0.1f)
            .Append(fade.DOFade(0f, arrivalDuration).SetEase(Ease.InOutSine))
            .OnComplete(() =>
            {
                fade.blocksRaycasts = false;
                fadeImage.color = roomColour;
            })
            .SetLink(gameObject);
    }

    private void Move(int direction)
    {
        int target = currentRoom + direction;
        if (moveSequence.IsActive() || target < 0 || target >= rooms.childCount)
            return;

        // three footsteps on carpet while the screen is black, and the room you're leaving goes quiet
        sounds.roomMove.Play();
        AmbiencePlayer.LeaveRoom();

        // black out, jump the camera, then fade back so the seams between rooms never show
        fade.blocksRaycasts = true;
        moveSequence = DOTween.Sequence()
            .Append(fade.DOFade(1f, fadeDuration))
            .AppendCallback(() => ShowRoom(target))
            .AppendInterval(0.2f)
            .Append(fade.DOFade(0f, fadeDuration))
            .OnComplete(() => fade.blocksRaycasts = false)
            .SetLink(gameObject);
    }

    private void ShowRoom(int index)
    {
        currentRoom = index;
        Transform room = rooms.GetChild(index);
        AmbiencePlayer.SetRoom(room.name);
        Vector3 roomPos = room.position;
        mainCamera.transform.position = new Vector3(roomPos.x, roomPos.y, mainCamera.transform.position.z);

        leftButton.gameObject.SetActive(index > 0);
        rightButton.gameObject.SetActive(index < rooms.childCount - 1);
    }
}

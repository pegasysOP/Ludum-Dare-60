using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class ChangeRoomInteractable : Interactable
{
    private Camera mainCamera;

    [SerializeField] float xOffset;
    [SerializeField] float yOffset;

    [SerializeField] float moveDuration;

    public void Start()
    {
        //TODO: This may not work if the camera app also has an additional camera
        mainCamera = Camera.main;

        if(Camera.allCamerasCount > 1)
        {
            Debug.LogWarning("There is more than one camera in the scene. ChangeRoomInteractable will use the first camera it finds.");
        }
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log(mainCamera.name);
        Vector3 targetPos = mainCamera.transform.position;
        targetPos.x += xOffset;
        targetPos.y += yOffset; 

        //TODO: Do we want to do fading or other transitions. 
        //TODO: Do we want to play a sound when we move? 

        StartCoroutine(MoveCamera(targetPos));
    }

    IEnumerator MoveCamera(Vector3 targetPos)
    {
        Vector3 startPos = mainCamera.transform.position;
        float elapsedTime = 0f;

        while(elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(elapsedTime / moveDuration);

            mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, t);

            yield return null;
        }

        mainCamera.transform.position = targetPos;
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        //TODO: Should we change room on up or down?
    }
}

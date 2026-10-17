using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public float yaw;
    public float pitch;
    public float mouseSensitivity;
    public float stickLookSpeed = 180f;
    public float maxLookAngle;

    private Vector3 cameraBaseLocalPos;
    private bool cameraBaseCached;
    private bool shaking;

    [Header("Target aim")]
    public Transform lookAtTarget;
    public float lookAtFollowSpeed = 12f;
    [Tooltip("World-space vertical offset added to lookAtTarget.position when aiming. Lets the camera target the head of a floor-anchored object.")]
    public float lookAtYOffset = 0f;

    private float sensitivitySetting = 1f;
    private InputAction lookAction;

    private void Awake()
    {
        sensitivitySetting = SettingsUtils.GetSensitivity();
    }

    private void Start()
    {
        lookAction = InputSystem.actions.FindAction("Look");
    }

    private void Update()
    {
        if (GameManager.Instance.LOCKED)
            return;
        
        Vector2 lookValue = lookAction.ReadValue<Vector2>();

        // stick is a rate, mouse is already per frame
        if (lookAction.activeControl?.device is Gamepad)
            lookValue *= stickLookSpeed * Time.deltaTime;
        else
            lookValue *= mouseSensitivity * 0.02f;

        yaw += lookValue.x * sensitivitySetting;
        pitch -= lookValue.y * sensitivitySetting;

        pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);

    }

    public void UpdateSensitivity(float value)
    {
        sensitivitySetting = value;
    }
}

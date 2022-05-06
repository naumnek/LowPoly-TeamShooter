using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.FPS.Gameplay;
using Unity.FPS.Game;
using Cinemachine;
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
using UnityEngine.InputSystem;
#endif

public class SpectatorMode : MonoBehaviour
{
    [Header("General")]
    public bool Spectator = false;

    [Header("Input System")]
    public PlayerInputHandler InputHandler;
    public PlayerInput playerInput;

    [Header("Cinemachine")]
    public CinemachineVirtualCamera spectatorVirtualCamera;
    public CinemachineVirtualCamera followPlayerVirtualCamera;
    public Transform PlayerCameraRoot;
    public int FlySpeed = 5;
    public float Sensitivity = 1f;
    [Tooltip("How far in degrees can you move the camera up")]
    public float TopClamp = 70.0f;
    [Tooltip("How far in degrees can you move the camera down")]
    public float BottomClamp = -30.0f;
    [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
    public float CameraAngleOverride = 0.0f;
    [Tooltip("For locking the camera position on all axis")]
    public bool LockCameraPosition = false;

    private Transform spectatorCamera;

    private const float _threshold = 0.01f;

    private bool mode = false;
    private bool IsCurrentDeviceMouse => playerInput.currentControlScheme == "KeyboardMouse";

    // cinemachine
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

    private void Awake()
    {
        EventManager.AddListener<PlayerSpawnEvent>(OnPlayerSpawnEvent);
    }

    private void OnDestroy()
    {
        EventManager.RemoveListener<PlayerSpawnEvent>(OnPlayerSpawnEvent);
    }
    public void OnPlayerSpawnEvent(PlayerSpawnEvent evt)
    {
    }

    void Start()
    {
        spectatorCamera = spectatorVirtualCamera.transform;
    }

    public void Activation()
    {
        mode = true;
    }

    private void LateUpdate()
    {
        CameraRotation();
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void CameraRotation()
    {
        // if there is an input and camera position is not fixed
        if (InputHandler.look.sqrMagnitude >= _threshold && !LockCameraPosition)
        {
            Sensitivity = InputHandler.LookSensitivity;

            //Don't multiply mouse input by Time.deltaTime;
            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

            _cinemachineTargetYaw += InputHandler.look.x * deltaTimeMultiplier * Sensitivity;
            _cinemachineTargetPitch += InputHandler.look.y * deltaTimeMultiplier * Sensitivity;
        }

        // clamp our rotations so our values are limited 360 degrees
        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        // Cinemachine will follow this target
        transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0.0f);
    }

    // Update is called once per frame
    void Update()
    {
        if (mode)
        {
            LockCameraPosition= InputHandler.LockCameraPosition;
            Spectator = InputHandler.SpectatorMode;
        }
        if (Spectator)
        {
            spectatorVirtualCamera.gameObject.SetActive(true);
            followPlayerVirtualCamera.gameObject.SetActive(false);
            Move();
        }
        else
        {
            spectatorVirtualCamera.gameObject.SetActive(false);
            followPlayerVirtualCamera.gameObject.SetActive(true);
            transform.position = PlayerCameraRoot.position;
            transform.rotation = PlayerCameraRoot.rotation;
        }
    }

    private void Move()
    {

        //move camera forward
        if (InputHandler.uparrow)
        {
            transform.position += transform.forward * FlySpeed * Time.deltaTime;
        }
        //move camera backwards
        if (InputHandler.downarrow)
        {
            transform.position -= transform.forward * FlySpeed * Time.deltaTime;

        }
        //move camera to the right
        if (InputHandler.rightarrow)
        {
            transform.position += transform.right * FlySpeed * Time.deltaTime;

        }
        if (InputHandler.leftarrow)
        {
            transform.position -= transform.right * FlySpeed * Time.deltaTime;
        }

        //move camera upwards
        if (InputHandler.upwards)
        {
            transform.position += spectatorCamera.up * FlySpeed * Time.deltaTime;
        }
        //move camera downwards
        if (InputHandler.downwards)
        {
            transform.position += spectatorCamera.up * -1 * FlySpeed * Time.deltaTime;
        }
	}
}

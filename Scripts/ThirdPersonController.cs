using UnityEngine;
using UnityEngine.Events;
using Unity.FPS.Game;
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
using UnityEngine.InputSystem;
#endif
using Unity.FPS.Gameplay;
using Cinemachine;
using Photon.Pun;

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
	[RequireComponent(typeof(PlayerInput))]
#endif
	public class ThirdPersonController : MonoBehaviour
	{

		[Header("Camera")]
		public Camera PlayerCamera;
		public LayerMask CastLayer;

		[Header("Parameters")]
		[Tooltip("How fast the character turns to face movement direction")]
		[Range(0.0f, 0.3f)]
		public float RotationSmoothTime = 0.12f;
		[Tooltip("Acceleration and deceleration")]
		public float SpeedChangeRate = 10.0f;
		public float Sensitivity = 1f;
		public float BodyRotationSensitivity = 5f;

		[Header("Cinemachine")]
		public CinemachineVirtualCamera PlayerFollowCamera;
		[Tooltip("How far in degrees can you move the camera up")]
		public float TopClamp = 30.0f;
		[Tooltip("How far in degrees can you move the camera down")]
		public float BottomClamp = -30.0f;
		[Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
		public float CameraAngleOverride = 0.0f;
		[Tooltip("For locking the camera position on all axis")]
		public bool LockCameraPosition = false;

		public Vector3 mouseWorldPosition { get; private set; } = Vector3.zero;

		// cinemachine
		private float m_CinemachineTargetYaw;
		private float m_CinemachineTargetPitch;
		public float ClampTargetPitch { get; private set; }

		private PlayerInput m_PlayerInput;
		private Animator m_Animator;
		private CharacterController m_Controller;
		private PlayerInputHandler m_PlayerInputsHandler;
		private PlayerWeaponsManager m_PlayerWeaponsManager;
		private PlayerCharacterController m_PlayerCharacterController;
		private PlayerController m_PlayerController;
		private GameObject m_MainCamera;
		private Transform m_PlayerBody;
		public Transform m_CameraTarget { get; private set; }

		private const float _threshold = 0.01f;

		private bool ServerPause = true;
		private bool MenuPause = false;

		private bool IsCurrentDeviceMouse => m_PlayerInput.currentControlScheme == "KeyboardMouse";

		private InputPlayerAssets _inputPlayerAssets;
		private SpectatorMode spectatorMode;

		private static ThirdPersonController instance;

		private void OnDestroy()
		{
			EventManager.RemoveListener<PlayerSpawnEvent>(OnPlayerSpawnEvent);
			EventManager.RemoveListener<GamePauseEvent>(OnGamePauseEvent);
			EventManager.RemoveListener<PlayerDeathEvent>(OnPlayerDeathEvent);
		}

		private void Awake()
		{
			instance = this;
			// get a reference to our main camera
			if (PlayerCamera == null)
			{
				PlayerCamera = Camera.main;
			}
			m_MainCamera = PlayerCamera.gameObject;

		}

		public static ThirdPersonController GetInstance() => instance;

		private void Start()
		{
			EventManager.AddListener<GamePauseEvent>(OnGamePauseEvent);
			EventManager.AddListener<PlayerSpawnEvent>(OnPlayerSpawnEvent);
			EventManager.AddListener<PlayerDeathEvent>(OnPlayerDeathEvent);


			PlayerController trigger = FindObjectOfType<PlayerController>();
			if (trigger != null && trigger.PhotonView.IsMine)
			{
				Activate(trigger);
			}
		}

		private void OnPlayerDeathEvent(PlayerDeathEvent evt)
		{
			ServerPause = evt.Die;
		}

		private void OnPlayerSpawnEvent(PlayerSpawnEvent evt)
		{
			Activate(evt.player);
		}
		private void OnGamePauseEvent(GamePauseEvent evt)
		{
			ServerPause = evt.ServerPause;
			MenuPause = evt.MenuPause;
		}

		public void Activate(PlayerController player)
		{
			m_PlayerInputsHandler = GetComponent<PlayerInputHandler>();
			m_PlayerWeaponsManager = GetComponent<PlayerWeaponsManager>();
			m_PlayerInput = GetComponent<PlayerInput>();

			m_PlayerBody = player.transform;
			m_CameraTarget = player.CameraTarget;
			PlayerFollowCamera.Follow = m_CameraTarget;
			//PlayerFollowCamera.LookAt = m_CameraTarget;

			m_Controller = m_PlayerBody.GetComponent<CharacterController>();
			m_Animator = player.Animator;
			m_PlayerController = m_PlayerBody.GetComponent<PlayerController>();

			ServerPause = false;
		}

		private void Update()
		{
			if (!ServerPause && m_PlayerController.controllable && PhotonNetwork.IsConnected)
			{
				//GroundedCheck();
				//JumpAndGravity();
				//if (!m_PlayerInputsHandler.shoot) Move();
			}
		}

		private void LateUpdate()
		{
			if (!ServerPause && !MenuPause && m_PlayerController.controllable && PhotonNetwork.IsConnected)
			{
				CameraRotation();
				OrientTowards(LookAtCamera());
			}
		}

		private Quaternion LookAtPlayer;

		public Vector3 LookAtCamera()
		{
			Vector2 screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
			Ray ray = PlayerCamera.ScreenPointToRay(screenCenterPoint);
			ray = PlayerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
			if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f, CastLayer))
			{
				mouseWorldPosition = raycastHit.point;
			}
			return mouseWorldPosition;
		}

		public void BodyRotation()
		{
			LookAtPlayer = new Quaternion(0, m_CameraTarget.rotation.y, 0, m_CameraTarget.rotation.w);
			LookAtPlayer.z = 0;
			//m_PlayerBody.rotation = Quaternion.Slerp(m_PlayerBody.rotation, new Quaternion(0, LookAtPlayer.y, 0, LookAtPlayer.w), Time.deltaTime * BodyRotationSensitivity);
			m_PlayerBody.transform.LookAt(m_PlayerWeaponsManager.mouseWorldPosition, Vector3.up);
		}
		public void OrientTowards(Vector3 lookPosition)
		{
			Vector3 lookDirection = Vector3.ProjectOnPlane(lookPosition - m_PlayerBody.position, Vector3.up).normalized;
			if (lookDirection.sqrMagnitude != 0f)
			{
				Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
				m_PlayerBody.rotation =
					Quaternion.Slerp(m_PlayerBody.rotation, targetRotation, Time.deltaTime * BodyRotationSensitivity);
			}
		}

		private void CameraRotation()
		{
			// if there is an input and camera position is not fixed
			if (m_PlayerInputsHandler.look.sqrMagnitude >= _threshold && !LockCameraPosition)
			{
				Sensitivity = m_PlayerInputsHandler.LookSensitivity;

				//Don't multiply mouse input by Time.deltaTime;
				float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

				m_CinemachineTargetYaw += m_PlayerInputsHandler.look.x * deltaTimeMultiplier * Sensitivity;
				m_CinemachineTargetPitch += m_PlayerInputsHandler.look.y * deltaTimeMultiplier * Sensitivity;
			}

			// clamp our rotations so our values are limited 360 degrees
			m_CinemachineTargetYaw = ClampAngle(m_CinemachineTargetYaw, float.MinValue, float.MaxValue);
			m_CinemachineTargetPitch = ClampAngle(m_CinemachineTargetPitch, BottomClamp, TopClamp);
			ClampTargetPitch = m_CinemachineTargetPitch;

			// Cinemachine will follow this target
			m_CameraTarget.rotation = Quaternion.Euler(m_CinemachineTargetPitch + CameraAngleOverride, m_CinemachineTargetYaw, 0.0f);
		}

		private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
		{
			if (lfAngle < -360f) lfAngle += 360f;
			if (lfAngle > 360f) lfAngle -= 360f;
			return Mathf.Clamp(lfAngle, lfMin, lfMax);
		}

		public void SetSensitivity(float newAimSensitivity)
		{
			Sensitivity = newAimSensitivity;
		}
	}
}
using Unity.FPS.Game;
using naumnek.FPS;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
using UnityEngine.InputSystem;
#endif

namespace Unity.FPS.Gameplay
{
    public class PlayerInputHandler : MonoBehaviour
    {
        [Header("References")]
        public GameFlowManager GameManager;

        public Vector2 movearrow { get; private set; }
        public float MoveAxisRaw { get; private set; }
        public Vector3 move { get; private set; }
        public Vector2 look { get; private set; }

        public int number;
        public bool reload;
        public bool shoot { get; private set; }
        public bool aim { get; private set; }
        public bool jump;
        public bool crouch;
        public bool sprint { get; private set; }

        //spectator camera 
        public bool uparrow { get; private set; }
        public bool downarrow { get; private set; }
        public bool rightarrow { get; private set; }
        public bool leftarrow { get; private set; }
        public bool upwards { get; private set; }
        public bool downwards { get; private set; }

        public bool aimode { get; private set; }
        public bool ToggleTexture { get; private set; }
        public bool SpectatorMode { get; private set; }
        public bool LockCameraPosition { get; private set; }
        public bool HideGameHUD { get; private set; }

        public bool SelectWeapon;
        public bool tab;
        public bool click;

        [Header("Movement Settings")]
        public bool analogMovement;

#if !UNITY_IOS || !UNITY_ANDROID
        [Header("Mouse Cursor Settings")]
        public bool cursorLocked = true;
        public bool cursorInputForLook = true;
#endif

        [Tooltip("Sensitivity multiplier for moving the camera around")]
        public float LookSensitivity = 1f;

        [Tooltip("Additional sensitivity multiplier for WebGL")]
        public float WebglLookSensitivityMultiplier = 0.25f;

        [Tooltip("Limit to consider an input when using a trigger on a controller")]
        public float TriggerAxisThreshold = 0.4f;

        [Tooltip("Used to flip the vertical input axis")]
        public bool InvertYAxis = false;

        [Tooltip("Used to flip the horizontal input axis")]
        public bool InvertXAxis = false;

        private InputPlayerAssets _InputPlayerAssets;
        private GameFlowManager m_GameFlowManager;
        private PlayerCharacterController m_PlayerCharacterController;

        private bool ServerPause = true;
        private bool MenuPause = true;

        private static PlayerInputHandler instance;

#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED

        public static PlayerInputHandler GetInstance() => instance;

        private void Awake()
        {
            instance = this;

            if (FileManager.GetSeed() == 12345678)
            {
                aimode = true;
                GameManager.SetActiveSpectatorInfo();
            }

            _InputPlayerAssets = new InputPlayerAssets();
            /*
            _InputPlayerAssets.Player.Sprint.performed += ctx =>
            {
                SprintInput(true);
            };
            _InputPlayerAssets.Player.Sprint.canceled += ctx =>
            {
                SprintInput(false);
            };

            _InputPlayerAssets.Player.Crouch.performed += ctx =>
            {
                SprintInput(true);
            };
            _InputPlayerAssets.Player.Crouch.canceled += ctx =>
            {
                SprintInput(false);
            };
            */
            _InputPlayerAssets.Player.ChangeWeapons.performed += ctx =>
            {
                if (HasPause) int.TryParse(ctx.control.name, out number);
            };

            _InputPlayerAssets.Player.AiMode.performed += ctx =>
            {
                if (HasPause) aimode = !aimode;
            };

            _InputPlayerAssets.Player.SpectatorMode.performed += ctx =>
            {
                if (HasPause) SpectatorMode = !SpectatorMode;
            };

            _InputPlayerAssets.Player.LockCameraPosition.performed += ctx =>
            {
                if (HasPause) LockCameraPosition = !LockCameraPosition;
            };

            _InputPlayerAssets.Player.ToggleTexture.performed += ctx =>
            {
                if (HasPause) ToggleTexture = true;
            };

            _InputPlayerAssets.Player.HideGameHUD.performed += ctx =>
            {
                if (HasPause)
                {
                    HideGameHUD = !HideGameHUD;
                    GameManager.SetActiveGameHUD(!HideGameHUD);
                }
            };

            EventManager.AddListener<GamePauseEvent>(OnGamePauseEvent);
            EventManager.AddListener<PlayerSpawnEvent>(OnPlayerSpawnEvent);
        }

        private void OnDestroy()
        {
            EventManager.RemoveListener<GamePauseEvent>(OnGamePauseEvent);
            EventManager.RemoveListener<PlayerSpawnEvent>(OnPlayerSpawnEvent);
        }

        private void OnGamePauseEvent(GamePauseEvent evt)
        {
            ServerPause = evt.ServerPause;
            MenuPause = evt.MenuPause;
        }
        private void OnPlayerSpawnEvent(PlayerSpawnEvent evt)
        {
            Activate(evt.player);
        }

        private void Activate(PlayerController player)
        {
            m_PlayerCharacterController = player.PlayerCharacterController;
            m_GameFlowManager = player.GameFlowManager;
            ServerPause = false;
        }

        private void OnEnable()
        {
            _InputPlayerAssets.Enable();
        }

        private void OnDisable()
        {
            _InputPlayerAssets.Disable();
        }

        public void OnUpwards(InputValue value)
        {
            UpwardsInput(value.isPressed);
        }

        public void OnDownwards(InputValue value)
        {
            DownwardsInput(value.isPressed);
        }

        public void OnUpArrow(InputValue value)
        {
            UpArrowInput(value.isPressed);
        }

        public void OnDownArrow(InputValue value)
        {
            DownArrowInput(value.isPressed);
        }

        public void OnRightArrow(InputValue value)
        {
            RightArrowInput(value.isPressed);
        }

        public void OnLeftArrow(InputValue value)
        {
            LeftArrowInput(value.isPressed);
        }
        public void OnSelectWeapon(InputValue value)
        {
            SelectWeaponInput(value.isPressed);
        }

        public void OnTab(InputValue value)
        {
            TabInput(value.isPressed);
        }


        public void OnMove(InputValue value)
        {
            MoveInput(value.Get<Vector2>());
        }
        public void OnMoveArrows(InputValue value)
        {
            MoveArrowsInput(value.Get<Vector2>());
        }

        public void OnLook(InputValue value)
        {
            if (cursorInputForLook)
            {
                LookInput(value.Get<Vector2>());
            }
        }

        public void OnShoot(InputValue value)
        {
            ShootInput(value.isPressed);
        }

        public void OnAim(InputValue value)
        {
            AimInput(value.isPressed);
        }

        public void OnReload(InputValue value)
        {
            ReloadInput(value.isPressed);
        }

        public void OnJump(InputValue value)
        {
            JumpInput(value.isPressed);
        }

        public void OnSprint(InputValue value)
        {
            SprintInput(value.isPressed);
        }

        public void OnCrouch(InputValue value)
        {
            CrouchInput(value.isPressed);
        }

#else
	// old input sys if we do decide to have it (most likely wont)...
#endif

        public void UpwardsInput(bool newState)
        {
            upwards = HasPause ? newState : false;
        }
        public void DownwardsInput(bool newState)
        {
            downwards = HasPause ? newState : false;
        }
        public void UpArrowInput(bool newState)
        {
            uparrow = HasPause ? newState : false;
        }
        public void DownArrowInput(bool newState)
        {
            downarrow = HasPause ? newState : false;
        }
        public void RightArrowInput(bool newState)
        {
            rightarrow = HasPause ? newState : false;
        }
        public void LeftArrowInput(bool newState)
        {
            leftarrow = HasPause ? newState : false;
        }
        public void SelectWeaponInput(bool newState)
        {
            SelectWeapon = ServerPause ? false : newState;
        }

        public void TabInput(bool newState)
        {
            tab = ServerPause ? false : newState;
        }

        public void MoveInput(Vector2 newDirection)
        {
            // constrain move input to a maximum magnitude of 1, otherwise diagonal movement might exceed the max move speed defined
            move = HasPause ? Vector3.ClampMagnitude(new Vector3(newDirection.x, 0f, newDirection.y), 1) : Vector3.zero;
        }

        public void MoveArrowsInput(Vector2 newDirection)
        {
            movearrow = HasPause ? newDirection : Vector2.zero;
        }

        public void LookInput(Vector2 newDirection)
        {
            look = HasPause ? newDirection : Vector2.zero;
        }

        public void ShootInput(bool newState)
        {
            shoot = HasPause ? newState : false;
            click = newState;
        }

        public void AimInput(bool newState)
        {
            aim = HasPause ? newState : false;
        }

        public void JumpInput(bool newState)
        {
            jump = HasPause ? newState : false;
        }
        public void CrouchInput(bool newState)
        {
            crouch = HasPause ? newState : false;
        }

        public void SprintInput(bool newState)
        {
            sprint = HasPause ? newState : false;
        }

        public void ReloadInput(bool newState)
        {
            reload = HasPause ? newState : false;
        }

        private bool HasPause { get { return !MenuPause && !ServerPause; } }

#if !UNITY_IOS || !UNITY_ANDROID
        
        private void OnApplicationFocus(bool hasFocus)
        {
            SetCursorState(cursorLocked);
        }

        private void SetCursorState(bool newState)
        {
            Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
        }
        
#endif
    }
}
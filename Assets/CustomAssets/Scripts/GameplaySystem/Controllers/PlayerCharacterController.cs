using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.Events;
using StarterAssets;
using Photon.Pun;

//Скрипт отвечает за общую настройку параметров аватара игрока. Параметры передвижения/Анимацию/Звуки.
//Также отвечает за принятия ввода клавиш отвечающих на передвижения. Также обрабатывает/отслеживает физику движения аватара.

namespace Unity.FPS.Gameplay
{
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerCharacterController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Reference to the main camera used for the player")]
        public Camera PlayerCamera;
        public ThirdPersonController ThirdPersonControllers;
        [Tooltip("Audio source for footsteps, jump, etc...")]
        private AudioSource AudioSource;


        [Header("Grounded")]
        [Tooltip("Force applied downward when in the air")]
        public float GravityDownForce = 20f;

        [Tooltip("Physic layers checked to consider the player grounded")]
        public LayerMask GroundCheckLayers = -1;

        [Tooltip("distance from the bottom of the character controller capsule to test for grounded")]
        public float GroundCheckDistance = 0.05f;

        [Header("Movement")]
        [Tooltip("Max movement speed when grounded (when not sprinting)")]
        public float MaxSpeedOnGround = 10f;

        [Tooltip(
            "Sharpness for the movement when grounded, a low value will make the player accelerate and decelerate slowly, a high value will do the opposite")]
        public float MovementSharpnessOnGround = 15;

        [Tooltip("Max movement speed when crouching")]
        [Range(0, 1)]
        public float MaxSpeedCrouchedRatio = 0.5f;

        [Tooltip("Max movement speed when not grounded")]
        public float MaxSpeedInAir = 10f;

        [Tooltip("Acceleration speed when in the air")]
        public float AccelerationSpeedInAir = 25f;

        [Tooltip("Multiplicator for the sprint speed (based on grounded speed)")]
        public float SprintSpeedModifier = 2f;

        [Tooltip("Height at which the player dies instantly when falling off the map")]
        public float KillHeight = -50f;

        [Header("Rotation")]
        [Tooltip("Rotation speed for moving the camera")]
        public float RotationSpeed = 200f;

        [Range(0.1f, 1f)]
        [Tooltip("Rotation speed multiplier when aiming")]
        public float AimingRotationMultiplier = 0.4f;

        [Header("Jump")]
        [Tooltip("Force applied upward when jumping")]
        public float JumpForce = 9f;

        [Header("Stance")]

        [Tooltip("Ratio (0-1) of the character height where the camera will be at")]
        public float CameraHeightRatio = 0.9f;

        [Tooltip("Height of character when standing")]
        public float CapsuleHeightStanding = 1.8f;

        [Tooltip("Height of character when crouching")]
        public float CapsuleHeightCrouching = 0.9f;

        [Tooltip("Speed of crouching transitions")]
        public float CrouchingSharpness = 10f;

        [Header("Audio")]
        [Tooltip("Amount of footstep sounds played when moving one meter")]
        public float FootstepSfxFrequency = 1f;

        [Tooltip("Amount of footstep sounds played when moving one meter while sprinting")]
        public float FootstepSfxFrequencyWhileSprinting = 1f;

        [Tooltip("Sound played for footsteps")]
        public AudioClip FootstepSfx;

        [Tooltip("Sound played when jumping")]
        public AudioClip JumpSfx;

        [Tooltip("Sound played when landing")]
        public AudioClip LandSfx;

        [Tooltip("Sound played when taking damage froma fall")]
        public AudioClip FallDamageSfx;

        [Header("Fall Damage")]
        [Tooltip("Whether the player will recieve damage when hitting the ground at high speed")]
        public bool RecievesFallDamage;

        [Tooltip("Minimun fall speed for recieving fall damage")]
        public float MinSpeedForFallDamage = 10f;

        [Tooltip("Fall speed for recieving th emaximum amount of fall damage")]
        public float MaxSpeedForFallDamage = 30f;

        [Tooltip("Damage recieved when falling at the mimimum speed")]
        public float FallDamageAtMinSpeed = 10f;

        [Tooltip("Damage recieved when falling at the maximum speed")]
        public float FallDamageAtMaxSpeed = 50f;

        public UnityAction<bool> OnStanceChanged;

        public PhotonView PlayerPhotonView { get; private set; }
        public Vector3 CharacterVelocity { get; set; }
        public bool IsGrounded { get; private set; }
        public bool HasJumpedThisFrame { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsCrouching { get; private set; }

        private bool MenuPause = false;
        private bool ServerPause = true;
        private Transform m_PlayerBody;
        public PlayerController PlayerController;

        public float RotationMultiplier
        {
            get
            {
                if (m_WeaponsManager.IsAiming)
                {
                    return AimingRotationMultiplier;
                }

                return 1f;
            }
        }

        public Actor m_Actor { get; private set; }
        private Animator m_Animator;
        Health m_Health;
        PlayerWeaponsManager m_PlayerWeaponsManager;
        PlayerInputHandler m_InputHandler;
        CharacterController m_Controller;
        PlayerWeaponsManager m_WeaponsManager;
        Vector3 m_GroundNormal;
        Vector3 m_CharacterVelocity;
        Vector3 m_LatestImpactSpeed;
        float m_LastTimeJumped = 0f;
        float m_CameraVerticalAngle = 0f;
        float m_FootstepDistanceCounter;
        float m_TargetCharacterHeight;

        private float _animationBlend;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDCrouch;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private int _animIDWeapon;

        const float k_JumpGroundingPreventionTime = 0.2f;
        const float k_GroundCheckDistanceInAir = 0.07f;

        private static PlayerCharacterController instance;

        //Custom naumnek
        private void OnDestroy()
        {
            EventManager.RemoveListener<PlayerSpawnEvent>(OnPlayerSpawnEvent);
            EventManager.RemoveListener<PlayerDeathEvent>(OnPlayerDeathEvent);
            EventManager.RemoveListener<GamePauseEvent>(OnGamePauseEvent);
        }

        private void Awake()
        {
            instance = this;

            EventManager.AddListener<PlayerSpawnEvent>(OnPlayerSpawnEvent);
            EventManager.AddListener<PlayerDeathEvent>(OnPlayerDeathEvent);
            EventManager.AddListener<GamePauseEvent>(OnGamePauseEvent);

            PlayerController trigger = FindObjectOfType<PlayerController>();
            if (trigger != null && trigger.PhotonView.IsMine)
            {
                PlayerController = trigger;
                Activate();
            }
        }

        public static PlayerCharacterController GetInstance() => instance;

        private void OnGamePauseEvent(GamePauseEvent evt)
        {
            ServerPause = evt.ServerPause;
            MenuPause = evt.MenuPause;
        }
        private void OnPlayerSpawnEvent(PlayerSpawnEvent evt)
        {
            PlayerController = evt.player;
            Activate();
        }

        void OnPlayerDeathEvent(PlayerDeathEvent evt)
        {
            IsDead = true;
        }

        public void Activate()
        {
            m_PlayerBody = PlayerController.transform;

            if (PlayerCamera == null) PlayerCamera = Camera.main;


            // fetch components on the same gameObject
            ThirdPersonControllers = PlayerController.ThirdPersonController;
            m_PlayerWeaponsManager = PlayerController.PlayerWeaponsManager;
            m_Animator = PlayerController.Animator;
            AudioSource = PlayerController.AudioSource;
            m_Controller = PlayerController.CharacterController;
            m_InputHandler = PlayerController.PlayerInputHandler;
            m_WeaponsManager = PlayerController.PlayerWeaponsManager;
            m_Health = PlayerController.Health;
            m_Actor = PlayerController.Actor;

            m_Controller.enableOverlapRecovery = true;

            m_PlayerWeaponsManager.OnSwitchedToWeapon += OnWeaponSwitched;

            AssignAnimationIDs();

            // force the crouch state to false when starting
            SetCrouchingState(false, true);
            UpdateCharacterHeight(true);

            ServerPause = false;
        }

        private void OnWeaponSwitched(WeaponController newWeapon)
        {
            if (newWeapon != null)
            {
                m_Animator.SetFloat(_animIDWeapon, newWeapon.IndexWeaponType());
            }
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDCrouch = Animator.StringToHash("Crouch");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            _animIDWeapon = Animator.StringToHash("IndexWeapon");
        }

        void Update()
        {
            if (!ServerPause && PlayerController.controllable)
            {
                HasJumpedThisFrame = false;

                bool wasGrounded = IsGrounded;
                GroundCheck();

                if (!IsGrounded && wasGrounded)
                {
                    m_Animator.SetBool(_animIDFreeFall, true);
                }

                // landing
                if (IsGrounded && !wasGrounded)
                {
                    // Fall damage
                    float fallSpeed = -Mathf.Min(CharacterVelocity.y, m_LatestImpactSpeed.y);
                    float fallSpeedRatio = (fallSpeed - MinSpeedForFallDamage) /
                                           (MaxSpeedForFallDamage - MinSpeedForFallDamage);
                    if (RecievesFallDamage && fallSpeedRatio > 0f)
                    {
                        float dmgFromFall = Mathf.Lerp(FallDamageAtMinSpeed, FallDamageAtMaxSpeed, fallSpeedRatio);
                        m_Health.TakeDamage(dmgFromFall, null, out bool IsDead);

                        // fall damage SFX
                        AudioSource.PlayOneShot(FallDamageSfx);
                    }
                    else
                    {
                        // landing
                        m_Animator.SetBool(_animIDJump, false);
                        m_Animator.SetBool(_animIDFreeFall, false);

                        // land SFX
                        AudioSource.PlayOneShot(LandSfx);

                        // permission to jump
                        m_InputHandler.jump = false;
                    }
                }

                // crouching
                if (m_InputHandler.crouch)
                {
                    m_InputHandler.crouch = false;
                    SetCrouchingState(!IsCrouching, false);
                }

                UpdateCharacterHeight(false);

                HandleCharacterMovement();
            }
        }

        void GroundCheck()
        {
            // Make sure that the ground check distance while already in air is very small, to prevent suddenly snapping to ground
            float chosenGroundCheckDistance =
                IsGrounded ? (m_Controller.skinWidth + GroundCheckDistance) : k_GroundCheckDistanceInAir;

            // reset values before the ground check
            IsGrounded = false;
            m_GroundNormal = Vector3.up;

            // only try to detect ground if it's been a short amount of time since last jump; otherwise we may snap to the ground instantly after we try jumping
            if (Time.time >= m_LastTimeJumped + k_JumpGroundingPreventionTime)
            {
                // if we're grounded, collect info about the ground normal with a downward capsule cast representing our character capsule
                if (Physics.CapsuleCast(GetCapsuleBottomHemisphere(), GetCapsuleTopHemisphere(m_Controller.height),
                    m_Controller.radius, Vector3.down, out RaycastHit hit, chosenGroundCheckDistance, GroundCheckLayers,
                    QueryTriggerInteraction.Ignore))
                {
                    // storing the upward direction for the surface found
                    m_GroundNormal = hit.normal;

                    // Only consider this a valid ground hit if the ground normal goes in the same direction as the character up
                    // and if the slope angle is lower than the character controller's limit
                    if (Vector3.Dot(hit.normal, m_PlayerBody.up) > 0f &&
                        IsNormalUnderSlopeLimit(m_GroundNormal))
                    {
                        IsGrounded = true;
                        // handle snapping to the ground
                        if (hit.distance > m_Controller.skinWidth)
                        {
                            m_Controller.Move(Vector3.down * hit.distance);
                        }
                    }
                }
            }
        }

        void HandleCharacterMovement()
        {
            /*
            // horizontal character rotation
            {
                // rotate the transform with the input speed around its local Y axis
                m_PlayerBody.Rotate(
                    new Vector3(0f, (m_InputHandler.look.x * RotationSpeed * RotationMultiplier),
                        0f), Space.Self);
            }
            */

            /*
            // vertical camera rotation
            {
                // add vertical inputs to the camera's vertical angle
                m_CameraVerticalAngle += m_InputHandler.look.y * RotationSpeed * RotationMultiplier;

                // limit the camera's vertical angle to min/max
                m_CameraVerticalAngle = Mathf.Clamp(m_CameraVerticalAngle, -89f, 89f);

                // apply the vertical angle as a local rotation to the camera transform along its right axis (makes it pivot up and down)
                PlayerCamera.transform.localEulerAngles = new Vector3(m_CameraVerticalAngle, 0, 0);
            }
            */

            // character movement handling
            bool isSprinting = m_InputHandler.sprint;
            {
                if (isSprinting)
                {
                    isSprinting = SetCrouchingState(false, false);
                }

                float speedModifier = isSprinting ? SprintSpeedModifier : 1f;

                Vector3 worldspaceMoveInput = IsCrouching ?
                    Vector3.zero : worldspaceMoveInput = m_PlayerBody.TransformVector(m_InputHandler.move);

                m_Animator.SetBool(_animIDGrounded, IsGrounded);
                // handle grounded movement
                if (IsGrounded)
                {

                    // calculate the desired velocity from inputs, max speed, and current slope
                    Vector3 targetVelocity = worldspaceMoveInput * MaxSpeedOnGround * speedModifier;
                    // reduce speed if crouching by crouch speed ratio
                    if (IsCrouching)
                        targetVelocity *= MaxSpeedCrouchedRatio;
                    targetVelocity = GetDirectionReorientedOnSlope(targetVelocity.normalized, m_GroundNormal) *
                                     targetVelocity.magnitude;

                    // smoothly interpolate between our current velocity and the target velocity based on acceleration speed
                    CharacterVelocity = Vector3.Lerp(CharacterVelocity, targetVelocity,
                        MovementSharpnessOnGround * Time.deltaTime);

                    // set target speed based on move speed, sprint speed and if sprint is pressed
                    float inputMagnitude = m_InputHandler.analogMovement ? m_InputHandler.move.magnitude : 1f;

                    m_Animator.SetFloat(_animIDSpeed, targetVelocity.magnitude);
                    m_Animator.SetFloat(_animIDMotionSpeed, inputMagnitude);

                    // jumping
                    if (IsGrounded && m_InputHandler.jump)
                    {
                        // force the crouch state to false
                        if (SetCrouchingState(false, false))
                        {
                            // start by canceling out the vertical component of our velocity
                            CharacterVelocity = new Vector3(CharacterVelocity.x, 0f, CharacterVelocity.z);

                            // then, add the jumpSpeed value upwards
                            CharacterVelocity += Vector3.up * JumpForce;

                            m_Animator.SetBool(_animIDJump, true);
                            // play sound
                            AudioSource.PlayOneShot(JumpSfx);

                            // remember last time we jumped because we need to prevent snapping to ground for a short time
                            m_LastTimeJumped = Time.time;
                            HasJumpedThisFrame = true;

                            // Force grounding to false
                            IsGrounded = false;
                            m_GroundNormal = Vector3.up;
                        }
                    }

                    // footsteps sound
                    float chosenFootstepSfxFrequency =
                        (isSprinting ? FootstepSfxFrequencyWhileSprinting : FootstepSfxFrequency);
                    if (m_FootstepDistanceCounter >= 1f / chosenFootstepSfxFrequency)
                    {
                        m_FootstepDistanceCounter = 0f;

                        AudioSource.PlayOneShot(FootstepSfx);
                    }

                    // keep track of distance traveled for footsteps sound
                    m_FootstepDistanceCounter += CharacterVelocity.magnitude * Time.deltaTime;
                }
                else // handle air movement
                {
                    // add air acceleration
                    CharacterVelocity += worldspaceMoveInput * AccelerationSpeedInAir * Time.deltaTime;

                    // limit air speed to a maximum, but only horizontally
                    float verticalVelocity = CharacterVelocity.y;
                    Vector3 horizontalVelocity = Vector3.ProjectOnPlane(CharacterVelocity, Vector3.up);
                    horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, MaxSpeedInAir * speedModifier);
                    CharacterVelocity = horizontalVelocity + (Vector3.up * verticalVelocity);

                    // apply the gravity to the velocity
                    CharacterVelocity += Vector3.down * GravityDownForce * Time.deltaTime;

                }
            }

            // apply the final calculated velocity value as a character movement
            Vector3 capsuleBottomBeforeMove = GetCapsuleBottomHemisphere();
            Vector3 capsuleTopBeforeMove = GetCapsuleTopHemisphere(m_Controller.height);
            m_Controller.Move(CharacterVelocity * Time.deltaTime);

            // detect obstructions to adjust velocity accordingly
            m_LatestImpactSpeed = Vector3.zero;
            if (Physics.CapsuleCast(capsuleBottomBeforeMove, capsuleTopBeforeMove, m_Controller.radius,
                CharacterVelocity.normalized, out RaycastHit hit, CharacterVelocity.magnitude * Time.deltaTime, -1,
                QueryTriggerInteraction.Ignore))
            {
                // We remember the last impact speed because the fall damage logic might need it
                m_LatestImpactSpeed = CharacterVelocity;

                CharacterVelocity = Vector3.ProjectOnPlane(CharacterVelocity, hit.normal);
            }
        }

        // Returns true if the slope angle represented by the given normal is under the slope angle limit of the character controller
        bool IsNormalUnderSlopeLimit(Vector3 normal)
        {
            return Vector3.Angle(m_PlayerBody.up, normal) <= m_Controller.slopeLimit;
        }

        // Gets the center point of the bottom hemisphere of the character controller capsule    
        Vector3 GetCapsuleBottomHemisphere()
        {
            return m_PlayerBody.position + (m_PlayerBody.up * m_Controller.radius);
        }

        // Gets the center point of the top hemisphere of the character controller capsule    
        Vector3 GetCapsuleTopHemisphere(float atHeight)
        {
            return m_PlayerBody.position + (m_PlayerBody.up * (atHeight - m_Controller.radius));
        }

        // Gets a reoriented direction that is tangent to a given slope
        public Vector3 GetDirectionReorientedOnSlope(Vector3 direction, Vector3 slopeNormal)
        {
            Vector3 directionRight = Vector3.Cross(direction, m_PlayerBody.up);
            return Vector3.Cross(slopeNormal, directionRight).normalized;
        }

        void UpdateCharacterHeight(bool force)
        {
            // Update height instantly
            if (force)
            {
                m_Controller.height = m_TargetCharacterHeight;
                m_Controller.center = Vector3.up * m_Controller.height * 0.5f;
                //PlayerCamera.transform.localPosition = Vector3.up * m_TargetCharacterHeight * CameraHeightRatio;
                m_Actor.AimPoint.transform.localPosition = m_Controller.center;
            }
            // Update smooth height
            else if (m_Controller.height != m_TargetCharacterHeight)
            {
                // resize the capsule and adjust camera position
                m_Controller.height = Mathf.Lerp(m_Controller.height, m_TargetCharacterHeight,
                    CrouchingSharpness * Time.deltaTime);
                m_Controller.center = Vector3.up * m_Controller.height * 0.5f;
                // PlayerCamera.transform.localPosition = Vector3.Lerp(PlayerCamera.transform.localPosition,Vector3.up * m_TargetCharacterHeight * CameraHeightRatio, CrouchingSharpness * Time.deltaTime);
                m_Actor.AimPoint.transform.localPosition = m_Controller.center;
            }
        }

        // returns false if there was an obstruction
        bool SetCrouchingState(bool crouched, bool ignoreObstructions)
        {
            // set appropriate heights
            if (crouched)
            {
                m_TargetCharacterHeight = CapsuleHeightCrouching;
            }
            else
            {
                // Detect obstructions
                if (!ignoreObstructions)
                {
                    Collider[] standingOverlaps = Physics.OverlapCapsule(
                        GetCapsuleBottomHemisphere(),
                        GetCapsuleTopHemisphere(CapsuleHeightStanding),
                        m_Controller.radius,
                        -1,
                        QueryTriggerInteraction.Ignore);
                    foreach (Collider c in standingOverlaps)
                    {
                        if (c != m_Controller)
                        {
                            return false;
                        }
                    }
                }

                m_TargetCharacterHeight = CapsuleHeightStanding;
            }

            if (OnStanceChanged != null)
            {
                OnStanceChanged.Invoke(crouched);
            }

            m_Animator.SetBool(_animIDCrouch, crouched);
            IsCrouching = crouched;
            return true;
        }
    }
}
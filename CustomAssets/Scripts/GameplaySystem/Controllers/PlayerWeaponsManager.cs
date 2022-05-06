using System.Collections.Generic;
using System.Linq;
using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.Events;
using Cinemachine;
using StarterAssets;
using UnityEngine.InputSystem;
using Unity.FPS.AI;
using Photon.Pun;
using Photon.Realtime;

namespace Unity.FPS.Gameplay
{
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerWeaponsManager : MonoBehaviour
    {
        public enum WeaponSwitchState
        {
            Up,
            Down,
            PutDownPrevious,
            PutUpNew,
        }
        [Header("References")]
        [Tooltip("Secondary camera used to avoid seeing weapon go throw geometries")]
        public Camera WeaponCamera;
        [Tooltip("Layer to set FPS weapon gameObjects to")]
        public LayerMask CastShootLayer;
        public CinemachineVirtualCamera AimVirtualCamera;

        [Header("Aim Options")]
        public float normalAimSensitivity = 1f;
        public float aimSensitivity = 0.5f;
        public float normalMoveSensitivity = 3f;
        public float moveSensitivity = 1f;

        [Tooltip("The speed at which the enemy rotates")]
        public float OrientationSpeed = 5f;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 15.0f;
        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -2.5f;

        [Header("Weapon Bob")]
        [Tooltip("Frequency at which the weapon will move around in the screen when the player is in movement")]
        public float BobFrequency = 10f;

        [Tooltip("How fast the weapon bob is applied, the bigger value the fastest")]
        public float BobSharpness = 10f;

        [Tooltip("Distance the weapon bobs when not aiming")]
        public float DefaultBobAmount = 0.05f;

        [Tooltip("Distance the weapon bobs when aiming")]
        public float AimingBobAmount = 0.02f;

        [Header("Weapon Recoil")]
        [Tooltip("This will affect how fast the recoil moves the weapon, the bigger the value, the fastest")]
        public float RecoilSharpness = 50f;

        [Tooltip("Maximum distance the recoil can affect the weapon")]
        public float MaxRecoilDistance = 0.5f;

        [Tooltip("How fast the weapon goes back to it's original position after the recoil is finished")]
        public float RecoilRestitutionSharpness = 10f;

        [Header("Misc")]
        [Tooltip("Speed at which the aiming animatoin is played")]
        public float AimingAnimationSpeed = 10f;

        [Tooltip("Field of view when not aiming")]
        public float DefaultFov = 60f;

        [Tooltip("Portion of the regular FOV to apply to the weapon camera")]
        public float WeaponFovMultiplier = 1f;

        [Tooltip("Delay before switching weapon a second time, to avoid recieving multiple inputs from mouse wheel")]
        public float WeaponSwitchDelay = 1f;



        private ThirdPersonController m_ThirdPersonController;
        private Animator m_Animator;

        public bool IsAiming { get; private set; }
        public int ActiveWeaponIndex { get; private set; }

        public UnityAction<WeaponController> OnSwitchedToWeapon;
        public UnityAction<WeaponController, int> OnAddedWeapon;
        public UnityAction<WeaponController, int> OnRemovedWeapon;
        public bool hasFired { get; private set; } = false;

        public WeaponController[] WeaponSlots { get; private set; } = new WeaponController[9]; // 9 available weapon slots
        PlayerInputHandler m_InputHandler;
        PlayerCharacterController m_PlayerCharacterController;
        float m_WeaponBobFactor;
        Vector3 m_LastCharacterPosition;
        Vector3 m_WeaponMainLocalPosition;
        Vector3 m_WeaponBobLocalPosition;
        Vector3 m_WeaponRecoilLocalPosition;
        Vector3 m_AccumulatedRecoil;
        float m_TimeStartedWeaponSwitch;
        WeaponSwitchState m_WeaponSwitchState;
        int m_WeaponSwitchNewWeaponIndex;
        WeaponController m_activeWeapon;
        private Actor m_Actor;
        private WeaponController[] WeaponsList;

        public Vector3 mouseWorldPosition { get; private set; } = Vector3.zero;
        private Vector3 weaponMuzzlePosition = Vector3.zero;

        private CharacterController m_CharacterController;
        private Transform m_PlayerBody;
        private Transform WeaponParentSocket;
        private Transform DefaultWeaponPosition;
        private Transform AimingWeaponPosition;
        private Transform DownWeaponPosition;
        private bool ServerPause = true;
        private bool MenuPause = false;
        public PlayerController m_PlayerController;
        public PhotonView PhotonViewPlayer;
        private static PlayerWeaponsManager instance;

        private void OnDestroy()
        {
            EventManager.RemoveListener<PlayerSpawnEvent>(OnPlayerSpawnEvent);
            EventManager.RemoveListener<GamePauseEvent>(OnGamePauseEvent);
            EventManager.RemoveListener<PlayerDeathEvent>(OnPlayerDeathEvent);
        }

        private void Awake()
        {
            instance = this;
        }

        public static PlayerWeaponsManager GetInstance() => instance;

        private void Start()
        {
            EventManager.AddListener<PlayerSpawnEvent>(OnPlayerSpawnEvent);
            EventManager.AddListener<GamePauseEvent>(OnGamePauseEvent);
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

        private void OnGamePauseEvent(GamePauseEvent evt)
        {
            ServerPause = evt.ServerPause;
            MenuPause = evt.MenuPause;
        }

        void Update()
        {
            if (!ServerPause)
            {
                // shoot handling
                m_activeWeapon = GetActiveWeapon();

                //detectionModule.PlayerHandleTargetDetection(m_Actor, m_SelfColliders);
                HasShoot();
                HasAiming();
            }
        }

        public void OnPlayerSpawnEvent(PlayerSpawnEvent evt)
        {
            m_PlayerController = evt.player;
            Activate(m_PlayerController);
        }

        public bool IsPointingEnemy()
        {
            if (m_activeWeapon)
            {
                if (Physics.Raycast(WeaponCamera.transform.position, WeaponCamera.transform.forward, out RaycastHit hit,
                    1000, -1, QueryTriggerInteraction.Ignore))
                {
                    return hit.collider.GetComponentInParent<Health>() != null;
                }
            }
            return false;
        }

        public bool IsHitEnemy { get; private set; } = false;
        public void OnHitEnemy(bool hit)
        {
            IsHitEnemy = hit;
        }

        public void Activate(PlayerController player)
        {
            PhotonViewPlayer = player.PhotonView;
            WeaponsList = player.SettingsManager.RequredWeaponsList;

            m_ThirdPersonController = player.ThirdPersonController;
            m_InputHandler = player.PlayerInputHandler;
            m_PlayerCharacterController = player.PlayerCharacterController;
            m_CharacterController = GetComponent<CharacterController>();

            m_Actor = player.Actor;

            m_PlayerBody = player.transform;

            WeaponParentSocket = player.WeaponParentSocket;
            DefaultWeaponPosition = player.DefaultWeaponPosition;
            AimingWeaponPosition = player.AimingWeaponPosition;
            DownWeaponPosition = player.DownWeaponPosition;

            SetFov(DefaultFov);
            AimVirtualCamera.Follow = player.CameraTarget;
            OrientationSpeed = m_ThirdPersonController.BodyRotationSensitivity;

            m_Animator = player.Animator;

            ActiveWeaponIndex = -1;
            m_WeaponSwitchState = WeaponSwitchState.Down;

            OnSwitchedToWeapon += OnWeaponSwitched;

            // Add starting weapons
            WeaponController[] Weapons = player.StartingWeapons;
            for (int i = 0; i < Weapons.Length; i++)
            {
                AddWeapon(Weapons[i].WeaponName);
            }

            SwitchWeapon(true);

            ServerPause = false;
        }

        public void OrientTowards(Vector3 lookPosition)
        {
            Vector3 lookDirection = Vector3.ProjectOnPlane(lookPosition - m_PlayerBody.position, Vector3.up).normalized;
            if (lookDirection.sqrMagnitude != 0f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                m_PlayerBody.rotation =
                    Quaternion.Slerp(m_PlayerBody.rotation, targetRotation, Time.deltaTime * OrientationSpeed);
            }
        }

        private void HasAiming()
        {
            // weapon switch handling
            if (!IsAiming &&
                (m_activeWeapon == null || !m_activeWeapon.IsCharging) &&
                (m_WeaponSwitchState == WeaponSwitchState.Up || m_WeaponSwitchState == WeaponSwitchState.Down))
            {
                int switchWeaponInput = 0;
                if (switchWeaponInput != 0)
                {
                    bool switchUp = switchWeaponInput > 0;
                    SwitchWeapon(switchUp);
                }
                else
                {
                    switchWeaponInput = m_InputHandler.number;
                    if (switchWeaponInput != 0)
                    {
                        if (GetWeaponAtSlotIndex(switchWeaponInput - 1) != null)
                            SwitchToWeaponIndex(switchWeaponInput - 1);
                    }
                }
            }
        }

        private void HasShoot()
        {
            WeaponController activeWeapon = GetActiveWeapon();
            if (activeWeapon != null && m_WeaponSwitchState == WeaponSwitchState.Up)
            {
                // handle aiming down sights
                if (!activeWeapon.DisableAiming) IsAiming = m_InputHandler.aim;
                else IsAiming = false;
                if (IsAiming)
                {
                    AimVirtualCamera.gameObject.SetActive(true);
                    m_ThirdPersonController.SetSensitivity(aimSensitivity);

                    /*
                    Vector3 worldAimTarget = mouseWorldPosition;
                    worldAimTarget.y = _playerBody.position.y;
                    Vector3 aimDirection = (worldAimTarget - _playerBody.position).normalized;

                    _playerBody.forward = Vector3.Lerp(_playerBody.forward, aimDirection, Time.deltaTime * 20f);
                    */
                }
                else
                {
                    AimVirtualCamera.gameObject.SetActive(false);
                    m_ThirdPersonController.SetSensitivity(normalAimSensitivity);
                }

                if (m_InputHandler.reload && !activeWeapon.IsReloading && !activeWeapon.AutomaticReload && activeWeapon.CurrentAmmoRatio < 1.0f)
                {
                    IsAiming = false;
                    activeWeapon.StartReload();
                    return;
                }

                // Handle logic 
                /*
                if (m_InputHandler.shoot)
                {
                    m_Animator.SetLayerWeight(1, Mathf.Lerp(m_Animator.GetLayerWeight(1), 1f, Time.deltaTime * 13f));
                    m_Animator.SetFloat("IndexWeapon", GetActiveWeapon().IndexWeaponType());

                }
                else m_Animator.SetLayerWeight(1, Mathf.Lerp(m_Animator.GetLayerWeight(1), 0f, Time.deltaTime * 13f));
                */

                if (AllowShootClamp() && m_InputHandler.shoot)
                {
                    hasFired = activeWeapon.HandleShootInputs(LookAtCamera());

                    // Handle accumulating recoil
                    if (hasFired)
                    {
                        m_AccumulatedRecoil += Vector3.back * activeWeapon.RecoilForce;
                        m_AccumulatedRecoil = Vector3.ClampMagnitude(m_AccumulatedRecoil, MaxRecoilDistance);
                    }
                }
                else
                {
                    activeWeapon.FirstShoot = false;
                }
            }
        }

        private bool AllowShootClamp()
        {
            float angleCamera = m_ThirdPersonController.ClampTargetPitch;
            return angleCamera <= TopClamp && angleCamera >= BottomClamp;
        }

        public Vector3 LookAtCamera()
        {
            Vector2 screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Ray ray = WeaponCamera.ScreenPointToRay(screenCenterPoint);
            ray = WeaponCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f, CastShootLayer))
            {
                mouseWorldPosition = raycastHit.point;
            }
            return mouseWorldPosition;
        }

        private Vector3 LookAtWeapon(WeaponController activeWeapon)
        {         
            if (Physics.Raycast(new Ray(activeWeapon.WeaponGunMuzzle.position, transform.forward), out RaycastHit raycastHit, 999f, ~CastShootLayer))
            {
                weaponMuzzlePosition = raycastHit.point;
            }
            return weaponMuzzlePosition;
        }


        // Update various animated features in LateUpdate because it needs to override the animated arm position
        void LateUpdate()
        {
            if (!ServerPause)
            {
                //UpdateWeaponAiming();
                //UpdateWeaponBob();
                UpdateWeaponRecoil();
                UpdateWeaponSwitching();

                // Set final weapon socket position based on all the combined animation influences
                // WeaponParentSocket.localPosition = m_WeaponMainLocalPosition + m_WeaponBobLocalPosition + m_WeaponRecoilLocalPosition;
            }
        }

        // Sets the FOV of the main camera and the weapon camera simultaneously
        public void SetFov(float fov)
        {
            //m_PlayerCharacterController.PlayerCamera.fieldOfView = fov;
            //WeaponCamera.fieldOfView = fov * WeaponFovMultiplier;
        }

        // Iterate on all weapon slots to find the next valid weapon to switch to
        public void SwitchWeapon(bool ascendingOrder)
        {
            int newWeaponIndex = -1;
            int closestSlotDistance = WeaponSlots.Length;
            for (int i = 0; i < WeaponSlots.Length; i++)
            {
                // If the weapon at this slot is valid, calculate its "distance" from the active slot index (either in ascending or descending order)
                // and select it if it's the closest distance yet
                if (i != ActiveWeaponIndex && GetWeaponAtSlotIndex(i) != null)
                {
                    int distanceToActiveIndex = GetDistanceBetweenWeaponSlots(ActiveWeaponIndex, i, ascendingOrder);

                    if (distanceToActiveIndex < closestSlotDistance)
                    {
                        closestSlotDistance = distanceToActiveIndex;
                        newWeaponIndex = i;
                    }
                }
            }

            // Handle switching to the new weapon index
            SwitchToWeaponIndex(newWeaponIndex);
        }

        // Switches to the given weapon index in weapon slots if the new index is a valid weapon that is different from our current one
        public void SwitchToWeaponIndex(int newWeaponIndex, bool force = false)
        {
            if (force || (newWeaponIndex != ActiveWeaponIndex && newWeaponIndex >= 0))
            {
                // Store data related to weapon switching animation
                m_WeaponSwitchNewWeaponIndex = newWeaponIndex;
                m_TimeStartedWeaponSwitch = Time.time;

                // Handle case of switching to a valid weapon for the first time (simply put it up without putting anything down first)
                if (GetActiveWeapon() == null)
                {
                    m_WeaponMainLocalPosition = DownWeaponPosition.localPosition;
                    m_WeaponSwitchState = WeaponSwitchState.PutUpNew;
                    ActiveWeaponIndex = m_WeaponSwitchNewWeaponIndex;

                    WeaponController newWeapon = GetWeaponAtSlotIndex(m_WeaponSwitchNewWeaponIndex);
                    if (OnSwitchedToWeapon != null)
                    {
                        OnSwitchedToWeapon.Invoke(newWeapon);
                    }
                }
                // otherwise, remember we are putting down our current weapon for switching to the next one
                else
                {
                    m_WeaponSwitchState = WeaponSwitchState.PutDownPrevious;
                }
            }
        }

        public WeaponController HasWeapon(WeaponController weaponPrefab)
        {
            if (weaponPrefab == null) return null;
            // Checks if we already have a weapon coming from the specified prefab
            for (var index = 0; index < WeaponSlots.Length; index++)
            {
                var w = WeaponSlots[index];
                if (w != null && w.WeaponName == weaponPrefab.WeaponName)
                {
                    return w;
                }
            }

            return null;
        }

        // Updates the weapon recoil animation
        void UpdateWeaponRecoil()
        {
            // if the accumulated recoil is further away from the current position, make the current position move towards the recoil target
            if (m_WeaponRecoilLocalPosition.z >= m_AccumulatedRecoil.z * 0.99f)
            {
                m_WeaponRecoilLocalPosition = Vector3.Lerp(m_WeaponRecoilLocalPosition, m_AccumulatedRecoil,
                    RecoilSharpness * Time.deltaTime);
            }
            // otherwise, move recoil position to make it recover towards its resting pose
            else
            {
                m_WeaponRecoilLocalPosition = Vector3.Lerp(m_WeaponRecoilLocalPosition, Vector3.zero,
                    RecoilRestitutionSharpness * Time.deltaTime);
                m_AccumulatedRecoil = m_WeaponRecoilLocalPosition;
            }
        }

        // Updates the animated transition of switching weapons
        void UpdateWeaponSwitching()
        {
            // Calculate the time ratio (0 to 1) since weapon switch was triggered
            float switchingTimeFactor = 0f;
            if (WeaponSwitchDelay == 0f)
            {
                switchingTimeFactor = 1f;
            }
            else
            {
                switchingTimeFactor = Mathf.Clamp01((Time.time - m_TimeStartedWeaponSwitch) / WeaponSwitchDelay);
            }

            // Handle transiting to new switch state
            if (switchingTimeFactor >= 1f)
            {
                if (m_WeaponSwitchState == WeaponSwitchState.PutDownPrevious)
                {
                    // Deactivate old weapon
                    WeaponController oldWeapon = GetWeaponAtSlotIndex(ActiveWeaponIndex);
                    if (oldWeapon != null)
                    {
                        oldWeapon.ShowWeapon(false);
                    }

                    ActiveWeaponIndex = m_WeaponSwitchNewWeaponIndex;
                    switchingTimeFactor = 0f;

                    // Activate new weapon
                    WeaponController newWeapon = GetWeaponAtSlotIndex(ActiveWeaponIndex);
                    if (OnSwitchedToWeapon != null)
                    {
                        OnSwitchedToWeapon.Invoke(newWeapon);
                    }

                    if (newWeapon)
                    {
                        m_TimeStartedWeaponSwitch = Time.time;
                        m_WeaponSwitchState = WeaponSwitchState.PutUpNew;
                    }
                    else
                    {
                        // if new weapon is null, don't follow through with putting weapon back up
                        m_WeaponSwitchState = WeaponSwitchState.Down;
                    }
                }
                else if (m_WeaponSwitchState == WeaponSwitchState.PutUpNew)
                {
                    m_WeaponSwitchState = WeaponSwitchState.Up;
                }
            }

            // Handle moving the weapon socket position for the animated weapon switching
            if (m_WeaponSwitchState == WeaponSwitchState.PutDownPrevious)
            {
                m_WeaponMainLocalPosition = Vector3.Lerp(DefaultWeaponPosition.localPosition,
                    DownWeaponPosition.localPosition, switchingTimeFactor);
            }
            else if (m_WeaponSwitchState == WeaponSwitchState.PutUpNew)
            {
                m_WeaponMainLocalPosition = Vector3.Lerp(DownWeaponPosition.localPosition,
                    DefaultWeaponPosition.localPosition, switchingTimeFactor);
            }
        }

        private int weaponSlot = 0;

        public bool AddWeapon(string weaponName)
        {
            for (int i = 0; i < WeaponsList.Length; i++)
            {
                // if we already hold this weapon type (a weapon coming from the same source prefab), don't add the weapon
                if (WeaponsList[i].name == weaponName && HasWeapon(WeaponsList[i]) != null) return false;
            }

            // search our weapon slots for the first free one, assign the weapon to it, and return true if we found one. Return false otherwise
            for (int i = 0; i < WeaponSlots.Length; i++)
            {
                // only add the weapon if the slot is free
                if (WeaponSlots[i] == null)
                {
                    weaponSlot = i;
                    m_PlayerController.PhotonView.RPC("AddPlayerWeapon", RpcTarget.AllViaServer, weaponName);
                    return true;
                }
            }

            // Handle auto-switching to weapon if no weapons currently
            if (GetActiveWeapon() == null)
            {
                SwitchWeapon(true);
            }

            return false;
        }

        // Adds a weapon to our inventory
        public bool SetWeapon(GameObject weaponObject)
        {
            // spawn the weapon prefab as child of the weapon socket
            WeaponController weaponInstance = weaponObject.GetComponent<WeaponController>();

            if (!m_PlayerController.controllable)
            {
                List<Renderer> weaponRenderer = weaponInstance.WeaponRenderer;
                for (int ii = 0; ii < weaponRenderer.Count; ii++)
                {
                    weaponRenderer[ii].enabled = false;
                }
            }

            weaponInstance.SourcePrefab = weaponInstance.gameObject;

            weaponInstance.transform.SetParent(WeaponParentSocket);
            weaponInstance.transform.localPosition = Vector3.zero;
            weaponInstance.transform.localRotation = Quaternion.identity;
            weaponInstance.transform.localScale = new Vector3(1, 1, 1);

            // Set owner to this gameObject so the weapon can alter projectile/damage logic accordingly
            weaponInstance.Owner = m_PlayerBody;

            weaponInstance.SetOptions(weaponSlot, m_PlayerController);

            weaponInstance.ShowWeapon(false);

            WeaponSlots[weaponSlot] = weaponInstance;

            if (OnAddedWeapon != null)
            {
                OnAddedWeapon.Invoke(weaponInstance, weaponSlot);
            }

            SwitchToWeaponIndex(weaponSlot);

            return true;

        }

        public bool SetWeaponRenderers(bool state)
        {
            for (int i = 0; i < WeaponSlots.Length; i++)
            {
                if (WeaponSlots[i] == null) continue;
                for (int ii = 0; ii < WeaponSlots[i].WeaponRenderer.Count; ii++)
                {
                     WeaponSlots[i].WeaponRenderer[ii].enabled = state;
                }
            }
            return true;
        }

        public bool RemoveWeapon(WeaponController weaponInstance)
        {
            
            // Look through our slots for that weapon
            for (int i = 0; i < WeaponSlots.Length; i++)
            {
                // when weapon found, remove it
                if (WeaponSlots[i].WeaponName == weaponInstance.WeaponName)
                {
                    WeaponController RemovedWeapon = WeaponSlots[i];
                    WeaponSlots[i] = null;

                    if (OnRemovedWeapon != null)
                    {
                        OnRemovedWeapon.Invoke(RemovedWeapon, i);
                    }

                    Destroy(RemovedWeapon.gameObject);

                    // Handle case of removing active weapon (switch to next weapon)
                    if (i == ActiveWeaponIndex)
                    {
                        SwitchWeapon(true);
                    }

                    return true;
                }
            }

            return false;
        }

        public WeaponController GetActiveWeapon()
        {
            return GetWeaponAtSlotIndex(ActiveWeaponIndex);
        }

        public WeaponController GetWeaponAtSlotIndex(int index)
        {
            // find the active weapon in our weapon slots based on our active weapon index
            if (index >= 0 &&
                index < WeaponSlots.Length)
            {
                return WeaponSlots[index];
            }

            // if we didn't find a valid active weapon in our weapon slots, return null
            return null;
        }

        // Calculates the "distance" between two weapon slot indexes
        // For example: if we had 5 weapon slots, the distance between slots #2 and #4 would be 2 in ascending order, and 3 in descending order
        int GetDistanceBetweenWeaponSlots(int fromSlotIndex, int toSlotIndex, bool ascendingOrder)
        {
            int distanceBetweenSlots = 0;

            if (ascendingOrder)
            {
                distanceBetweenSlots = toSlotIndex - fromSlotIndex;
            }
            else
            {
                distanceBetweenSlots = -1 * (toSlotIndex - fromSlotIndex);
            }

            if (distanceBetweenSlots < 0)
            {
                distanceBetweenSlots = WeaponSlots.Length + distanceBetweenSlots;
            }

            return distanceBetweenSlots;
        }

        void OnWeaponSwitched(WeaponController newWeapon)
        {
            if (newWeapon != null)
            {
                newWeapon.ShowWeapon(true);
            }
        }
    }
}
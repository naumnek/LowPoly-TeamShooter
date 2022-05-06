using System.Collections.Generic;
using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using LowLevelGenerator.Scripts;
using naumnek.FPS;
using naumnek.Settings;
using Photon.Pun;
using System.Collections;
using Photon.Pun.Demo.Asteroids;
using System.Linq;

namespace Unity.FPS.AI
{
    [RequireComponent(typeof(Health), typeof(Actor), typeof(NavMeshAgent))]
    public class EnemyController : MonoBehaviour
    {
        public enum WeaponSwitchState
        {
            Up,
            Down,
            PutDownPrevious,
            PutUpNew,
        }

        [Header("Info")]
        [Tooltip("Strenght the enemy")]
        public string EnemyName = "Enemy_Robot_R0";
        public LayerMask EnemyLayer;
        public SkinnedMeshRenderer CharacterSkinned;
        public Renderer BotRenderer;
        public GameObject HealthBarPivot;
        public int Rank = 0;
        public bool Boss = false;
        public Transform WeaponParentSocket;
        public Transform DefaultWeaponPosition;
        public Transform AimingWeaponPosition;
        public Transform DownWeaponPosition;

        [Header("Parameters")]
        [Tooltip("The Y height at which the enemy will be automatically killed (if it falls off of the level)")]
        public float SelfDestructYHeight = -20f;

        [Tooltip("The distance at which the enemy considers that it has reached its current path destination point")]
        public float PathReachingRadius = 2f;

        [Tooltip("The speed at which the enemy rotates")]
        public float OrientationSpeed = 10f;

        [Tooltip("Delay after death where the GameObject is destroyed (to allow for animation)")]
        public float DeathDuration = 0f;


        [Header("Weapons Parameters")]
        [Tooltip("Allow weapon swapping for this enemy")]
        public bool SwapToNextWeapon = false;

        [Tooltip("Time delay between a weapon swap and the next attack")]
        public float DelayAfterWeaponSwap = 0f;

        [Header("Eye color")]
        [Tooltip("Material for the eye color")]
        public Material EyeColorMaterial;

        [Tooltip("The default color of the bot's eye")]
        [ColorUsageAttribute(true, true)]
        public Color DefaultEyeColor;

        [Tooltip("The attack color of the bot's eye")]
        [ColorUsageAttribute(true, true)]
        public Color AttackEyeColor;

        [Header("Flash on hit")]
        [Tooltip("The material used for the body of the hoverbot")]
        public Material BodyMaterial;

        [Tooltip("The gradient representing the color of the flash on hit")]
        [GradientUsageAttribute(true)]
        public Gradient OnHitBodyGradient;

        [Tooltip("The duration of the flash on hit")]
        public float FlashOnHitDuration = 0.5f;

        [Header("Sounds")]
        [Tooltip("Sound played when recieving damages")]
        public AudioClip DamageTick;

        //[Header("VFX")] [Tooltip("The VFX prefab spawned when the enemy dies")]
        //public GameObject DeathVfx;

        [Tooltip("The point at which the death VFX is spawned")]
        public Transform DeathVfxSpawnPoint;

        [Header("Loot")]
        [Tooltip("The object this enemy can drop when dying")]
        public GameObject LootPrefab;

        [Tooltip("The chance the object has to drop")]
        [Range(0, 1)]
        public float DropRate = 1f;

        [Header("Debug Display")]
        [Tooltip("Color of the sphere gizmo representing the path reaching range")]
        public Color PathReachingRangeColor = Color.yellow;

        [Tooltip("Color of the sphere gizmo representing the attack range")]
        public Color AttackRangeColor = Color.red;

        [Tooltip("Color of the sphere gizmo representing the detection range")]
        public Color DetectionRangeColor = Color.blue;

        [Header("References")]
        public SpawnSection ParentSpawnSection;

        public UnityAction onAttack;
        public UnityAction onDetectedTarget;
        public UnityAction onLostTarget;
        public UnityAction onDamaged;

        List<RendererIndexData> m_BodyRenderers = new List<RendererIndexData>();
        MaterialPropertyBlock m_BodyFlashMaterialPropertyBlock;
        float m_LastTimeDamaged = float.NegativeInfinity;

        RendererIndexData m_EyeRendererData;
        MaterialPropertyBlock m_EyeColorMaterialPropertyBlock;

        public PatrolPath CurrentPatrolPath { get; set; }
        public PatrolPath[] PatrolPaths { get; set; }
        public Actor KnownDetectedTarget => DetectionModule.KnownDetectedTarget;
        public bool IsTargetInAttackRange => DetectionModule.IsTargetInAttackRange;
        public bool IsSeeingTarget => DetectionModule.IsSeeingTarget;
        public bool HadKnownTarget => DetectionModule.HadKnownTarget;
        public NavMeshAgent NavMeshAgent { get; private set; }
        public DetectionModule DetectionModule { get; private set; }



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

        [Tooltip("Layer to set FPS weapon gameObjects to")]
        public LayerMask IgnoreLayer;

        [Header("Aim Options")]
        [SerializeField] private float normalAimSensitivity = 1f;
        [SerializeField] private float aimSensitivity = 0.5f;
        [SerializeField] private float normalMoveSensitivity = 3f;
        [SerializeField] private float moveSensitivity = 1f;

        public bool IsAiming { get; private set; }
        public bool IsPointingAtEnemy { get; private set; }
        public int ActiveWeaponIndex { get; private set; }

        public UnityAction<WeaponController> OnSwitchedToWeapon;
        public UnityAction<WeaponController, int> OnAddedWeapon;
        public UnityAction<WeaponController, int> OnRemovedWeapon;
        public bool hasFired { get; private set; } = false;

        WeaponController[] m_WeaponSlots = new WeaponController[9]; // 9 available weapon slots
        Vector3 m_LastCharacterPosition;
        Vector3 m_WeaponMainLocalPosition;
        Vector3 m_WeaponRecoilLocalPosition;
        Vector3 m_AccumulatedRecoil;
        float m_TimeStartedWeaponSwitch;
        WeaponSwitchState m_WeaponSwitchState;
        int m_WeaponSwitchNewWeaponIndex;

        public bool controllable { get; private set; } = true;

        private Transform Spawnpoint;
        public Vector3 mouseWorldPosition { get; private set; } = Vector3.zero;

        private WeaponController[] WeaponsList;
        private Vector3 lookPositionTarget;
        private int PatrolPathIndex;
        private bool LookForward;

        [System.Serializable]
        public struct RendererIndexData
        {
            public Renderer Renderer;
            public int MaterialIndex;

            public RendererIndexData(Renderer renderer, int index)
            {
                Renderer = renderer;
                MaterialIndex = index;
            }
        }

        int m_PathDestinationNodeIndex;
        ActorsManager m_ActorsManager;
        GameFlowManager m_GameFlowManager;
        Health m_Health;
        public SettingsManager SettingsManager { get; private set; }
        public EnemyMobile EnemyMobile { get; private set; }
        public Actor Actor { get; private set; }
        Collider[] m_SelfColliders;
        bool m_WasDamagedThisFrame;
        float m_LastTimeWeaponSwapped = Mathf.NegativeInfinity;

        private Animator m_Animator;
        private WeaponController[] m_Weapons;
        public WeaponController[] WeaponSlots { get; private set; } = new WeaponController[9]; // 9 available weapon slots
        public WeaponController CurrentWeapon { get; private set; }
        NavigationModule m_NavigationModule;

        public PhotonView EnemyPhotonView { get; private set; }
        private Transform m_LookWeaponMuzzle;

        private int score = 0;
        public bool ServerPause { get; private set; } = true;

        private void OnDestroy()
        {
            EventManager.RemoveListener<PlayerSpawnEvent>(OnPlayerSpawnEvent);
            EventManager.RemoveListener<EndGameEvent>(OnEndGameEvent);
        }
        public void SetSpawnSection(SpawnSection spawnSection)
        {
            ParentSpawnSection = spawnSection;
        }

        public void SetDrop(EnemyDrop drop)
        {
            LootPrefab = drop.LootPrefab;
            DropRate = drop.DropRate;
        }

        private void Start()
        {
            EventManager.AddListener<PlayerSpawnEvent>(OnPlayerSpawnEvent);
            EventManager.AddListener<EndGameEvent>(OnEndGameEvent);

            Activate();
        }

        private void OnPlayerSpawnEvent(PlayerSpawnEvent evt)
        {

        }

        private void Activate()
        {
            m_Health = GetComponent<Health>();
            Actor = GetComponent<Actor>();
            NavMeshAgent = GetComponent<NavMeshAgent>();
            m_SelfColliders = GetComponentsInChildren<Collider>();
            EnemyMobile = GetComponent<EnemyMobile>();
            EnemyPhotonView = GetComponent<PhotonView>();
            m_Animator = GetComponent<Animator>();

            m_ActorsManager = ActorsManager.GetInstance();
            m_GameFlowManager = GameFlowManager.GetInstance();
            SettingsManager = SettingsManager.GetInstance();

            // Subscribe to damage & death actions
            m_Health.OnDie += OnDie;
            m_Health.OnDamaged += OnDamaged;

            // Find and initialize all weapons
            /*
            FindAndInitializeAllWeapons();
            var weapon = GetCurrentWeapon();
            weapon.ShowWeapon(true);
            */

            DetectionModule[] detectionModules = GetComponentsInChildren<DetectionModule>();
            // Initialize detection module
            DetectionModule = detectionModules[0];
            DetectionModule.onDetectedTarget += OnDetectedTarget;
            DetectionModule.onLostTarget += OnLostTarget;
            onAttack += DetectionModule.OnAttack;

            var navigationModules = GetComponentsInChildren<NavigationModule>();
            // Override navmesh agent data
            if (navigationModules.Length > 0)
            {
                m_NavigationModule = navigationModules[0];
                NavMeshAgent.speed = m_NavigationModule.MoveSpeed;
                NavMeshAgent.angularSpeed = m_NavigationModule.AngularSpeed;
                NavMeshAgent.acceleration = m_NavigationModule.Acceleration;
            }

            m_BodyFlashMaterialPropertyBlock = new MaterialPropertyBlock();

            ActiveWeaponIndex = -1;
            m_WeaponSwitchState = WeaponSwitchState.Down;
            OnSwitchedToWeapon += OnWeaponSwitched;

            // Add starting weapons
            WeaponsList = SettingsManager.RequredWeaponsList;

            EnemyPhotonView.RPC("SetBotSettings", RpcTarget.AllViaServer);
        }

        private void OnEndGameEvent(EndGameEvent evt)
        {
            ServerPause = true;
            controllable = false;

            DetectionModule.ResetDetectionTarget();
            EnemyMobile.ResetAiState();
            ResetPathDestination();
            NavMeshAgent.enabled = false;
        }

        private bool waitCheck = false;
        private IEnumerator WaitForCheck(float time)
        {
            waitCheck = true;
            yield return new WaitForSeconds(time);
            Debug.Log("live");
            waitCheck = false;
        }

        void Update()
        {
            if (!ServerPause && controllable)
            {
                //if (!waitCheck) StartCoroutine(WaitForCheck(5f));
                EnsureIsWithinLevelBounds();
                OrientTowards();

                DetectionModule.HandleTargetDetection(Actor, m_SelfColliders);

                Color currentColor = OnHitBodyGradient.Evaluate((Time.time - m_LastTimeDamaged) / FlashOnHitDuration);
                m_BodyFlashMaterialPropertyBlock.SetColor("_EmissionColor", currentColor);
                foreach (var data in m_BodyRenderers)
                {
                    data.Renderer.SetPropertyBlock(m_BodyFlashMaterialPropertyBlock, data.MaterialIndex);
                }

                m_WasDamagedThisFrame = false;
            }
        }


        public void SetSpawn(Transform spawnpoint)
        {
            Spawnpoint = spawnpoint;
        }

        private void OnDie()
        {
            EnemyPhotonView.RPC("DieBot", RpcTarget.AllViaServer);
        }

        #region COROUTINES

        private IEnumerator WaitForRespawn()
        {
            yield return new WaitForSeconds(AsteroidsGame.PLAYER_RESPAWN_TIME);

            RespawnBot();
        }

        private IEnumerator WaitForDisableInvulnerable()
        {
            yield return new WaitForSeconds(AsteroidsGame.PLAYER_INVULNERABLE_TIME);
            m_Health.DisableInvulnerable();
        }

        #endregion

        [PunRPC]
        private void SetBotSettings()
        {
            SettingsManager settings = SettingsManager.GetInstance();
            CharacterSkinned.sharedMesh = settings.Customization.GetRandomModel();
            CharacterSkinned.material = settings.Customization.GetRandomMaterial();
            WeaponsList = settings.RequredWeaponsList;

            int indexWeapon = Random.Range(0, WeaponsList.Length);
            AddWeapon(WeaponsList[indexWeapon].WeaponName);
            SwitchWeapon(true);

            ServerPause = false;
        }

        [PunRPC]
        public void DieBot()
        {
            controllable = false;

            for (int i = 0; i < m_SelfColliders.Length; i++)
            {
                m_SelfColliders[i].enabled = false;
            }
            BotRenderer.enabled = false;
            HealthBarPivot.SetActive(false);

            m_Weapons = WeaponSlots.Where(ws => ws != null).ToArray();

            for (int i = 0; i < m_Weapons.Length; i++)
            {
                for (int ii = 0; ii < m_Weapons[i].WeaponRenderer.Count; ii++)
                {
                    m_Weapons[i].WeaponRenderer[ii].enabled = false;
                }
            }

            NavMeshAgent.enabled = false;
            //DetectionModule.ResetDetectionTarget();
            EnemyMobile.ResetAiState();
            ResetPathDestination();

            PatrolPathIndex++;
            if (PatrolPathIndex > PatrolPaths.Length - 1) PatrolPathIndex = 0;
            CurrentPatrolPath = PatrolPaths[Random.Range(0, PatrolPaths.Length)];

            transform.position = Spawnpoint.position;
            transform.rotation = Spawnpoint.rotation;
            StartCoroutine(WaitForRespawn());
        }

        public void RespawnBot()
        {
            for (int i = 0; i < m_SelfColliders.Length; i++)
            {
                m_SelfColliders[i].enabled = true;
            }
            BotRenderer.enabled = true;
            HealthBarPivot.SetActive(true);

            for (int i = 0; i < m_Weapons.Length; i++)
            {
                for (int ii = 0; ii < m_Weapons[i].WeaponRenderer.Count; ii++)
                {
                    m_Weapons[i].WeaponRenderer[ii].enabled = true;
                }
            }

            m_Health.Heal(m_Health.MaxHealth);
            m_Health.IsDead = false;

            NavMeshAgent.enabled = true;
            controllable = true;

            StartCoroutine(WaitForDisableInvulnerable());
        }

        void EnsureIsWithinLevelBounds()
        {
            // at every frame, this tests for conditions to kill the enemy
            if (transform.position.y < SelfDestructYHeight)
            {
                Destroy(gameObject);
                return;
            }
        }

        void OnLostTarget()
        {
            onLostTarget.Invoke();

            // Set the eye attack color and property block if the eye renderer is set
            if (m_EyeRendererData.Renderer != null)
            {
                m_EyeColorMaterialPropertyBlock.SetColor("_EmissionColor", DefaultEyeColor);
                m_EyeRendererData.Renderer.SetPropertyBlock(m_EyeColorMaterialPropertyBlock,
                    m_EyeRendererData.MaterialIndex);
            }
        }

        void OnDetectedTarget()
        {
            onDetectedTarget.Invoke();

            // Set the eye default color and property block if the eye renderer is set
            if (m_EyeRendererData.Renderer != null)
            {
                m_EyeColorMaterialPropertyBlock.SetColor("_EmissionColor", AttackEyeColor);
                m_EyeRendererData.Renderer.SetPropertyBlock(m_EyeColorMaterialPropertyBlock,
                    m_EyeRendererData.MaterialIndex);
            }
        }
        bool IsPathValid()
        {
            return CurrentPatrolPath && CurrentPatrolPath.PathNodes.Count > 0;
        }

        public void ResetPathDestination()
        {
            m_PathDestinationNodeIndex = 0;
        }

        public void SetPathDestinationToClosestNode()
        {
            if (IsPathValid())
            {
                int closestPathNodeIndex = 0;
                for (int i = 0; i < CurrentPatrolPath.PathNodes.Count; i++)
                {
                    float distanceToPathNode = CurrentPatrolPath.GetDistanceToNode(transform.position, i);
                    if (distanceToPathNode < CurrentPatrolPath.GetDistanceToNode(transform.position, closestPathNodeIndex))
                    {
                        closestPathNodeIndex = i;
                    }
                }

                m_PathDestinationNodeIndex = closestPathNodeIndex;
            }
            else
            {
                m_PathDestinationNodeIndex = 0;
            }
        }

        public Vector3 GetDestinationOnPath()
        {
            if (IsPathValid())
            {
                return CurrentPatrolPath.GetPositionOfPathNode(m_PathDestinationNodeIndex);
            }
            else
            {
                return transform.position;
            }
        }

        public void OrientTowards()
        {
            Quaternion targetRotation = Quaternion.identity;

            if (lookPositionTarget == Vector3.zero)
            {
                //Looking forward while walking with navMeshAgent
                if (NavMeshAgent.velocity.sqrMagnitude > Mathf.Epsilon)
                {
                    targetRotation = Quaternion.LookRotation(NavMeshAgent.velocity.normalized);
                }
            }
            else
            {
                m_LookWeaponMuzzle.LookAt(lookPositionTarget);
                Vector3 lookDirection = Vector3.ProjectOnPlane(lookPositionTarget - transform.position, Vector3.up).normalized;
                if (lookDirection.sqrMagnitude != 0f)
                {
                    targetRotation = Quaternion.LookRotation(lookDirection);
                }
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * OrientationSpeed);
        }

        private float m_AnimationBlend;

        public void SetNavDestination(Vector3 destination)
        {
            if (NavMeshAgent)
            {
                // update animator if using character
                m_AnimationBlend = Mathf.Lerp(m_AnimationBlend, NavMeshAgent.velocity.magnitude, Time.deltaTime * NavMeshAgent.acceleration);

                m_Animator.SetFloat("IndexWeapon", CurrentWeapon.IndexWeaponType());
                m_Animator.SetLayerWeight(1, Mathf.Lerp(m_Animator.GetLayerWeight(1), 0f, Time.deltaTime * 13f));
                m_Animator.SetFloat("Speed", m_AnimationBlend);
                m_Animator.SetFloat("MotionSpeed", 1f);

                NavMeshAgent.SetDestination(destination);
            }
        }

        public void SetLookPositionTarget(Vector3 lookPosition)
        {
            lookPositionTarget = lookPosition;
        }

        public void UpdatePathDestination(bool inverseOrder = false)
        {
            if (IsPathValid())
            {
                // Check if reached the path destination
                if ((transform.position - GetDestinationOnPath()).magnitude <= PathReachingRadius)
                {
                    // increment path destination index
                    m_PathDestinationNodeIndex =
                        inverseOrder ? (m_PathDestinationNodeIndex - 1) : (m_PathDestinationNodeIndex + 1);
                    if (m_PathDestinationNodeIndex < 0)
                    {
                        m_PathDestinationNodeIndex += CurrentPatrolPath.PathNodes.Count;
                    }

                    if (m_PathDestinationNodeIndex >= CurrentPatrolPath.PathNodes.Count)
                    {
                        m_PathDestinationNodeIndex -= CurrentPatrolPath.PathNodes.Count;
                    }
                }
            }
        }

        void OnDamaged(float damage, GameObject damageSource)
        {
            // test if the damage source is the player
            if (damageSource && damageSource.GetComponent<Health>())
            {
                print(damageSource.name);
                // pursue the player
                DetectionModule.OnDamaged(damageSource);

                onDamaged?.Invoke();
                m_LastTimeDamaged = Time.time;

                // play the damage tick sound
                if (DamageTick && !m_WasDamagedThisFrame)
                    AudioUtility.CreateSFX(DamageTick, transform.position, AudioUtility.AudioGroups.DamageTick, 0f);

                m_WasDamagedThisFrame = true;
            }
        }

        /*
        void OnDie()
        {
            // spawn a particle system when dying
            var vfx = Instantiate(DeathVfx, DeathVfxSpawnPoint.position, Quaternion.identity);
            Destroy(vfx, 5f);
            // tells the game flow manager to handle the enemy destuction
            m_EnemyManager.UnregisterEnemy(this);
            // loot an object
            if (TryDropItem())
            {
                PhotonNetwork.InstantiateRoomObject(LootPrefab.name, transform.position, Quaternion.identity, 0);
            }
            if (Boss)
            {
                WaveCompletedEvent evt = Events.WaveCompletedEvent;
                evt.BossKillCount++;
                EventManager.Broadcast(evt);
            }
            // this will call the OnDestroy function
            StartCoroutine(WaitDie());
        }
        */

        private IEnumerator WaitDie()
        {
            yield return new WaitForSeconds(DeathDuration);
            //PhotonNetwork.InstantiateRoomObject(EnemyName, transform.position, transform.rotation);
            PhotonNetwork.Destroy(gameObject);

        }

        public void OrientWeaponsTowards(Vector3 lookPosition)
        {
            // orient weapon towards player
            Vector3 weaponForward = (lookPosition - CurrentWeapon.WeaponRoot.transform.position).normalized;
            CurrentWeapon.transform.forward = weaponForward;
        }
        public void OnEnemyShoot(int weaponIndex, Vector3 targetWorldPosition)
        {
            EnemyPhotonView.RPC("EnemyFire", RpcTarget.AllViaServer, weaponIndex, targetWorldPosition);
        }

        [PunRPC]
        public void EnemyFire(int weaponIndex, Vector3 targetWorldPosition)
        {
            CurrentWeapon.HandleShoot(targetWorldPosition);
        }

        public bool TryAtack(Vector3 enemyPosition)
        {
            if (m_GameFlowManager.GameIsEnding)
                return false;

            //OrientWeaponsTowards(enemyPosition);

            if ((m_LastTimeWeaponSwapped + DelayAfterWeaponSwap) >= Time.time)
                return false;

            // Shoot the weapon
            bool didFire = GetActiveWeapon().HandleShootInputs(enemyPosition);

            if (didFire && onAttack != null)
            {
                onAttack.Invoke();
            }

            return didFire;
        }

        public bool TryDropItem()
        {
            if (DropRate == 0 || LootPrefab == null)
                return false;
            else if (DropRate == 1)
                return true;
            else
                return (Random.value <= DropRate);
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

                    if (SwapToNextWeapon)
                    {
                        m_LastTimeWeaponSwapped = Time.time;
                    }
                    else
                    {
                        m_LastTimeWeaponSwapped = Mathf.NegativeInfinity;
                    }

                    WeaponController newWeapon = GetWeaponAtSlotIndex(m_WeaponSwitchNewWeaponIndex);
                    CurrentWeapon = newWeapon;
                    if (OnSwitchedToWeapon != null)
                    {
                        OnSwitchedToWeapon?.Invoke(newWeapon);
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

        // Adds a weapon to our inventory
        [PunRPC]
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
                    // spawn the weapon prefab as child of the weapon socket
                    GameObject weapon = SettingsManager.GetInstance().RequredWeaponsList.
                        Where(w => w.WeaponName == weaponName).FirstOrDefault().WeaponPrefab;

                    GameObject weaponObject = Instantiate(weapon, WeaponParentSocket.position, WeaponParentSocket.rotation);
                    WeaponController weaponInstance = weaponObject.GetComponent<WeaponController>();

                    weaponInstance.SourcePrefab = weaponInstance.gameObject;

                    weaponInstance.transform.SetParent(WeaponParentSocket);
                    weaponInstance.transform.localPosition = Vector3.zero;
                    weaponInstance.transform.localRotation = Quaternion.identity;
                    weaponInstance.transform.localScale = new Vector3(1, 1, 1);

                    // Set owner to this gameObject so the weapon can alter projectile/damage logic accordingly
                    weaponInstance.Owner = transform;

                    weaponInstance.SetOptions(i, this);
                    weaponInstance.AutomaticReload = true;
                    weaponInstance.InfinityAmmo = true;

                    weaponInstance.ShowWeapon(false);

                    WeaponSlots[i] = weaponInstance;

                    if (OnAddedWeapon != null)
                    {
                        OnAddedWeapon.Invoke(weaponInstance, i);
                    }

                    SwitchToWeaponIndex(i);

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

        public bool RemoveWeapon(WeaponController weaponInstance)
        {
            // Look through our slots for that weapon
            for (int i = 0; i < WeaponSlots.Length; i++)
            {
                // when weapon found, remove it
                if (WeaponSlots[i] == weaponInstance)
                {
                    WeaponSlots[i] = null;

                    if (OnRemovedWeapon != null)
                    {
                        OnRemovedWeapon.Invoke(weaponInstance, i);
                    }

                    Destroy(weaponInstance.gameObject);

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

        public void SetPatrolPath(PatrolPath[] patrols, int PatrolPathIndex)
        {
            PatrolPaths = patrols;
            PatrolPaths[PatrolPathIndex].EnemiesToAssign.Add(this);
            CurrentPatrolPath = PatrolPaths[PatrolPathIndex];
        }
        void OnWeaponSwitched(WeaponController newWeapon)
        {
            if (newWeapon != null)
            {
                m_Animator.SetFloat("IndexWeapon", newWeapon.IndexWeaponType());

                m_LookWeaponMuzzle = newWeapon.WeaponGunMuzzle;

                EnemyMobile.OnWeaponSwitched(newWeapon);

                newWeapon.ShowWeapon(true);
            }
        }
    }
}
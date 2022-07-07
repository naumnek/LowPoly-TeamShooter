using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using System.Collections;
using Unity.FPS.Gameplay;
using naumnek.Settings;

//Скрипт отвечающий за режимы передвижения ботов:
//Патрулирования - работает когда противников (Тоесть тех кто )

namespace Unity.FPS.AI
{
    [RequireComponent(typeof(EnemyController))]
    public class EnemyMobile : MonoBehaviour
    {

        public enum AIState
        {
            Patrol,
            Follow,
            Attack,
        }

        public GameObject Body;
        public Animator m_Animator;

        [Header("NavMeshAgent Options")]
        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 1f;

        [Header("Stopping NavMeshAgent")]
        [Tooltip("MoveSpeed NavMeshAgent")]
        public float StopDistanceNearestActors = 5f;
        public float StopAngleNearestActors = 60f;
        [Range(1f, 2f)]
        public float MultiplerStopping = 1f;
        [Range(0f, 1f)]
        public float MaxForceStoppingAgent = 0.5f;

        [Header("Attack Options")]
        [Tooltip("Fraction of the enemy's attack range at which it will stop moving towards target while attacking")]
        public float AttackRange = 30f;
        [Tooltip("Fraction of the enemy's attack range at which it will stop moving towards target while attacking")]
        [Range(0f, 1f)]
        public float AttackStopDistanceRatio = 1f;
        public float DividerAttackStopDistanceRatio = 10f;

        [Tooltip("Pointing target ray")]
        public LayerMask IgnoreLookWeaponeLayer;
        public LayerMask IgnoreLookForwardLayer;
        //

        [Header("Others")]
        [Tooltip("The random hit damage effects")]
        public ParticleSystem[] RandomHitSparks;
        public ParticleSystem[] OnDetectVfx;
        //public AudioClip OnDetectSfx;

        [Header("Sound")] public AudioClip MovementSound;
        public MinMaxFloat PitchDistortionMovementSpeed;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;
        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;
        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;
        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        public float DunamicAttackRange { get; private set; } = 50f;
        public AIState AiState { get; private set; }
        EnemyController m_EnemyController;
        DetectionModule m_DetectionModule;
        MaxValueAttributes m_MaxWeaponAttributes;
        SettingsManager m_SettingsManager;
        AudioSource m_AudioSource;

        const string k_AnimMoveSpeedParameter = "MoveSpeed";
        const string k_AnimAttackParameter = "Attack";
        const string k_AnimAlertedParameter = "Alerted";
        const string k_AnimOnDamagedParameter = "OnDamaged";
        private float _animationBlend;
        private NavMeshAgent m_NavMeshAgent;
        private float DelayBetweenShots = 0.2f;
        private bool CheckBetweenShots;

        private bool ServerPause = true;
        private float m_WeaponAttackStopDistanceRatio = 1f;
        private float m_DunamicAttackStopDistanceRatio = 1f;
        private float MoveSpeed = 3.5f;

        private Transform m_LookWeaponMuzzle;

        private void OnDestroy()
        {
            EventManager.RemoveListener<PlayerSpawnEvent>(OnPlayerSpawnEvent);
            EventManager.RemoveListener<EndGameEvent>(OnEndGameEvent);
        }

        private void Start()
        {
            m_WeaponAttackStopDistanceRatio = AttackStopDistanceRatio;
            m_DunamicAttackStopDistanceRatio = AttackStopDistanceRatio;

            EventManager.AddListener<PlayerSpawnEvent>(OnPlayerSpawnEvent);
            EventManager.AddListener<EndGameEvent>(OnEndGameEvent);

            Activate();

            PlayerController trigger = FindObjectOfType<PlayerController>();
            if (trigger != null)
            {
                //Activate();
            }
        }

        private void OnEndGameEvent(EndGameEvent evt)
        {
            ServerPause = true;
        }

        void OnPlayerSpawnEvent(PlayerSpawnEvent evt)
        {
            //Activate();
        }

        private void Activate()
        {
            m_EnemyController = GetComponent<EnemyController>();
            m_DetectionModule = GetComponentInChildren<DetectionModule>();
            m_MaxWeaponAttributes = m_EnemyController.SettingsManager.MaxWeaponAttributes;

            m_NavMeshAgent = GetComponent<NavMeshAgent>();
            m_Animator = GetComponent<Animator>();
            m_AudioSource = GetComponent<AudioSource>();

            // adding a audio source to play the movement sound on it
            m_AudioSource.clip = MovementSound;
            m_AudioSource.Play();
            MoveSpeed = m_EnemyController.NavMeshAgent.speed;

            m_EnemyController.onAttack += OnAttack;
            m_EnemyController.onDetectedTarget += OnDetectedTarget;
            m_EnemyController.onLostTarget += OnLostTarget;
            m_EnemyController.SetPathDestinationToClosestNode();
            m_EnemyController.onDamaged += OnDamaged;

            // Start patrolling
            AiState = AIState.Patrol;

            ServerPause = false;
        }

        public void ResetAiState()
        {
            AiState = AIState.Patrol;
        }

        private float moveSpeed = 1f;

        private bool waitCheck = false;
        private IEnumerator WaitForCheck(float time)
        {
            waitCheck = true;
            yield return new WaitForSeconds(time);
            waitCheck = false;
        }

        void Update()
        {
            if (!ServerPause && m_LookWeaponMuzzle != null && m_EnemyController.controllable)
            {
                //GroundedCheck();

                UpdateAiStateTransitions();
                UpdateCurrentAiState();

                moveSpeed = m_NavMeshAgent.velocity.magnitude;

                //Animator.SetFloat(k_AnimMoveSpeedParameter, moveSpeed);

                // changing the pitch of the movement sound depending on the movement speed
                m_AudioSource.pitch = Mathf.Lerp(PitchDistortionMovementSpeed.Min, PitchDistortionMovementSpeed.Max,
                    moveSpeed / m_NavMeshAgent.speed);

            }
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

            // update animator if using character
            m_Animator.SetBool("Grounded", Grounded);
        }

        void UpdateAiStateTransitions()
        {
            // Handle transitions 
            switch (AiState)
            {
                case AIState.Follow:
                    // Transition to attack when there is a line of sight to the target
                    if (m_EnemyController.IsSeeingTarget && m_EnemyController.IsTargetInAttackRange)
                    {
                        AiState = AIState.Attack;
                        m_EnemyController.SetNavDestination(transform.position);
                    }

                    break;
                case AIState.Attack:
                    // Transition to follow when no longer a target in attack range
                    if (!m_EnemyController.IsTargetInAttackRange)
                    {
                        AiState = AIState.Follow;
                    }

                    break;
            }
        }

        void UpdateCurrentAiState()
        {
            // Handle logic 
            Vector3 destinationPath = m_EnemyController.GetDestinationOnPath();

            Vector3 forwardPointing = Vector3.zero;
            Vector3 weaponPointing = Vector3.zero;
            Vector3 targetPosition = Vector3.zero;

            bool IsEnemyInAttackRange = false;

            if (m_DetectionModule.KnownDetectedTarget != null)
            {
                forwardPointing = IsForwardPointingTarget(m_EnemyController.KnownDetectedTarget);
                weaponPointing = IsWeaponPointingTarget(m_EnemyController.KnownDetectedTarget);
                targetPosition = m_DetectionModule.KnownDetectedTarget.AimPoint.position;

                IsEnemyInAttackRange = Vector3.Distance(targetPosition,m_DetectionModule.DetectionSourcePoint.position)
                        >= (m_DunamicAttackStopDistanceRatio * DunamicAttackRange);
            }

            switch (AiState)
            {
                case AIState.Patrol:
                    m_EnemyController.UpdatePathDestination();

                    m_EnemyController.SetNavDestination(destinationPath);

                    m_EnemyController.SetLookPositionTarget(Vector3.zero);
                    break;
                case AIState.Follow:
                    m_EnemyController.SetNavDestination(targetPosition);

                    m_EnemyController.SetLookPositionTarget(Vector3.zero);

                    break;
                case AIState.Attack:
                    if (IsEnemyInAttackRange)
                    {
                        /*
                        _animationBlend = Mathf.Lerp(_animationBlend, moveSpeed, Time.deltaTime * m_NavMeshAgent.acceleration);
                        m_Animator.SetLayerWeight(1, Mathf.Lerp(m_Animator.GetLayerWeight(1), 0f, Time.deltaTime * 13f));
                        m_Animator.SetFloat("Speed", _animationBlend);
                        m_Animator.SetFloat("MotionSpeed", 1f);
                        */

                        m_EnemyController.SetNavDestination(targetPosition);
                    }
                    else
                    {
                        m_EnemyController.SetNavDestination(transform.position);
                    }
                    m_EnemyController.SetLookPositionTarget(targetPosition);

                    if (weaponPointing != Vector3.zero)
                    {
                        m_Animator.SetLayerWeight(1, Mathf.Lerp(m_Animator.GetLayerWeight(1), 1f, Time.deltaTime * 13f));
                        m_EnemyController.TryAtack(weaponPointing);
                    }
                    else
                    {
                        OnHitEnemy(false);
                    }
                    break;
            }

            float distanceNearestActor = m_DetectionModule.HasDistanceNearestForwardActor(StopAngleNearestActors) / StopDistanceNearestActors;

            if (distanceNearestActor < 1)
            {
                float stopping = Mathf.Max(MaxForceStoppingAgent, (distanceNearestActor * MultiplerStopping));
                m_EnemyController.NavMeshAgent.speed = MoveSpeed * stopping;
            }
            else
            {
                m_EnemyController.NavMeshAgent.speed = MoveSpeed;
            }
            //OrientTowards(m_DetectionModule.KnownDetectedTarget.AimPoint.position);
        }

        public void OnHitEnemy(bool hit)
        {
            if (CheckBetweenShots) return;
            CheckBetweenShots = true;
            m_DunamicAttackStopDistanceRatio = hit ? AttackStopDistanceRatio
                    : m_DunamicAttackStopDistanceRatio - (AttackStopDistanceRatio / DividerAttackStopDistanceRatio);

            if (m_DunamicAttackStopDistanceRatio < 0) m_DunamicAttackStopDistanceRatio = AttackStopDistanceRatio / DividerAttackStopDistanceRatio;
            /*
            if (m_DetectionModule.KnownDetectedTarget != null)
            {
                Vector3 targetPosition = m_DetectionModule.KnownDetectedTarget.AimPoint.position;
                Debug.Log("Distance: " + Vector3.Distance(targetPosition, m_DetectionModule.DetectionSourcePoint.position) +
                   "|\n Attack " + CurrentWeapon.WeaponName + ": " + m_DunamicAttackStopDistanceRatio + " * " + DunamicAttackRange + " = " + (m_DunamicAttackStopDistanceRatio * DunamicAttackRange));
            }
            */
            StartCoroutine(WairBetweenShot());
        }

        private WeaponController CurrentWeapon;

        public void OnWeaponSwitched(WeaponController newWeapon)
        {
            CurrentWeapon = newWeapon;
            m_LookWeaponMuzzle = newWeapon.WeaponGunMuzzle;

            //float spreadAngle = newWeapon.BulletSpreadAngle < 1 ? 1 : m_MaxWeaponAttributes.BulletSpreadAngle / newWeapon.BulletSpreadAngle;
            //float bulletsPerShot = newWeapon.BulletsPerShot < 1 ? 1 : m_MaxWeaponAttributes.BulletsPerShoot / newWeapon.BulletsPerShot;
            //MultiplerAttackRange = spreadAngle * bulletsPerShot;

            // WeaponTypes: Rifle, Pistol, ShotGun, MiniGun, Machine, MachineGun, Grenade, Rocket

            DelayBetweenShots = newWeapon.DelayBetweenShots;
            DunamicAttackRange = AttackRange * newWeapon.MultiplerAttackRange;

            m_DunamicAttackStopDistanceRatio = AttackStopDistanceRatio;

            //Debug.Log("OnWeaponSwitched " + newWeapon.WeaponName + ": " + spreadAngle + " * " + bulletsPerShot + " * " + DelayBetweenShots + " = " + MultiplerAttackRange);
        }

        private IEnumerator WairBetweenShot()
        {
            yield return new WaitForSeconds(DelayBetweenShots);
            CheckBetweenShots = false;
        }

        private Vector3 IsForwardPointingTarget(Actor target)
        {
            // Pointing at enemy handling
            if (Physics.Raycast(new Ray(m_LookWeaponMuzzle.position, m_LookWeaponMuzzle.forward), out RaycastHit raycastHit, 999f, ~IgnoreLookForwardLayer))
            {
                Actor hitActor = raycastHit.collider.GetComponentInParent<Actor>();
                if (hitActor != null && hitActor == target)
                {
                    return raycastHit.point;
                }
            }
            return Vector3.zero;
        }

        private Vector3 IsWeaponPointingTarget(Actor target)
        {
            // Pointing at enemy handling
            if (Physics.Raycast(new Ray(m_LookWeaponMuzzle.position, m_LookWeaponMuzzle.forward), out RaycastHit raycastHit, 999f, ~IgnoreLookWeaponeLayer))
            {
                Actor hitActor = raycastHit.collider.GetComponentInParent<Actor>();
                if (hitActor != null && hitActor == target)
                {
                    return raycastHit.point;
                }
            }
            return Vector3.zero;
        }


        void OnAttack()
        {
            //Animator.SetTrigger(k_AnimAttackParameter);
        }

        void OnDetectedTarget()
        {
            if (AiState == AIState.Patrol)
            {
                AiState = AIState.Follow;
            }

            for (int i = 0; i < OnDetectVfx.Length; i++)
            {
                //OnDetectVfx[i].Play();
            }

            //Animator.SetBool(k_AnimAlertedParameter, true);
        }

        void OnLostTarget()
        {
            if (AiState == AIState.Follow || AiState == AIState.Attack)
            {
                AiState = AIState.Patrol;
                m_DunamicAttackStopDistanceRatio = AttackStopDistanceRatio;
            }

            for (int i = 0; i < OnDetectVfx.Length; i++)
            {
                //OnDetectVfx[i].Stop();
            }

            //Animator.SetBool(k_AnimAlertedParameter, false);
        }

        void OnDamaged()
        {
            if (RandomHitSparks.Length > 0)
            {
                int n = Random.Range(0, RandomHitSparks.Length);
                //RandomHitSparks[n].Play();
            }

            //Animator.SetTrigger(k_AnimOnDamagedParameter);
        }
    }
}
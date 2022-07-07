using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Unity.FPS.Gameplay;
using Photon.Pun;
using Unity.FPS.AI;
using Photon.Realtime;
using Photon.Pun.UtilityScripts;
using Photon.Pun.Demo.Asteroids;
using Hashtable = ExitGames.Client.Photon.Hashtable;

//Скрипт отвечает за параметры оружия(Вид оружия, тип стрельбы, какие патроны и т.д)
//Здесь устанавливается внешний вид прицела и иконка оружия.
//Также скрипт отвечает за поведения оружия(перезарядка, интервал между выстрелами, настройка выстрелов)

namespace Unity.FPS.Game
{
    public enum WeaponShootType
    {
        Manual,
        Automatic,
        Charge,
    }
    public enum WeaponType
    {
        Rifle,
        Pistol,
        ShotGun,
        MiniGun,
        Machine,
        MachineGun,
        Grenade,
        Rocket,
    }

    [System.Serializable]
    public struct CrosshairData
    {
        [Tooltip("The image that will be used for this weapon's crosshair")]
        public Sprite CrosshairSprite;

        [Tooltip("The size of the crosshair image")]
        public int CrosshairSize;

        [Tooltip("The color of the crosshair image")]
        public Color CrosshairColor;
    }

    [RequireComponent(typeof(AudioSource))]
    public class WeaponController : MonoBehaviour
    {
        [Header("Information")] [Tooltip("The name that will be displayed in the UI for this weapon")]
        public string WeaponName;
        public GameObject WeaponPrefab;

        [Tooltip("The name that will be displayed in the UI for this weapon")]
        public WeaponType Type = WeaponType.Rifle;
        [Tooltip("Only bots options.")]
        public float MultiplerAttackRange = 1f;

        public bool PlayerWeapon = false;

        [Tooltip("The image that will be displayed in the UI for this weapon")]
        public Sprite WeaponIcon;
        public List<Renderer> WeaponRenderer = new List<Renderer> { };

        [Tooltip("Default data for the crosshair")]
        public CrosshairData CrosshairDataDefault;

        [Tooltip("Data for the crosshair when targeting an enemy")]
        public CrosshairData CrosshairDataTargetInSight;

        [Tooltip("Data for the sight when hitting an enemy")]
        public CrosshairData CrosshairDataHitTarget;

        [Header("Internal References")]
        [Tooltip("The root object for the weapon, this is what will be deactivated when the weapon isn't active")]
        public GameObject WeaponRoot;

        [Tooltip("Tip of the weapon, where the projectiles are shot")]
        public Transform WeaponGunMuzzle;

        [Header("Shoot Parameters")] [Tooltip("The type of weapon wil affect how it shoots")]
        public WeaponShootType ShootType;

        [Tooltip("The projectile prefab")] public ProjectileBase ProjectilePrefab;

        [Tooltip("Minimum duration between two shots")]
        public float DelayBetweenShots = 0.5f;

        [Tooltip("Angle for the cone in which the bullets will be shot randomly (0 means no spread at all)")]
        public float BulletSpreadAngle = 0f;

        [Tooltip("Amount of bullets per shot")]
        public int BulletsPerShot = 1;

        [Tooltip("Force that will push back the weapon after each shot")] [Range(0f, 2f)]
        public float RecoilForce = 1;

        [Header("Aiming")]
        [Tooltip("Disabled aiming the weapon")]
        public bool DisableAiming = false;

        [Tooltip("Ratio of the default FOV that this weapon applies while aiming")] [Range(0f, 1f)]
        public float AimZoomRatio = 1f;

        [Tooltip("Translation to apply to weapon arm when aiming with this weapon")]
        public Vector3 AimOffset;

        [Header("Ammo Parameters")]
        [Tooltip("Has ammo does not need to be replenished")]
        public bool InfinityAmmo = false;
        [Tooltip("Should the player manually reload")]
        public bool AutomaticReload = true;
        [Tooltip("Has physical clip on the weapon and ammo shells are ejected when firing")]
        public bool HasPhysicalBullets = false;
        [Tooltip("Maximum amount of bullets in a clip")]
        public float MaxBullets = 30f;
        [Tooltip("Maximum amount of ammo in the gun")]
        public int MaxAmmo = 120;
        [Tooltip("Start number of bullets in a clip")]
        public float StartBullets = 30f;
        [Tooltip("Start amount of ammo in the gun")]
        public int StartMaxAmmo = 60;
        [Tooltip("Bullet Shell Casing")]
        public GameObject ShellCasing;
        [Tooltip("Weapon Ejection Port for physical ammo")]
        public Transform EjectionPort;
        [Tooltip("Force applied on the shell")]
        [Range(0.0f, 5.0f)] public float ShellCasingEjectionForce = 2.0f;
        [Tooltip("Maximum number of shell that can be spawned before reuse")]
        [Range(1, 30)] public int ShellPoolSize = 1;
        [Tooltip("Amount of ammo reloaded per second")]
        public float AmmoReloadRate = 1f;

        [Tooltip("Delay after the last shot before starting to reload")]
        public float AmmoReloadDelay = 2f;

        [Header("Charging parameters (charging weapons only)")]
        [Tooltip("Trigger a shot when maximum charge is reached")]
        public bool AutomaticReleaseOnCharged;

        [Tooltip("Duration to reach maximum charge")]
        public float MaxChargeDuration = 2f;

        [Tooltip("Initial ammo used when starting to charge")]
        public float AmmoUsedOnStartCharge = 1f;

        [Tooltip("Additional ammo used when charge reaches its maximum")]
        public float AmmoUsageRateWhileCharging = 1f;

        public bool FirstShoot = false;
        [Header("Audio & Visual")]
        [Tooltip("Start shoot weapon waits for seconds (only player)")]
        public float WaitShootWeapon = 1f;

        [Tooltip("Optional weapon animator for OnShoot animations")]
        public Animator WeaponAnimator;

        [Tooltip("Prefab of the muzzle flash")]
        public GameObject MuzzleFlashPrefab;

        [Tooltip("Layer hit projectile")]
        public LayerMask aimColliderLayerMask = new LayerMask();

        [Tooltip("Unparent the muzzle flash instance on spawn")]
        public bool UnparentMuzzleFlash;

        [Tooltip("sound played when shooting")]
        public AudioClip ShootSfx;

        [Tooltip("Sound played when changing to this weapon")]
        public AudioClip ChangeWeaponSfx;

        [Tooltip("sound played when have 0 bullets")]
        public AudioClip EmptyMagazinSfx;

        [Tooltip("Continuous Shooting Sound")] public bool UseContinuousShootSound = false;
        public AudioClip ContinuousShootStartSfx;
        public AudioClip ContinuousShootLoopSfx;
        public AudioClip ContinuousShootEndSfx;
        AudioSource m_ContinuousShootAudioSource = null;
        bool m_WantsToShoot = false;

        public UnityAction OnShoot;
        public event Action OnShootProcessed;

        public float CarriedAmmo { get; private set; }
        float m_CurrentBullets;
        float m_LastTimeShot = Mathf.NegativeInfinity;
        public float LastChargeTriggerTimestamp { get; private set; }
        Vector3 m_LastMuzzlePosition;

        public Transform Owner { get; set; }
        public GameObject SourcePrefab { get; set; }
        public bool IsCharging { get; private set; }
        public float CurrentAmmoRatio { get; private set; }
        public bool IsWeaponActive { get; private set; }
        public bool IsCooling { get; private set; }
        public float CurrentCharge { get; private set; }
        public Vector3 MuzzleWorldVelocity { get; private set; }

        public float GetAmmoNeededToShoot() =>
            (ShootType != WeaponShootType.Charge ? 1f : Mathf.Max(1f, AmmoUsedOnStartCharge)) /
            (MaxBullets * BulletsPerShot);

        public int GetCarriedAmmo() => Mathf.FloorToInt(CarriedAmmo);
        public int GetCurrentBullets() => Mathf.FloorToInt(m_CurrentBullets);

        AudioSource m_ShootAudioSource;

        public bool IsReloading { get; private set; }

        const string k_AnimAttackParameter = "Attack";

        private Queue<Rigidbody> m_PhysicalAmmoPool;
        PlayerInputHandler m_InputHandler;

        private bool FirstAutomaticReload = false;
        private PhotonView photonView;
        public PlayerWeaponsManager WeaponManager { get; private set; }
        private PlayerController m_PlayerController;
        private EnemyController m_EnemyController;
        private int IndexWeaponList = 0;

        public void SetOptions(int index, PlayerController playerController)
        {
            IndexWeaponList = index;

            m_PlayerController = playerController;
            WeaponManager = m_PlayerController.PlayerWeaponsManager;
            photonView = m_PlayerController.PhotonView;
        }
        public void SetOptions(int index, EnemyController enemyController)
        {
            IndexWeaponList = index;

            m_EnemyController = enemyController;
            photonView = m_EnemyController.EnemyPhotonView;
        }

        public void ResetAutomaticReload()
        {
            AutomaticReload = FirstAutomaticReload;
        }

        private void Start()
        {

            FirstAutomaticReload = AutomaticReload;
            m_CurrentBullets = StartBullets;
            CarriedAmmo = InfinityAmmo ? 0 : StartMaxAmmo;
            m_LastMuzzlePosition = WeaponGunMuzzle.position;

            m_ShootAudioSource = GetComponent<AudioSource>();
            DebugUtility.HandleErrorIfNullGetComponent<AudioSource, WeaponController>(m_ShootAudioSource, this,
                gameObject);

            if (UseContinuousShootSound)
            {
                m_ContinuousShootAudioSource = gameObject.AddComponent<AudioSource>();
                m_ContinuousShootAudioSource.playOnAwake = false;
                m_ContinuousShootAudioSource.clip = ContinuousShootLoopSfx;
                m_ContinuousShootAudioSource.outputAudioMixerGroup =
                    AudioUtility.GetAudioGroup(AudioUtility.AudioGroups.WeaponShoot);
                m_ContinuousShootAudioSource.loop = true;
            }

            if (HasPhysicalBullets)
            {
                m_PhysicalAmmoPool = new Queue<Rigidbody>(ShellPoolSize);

                for (int i = 0; i < ShellPoolSize; i++)
                {
                    GameObject shell = Instantiate(ShellCasing, transform);
                    shell.SetActive(false);
                    m_PhysicalAmmoPool.Enqueue(shell.GetComponent<Rigidbody>());
                }
            }
        }

        public float IndexWeaponType()
        {
            // WeaponTypes: Rifle, Pistol, ShotGun, MiniGun, Machine, MachineGun, Grenade, Rocket
            float IndexWeapon = 0f;
            switch (Type)
            {
                case WeaponType.Rifle:
                    IndexWeapon = 0f;
                    break;
                case WeaponType.Pistol:
                    IndexWeapon = 1f;
                    break;
                case WeaponType.ShotGun:
                    IndexWeapon = 0f;
                    break;
                case WeaponType.MiniGun:
                    IndexWeapon = 0f;
                    break;
                case WeaponType.Machine:
                    IndexWeapon = 0f;
                    break;
                case WeaponType.MachineGun:
                    IndexWeapon = 0f;
                    break;
            }
            return IndexWeapon;
        }

        public void AddCarriablePhysicalBullets(int count)
        {
            if (CarriedAmmo + count < MaxAmmo) CarriedAmmo += count;
            else CarriedAmmo = MaxAmmo;
        }
        void ShootShell()
        {
            Rigidbody nextShell = m_PhysicalAmmoPool.Dequeue();

            nextShell.transform.position = EjectionPort.transform.position;
            nextShell.transform.rotation = EjectionPort.transform.rotation;
            nextShell.gameObject.SetActive(true);
            nextShell.transform.SetParent(null);
            nextShell.collisionDetectionMode = CollisionDetectionMode.Continuous;
            nextShell.AddForce(nextShell.transform.up * ShellCasingEjectionForce, ForceMode.Impulse);

            m_PhysicalAmmoPool.Enqueue(nextShell);
        }

        void PlaySFX(AudioClip sfx) => AudioUtility.CreateSFX(sfx, transform.position, AudioUtility.AudioGroups.WeaponShoot, 0.0f);


        void Reload()
        {
            /*if (m_CarriedAmmo > 0)
            {
                m_CurrentAmmo = Mathf.Min(m_CarriedAmmo, ClipSize);
            }

            IsReloading = false;*/
        }

        public void StartReload()
        {
            if (CarriedAmmo >= 0)
            {
                if(HasPhysicalBullets) GetComponent<Animator>().SetTrigger("Reload");
                IsReloading = true;
            }
        }

        void Update()
        {
            UpdateAmmo();
            UpdateCharge();
            UpdateContinuousShootSound();

            if (Time.deltaTime > 0)
            {
                MuzzleWorldVelocity = (WeaponGunMuzzle.position - m_LastMuzzlePosition) / Time.deltaTime;
                m_LastMuzzlePosition = WeaponGunMuzzle.position;
            }
        }

        void UpdateAmmo()
        {
            if (AutomaticReload || IsReloading)
            {
                if(m_CurrentBullets < MaxBullets && !IsCharging)
                {
                    // reloads weapon over time
                    m_CurrentBullets += AmmoReloadRate * Time.deltaTime;
                    CarriedAmmo -= AmmoReloadRate * Time.deltaTime;

                    // limits ammo to max value
                    m_CurrentBullets = Mathf.Clamp(m_CurrentBullets, 0, MaxBullets);
                    CarriedAmmo = Mathf.Clamp(CarriedAmmo, 0, MaxAmmo);

                    IsCooling = true;
                }
                else
                {
                    IsReloading = false;
                    IsCooling = false;
                }
            }

            if (MaxBullets == Mathf.Infinity)
            {
                CurrentAmmoRatio = 1f;
            }
            else
            {
                CurrentAmmoRatio = m_CurrentBullets / MaxBullets;
            }
        }

        void UpdateCharge()
        {
            if (IsCharging)
            {
                if (CurrentCharge < 1f)
                {
                    float chargeLeft = 1f - CurrentCharge;

                    // Calculate how much charge ratio to add this frame
                    float chargeAdded = 0f;
                    if (MaxChargeDuration <= 0f)
                    {
                        chargeAdded = chargeLeft;
                    }
                    else
                    {
                        chargeAdded = (1f / MaxChargeDuration) * Time.deltaTime;
                    }

                    chargeAdded = Mathf.Clamp(chargeAdded, 0f, chargeLeft);

                    // See if we can actually add this charge
                    float ammoThisChargeWouldRequire = chargeAdded * AmmoUsageRateWhileCharging;
                    if (ammoThisChargeWouldRequire <= m_CurrentBullets)
                    {
                        // Use ammo based on charge added
                        UseAmmo(ammoThisChargeWouldRequire);

                        // set current charge ratio
                        CurrentCharge = Mathf.Clamp01(CurrentCharge + chargeAdded);
                    }
                }
            }
        }

        void UpdateContinuousShootSound()
        {
            if (UseContinuousShootSound)
            {
                if (m_WantsToShoot && m_CurrentBullets >= 1f)
                {
                    if (!m_ContinuousShootAudioSource.isPlaying)
                    {
                        m_ShootAudioSource.PlayOneShot(ShootSfx);
                        m_ShootAudioSource.PlayOneShot(ContinuousShootStartSfx);
                        m_ContinuousShootAudioSource.Play();
                    }
                }
                else if (m_ContinuousShootAudioSource.isPlaying)
                {
                    m_ShootAudioSource.PlayOneShot(ContinuousShootEndSfx);
                    m_ContinuousShootAudioSource.Stop();
                }
            }
        }

        public void ShowWeapon(bool show)
        {
            WeaponRoot.SetActive(show);

            if (show && ChangeWeaponSfx != null)
            {
                //m_ShootAudioSource.PlayOneShot(ChangeWeaponSfx);
            }

            IsWeaponActive = show;
        }

        public void UseAmmo(float amount)
        {
            m_CurrentBullets = Mathf.Clamp(m_CurrentBullets - amount, 0f, MaxBullets);
            m_LastTimeShot = Time.time;
        }

        public bool HandleShootInputs(Vector3 targetWorldPosition)
        {
            m_WantsToShoot = true;

            switch (ShootType)
            {
                case WeaponShootType.Manual:
                    return TryShoot(targetWorldPosition);

                case WeaponShootType.Automatic:
                    return TryShoot(targetWorldPosition);

                case WeaponShootType.Charge:
                    TryBeginCharge();

                    // Check if we released charge or if the weapon shoot autmatically when it's fully charged
                    if (AutomaticReleaseOnCharged && CurrentCharge >= 1f)
                    {
                        return TryReleaseCharge();
                    }

                    return false;

                default:
                    return false;
            }
        }
        public float GetCurrentAnimatorTime(Animator targetAnim, int layer = 0)
        {
            AnimatorStateInfo animState = targetAnim.GetCurrentAnimatorStateInfo(layer);
            float currentTime = animState.normalizedTime % 1;
            return currentTime;
        }

        public bool TryShoot(Vector3 targetPosition)
        {
            if(m_CurrentBullets < 1f) m_ShootAudioSource.PlayOneShot(EmptyMagazinSfx);

            if (m_CurrentBullets >= 1f
                && m_LastTimeShot + DelayBetweenShots < Time.time)
            {
                FirstShoot = true;
                m_LastTimeShot = Time.time;
                if (m_PlayerController != null) m_PlayerController.OnPlayerShoot(IndexWeaponList, targetPosition);
                if (m_EnemyController != null) m_EnemyController.OnEnemyShoot(IndexWeaponList, targetPosition);
                //HandleShoot();
                m_CurrentBullets -= 1f;

                return true;
            }

            return false;
        }

        bool TryBeginCharge()
        {
            if (!IsCharging
                && m_CurrentBullets >= AmmoUsedOnStartCharge
                && Mathf.FloorToInt((m_CurrentBullets - AmmoUsedOnStartCharge) * BulletsPerShot) > 0
                && m_LastTimeShot + DelayBetweenShots < Time.time)
            {
                FirstShoot = true;
                UseAmmo(AmmoUsedOnStartCharge);

                LastChargeTriggerTimestamp = Time.time;
                IsCharging = true;

                return true;
            }

            return false;
        }

        bool TryReleaseCharge()
        {
            if (IsCharging)
            {
                //HandleShoot();

                CurrentCharge = 0f;
                IsCharging = false;

                return true;
            }

            return false;
        }

        public void HandleShoot(Vector3 targetPosition)
        {
            if (targetPosition == null) targetPosition = Vector3.zero;
            int bulletsPerShotFinal = ShootType == WeaponShootType.Charge
                ? Mathf.CeilToInt(CurrentCharge * BulletsPerShot)
                : BulletsPerShot;

            // spawn all bullets with random direction
            for (int i = 0; i < bulletsPerShotFinal; i++)
            {
                // Projectile Shoot
                Vector3 aimDir = (targetPosition - WeaponGunMuzzle.position).normalized;
                Vector3 shotDirection = GetShotDirectionWithinSpread(aimDir);

                ProjectileBase newProjectile = Instantiate(ProjectilePrefab, WeaponGunMuzzle.position,
                    Quaternion.LookRotation(shotDirection));
                newProjectile.Shoot(this);
            }

            // muzzle flash
            if (MuzzleFlashPrefab != null)
            {
                GameObject muzzleFlashInstance = Instantiate(MuzzleFlashPrefab, WeaponGunMuzzle.position,
                    WeaponGunMuzzle.rotation, WeaponGunMuzzle.transform);
                // Unparent the muzzleFlashInstance
                if (UnparentMuzzleFlash)
                {
                    muzzleFlashInstance.transform.SetParent(null);
                }

                Destroy(muzzleFlashInstance, 2f);
            }

            if (!InfinityAmmo)
            {
                if (HasPhysicalBullets) ShootShell();
                //m_CarriedAmmo--;
            }

            m_LastTimeShot = Time.time;

            // play shoot SFX
            if (ShootSfx && !UseContinuousShootSound)
            {
                m_ShootAudioSource.PlayOneShot(ShootSfx);
            }

            // Trigger attack animation if there is any
            if (WeaponAnimator)
            {
                //WeaponAnimator.SetTrigger(k_AnimAttackParameter);
            }

            OnShoot?.Invoke();
            OnShootProcessed?.Invoke();
        }

        public Vector3 GetShotDirectionWithinSpread(Vector3 shootDirection)
        {
            float spreadAngleRatio = BulletSpreadAngle / 180f;
            Vector3 spreadWorldDirection = Vector3.Slerp(shootDirection, UnityEngine.Random.insideUnitSphere,
                spreadAngleRatio);

            return spreadWorldDirection;
        }
    }
    public class ShootType
    {

    }
}
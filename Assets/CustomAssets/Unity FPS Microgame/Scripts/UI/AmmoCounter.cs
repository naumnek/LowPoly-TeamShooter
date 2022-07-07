using TMPro;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.FPS.UI
{
    public class AmmoCounter : MonoBehaviour
    {
        [Tooltip("CanvasGroup to fade the ammo UI")]
        public CanvasGroup CanvasGroup;

        [Tooltip("Image for the weapon icon")] 
        public Image WeaponImage;

        public Color WeaponDefaultColor;
        public Color WeaponReloadColor;

        [Tooltip("Text for Weapon index")] 
        public Text WeaponIndexText;

        [Tooltip("Image for the weapon infinity ammo icon")]
        public Image CarriedInfinityAmmoIcon;

        [Tooltip("Text for Bullet Counter")] 
        public Text CurrentBulletsCounter;

        [Tooltip("Text for Bullet Supple Counter")]
        public Text CarriedAmmoCounter;

        [Header("Selection")] [Range(0, 1)] [Tooltip("Opacity when weapon not selected")]
        public float UnselectedOpacity = 0.5f;

        [Tooltip("Scale when weapon not selected")]
        public Vector3 UnselectedScale;

        [Tooltip("Sharpness for the fill ratio movements")]
        public float AmmoFillMovementSharpness = 20f;

        public int WeaponCounterIndex { get; set; }

        PlayerWeaponsManager m_PlayerWeaponsManager;
        WeaponController m_Weapon;
        private Vector3 StartScale;

        void Awake()
        {
            EventManager.AddListener<AmmoPickupEvent>(OnAmmoPickup);
            StartScale = transform.localScale;
            UnselectedScale = StartScale * 0.8f;
        }

        void OnAmmoPickup(AmmoPickupEvent evt)
        {
            if (evt.Weapon == m_Weapon)
            {
                CarriedAmmoCounter.text = m_Weapon.GetCarriedAmmo().ToString();
            }
        }

        public void Initialize(WeaponController weapon, int weaponIndex)
        {
            m_Weapon = weapon;
            WeaponCounterIndex = weaponIndex;
            WeaponImage.GetComponent<RectTransform>().sizeDelta = 
                new Vector2(weapon.WeaponIcon.rect.width, weapon.WeaponIcon.rect.height);
            WeaponImage.sprite = weapon.WeaponIcon;
            if (weapon.InfinityAmmo)
            {
                CarriedAmmoCounter.gameObject.SetActive(false);
                CarriedInfinityAmmoIcon.gameObject.SetActive(true);
            }
            else
                CurrentBulletsCounter.text = weapon.GetCurrentBullets().ToString();
                CarriedAmmoCounter.text = weapon.GetCarriedAmmo().ToString();

            m_PlayerWeaponsManager = FindObjectOfType<PlayerWeaponsManager>();
            DebugUtility.HandleErrorIfNullFindObject<PlayerWeaponsManager, AmmoCounter>(m_PlayerWeaponsManager, this);

            WeaponIndexText.text = (WeaponCounterIndex + 1).ToString();
        }

        void Update()
        {
            //AmmoFillImage.fillAmount = Mathf.Lerp(AmmoFillImage.fillAmount, currenFillRatio, Time.deltaTime * AmmoFillMovementSharpness);

            CurrentBulletsCounter.text = m_Weapon.GetCurrentBullets().ToString();
            CarriedAmmoCounter.text = m_Weapon.GetCarriedAmmo().ToString();

            bool isActiveWeapon = m_Weapon == m_PlayerWeaponsManager.GetActiveWeapon();

            CanvasGroup.alpha = Mathf.Lerp(CanvasGroup.alpha, isActiveWeapon ? 1f : UnselectedOpacity,
                Time.deltaTime * 10);
            transform.localScale = Vector3.Lerp(transform.localScale, isActiveWeapon ? StartScale : UnselectedScale,
                Time.deltaTime * 10);

            WeaponImage.color =
                m_Weapon.GetCarriedAmmo() > 0 && m_Weapon.GetCurrentBullets() == 0 && m_Weapon.IsWeaponActive ?
                WeaponReloadColor : WeaponDefaultColor;
        }

        void Destroy()
        {
            EventManager.RemoveListener<AmmoPickupEvent>(OnAmmoPickup);
        }
    }
}
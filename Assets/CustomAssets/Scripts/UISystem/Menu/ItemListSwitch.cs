using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.FPS.Game;
using naumnek.Settings;
using System.Linq;
using Unity.FPS.Gameplay;

//Скрипт для настройки установки параметров лута в префаб карточки.

namespace naumnek.Menu
{
    public class ItemListSwitch : MonoBehaviour
    {
        [Header("General")]
        public Text ItemNameText;
        public Image ItemIcon;
        public float MultiplierItemIcon = 0.5f;
        public Button ItemChooseButton;
        public Sprite InactiveSpriteButton;
        public GameObject BlackMask;
        public GameObject ImagePaid;
        public GameObject ImageBlocked;

        [Header("Attributes")]
        public UIAttribute MaxBullets;
        public UIAttribute Damage;
        public UIAttribute BulletSpeed;
        public UIAttribute BulletSpreadAngle;
        public UIAttribute BulletsPerShoot;
        public float SpeedValueBar = 0.5f;
        List<UIAttribute> Weapons;

        private Image ChooseButtonImage;
        private Sprite DefaultSpriteButton;
        private string roomName;
        private Items m_Item;
        private SwitchItemMenu m_ItemMenu;
        private float time = 0f;
        private bool IsSetValue;
        private bool HasActive;

        private void Start()
        {
        }

        private void Update()
        {
            if (!IsSetValue) return;

            for(int i = 0; i < Weapons.Count; i++)
            {
                if (time < 1)
                {
                    time += HasActive ? Time.deltaTime * SpeedValueBar : 1;
                    Weapons[i].LoadingBar.value = time * Weapons[i].Value;
                    if (Weapons.All(w => w.LoadingBar.value >= w.Value)) 
                    {
                        IsSetValue = false;
                    }
                }
            }
        }
        public void OnItemClicked()
        {
            m_ItemMenu.GiveItemPlayer(m_Item, this);
        }

        public void Initialize(Items item, SwitchItemMenu ItemMenu, bool isActive)
        {
            HasActive = isActive && !item.IsPaid && !item.IsBlocked;

            BlackMask.SetActive(!HasActive);
            ImageBlocked.SetActive(item.IsBlocked);
            ImagePaid.SetActive(item.IsPaid);

            Weapons = new List<UIAttribute>
            { MaxBullets, Damage, BulletSpeed, BulletSpreadAngle, BulletsPerShoot};
            ChooseButtonImage = ItemChooseButton.GetComponent<Image>();
            DefaultSpriteButton = ChooseButtonImage.sprite;

            ItemChooseButton.interactable = HasActive;
            ChooseButtonImage.sprite =
                HasActive ? 
                DefaultSpriteButton : 
                InactiveSpriteButton;

            roomName = item.Name();
            m_Item = item;
            m_ItemMenu = ItemMenu;

            ItemNameText.text = item.Name();
            Sprite SpriteItemIcon = item.Weapon.WeaponIcon;
            ItemIcon.sprite = SpriteItemIcon;

            ItemIcon.GetComponent<RectTransform>().sizeDelta =
                new Vector2(SpriteItemIcon.rect.width * MultiplierItemIcon, SpriteItemIcon.rect.height * MultiplierItemIcon);

            MaxValueAttributes maxValueAttributes = ItemMenu.SettingsManager.MaxWeaponAttributes;

            MaxBullets.SetValue(item.Attributes.MaxBullets, maxValueAttributes.MaxBullets);
            Damage.SetValue(item.Attributes.Damage, maxValueAttributes.Damage);
            BulletSpeed.SetValue(item.Attributes.BulletSpeed, maxValueAttributes.BulletSpeed);
            BulletSpreadAngle.SetValue(item.Attributes.SpreadAngle, maxValueAttributes.BulletSpreadAngle);
            BulletsPerShoot.SetValue(item.Attributes.BulletsPerShoot, maxValueAttributes.BulletsPerShoot);

            IsSetValue = true;
        }
    }
    [System.Serializable]
    public class UIAttribute
    {
        public Slider LoadingBar;
        public Text TextValue;
        public float Value { get; private set; }
        public bool FullLoadingBar;

        public void SetValue(float newValue, float maxValue)
        {
            LoadingBar.maxValue = maxValue;
            Value = newValue;
            TextValue.text = newValue.ToString();
        }
    }
}

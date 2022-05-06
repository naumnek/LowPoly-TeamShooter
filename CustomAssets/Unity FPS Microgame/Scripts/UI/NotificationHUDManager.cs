using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.FPS.UI
{
    public class NotificationHUDManager : MonoBehaviour
    {
        public Color DefaultColor;
        [Tooltip("UI panel containing the layoutGroup for displaying notifications")]
        public RectTransform NotificationPanel;

        [Tooltip("Prefab for the notifications")]
        public GameObject NotificationPrefab;

        void Awake()
        {
            PlayerWeaponsManager playerWeaponsManager = PlayerWeaponsManager.GetInstance();
            playerWeaponsManager.OnAddedWeapon += OnPickupWeapon;

            EventManager.AddListener<ObjectiveUpdateEvent>(OnObjectiveUpdateEvent);
        }

        void OnObjectiveUpdateEvent(ObjectiveUpdateEvent evt)
        {
            if (!string.IsNullOrEmpty(evt.NotificationText))
                CreateNotification(evt.NotificationText, DefaultColor);
        }

        public void OnTeamsKill(string killMissage, Color KillColor)
        {
            CreateNotification(killMissage, KillColor);
        }

        void OnPickupWeapon(WeaponController weaponController, int index)
        {
            if (index != 0)
                CreateNotification("Picked up weapon : " + weaponController.WeaponName, DefaultColor);
        }

        void OnUnlockJetpack(bool unlock)
        {
            CreateNotification("Jetpack unlocked", DefaultColor);
        }

        public void CreateNotification(string text, Color notificationColor)
        {
            GameObject notificationInstance = Instantiate(NotificationPrefab, NotificationPanel);
            Image notificationImage = notificationInstance.GetComponentInChildren<Image>();         
            notificationImage.color = notificationColor;
            notificationInstance.transform.SetSiblingIndex(0);

            NotificationToast toast = notificationInstance.GetComponent<NotificationToast>();
            if (toast)
            {
                toast.Initialize(text);
            }
        }

        void OnDestroy()
        {
            EventManager.RemoveListener<ObjectiveUpdateEvent>(OnObjectiveUpdateEvent);
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.FPS.Game;
using naumnek.Settings;
using Photon.Realtime;
using Photon.Pun.Demo.Asteroids;
using Unity.FPS.Gameplay;

//Скрипт отвечает за карточки выбора оружия(варианты выбора оружия, что предоставляется игроку на выбор после спавна его персонажа).

namespace naumnek.Menu
{
    public class SwitchItemMenu : MonoBehaviour
    {
        [Header("General")]
        [Tooltip("Root GameObject of the menu used to toggle its activation")]
        public GameObject MenuRoot;

        [Header("SwitchItems List Panel")]
        public GameObject ItemListContent;
        public GameObject WeaponsElementPrefab;
        public Button CloseItemMenuButton;
        public bool FixedPanel { get; private set; }
        public List<WeaponController> PaidWeapons { get; private set; }

        private Items[] ItemList;

        private Dictionary<string, Items> cachedRoomList;
        private Dictionary<string, GameObject> roomListEntries;
        private List<Transform> WeaponsElements;

        public SettingsManager SettingsManager { get; private set; }
        private PlayerController m_PlayerController;
        private Player m_InfoPlayer;
        private GameFlowManager FlowManager;
        private bool IsActive = true;
        private List<WeaponAttributes> m_WeaponsAttributes;

        private static SwitchItemMenu instance;
        public static SwitchItemMenu GetInstance() => instance;

        private void OnDestroy()
        {
            EventManager.RemoveListener<PlayerSpawnEvent>(OnPlayerSpawnEvent);
            EventManager.RemoveListener<PlayerDeathEvent>(OnPlayerDeathEvent);
        }

        private void Awake()
        {
            instance = this;
            m_WeaponsAttributes = new List<WeaponAttributes> ();

            EventManager.AddListener<PlayerSpawnEvent>(OnPlayerSpawnEvent);
            EventManager.AddListener<PlayerDeathEvent>(OnPlayerDeathEvent);
        }

        private void OnPlayerSpawnEvent(PlayerSpawnEvent evt)
        {
            Activate(evt.player);
        }

        private void OnPlayerDeathEvent(PlayerDeathEvent evt)
        {
            if (evt.Die)
            {
                ResetItems(ItemList);
                StartCoroutine(MaxWaitPaid());
            }
        }

        // Start is called before the first frame update
        private void Start()
        {
            cachedRoomList = new Dictionary<string, Items>();
            roomListEntries = new Dictionary<string, GameObject>();
            PaidWeapons = new List<WeaponController> { };
            WeaponsElements = new List<Transform> { };
        }

        private void Activate(PlayerController player)
        {
            m_PlayerController = player;
            SettingsManager = player.SettingsManager;
            m_InfoPlayer = player.PhotonPlayer;

            FlowManager = player.GameFlowManager;

            ItemList = SettingsManager.RequredItems;

            for (int i = 0; i < ItemList.Length; i++)
            {
                m_WeaponsAttributes.Add(ItemList[i].Attributes);
            }
            //WeaponsLists.Add(Instantiate(WeaponsListPrefab, ItemListContent.transform).transform);

            OnRoomListUpdate(ItemList);

            if (MenuRoot.activeSelf)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void OnRoomListUpdate(Items[] itemList)
        {
            ClearRoomListView();

            UpdateCachedRoomList(itemList);
            UpdateRoomListView();
        }

        private void ClearRoomListView()
        {
            foreach (GameObject entry in roomListEntries.Values)
            {
                DestroyImmediate(entry.gameObject);
            }

            roomListEntries.Clear();
        }

        public void ResetItems(Items[] roomList)
        {
            IsActive = true;

            OnRoomListUpdate(roomList);
            FlowManager.SetItemListSwitch(true);
        }

        private void UpdateCachedRoomList(Items[] roomList)
        {
            foreach (Items info in roomList)
            {
                // Remove menu from cached menu list if it got closed, became invisible or was marked as removed
                if (info.RemovedFromList || info.IsUnvisibly)
                {
                    if (cachedRoomList.ContainsKey(info.Name()))
                    {
                        cachedRoomList.Remove(info.Name());
                    }

                    continue;
                }

                // Update cached menu info
                if (cachedRoomList.ContainsKey(info.Name()))
                {
                    cachedRoomList[info.Name()] = info;
                }
                // Add new menu info to cache
                else
                {
                    cachedRoomList.Add(info.Name(), info);
                }
            }
        }

        private void UpdateRoomListView()
        {
            foreach (Items info in cachedRoomList.Values)
            {
                GameObject entry = Instantiate(WeaponsElementPrefab, ItemListContent.transform);
                entry.transform.localScale = Vector3.one;

                ItemListSwitch ItemListSwitch = entry.GetComponent<ItemListSwitch>();
                ItemListSwitch.ItemChooseButton.onClick.AddListener
                    (delegate () { FlowManager.AudioWeaponButtonClick.Play(); });

                ItemListSwitch.Initialize(info, this, IsActive);

                roomListEntries.Add(info.Name(), entry);
            }
        }

        public void GiveItemPlayer(Items itemInfo, ItemListSwitch itemSwitch)
        {
            if(!itemInfo.IsPaid && !itemInfo.IsBlocked)
            {
                if (IsActive)
                {
                    for (int i = 0; i < PaidWeapons.Count; i++)
                    {
                        m_PlayerController.PlayerWeaponsManager.RemoveWeapon(PaidWeapons[i]);
                        PaidWeapons.RemoveAt(i);
                    }
                    foreach (Items info in ItemList)
                    {
                        info.Reset();
                    }
                }

                PaidWeapons.Add(itemInfo.Weapon);
                CloseItemMenuButton.gameObject.SetActive(true);
                m_PlayerController.PlayerWeaponsManager.AddWeapon(itemInfo.Weapon.WeaponName);

                itemInfo.OnPaid();

                if (PaidWeapons.Count == AsteroidsGame.MAX_PAID_WEAPONS) FixedPanel = false;
                IsActive = PaidWeapons.Count < AsteroidsGame.MAX_PAID_WEAPONS;

                OnRoomListUpdate(ItemList);
            }
        }

        private IEnumerator MaxWaitPaid()
        {
            yield return new WaitForSeconds(AsteroidsGame.TIME_TO_BLOCK_SELECTION);
            IsActive = false;
            OnRoomListUpdate(ItemList);
        }

        public void CloseItemMenu()
        {
            FlowManager.SetItemListSwitch(false);
        }
    }
}
using ExitGames.Client.Photon;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using naumnek.FPS;
using naumnek.Settings;
using System.Linq;

namespace Photon.Pun.Demo.Asteroids
{
    public class LobbyMainPanel : MonoBehaviourPunCallbacks
    {
        [Header("Scene")]
        public string DefaultGameSceneName = "PolygonSciFiCity_Demo";
        public byte MinTeams = 2;
        public byte MaxTeams = 4;
        public byte MinPlayersPerTeam = 1;
        public byte MaxPlayersPerTeam = 10;

        [Header("Notification Panel")]
        public GameObject ComingSoonNotifucation;
        public float WaitToDisableNotification = 2.5f;

        [Header("Login Panel")]
        public GameObject LoginPanel;
        public InputField PlayerNameInput;

        [Header("Selection Panel")]
        public GameObject SelectionPanel;
        public Text AvatarNickName;
        public Text CoinsTextValue;
        public Text GemsTextValue;

        [Header("Options Panel")]
        public GameObject OptionsPanel;

        [Header("Maps Select")]
        public GameObject MapSelect;
        public MapSelect[] Maps;
        public Button ChangeMapButton;
        public Sprite DefaultChangeMapButton;
        public Sprite ActivateChangeMapButton;

        [Header("Customize Panel")]
        public Button SelectCharacter;
        public Button LeftArrow;
        public Button RightArrow;
        public GameObject NoneNFTText;
        public GameObject SwitchCharacterPanel;
        public Toggle IsCurrentSkinToggle;
        public SkinnedMeshRenderer CharacterSkinned;

        [Header("Create Room Panel")]
        public GameObject CreateRoomPanel;

        public GameObject CountTeams;
        public TMP_InputField RoomNameInputField;
        public Slider CountTeamsSlider;
        public TMP_Text CountTeamsValue;
        public Slider MaxPlayerSlider;
        public TMP_Text MaxPlayersValue;

        private float MaxPlayersMultiplier;

        [Header("Join Random Room Panel")]
        public GameObject JoinRandomRoomPanel;

        [Header("Room List Panel")]
        public GameObject RoomListPanel;

        public GameObject RoomListContent;
        public GameObject RoomListEntryPrefab;

        [Header("Inside Room Panel")]
        public Text Title;
        public GameObject InsideRoomPanel;
        public GameObject TeamRoomPanel;

        public Button PlayerReadyButton;
        public Button StartGameButton;
        public GameObject PlayerListEntryPrefab;
        public GameObject BotListEntryPrefab;



        private List<GameObject> m_GridPlayersTeams;
        private List<GameObject> m_PlayerListTeams;
        private Dictionary<string, RoomInfo> cachedRoomList;
        private Dictionary<string, GameObject> roomListEntries;
        private Dictionary<int, GameObject> playerListEntries;
        private GameObject[,] actorsListEntries;

        private string GameSceneName = "PolygonSciFiCity_Demo";
        private CustomizationInfo m_Customization;
        private SettingsManager m_SettingsManager;
        private static LobbyMainPanel instance;
        private Vector2 InsideRoomPanelSpacing;

        #region UNITY

        public static LobbyMainPanel GetInstance() => instance;

        public void Awake()
        {
            instance = this;
            PhotonNetwork.AutomaticallySyncScene = true;

            InsideRoomPanelSpacing = InsideRoomPanel.GetComponent<GridLayoutGroup>().spacing;

            cachedRoomList = new Dictionary<string, RoomInfo>();
            roomListEntries = new Dictionary<string, GameObject>();
            
            PlayerNameInput.text = "Player " + Random.Range(1000, 10000);

            GameSceneName = DefaultGameSceneName;

            if (MaxTeams == 2) CountTeams.SetActive(false);

            CountTeamsSlider.minValue = MinTeams;
            CountTeamsSlider.maxValue = MaxTeams;
            CountTeamsSlider.value = MinTeams;
            OnCountTeamsSlider();

            MaxPlayerSlider.minValue = MinPlayersPerTeam;
            MaxPlayerSlider.maxValue = MaxPlayersPerTeam;
            MaxPlayerSlider.value = MinPlayersPerTeam;
            OnMaxPlayersSlider();
        }

        private void Start()
        {
            m_SettingsManager = SettingsManager.GetInstance();
            m_Customization = m_SettingsManager.Customization;
            CharacterSkinned.sharedMesh = m_Customization.GetCurrentModel();
            CharacterSkinned.material = m_Customization.GetCurrentMaterial();

            string playerName = m_SettingsManager.PlayerInfo.Name;
            if (playerName != null)
            {
                CoinsTextValue.text = m_SettingsManager.PlayerInfo.Score.ToString();
                OnLogin(playerName);
            }
        }

        #endregion

        private System.Collections.IEnumerator WaitNotification()
        {
            ComingSoonNotifucation.SetActive(true);
            yield return new WaitForSeconds(WaitToDisableNotification);
            ComingSoonNotifucation.SetActive(false);
        }

        #region PUN CALLBACKS


        public override void OnConnectedToMaster()
        {
            this.SetActivePanel(SelectionPanel.name);
        }

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            ClearRoomListView();

            UpdateCachedRoomList(roomList);
            UpdateRoomListView();
        }

        public override void OnJoinedLobby()
        {
            // whenever this joins a new lobby, clear any previous room lists
            cachedRoomList.Clear();
            ClearRoomListView();
        }

        // note: when a client joins / creates a room, OnLeftLobby does not get called, even if the client was in a lobby before
        public override void OnLeftLobby()
        {
            cachedRoomList.Clear();
            ClearRoomListView();
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            SetActivePanel(SelectionPanel.name);
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            SetActivePanel(SelectionPanel.name);
        }

        public void OnOpenCustomizePanel()
        {
            SetActivePanel(SwitchCharacterPanel.name);
            CharacterSkinned.enabled = true;
        }

        public void OnOpenOptionsPanel()
        {
            SetActivePanel(OptionsPanel.name);
        }

        public void OnOpenMapSelectPanel()
        {
            SetActivePanel(MapSelect.name);
        }

        public void OnLeaveCustomizePanel()
        {
            CharacterSkinned.enabled = false;
            SetActivePanel(SelectionPanel.name);
        }

        public void OnBackSelectionPanel()
        {
            SetActivePanel(SelectionPanel.name);
        }

        public void OnSelectModelClicked()
        {
            m_Customization.SetCurrentIndexModel();
            IsCurrentSkinToggle.isOn = m_Customization.CheckIndexModel();
        }
        public void OnSelectMaterialClicked()
        {
            m_Customization.SetCurrentIndexMaterial();
            IsCurrentSkinToggle.isOn = m_Customization.CheckIndexMaterial();
        }

        public void OnLeftModelClicked()
        {
            CharacterSkinned.sharedMesh = m_Customization.LeftModel();
            IsCurrentSkinToggle.isOn = m_Customization.CheckIndexModel();
        }
        public void OnRightModelClicked()
        {
            CharacterSkinned.sharedMesh = m_Customization.RightModel();
            IsCurrentSkinToggle.isOn = m_Customization.CheckIndexModel();
        }

        public void OnLeftMaterialClicked()
        {
            CharacterSkinned.material = m_Customization.LeftMaterial();
            IsCurrentSkinToggle.isOn = m_Customization.CheckIndexMaterial();
        }
        public void OnRightMaterialClicked()
        {
            CharacterSkinned.material = m_Customization.RightMaterial();
            IsCurrentSkinToggle.isOn = m_Customization.CheckIndexMaterial();
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            string roomName = "Room " + Random.Range(1000, 10000);

            byte maxPlayers = (byte)(MinPlayersPerTeam * MinTeams);

            RoomOptions options = new RoomOptions {MaxPlayers = maxPlayers, CountTeams = MinTeams};

            PhotonNetwork.CreateRoom(roomName, options, null);
        }

        public override void OnJoinedRoom()
        {
            // joining (or entering) a room invalidates any cached lobby room list (even if LeaveLobby was not called due to just joining a room)
            cachedRoomList.Clear();

            Title.text = PhotonNetwork.CurrentRoom.Name;
            SetActivePanel(InsideRoomPanel.name);

            Room currentRoom = PhotonNetwork.CurrentRoom;

            int countTeams = currentRoom.CountTeams;
            int maxPlayers = currentRoom.MaxPlayers;
            int playerCount = currentRoom.PlayerCount;

            if (playerListEntries == null)
            {
                m_GridPlayersTeams = new List<GameObject> { };
                m_PlayerListTeams = new List<GameObject> { };
                playerListEntries = new Dictionary<int, GameObject>();
                actorsListEntries = new GameObject[countTeams, maxPlayers / countTeams];
            }

            GridLayoutGroup GridLayoutRoomPanel = InsideRoomPanel.GetComponent<GridLayoutGroup>();
            GridLayoutRoomPanel.constraintCount = countTeams;
            GridLayoutRoomPanel.spacing = new Vector2(InsideRoomPanelSpacing.x * (countTeams == 2 ? 8 : 1), InsideRoomPanelSpacing.y);

            for (int i = 0; i < countTeams; i++)
            {

                GameObject team = Instantiate(TeamRoomPanel, InsideRoomPanel.transform);
                team.transform.localScale = Vector3.one;

                TeamRoomPanel teamRoom = team.GetComponent<TeamRoomPanel>();

                teamRoom.TeamNumberText.text = "Team " + i;
                teamRoom.GridLayoutPlayers.constraintCount = countTeams == 2 ? 2 : 1;

                m_GridPlayersTeams.Add(teamRoom.GridPlayers);
                m_PlayerListTeams.Add(team);

                //Add bots
                for (int ii = 0; ii < currentRoom.MaxPlayers / currentRoom.CountTeams; ii++)
                {
                    GameObject entry = Instantiate(BotListEntryPrefab, teamRoom.GridPlayers.transform);
                    entry.transform.localScale = Vector3.one;

                    entry.GetComponent<BotListEntry>().Initialize(i, "Bot " + Random.Range(1000, 10000));
                    entry.GetComponent<BotListEntry>().SetBotReady(true);

                    actorsListEntries[i, ii] = entry;
                }

                //return if team none players
                if (!currentRoom.Teams.ContainsKey(i)) continue;

                List<Player> playersTeam = currentRoom.Teams[i];

                foreach(Player player in playersTeam)
                {
                    int indexPlayerInTeam = PhotonNetwork.CurrentRoom.IndexPlayerInTeam(player);

                    GameObject bot = actorsListEntries[player.TeamNumber, indexPlayerInTeam];
                    if (bot != null)
                    {
                        Destroy(bot);
                    }
                }

                //Add players
                for (int ii = 0; ii < playersTeam.Count; ii++)
                {
                    GameObject entry = Instantiate(PlayerListEntryPrefab, teamRoom.GridPlayers.transform);
                    entry.transform.localScale = Vector3.one;
                    entry.GetComponent<PlayerListEntry>().Initialize(playersTeam[ii].TeamNumber, playersTeam[ii].ActorNumber, playersTeam[ii].NickName);

                    if (playersTeam[ii].CustomProperties.TryGetValue(AsteroidsGame.PLAYER_READY, out object isPlayerReady))
                    {
                        entry.GetComponent<PlayerListEntry>().SetPlayerReady((bool)isPlayerReady);
                    }

                    playerListEntries.Add(playersTeam[ii].ActorNumber, entry);
                    actorsListEntries[i, ii] = entry;
                }
            }

            StartGameButton.gameObject.SetActive(CheckPlayersReady());

            Hashtable props = new Hashtable
            {
                {AsteroidsGame.PLAYER_LOADED_LEVEL, false}
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        private void AddBot()
        {

        }

        public override void OnLeftRoom()
        {
            SetActivePanel(SelectionPanel.name);


            for (int i = 0; i < m_PlayerListTeams.Count; i++)
            {
                Destroy(m_PlayerListTeams[i].gameObject);
            }
            for (int i = 0; i < m_GridPlayersTeams.Count; i++)
            {
                Destroy(m_GridPlayersTeams[i].gameObject);
            }
            foreach (GameObject entry in playerListEntries.Values)
            {
                Destroy(entry.gameObject);
            }

            playerListEntries.Clear();
            playerListEntries = null;
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            int indexPlayerInTeam = PhotonNetwork.CurrentRoom.IndexPlayerInTeam(newPlayer);

            GameObject bot = actorsListEntries[newPlayer.TeamNumber, indexPlayerInTeam];
            if(bot != null)
            {
                Destroy(bot);
            }
            Debug.Log(newPlayer.NickName + ": " + newPlayer.TeamNumber);
            GameObject entry = Instantiate(PlayerListEntryPrefab, m_GridPlayersTeams[newPlayer.TeamNumber].transform);
            entry.transform.localScale = Vector3.one;
            entry.GetComponent<PlayerListEntry>().Initialize(newPlayer.TeamNumber, newPlayer.ActorNumber, newPlayer.NickName);
            playerListEntries.Add(newPlayer.ActorNumber, entry);
            actorsListEntries[newPlayer.TeamNumber, indexPlayerInTeam] = entry;

            StartGameButton.gameObject.SetActive(CheckPlayersReady());
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            int indexPlayerInTeam = PhotonNetwork.CurrentRoom.IndexPlayerInTeam(otherPlayer);

            GameObject bot = actorsListEntries[otherPlayer.TeamNumber, indexPlayerInTeam];

            //Add bots
            GameObject entry = Instantiate(BotListEntryPrefab, m_GridPlayersTeams[otherPlayer.TeamNumber].transform);
            entry.transform.localScale = Vector3.one;

            entry.GetComponent<BotListEntry>().Initialize(otherPlayer.TeamNumber, "Bot " + Random.Range(1000, 10000));
            entry.GetComponent<BotListEntry>().SetBotReady(true);

            actorsListEntries[otherPlayer.TeamNumber, indexPlayerInTeam] = entry;

            Destroy(playerListEntries[otherPlayer.ActorNumber].gameObject);
            playerListEntries.Remove(otherPlayer.ActorNumber);

            StartGameButton.gameObject.SetActive(CheckPlayersReady());
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber == newMasterClient.ActorNumber)
            {
                StartGameButton.gameObject.SetActive(CheckPlayersReady());
            }
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            if (playerListEntries == null)
            {
                m_GridPlayersTeams = new List<GameObject> { };
                m_PlayerListTeams = new List<GameObject> { };
                playerListEntries = new Dictionary<int, GameObject>();
            }

            GameObject entry;
            if (playerListEntries.TryGetValue(targetPlayer.ActorNumber, out entry))
            {
                object isPlayerReady;
                if (changedProps.TryGetValue(AsteroidsGame.PLAYER_READY, out isPlayerReady))
                {
                    entry.GetComponent<PlayerListEntry>().SetPlayerReady((bool)isPlayerReady);
                }
            }

            StartGameButton.gameObject.SetActive(CheckPlayersReady());
        }

        #endregion

        #region UI CALLBACKS

        public void OnCallFreeCharactersText()
        {
            NoneNFTText.SetActive(false);
            SelectCharacter.interactable = true;
            LeftArrow.interactable = true;
            RightArrow.interactable = true;
            CharacterSkinned.enabled = true;
        }

        public void OnCallNoneNFTCharactersText()
        {
            NoneNFTText.SetActive(true);
            SelectCharacter.interactable = false;
            LeftArrow.interactable = false;
            RightArrow.interactable = false;
            CharacterSkinned.enabled = false;
        }

        public void OnCallComingSoonNotification()
        {
            StartCoroutine(WaitNotification());
        }

        public void OnDisableComingSoonNotification()
        {
            ComingSoonNotifucation.SetActive(false);
        }

        public void OnButtonRandomNickNameClicked()
        {
            PlayerNameInput.text = "Player " + Random.Range(1000, 10000);
        }

        public void OnBackButtonClicked()
        {
            if (PhotonNetwork.InLobby)
            {
                PhotonNetwork.LeaveLobby();
            }

            SetActivePanel(SelectionPanel.name);
        }

        public void OnCountTeamsSlider()
        {
            CountTeamsValue.text = CountTeamsSlider.value.ToString();
            OnMaxPlayersSlider();
        }

        public void OnMaxPlayersSlider()
        {
            MaxPlayersValue.text = (CountTeamsSlider.value * MaxPlayerSlider.value).ToString();
        }

        public void OnFastCreateRoomButtonClicked()
        {
            string roomName = "Room " + Random.Range(1000, 10000);

            byte countTeams = MinTeams;
            byte maxPlayers = (byte)(MinTeams * MinPlayersPerTeam);

            RoomOptions options = new RoomOptions { MaxPlayers = maxPlayers, CountTeams = countTeams, PlayerTtl = 10000 };

            PhotonNetwork.CreateRoom(roomName, options, null);
        }

        public void OnCreateRoomButtonClicked()
        {
            string roomName = RoomNameInputField.text;
            roomName = (roomName.Equals(string.Empty)) ? "Room " + Random.Range(1000, 10000) : roomName;

            byte countTeams;
            byte maxPlayers;

            byte.TryParse(CountTeamsValue.text, out countTeams);
            byte.TryParse(MaxPlayersValue.text, out maxPlayers);

            countTeams = (byte)Mathf.Clamp(countTeams, MinTeams, MaxTeams);
            maxPlayers = (byte)Mathf.Clamp(maxPlayers, countTeams * MinPlayersPerTeam, countTeams * MaxPlayersPerTeam);

            RoomOptions options = new RoomOptions {MaxPlayers = maxPlayers, CountTeams = countTeams, PlayerTtl = 10000 };
            options.MaxPlayers = maxPlayers;

            PhotonNetwork.CreateRoom(roomName, options, null);
        }

        public void OnSelectMap(MapSelect map)
        {
            for (int i = 0; i < Maps.Length; i++)
            {             
                Maps[i].SetStatePressed(Maps[i] == map);
            }

            bool availably = map.SceneName != "";
            GameSceneName = availably ? map.SceneName : GameSceneName;

            ChangeMapButton.interactable = availably;
            ChangeMapButton.GetComponent<Image>().sprite = availably ?
                ActivateChangeMapButton : DefaultChangeMapButton;
        }

        public void OnJoinRandomRoomButtonClicked()
        {
            SetActivePanel(JoinRandomRoomPanel.name);

            PhotonNetwork.JoinRandomRoom();
        }

        public void OnLeaveGameButtonClicked()
        {
            PhotonNetwork.LeaveRoom();
        }

        public void OnLoginButtonClicked()
        {
            string playerName = PlayerNameInput.text;

            if (!playerName.Equals(""))
            {
                AvatarNickName.text = playerName;
                PhotonNetwork.LocalPlayer.NickName = playerName;
                PhotonNetwork.ConnectUsingSettings();

                SettingsManager.GetInstance().PlayerInfo.SetPlayer(playerName);
            }
            else
            {
                Debug.LogError("Player Name is invalid.");
            }
        }

        public void OnLogin(string playerName)
        {
            PlayerNameInput.text = playerName;
            AvatarNickName.text = playerName;
            PhotonNetwork.LocalPlayer.NickName = playerName;
            PhotonNetwork.ConnectUsingSettings();
        }

        public void OnRoomListButtonClicked()
        {
            if (!PhotonNetwork.InLobby)
            {
                PhotonNetwork.JoinLobby();
            }

            SetActivePanel(RoomListPanel.name);
        }
        public void OnReadyGameButtonClicked()
        {
            PlayerReadyButton.gameObject.SetActive(false);
        }

        public void OnStartGameButtonClicked()
        {
            if (PlayerReadyButton.gameObject.activeSelf) return;
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;

            FileManager.LoadScene(GameSceneName, 0);
        }

        #endregion

        private bool CheckPlayersReady()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                return false;
            }

            foreach (Player p in PhotonNetwork.PlayerList)
            {
                object isPlayerReady;
                if (p.CustomProperties.TryGetValue(AsteroidsGame.PLAYER_READY, out isPlayerReady))
                {
                    if (!(bool) isPlayerReady)
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
        
        private void ClearRoomListView()
        {
            foreach (GameObject entry in roomListEntries.Values)
            {
                Destroy(entry.gameObject);
            }

            roomListEntries.Clear();
        }

        public void LocalPlayerPropertiesUpdated()
        {
            StartGameButton.gameObject.SetActive(CheckPlayersReady());
        }

        private void SetActivePanel(string activePanel)
        {
            OptionsPanel.SetActive(activePanel.Equals(OptionsPanel.name));
            LoginPanel.SetActive(activePanel.Equals(LoginPanel.name));
            SelectionPanel.SetActive(activePanel.Equals(SelectionPanel.name));
            MapSelect.SetActive(activePanel.Equals(MapSelect.name));          
            SwitchCharacterPanel.SetActive(activePanel.Equals(SwitchCharacterPanel.name));
            CreateRoomPanel.SetActive(activePanel.Equals(CreateRoomPanel.name));
            JoinRandomRoomPanel.SetActive(activePanel.Equals(JoinRandomRoomPanel.name));
            RoomListPanel.SetActive(activePanel.Equals(RoomListPanel.name));    // UI should call OnRoomListButtonClicked() to activate this
            InsideRoomPanel.SetActive(activePanel.Equals(InsideRoomPanel.name));

            if(CreateRoomPanel.activeSelf || JoinRandomRoomPanel.activeSelf)
                PlayerReadyButton.gameObject.SetActive(true);
        }

        private void UpdateCachedRoomList(List<RoomInfo> roomList)
        {
            foreach (RoomInfo info in roomList)
            {
                // Remove room from cached room list if it got closed, became invisible or was marked as removed
                if (!info.IsOpen || !info.IsVisible || info.RemovedFromList)
                {
                    if (cachedRoomList.ContainsKey(info.Name))
                    {
                        cachedRoomList.Remove(info.Name);
                    }

                    continue;
                }

                // Update cached room info
                if (cachedRoomList.ContainsKey(info.Name))
                {
                    cachedRoomList[info.Name] = info;
                }
                // Add new room info to cache
                else
                {
                    cachedRoomList.Add(info.Name, info);
                }
            }
        }

        private void UpdateRoomListView()
        {
            foreach (RoomInfo info in cachedRoomList.Values)
            {
                GameObject entry = Instantiate(RoomListEntryPrefab, RoomListContent.transform);
                entry.transform.SetParent(RoomListContent.transform);
                entry.transform.localScale = Vector3.one;
                entry.GetComponent<RoomListEntry>().Initialize(info.Name, (byte)info.PlayerCount, info.MaxPlayers);

                roomListEntries.Add(info.Name, entry);
            }
        }
    }
}
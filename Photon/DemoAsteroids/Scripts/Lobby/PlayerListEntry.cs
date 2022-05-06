// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlayerListEntry.cs" company="Exit Games GmbH">
//   Part of: Asteroid Demo,
// </copyright>
// <summary>
//  Player List Entry
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using TMPro;

using ExitGames.Client.Photon;
using Photon.Realtime;
using Photon.Pun.UtilityScripts;

namespace Photon.Pun.Demo.Asteroids
{
    public class PlayerListEntry : MonoBehaviour
    {
        [Header("UI References")]
        public TMP_Text PlayerNameText;

        public Image PlayerReadyImage;

        private int ownerId;
        private int teamNumber;
        private bool isPlayerReady;
        private LobbyMainPanel lobbyMainPanel;
        private Button PlayerReadyButton;

        #region UNITY

        public void OnEnable()
        {
            PlayerNumbering.OnPlayerNumberingChanged += OnPlayerNumberingChanged;
        }

        public void Start()
        {
            lobbyMainPanel = FindObjectOfType<LobbyMainPanel>();
            PlayerReadyButton = lobbyMainPanel.PlayerReadyButton;
            if (PhotonNetwork.LocalPlayer.ActorNumber != ownerId)
            {

            }
            else
            {
                Hashtable initialProps = new Hashtable() {{AsteroidsGame.PLAYER_READY, isPlayerReady}, {AsteroidsGame.PLAYER_LIVES, AsteroidsGame.PLAYER_MAX_LIVES}};
                PhotonNetwork.LocalPlayer.SetCustomProperties(initialProps);
                PhotonNetwork.LocalPlayer.SetScore(0);

                PlayerReadyButton.onClick.AddListener(() =>
                {
                    isPlayerReady = !isPlayerReady;
                    SetPlayerReady(isPlayerReady);

                    Hashtable props = new Hashtable() {{AsteroidsGame.PLAYER_READY, isPlayerReady}};
                    PhotonNetwork.LocalPlayer.SetCustomProperties(props);

                    if (PhotonNetwork.IsMasterClient)
                    {
                        lobbyMainPanel.LocalPlayerPropertiesUpdated();
                    }
                });
            }
        }

        public void OnDisable()
        {
            PlayerNumbering.OnPlayerNumberingChanged -= OnPlayerNumberingChanged;
        }

        #endregion

        public void Initialize(int playerTeam, int playerId, string playerName)
        {
            teamNumber = playerTeam;
            ownerId = playerId;
            PlayerNameText.text = playerName;
        }

        private void OnPlayerNumberingChanged()
        {
            Player localPlayer = PhotonNetwork.LocalPlayer;
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                if(p.TeamNumber == localPlayer.TeamNumber)
                {
                    PlayerNameText.color = Color.green;
                }
                else
                {
                    PlayerNameText.color = Color.red;
                }
                if (p == localPlayer)
                {
                    PlayerNameText.color = Color.yellow;
                }
            }
        }

        public void SetPlayerReady(bool playerReady)
        {
            //PlayerReadyButton.GetComponentInChildren<Text>().text = playerReady ? "Ready!" : "Ready?";
            PlayerReadyImage.enabled = playerReady;
        }
    }
}
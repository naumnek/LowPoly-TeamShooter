
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using ExitGames.Client.Photon;
using Photon.Realtime;
using Photon.Pun.UtilityScripts;
using Photon.Pun.Demo.Asteroids;

public class BotListEntry : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text BotNameText;

    public Image PlayerReadyImage;

    private int teamNumber;
    private LobbyMainPanel lobbyMainPanel;
    private Button PlayerReadyButton;

    #region UNITY

    public void Start()
    {
        lobbyMainPanel = FindObjectOfType<LobbyMainPanel>();
        PlayerReadyButton = lobbyMainPanel.PlayerReadyButton;
    }

    #endregion

    public void Initialize(int botTeam, string botName)
    {
        teamNumber = botTeam;
        BotNameText.text = botName;
    }

    public void SetBotReady(bool playerReady)
    {
        //PlayerReadyButton.GetComponentInChildren<Text>().text = playerReady ? "Ready!" : "Ready?";
        PlayerReadyImage.enabled = playerReady;
    }
}

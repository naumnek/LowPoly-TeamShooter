using UnityEngine;
using TMPro;
using Unity.FPS.Game;
using Photon.Pun;
using Photon.Realtime;
using Photon.Pun.Demo.Asteroids;
using System.Collections.Generic;
using UnityEngine.UI;

namespace Unity.FPS.UI
{
    public class TeamsKillCounter : MonoBehaviour
    {
        public NotificationHUDManager NotificationHUDManager;

        [Tooltip("The text field displaying the team counter")]
        public Text PlayerNickName;
        public Text PlayerKill; 
        public Text FriendlyCounter;
        public Text EnemyCounter;
        public Color FriendlyTeamNotificationColor;
        public Color EnemyTeamNotificationColor;

        private List<int> TeamsKillScores = new List<int> { };
        private int FriendlyAffiliation;
        private int Kill;
        private Player player;

        private void OnDestroy()
        {
            EventManager.RemoveListener<KillEvent>(OnKillEvent);
        }

        private void Start()
        {
            player = PhotonNetwork.LocalPlayer;
            PlayerNickName.text = player.NickName;

            int countTeams = PhotonNetwork.CurrentRoom.CountTeams + 1;
            for(int i = 0;i < countTeams; i++)
            {
                TeamsKillScores.Add(0);
            }

            FriendlyAffiliation = PhotonNetwork.LocalPlayer.TeamNumber;

            EventManager.AddListener<KillEvent>(OnKillEvent);
        }

        public void OnKillEvent(KillEvent evt)
        {
            if(evt.killer.PhotonPlayer == player)
            {
                Kill++;
                PlayerKill.text = Kill.ToString();
            }

            int team = evt.killer.Affiliation;

            TeamsKillScores[team] += AsteroidsGame.TEAM_SCORE_FOR_KILL;

            if(team == FriendlyAffiliation)
                FriendlyCounter.text = TeamsKillScores[team].ToString();         
            else
                EnemyCounter.text = TeamsKillScores[team].ToString();

            NotificationHUDManager.OnTeamsKill(evt.killed.Nickname + " killed by " + evt.killer.Nickname,
                team == FriendlyAffiliation ? FriendlyTeamNotificationColor : EnemyTeamNotificationColor);
        }
    }
}

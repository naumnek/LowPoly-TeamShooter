// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AsteroidsGameManager.cs" company="Exit Games GmbH">
//   Part of: Asteroid demo
// </copyright>
// <summary>
//  Game Manager for the Asteroid Demo
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections;
using StarterAssets;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.FPS.Game;
using naumnek.Settings;
using naumnek.FPS;
using System.Linq;
using Unity.FPS.AI;

using Photon.Realtime;
using Photon.Pun.UtilityScripts;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Photon.Pun.Demo.Asteroids
{
    public class AsteroidsGameManager : MonoBehaviourPunCallbacks
    {
        [Header("General")]
        public string GameSceneName = "DemoAsteroids-LobbyScene";

        public Transform[] Teams;
        public Dictionary<int, Transform[]> SpawnpointsTeams;

        public GameObject PlayerPrefab;

        public Transform[] SpawnpointPlayers;

        public GameObject[] EnemyPrefab;

        public Transform[] SpawnpointEnemy;

        public PatrolPathTeams[] PatrolPathTeams;

        public GameObject[] LootsPrefab;

        public Transform[] SpawnpointLoots;

        [Header("Others")]
        public static AsteroidsGameManager Instance = null;

        public TMP_Text[] ResultMatchInfoText;

        public Text GameInfoText;

        public float WaitForEndGame = 10f;

        private System.Random ran = new System.Random();

        private List<Transform> LootsOfScene = new List<Transform> { };

        private List<Transform> BotsOfScene = new List<Transform> { };

        private List<Actor> ActorsOfScene = new List<Actor> { };

        private Dictionary<Actor, int> ActorScores = new Dictionary<Actor, int> { };
        private Dictionary<int, int> TeamsScores = new Dictionary<int, int> { };

        private bool IsWin = false;

        private Timer m_Timer;

        private int WinTeam;
        private string TopPlayer;
        private int currentWinScore;
        private bool ExitEvent = false;

        //public GameObject[] AsteroidPrefabs;

        #region UNITY

        public void Awake()
        {
            Instance = this;
        }

        public override void OnEnable()
        {
            base.OnEnable();

            CountdownTimer.OnCountdownTimerHasExpired += OnCountdownTimerIsExpired;
        }

        public void Start()
        {
            m_Timer = Timer.GetInstance();

            Hashtable props = new Hashtable
            {
                {AsteroidsGame.PLAYER_LOADED_LEVEL, true}
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            SpawnpointsTeams = new Dictionary<int, Transform[]> { };

            EventManager.AddListener<KillEvent>(OnKillEvent);
            EventManager.AddListener<ExitEvent>(OnExitEvent);
        }

        private void OnDestroy()
        {
            EventManager.RemoveListener<KillEvent>(OnKillEvent);
            EventManager.RemoveListener<ExitEvent>(OnExitEvent);
        }

        public override void OnDisable()
        {
            base.OnDisable();

            CountdownTimer.OnCountdownTimerHasExpired -= OnCountdownTimerIsExpired;
        }

        #endregion

        #region COROUTINES

        private void SpawnBot(int team, int positionTeam)
        {
            Transform spawn = SpawnpointsTeams[team][positionTeam];
            GameObject bot = PhotonNetwork.InstantiateRoomObject(EnemyPrefab[ran.Next(1, EnemyPrefab.Length) - 1].name,
                spawn.position, spawn.rotation, 0);

            Actor actor = bot.GetComponent<Actor>();
            EnemyController controller = bot.GetComponent<EnemyController>();

            actor.SetAffiliation(controller, team);
            controller.SetSpawn(spawn);

            controller.SetPatrolPath(PatrolPathTeams[team].PatrolPathBots, positionTeam);

            BotsOfScene.Add(bot.transform);
            ActorsOfScene.Add(actor);
        }

        private void StartSpawnBots()
        {
            Room currentRoom = PhotonNetwork.CurrentRoom;
            int placeInTeam = currentRoom.MaxPlayers / currentRoom.CountTeams;

            for (byte i = 0; i < currentRoom.CountTeams; i++)
            {
                for (int ii = currentRoom.CountPlayersInTeam(i); ii < placeInTeam; ii++)
                {
                    SpawnBot(i, ii);
                }
            }
        }

        private IEnumerator SpawnLoots()
        {
            while (true)
            {
                yield return new WaitForSeconds(AsteroidsGame.LOOTS_RESPAWN_TIME);
                for (int i = 0; i < LootsOfScene.Count; i++)
                {
                    if(LootsOfScene[i] != null) PhotonNetwork.Destroy(LootsOfScene[i].gameObject);
                    LootsOfScene.RemoveAt(i);
                }

                for (int i = 0; i < SpawnpointLoots.Length; i++)
                {

                    LootsOfScene.Add(PhotonNetwork.InstantiateRoomObject
                        (LootsPrefab[ran.Next(1, LootsPrefab.Length) - 1].name,                        
                        SpawnpointLoots[i].position, SpawnpointLoots[i].rotation, 0).transform);
                }
            }
        }

        private IEnumerator EndOfGame()
        {
            if (PhotonNetwork.LocalPlayer.TeamNumber == WinTeam 
                || TeamsScores.Any(t => t.Value == 0)) IsWin = true;
            if (ExitEvent) IsWin = false;

            EndGameEvent evt = Events.EndGameEvent;
            evt.win = IsWin;
            EventManager.Broadcast(evt);

            float timer = WaitForEndGame;

            while (timer > 0.0f)
            {
                for(int i = 0; i < ResultMatchInfoText.Length;i++)
                {
                    ResultMatchInfoText[i].text = string.Format("Returning to login screen in {0} seconds.", timer.ToString("n2").Split(',')[0]);
                }
                //InfoText.color = PhotonNetwork.LocalPlayer.TeamNumber == WinTeam ? Color.green : Color.red;

                yield return new WaitForEndOfFrame();

                timer -= Time.deltaTime;
            }

            PhotonNetwork.LeaveRoom();

            FileManager.EndGame(GameSceneName, IsWin ? "Win" : "Lose");
        }

        #endregion

        #region PUN CALLBACKS

        public override void OnDisconnected(DisconnectCause cause)
        {
            //UnityEngine.SceneManagement.SceneManager.LoadScene(GameSceneName);
        }

        public override void OnLeftRoom()
        {
            PhotonNetwork.Disconnect();
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber == newMasterClient.ActorNumber)
            {
                //StartCoroutine(SpawnEnemy());
            }
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                SpawnBot(otherPlayer.TeamNumber, PhotonNetwork.CurrentRoom.IndexPlayerInTeam(otherPlayer));
            }
            //CheckEndOfGame();
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            if (changedProps.ContainsKey(AsteroidsGame.PLAYER_LIVES))
            {
                //CheckEndOfGame();
                return;
            }

            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }


            // if there was no countdown yet, the master client (this one) waits until everyone loaded the level and sets a timer start
            int startTimestamp;
            bool startTimeIsSet = CountdownTimer.TryGetStartTime(out startTimestamp);

            if (changedProps.ContainsKey(AsteroidsGame.PLAYER_LOADED_LEVEL))
            {
                if (CheckAllPlayerLoadedLevel())
                {
                    if (!startTimeIsSet)
                    {
                        CountdownTimer.SetStartTime();
                    }
                }
                else
                {
                    // not all players loaded yet. wait:
                    GameInfoText.text = "Waiting for other players...";
                }
            }
        
        }

        #endregion


        // called by OnCountdownTimerIsExpired() when the timer ended
        private void StartGame()
        {
            Debug.Log("StartGame!");

            // on rejoin, we have to figure out if the spaceship exists or not
            // if this is a rejoin (the ship is already network instantiated and will be setup via event) we don't need to call PN.Instantiate



            float angularStart = (360.0f / PhotonNetwork.CurrentRoom.PlayerCount) * PhotonNetwork.LocalPlayer.GetPlayerNumber();
            float x = 20.0f * Mathf.Sin(angularStart * Mathf.Deg2Rad);
            float z = 20.0f * Mathf.Cos(angularStart * Mathf.Deg2Rad);
            Vector3 position = new Vector3(x, 0.0f, z);
            Quaternion rotation = Quaternion.Euler(0.0f, angularStart, 0.0f);

            Room currentRoom = PhotonNetwork.CurrentRoom;
            Player player = PhotonNetwork.LocalPlayer;
            Player[] AllPlayer = PhotonNetwork.PlayerList;

            int indexPosition = ran.Next(1, SpawnpointPlayers.Length) - 1;


            for (int i = 0; i < Teams.Length; i++)
            {
                Transform[] points = Teams[i].GetComponentsInChildren<Transform>();
                points.Where(p => p != Teams[i]);

                SpawnpointsTeams[i] = new Transform[] { };
                for (int ii = 0; ii < points.Length; ii++)
                {
                    SpawnpointsTeams[i] = points;
                }
            }

            int team = player.TeamNumber;
            int index = currentRoom.IndexPlayerInTeam(player);

            //check index array spawnpoints from null. If null - spawn random spawnpoint
            Transform spawn = SpawnpointsTeams[team][index] != null ? 
                SpawnpointsTeams[team][index] : 
                SpawnpointsTeams[team][ran.Next(0, index)];

            GameObject ObjectPlayer = PhotonNetwork.Instantiate(PlayerPrefab.name, spawn.position, spawn.rotation, 0);

            PlayerController controller = ObjectPlayer.GetComponent<PlayerController>();

            controller.SetSpawn(spawn);
            /*
            for (int i = 0; i < AllPlayer.Length; i++)
            {
                if(AllPlayer[i] == player)
                {
                    ObjectPlayer.transform.position = SpawnpointPlayers[i].position;
                    ObjectPlayer.transform.rotation = SpawnpointPlayers[i].rotation;
                }
            }
            */

            Debug.Log("Player: " + player.NickName + " - N:" + player.GetPlayerNumber() + "/" + PhotonNetwork.CurrentRoom.IndexPlayerInTeam(player));

            if (PhotonNetwork.IsMasterClient)
            {

                //StartCoroutine(SpawnEnemy());
                StartSpawnBots();
                //StartCoroutine(SpawnLoots());
            }
        }

        private bool CheckAllPlayerLoadedLevel()
        {
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                object playerLoadedLevel;

                if (p.CustomProperties.TryGetValue(AsteroidsGame.PLAYER_LOADED_LEVEL, out playerLoadedLevel))
                {
                    if ((bool) playerLoadedLevel)
                    {
                        continue;
                    }
                }

                return false;
            }

            return true;
        }

        private bool EndGame = false;

        private void OnKillEvent(KillEvent evt)
        {
            if (EndGame) return;

            Actor killer = evt.killer;

            if (!ActorScores.ContainsKey(killer)) ActorScores[killer] = 0;
            if (!TeamsScores.ContainsKey(killer.Affiliation)) TeamsScores[killer.Affiliation] = 0;

            ActorScores[killer] += AsteroidsGame.TEAM_SCORE_FOR_KILL;
            TeamsScores[killer.Affiliation] += AsteroidsGame.TEAM_SCORE_FOR_KILL;

            if (ActorScores[killer] > currentWinScore)
            {
                TopPlayer = killer.Nickname;
                currentWinScore = ActorScores[killer];
            }
            if (TeamsScores[killer.Affiliation] >= AsteroidsGame.NUMBER_KILLS_TO_TEAM_WIN)
            {
                EndGame = true;
                WinTeam = killer.Affiliation;
                if (PhotonNetwork.IsMasterClient)
                {
                    StopAllCoroutines();
                }

                StartCoroutine(EndOfGame());
            }
        }

        private void Update()
        {
            if (EndGame) return;

            if (m_Timer.seconds >= AsteroidsGame.MAX_TIMES_MATCH_WIN)
            {
                EndGame = true;
                int topScore = 0;
                for(int i = 0; i< TeamsScores.Count; i++)
                {
                    if(TeamsScores[i] > topScore)
                    {
                        topScore = TeamsScores[i];
                        WinTeam = TeamsScores[i];
                    }
                }
                if (PhotonNetwork.IsMasterClient)
                {
                    StopAllCoroutines();
                }

                StartCoroutine(EndOfGame());
            }
        }

        private void OnExitEvent(ExitEvent evt)
        {
            ExitEvent = true;
            if (PhotonNetwork.IsMasterClient)
            {
                StopAllCoroutines();
            }

            StartCoroutine(EndOfGame());
        }

        private void OnCountdownTimerIsExpired()
        {
            StartGame();
        }
    }

    [System.Serializable]
    public class PatrolPathTeams
    {
        public PatrolPath[] PatrolPathBots;
    }
}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using naumnek.FPS;
using System.Collections.Generic;
using Unity.FPS.Gameplay;
using Photon.Pun;
using Photon.Realtime;
using naumnek.Menu;
using naumnek.Settings;
using Photon.Pun.UtilityScripts;
using Photon.Pun.Demo.Asteroids;

namespace Unity.FPS.Game
{
    public class GameFlowManager : MonoBehaviour
    {
        [Header("Others")]
        public PlayerInputHandler m_PlayerInputHandler;
        [Header("Statictics")]
        public GameObject StaticticsPanel;
        public Text ValueResult;
        public Text ValueKilling;
        public Text ValueTotal;
        public GameObject WinResult;
        public GameObject LoseResult;
        [Header("UI")]
        public GameObject TabMenu;
        public GameObject ItemListSwitch;
        public GameObject FeedbackFlashCanvas;
        public GameObject HUD;
        public GameObject SpectatorInfo;
        [Header("Parameters")]
        public AudioSource AudioWeaponButtonClick;
        public AudioSource AudioButtonClick;
        public AudioSource MusicSource;
        public List<AudioClip> AllMusics = new List<AudioClip>();
        int NumberMusic;
        System.Random ran = new System.Random();

        [Tooltip("Duration of the fade-to-black at the end of the game")]
        public float EndSceneLoadDelay = 3f;


        [Tooltip("The canvas group of the fade-to-black screen")]
        public CanvasGroup EndGameFadeCanvasGroup;

        [Header("Win")][Tooltip("This string has to be the name of the scene you want to load when winning")]
        public string WinSceneName = "MainMenu";

        [Tooltip("Duration of delay before the fade-to-black, if winning")]
        public float DelayBeforeFadeToBlack = 4f;

        [Tooltip("Win game message")]
        public string WinGameMessage;
        [Tooltip("Duration of delay before the win message")]
        public float DelayBeforeWinMessage = 2f;

        [Tooltip("Sound played on win")] public AudioClip VictorySound;

        [Header("Lose")][Tooltip("This string has to be the name of the scene you want to load when losing")]
        public string LoseSceneName = "MainMenu";

        string Result = "None";

        public bool GameIsEnding { get; private set; }

        float m_TimeLoadEndGameScene;
        string m_SceneToLoad;
        private bool ServerPause = true;
        private SwitchItemMenu m_SwitchItemMenu;
        private PlayerController m_PlayerController;
        private SettingsManager m_SettingsManager;

        private static GameFlowManager instance;
        public static GameFlowManager GetInstance() => instance;

        void OnDestroy()
        {
            EventManager.RemoveListener<PlayerSpawnEvent>(OnPlayerSpawnEvent);
            EventManager.RemoveListener<SwitchMusicEvent>(OnSwitchMusic);
            EventManager.RemoveListener<GamePauseEvent>(OnGamePause);
            EventManager.RemoveListener<EndGameEvent>(OnEndGame);
            EventManager.RemoveListener<ExitEvent>(OnExitMenu);
            EventManager.RemoveListener<AllObjectivesCompletedEvent>(OnAllObjectivesCompleted);
            //EventManager.RemoveListener<PlayerDeathEvent>(OnPlayerDeath);
        }

        void Awake()
        {
            instance = this;

            EventManager.AddListener<PlayerSpawnEvent>(OnPlayerSpawnEvent);
            EventManager.AddListener<SwitchMusicEvent>(OnSwitchMusic);
            EventManager.AddListener<GamePauseEvent>(OnGamePause);
            EventManager.AddListener<EndGameEvent>(OnEndGame);
            EventManager.AddListener<ExitEvent>(OnExitMenu);
            EventManager.AddListener<AllObjectivesCompletedEvent>(OnAllObjectivesCompleted);

            //EventManager.AddListener<PlayerDeathEvent>(OnPlayerDeath);

            PlayerController trigger = FindObjectOfType<PlayerController>();
            if (trigger != null && trigger.PhotonView.IsMine)
            {
                Activate(trigger);
            }
        }

        private void Start()
        {
            m_SwitchItemMenu = SwitchItemMenu.GetInstance();
        }

        private void OnPlayerSpawnEvent(PlayerSpawnEvent evt)
        {
            Activate(evt.player);
        }

        private void Activate(PlayerController player)
        {
            m_PlayerController = player;
            m_PlayerInputHandler = player.PlayerInputHandler;
            m_SettingsManager = player.SettingsManager;
            AudioUtility.SetMasterVolume(1);
            NumberMusic = ran.Next(0, AllMusics.Count);
            int l = AllMusics.Count;
            string random = "/";
            for (int i = 0; i < 10; i++)
            {
                random += ran.Next(0, l);
            }

            SetMusic();
            MusicSource.Pause();

            SetItemListSwitch(true);
            ServerPause = false;
        }

        void OnGamePause(GamePauseEvent evt)
        {
            CursorState(evt.MenuPause || evt.ServerPause);
            if (evt.MenuPause) MusicSource.Pause();
            else MusicSource.Pause();
        }

        void OnSwitchMusic(SwitchMusicEvent evt)
        {
            switch (evt.SwitchMusic)
            {
                case ("left"):
                    NumberMusic--;
                    SetMusic();
                    break;
                case ("right"):
                    NumberMusic++;
                    SetMusic();
                    break;
            }
        }

        void SetMusic()
        {
            if (NumberMusic < 0) NumberMusic = AllMusics.Count - 1;
            if (NumberMusic > AllMusics.Count - 1) NumberMusic = 0;
            MusicSource.clip = AllMusics[NumberMusic];
            SwitchMusicEvent evt = Events.SwitchMusicEvent;
            evt.Music = AllMusics[NumberMusic];
            evt.SwitchMusic = "none";
            EventManager.Broadcast(evt);
        }

        public void SetActiveSpectatorInfo()
        {
            SpectatorInfo.SetActive(true);
        }

        public void SetActiveGameHUD(bool active)
        {
            HUD.SetActive(active);
            FeedbackFlashCanvas.SetActive(active);
        }

        public void SetItemListSwitch(bool active)
        {
            float distanceFromSpawn = 
                Vector3.Distance(m_PlayerController.Spawnpoint.position, m_PlayerController.transform.position);

            //Debug.Log((m_SwitchItemMenu.FixedPanel && distanceFromSpawn > AsteroidsGame.DISTANCE_OPEN_SELECT_WEAPON) + "/" + (m_SwitchItemMenu.FixedPanel));
            
            if (m_SwitchItemMenu.FixedPanel && active == false 
                || distanceFromSpawn > AsteroidsGame.DISTANCE_OPEN_SELECT_WEAPON
                || TabMenu.activeSelf) 
                return;

            SetActiveGameHUD(!active);

            ItemListSwitch.SetActive(active);

            CursorState(active);
            SetPauseMenuActivation(active);
        }

        public void SetTabMenu(bool active)
        {
            if (m_SwitchItemMenu.FixedPanel) return;

            if (ItemListSwitch.activeSelf)
            {
                SetItemListSwitch(false);
                return;
            }

            SetActiveGameHUD(!active);

            TabMenu.SetActive(active);

            CursorState(active);
            SetPauseMenuActivation(active);
        }

        private void CursorState(bool state)
        {
            Cursor.visible = state;
            if (state) Cursor.lockState = CursorLockMode.None;
            else Cursor.lockState = CursorLockMode.Locked;
        }

        void SetPauseMenuActivation(bool pause)
        {
            GamePauseEvent evt = Events.GamePauseEvent;
            evt.MenuPause = pause;
            EventManager.Broadcast(evt);
        }

        void Update()
        {
            if (GameIsEnding) // GameIsEnding
            {
                float timeRatio = 1 - (m_TimeLoadEndGameScene - Time.time) / EndSceneLoadDelay;
                //EndGameFadeCanvasGroup.alpha = timeRatio;

                //AudioUtility.SetMasterVolume(1 - timeRatio);

                // See if it's time to load the end scene (after the delay)
                if (Time.time >= m_TimeLoadEndGameScene)
                {
                    GameIsEnding = false;
                    //FileManager.EndGame(WinSceneName, Result);
                }
            }
            if (m_PlayerInputHandler != null && !GameIsEnding)
            {
                if ((TabMenu.activeSelf || ItemListSwitch.activeSelf) && m_PlayerInputHandler.click)
                {
                    m_PlayerInputHandler.click = false;
                    CursorState(true);
                }

                if (m_PlayerInputHandler.tab)
                {
                    m_PlayerInputHandler.tab = false; 
                    SetTabMenu(!TabMenu.activeSelf);
                }

                if (m_PlayerInputHandler.SelectWeapon)
                {
                    m_PlayerInputHandler.SelectWeapon = false; 
                    SetItemListSwitch(!ItemListSwitch.activeSelf);
                }
            }
        }
        void OnExitMenu(ExitEvent evt)
        {
            Result = "Exit";
            ResultEndGame(false);
        }
        void OnEndGame(EndGameEvent evt)
        {
            ResultEndGame(evt.win);
        }

        void OnAllObjectivesCompleted(AllObjectivesCompletedEvent evt)
        {
            ResultEndGame(true); 
        }


        void ResultEndGame(bool win)
        {
            GamePauseEvent gpe = Events.GamePauseEvent;
            gpe.MenuPause = true;
            gpe.ServerPause = true;
            EventManager.Broadcast(gpe);

            SetActiveGameHUD(false);
            SetTabMenu(false);
            // unlocks the cursor before leaving the scene, to be able to click buttons
            //Cursor.lockState = CursorLockMode.None;
            // Cursor.visible = true;

            // Remember that we need to load the appropriate end scene after a delay
            GameIsEnding = true;
            //EndGameFadeCanvasGroup.gameObject.SetActive(true);
            if (win)
            {
                WinResult.SetActive(true);
                Result = "Win";

                m_SceneToLoad = WinSceneName;
                m_TimeLoadEndGameScene = Time.time + EndSceneLoadDelay + DelayBeforeFadeToBlack;

                MusicSource.Pause();

                // play a sound on win
                var audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.clip = VictorySound;
                audioSource.playOnAwake = false;
                audioSource.outputAudioMixerGroup = AudioUtility.GetAudioGroup(AudioUtility.AudioGroups.HUDVictory);
                audioSource.PlayScheduled(AudioSettings.dspTime + DelayBeforeWinMessage);


                // create a game message
                //var message = Instantiate(WinGameMessagePrefab).GetComponent<DisplayMessage>();
                //if (message)
                //{
                //    message.delayBeforeShowing = delayBeforeWinMessage;
                //    message.GetComponent<Transform>().SetAsLastSibling();
                //}

                //DisplayMessageEvent displayMessage = Events.DisplayMessageEvent;
                //displayMessage.Message = WinGameMessage;
                //displayMessage.DelayBeforeDisplay = DelayBeforeWinMessage;
                //EventManager.Broadcast(displayMessage);
            }
            else
            {
                LoseResult.SetActive(true);
                Result = "Lose";

                m_SceneToLoad = LoseSceneName;
                m_TimeLoadEndGameScene = Time.time + EndSceneLoadDelay + DelayBeforeFadeToBlack;
            }

            StaticticsPanel.SetActive(true);

            int resultValue = 0;
            switch (Result)
            {
                case "Win":
                    resultValue = AsteroidsGame.PLAYER_SCORE_FOR_WIN;
                    break;
                case "Lose":
                    resultValue = AsteroidsGame.PLAYER_SCORE_FOR_LOSE;
                    break;
                case "Exit":
                    resultValue = AsteroidsGame.PLAYER_SCORE_FOR_EXIT;
                    break;
            }

            int killing = m_PlayerController.PhotonPlayer.GetScore();
            ValueResult.text = Result + " +(" + resultValue + ")";
            ValueKilling.text = killing.ToString() + " (+" + (killing * AsteroidsGame.PLAYER_SCORE_FOR_KILL).ToString() + ")";
            int total = (resultValue + killing * AsteroidsGame.PLAYER_SCORE_FOR_KILL);
            ValueTotal.text = "+" + total.ToString();

            m_SettingsManager.PlayerInfo.AddPlayerScore(total);
            //PhotonNetwork.LeaveLobby();
        }
    }
}
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using naumnek.FPS;
using UnityEngine.Audio;
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
using UnityEngine.InputSystem;
using Photon.Pun;
#endif
using StarterAssets;

namespace Unity.FPS.UI
{
    public class InGameMenuManager : MonoBehaviour
    {
        [Tooltip("Root GameObject of the menu used to toggle its activation")]
        public GameObject TabMenu;

        [Tooltip("Master volume when menu is open")] [Range(0.001f, 1f)]
        public float VolumeWhenMenuOpen = 0.5f;

        public Slider MusicVolume;
        public TMP_InputField SwitchMusic;
        public AudioMixer MusicMixer;

        [Tooltip("Slider component for look sensitivity")]
        public Slider LookSensitivity;

        [Tooltip("Toggle component for shadows")]
        public Toggle ShadowsToggle;

        [Tooltip("Toggle component for framerate display")]
        public Toggle FramerateToggle;

        private PlayerInputHandler m_PlayerInputsHandler;
        private Health m_PlayerHealth;
        private FramerateCounter m_FramerateCounter;

        private GameFlowManager m_GameFlowManager;
        public PlayerController PlayerController;
        private bool ServerPause = false;
        private bool MenuPause = false;

        void Awake()
        {
            EventManager.AddListener<GamePauseEvent>(OnGamePauseEvent);
            EventManager.AddListener<PlayerSpawnEvent>(OnPlayerSpawnEvent);
            EventManager.AddListener<SwitchMusicEvent>(OnSwitchMusic);
        }

        private void OnDestroy()
        {
            EventManager.RemoveListener<PlayerSpawnEvent>(OnPlayerSpawnEvent);
            EventManager.RemoveListener<GamePauseEvent>(OnGamePauseEvent);
            EventManager.RemoveListener<SwitchMusicEvent>(OnSwitchMusic);

        }

        private void OnGamePauseEvent(GamePauseEvent evt)
        {
            ServerPause = evt.ServerPause;
            MenuPause = evt.MenuPause;
        }

        private void OnPlayerSpawnEvent(PlayerSpawnEvent evt)
        {
            Activate(evt.player);
        }

        private void Activate(PlayerController player)
        {
            m_PlayerInputsHandler = PlayerInputHandler.GetInstance();

            m_PlayerHealth = player.Health;

            m_FramerateCounter = FindObjectOfType<FramerateCounter>();
            DebugUtility.HandleErrorIfNullFindObject<FramerateCounter, InGameMenuManager>(m_FramerateCounter, this);

            ShadowsToggle.isOn = QualitySettings.shadows != ShadowQuality.Disable;
            FramerateToggle.isOn = m_FramerateCounter.UIText.gameObject.activeSelf;

            m_GameFlowManager = player.GameFlowManager;

            if (PlayerPrefs.GetString("CheckSave") == "yes") LoadSettings();

            ServerPause = false;
        }

        private void LoadSettings() //загружаем информацию из файлов
        {
            MusicVolume.value = PlayerPrefs.GetFloat("MusicVolume");
            MusicMixer.SetFloat("musicVolume", -(25 - MusicVolume.value));

            LookSensitivity.value = PlayerPrefs.GetFloat("LookSensitivity");
            m_PlayerInputsHandler.LookSensitivity = LookSensitivity.value / 50;
        }

        public void Exit()
        {
            //SetPauseMenuActivation(false);

            EventManager.Broadcast(Events.ExitEvent);
        }

        public void ClosePauseMenu()
        {
            m_GameFlowManager.SetTabMenu(false);
        }

        public void SetMusicVolume(Text valueText) //установка громкости звука
        {
            valueText.text = (MusicVolume.value * 4).ToString();
            MusicMixer.SetFloat("musicVolume", -(25 - MusicVolume.value));
        }

        void OnSwitchMusic(SwitchMusicEvent evt)
        {
            if(SwitchMusic != null) SwitchMusic.text = evt.Music.name;
        }

        public void SetSwitchMusic(string Switch) //установка громкости звука
        {
            if (SwitchMusic != null)
            {
                SwitchMusicEvent evt = Events.SwitchMusicEvent;
                evt.SwitchMusic = Switch;
                EventManager.Broadcast(evt);
            }
        }



        public void SetLookSensitivity(Text value)
        {
            value.text = LookSensitivity.value.ToString();
            m_PlayerInputsHandler.LookSensitivity = LookSensitivity.value / 50;
        }

        public void SetShadows(Toggle toggle)
        {
            QualitySettings.shadows = toggle.isOn ? ShadowQuality.All : ShadowQuality.Disable;
        }

        public void SetFramerateCounter(Toggle toggle)
        {
            m_FramerateCounter.UIText.gameObject.SetActive(toggle.isOn);
        }
    }
}
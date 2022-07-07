using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.FPS.UI
{
    public class PlayerHealthBar : MonoBehaviour
    {
        public Text PlayerHealthAmount;
        public Slider PlayerHealth;

        [Tooltip("Image component dispplaying current health")]
        public Image HealthFillImage;

        Health m_PlayerHealth;
        private bool Pause = true;

        private void OnDestroy()
        {
            EventManager.RemoveListener<PlayerSpawnEvent>(OnPlayerSpawnEvent);
            EventManager.RemoveListener<GamePauseEvent>(OnGamePauseEvent);
        }

        private void Start()
        {
            EventManager.AddListener<GamePauseEvent>(OnGamePauseEvent);
            EventManager.AddListener<PlayerSpawnEvent>(OnPlayerSpawnEvent);
        }

        private void OnGamePauseEvent(GamePauseEvent evt)
        {
            Pause = evt.ServerPause;
        }

        private void OnPlayerSpawnEvent(PlayerSpawnEvent evt)
        {
            Activate(evt.player);
        }

        private void Activate(PlayerController player)
        {
            PlayerCharacterController playerCharacterController =
                GameObject.FindObjectOfType<PlayerCharacterController>();
            DebugUtility.HandleErrorIfNullFindObject<PlayerCharacterController, PlayerHealthBar>(
                playerCharacterController, this);

            m_PlayerHealth = player.Health;
            DebugUtility.HandleErrorIfNullGetComponent<Health, PlayerHealthBar>(m_PlayerHealth, this,
                playerCharacterController.gameObject);

            Pause = false;
        }

        void Update()
        {
            if (!Pause)
            {
                // update health bar value
                PlayerHealth.value = m_PlayerHealth.CurrentHealth / m_PlayerHealth.MaxHealth;
                PlayerHealthAmount.text = m_PlayerHealth.CurrentHealth.ToString().Split('.')[0] + "/" + m_PlayerHealth.MaxHealth.ToString();
                //HealthFillImage.fillAmount = m_PlayerHealth.CurrentHealth / m_PlayerHealth.MaxHealth;
            }
        }
    }
}
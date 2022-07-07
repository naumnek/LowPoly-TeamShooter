using Unity.FPS.Game;
using UnityEngine;
using Photon.Pun;

namespace Unity.FPS.Gameplay
{
    public class HealthPickup : Pickup
    {
        [Header("Parameters")] [Tooltip("Amount of health to heal on pickup")]
        public float HealAmount;

        protected override void OnPicked(PlayerController player)
        {
            Health playerHealth = player.Health;
            if (playerHealth && playerHealth.CanPickup())
            {
                playerHealth.Heal(HealAmount);
                PlayPickupFeedback();
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }
}
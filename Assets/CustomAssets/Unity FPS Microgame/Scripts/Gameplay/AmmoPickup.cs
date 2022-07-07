using Unity.FPS.Game;
using UnityEngine;
using Photon.Pun;

namespace Unity.FPS.Gameplay
{
    public class AmmoPickup : Pickup
    {
        [Tooltip("Weapon those bullets are for")]
        public WeaponController Weapon;

        [Tooltip("Number of bullets the player gets")]
        public int BulletCount = 10;

        protected override void OnPicked(PlayerController byPlayer)
        {
            PlayerWeaponsManager playerWeaponsManager = byPlayer.PlayerWeaponsManager;
            if (playerWeaponsManager)
            {
                WeaponController weapon = playerWeaponsManager.HasWeapon(Weapon);

                if (weapon != null && weapon.CarriedAmmo < weapon.MaxAmmo)
                {
                    weapon.AddCarriablePhysicalBullets(BulletCount);

                    AmmoPickupEvent evt = Events.AmmoPickupEvent;
                    evt.Weapon = weapon;
                    EventManager.Broadcast(evt);

                    PlayPickupFeedback();
                    PhotonNetwork.Destroy(gameObject);
                }
            }
        }
    }
}

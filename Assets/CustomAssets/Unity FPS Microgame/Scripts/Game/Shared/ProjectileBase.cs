using UnityEngine;
using UnityEngine.Events;
using Photon.Realtime;

namespace Unity.FPS.Game
{
    public abstract class ProjectileBase : MonoBehaviour
    {
        public WeaponController Weapon { get; private set; }

        public Transform Owner;
        public Actor OwnerActor { get; private set; }

        public Vector3 InitialPosition { get; private set; }
        public Vector3 InitialDirection { get; private set; }
        public Vector3 InheritedMuzzleVelocity { get; private set; }
        public float InitialCharge { get; private set; }

        public UnityAction OnShoot;

        public void Shoot(WeaponController controller)
        {
            Weapon = controller;
            Owner = controller.Owner;
            OwnerActor = Owner.GetComponent<Actor>();
            InitialPosition = transform.position;
            InitialDirection = transform.forward;
            InheritedMuzzleVelocity = controller.MuzzleWorldVelocity;
            InitialCharge = controller.CurrentCharge;

            OnShoot?.Invoke();
        }
    }
}
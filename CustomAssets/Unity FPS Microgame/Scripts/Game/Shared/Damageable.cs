using UnityEngine;

namespace Unity.FPS.Game
{
    public class Damageable : MonoBehaviour
    {
        [Tooltip("Multiplier to apply to the received damage")]
        public float DamageMultiplier = 1f;

        [Range(0, 1)] [Tooltip("Multiplier to apply to self damage")]
        public float SensibilityToSelfdamage = 0.5f;

        public Health Health { get; private set; }
        public Actor Actor { get; private set; }

        void Awake()
        {
            Actor = GetComponent<Actor>();
            Health = GetComponent<Health>();

            // find the component either at the same level, or higher in the hierarchy
            if (!Actor) Actor = GetComponentInParent<Actor>();
            if (!Health) Health = GetComponentInParent<Health>();
        }

        public void InflictDamage(float damage, bool isExplosionDamage, GameObject damageSource, out bool Dead)
        {
            Dead = false;
            if (Health)
            {
                var totalDamage = damage;

                // skip the crit multiplier if it's from an explosion
                if (!isExplosionDamage)
                {
                    totalDamage *= DamageMultiplier;
                }

                // potentially reduce damages if inflicted by self
                if (Health.gameObject == damageSource)
                {
                    totalDamage *= SensibilityToSelfdamage;
                }

                // apply the damages
                Health.TakeDamage(totalDamage, damageSource, out Dead);
            }
        }
    }
}
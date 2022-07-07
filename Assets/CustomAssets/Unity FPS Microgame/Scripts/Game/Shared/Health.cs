using UnityEngine;
using UnityEngine.Events;
using Photon.Pun;
using System.Collections.Generic;
using System.Collections;
using Photon.Pun.Demo.Asteroids;

//Скрипт отвечает за здоровье персонажа(бота или игрока)
//Тоесть реагирует на/принимает события на урон.

namespace Unity.FPS.Game
{
    public class Health : MonoBehaviour
    {
        [Tooltip("Maximum amount of health")] public float MaxHealth = 100f;
        [Tooltip("Heal by passive regeneration")] public float PassiveHeal = 5f;

        [Tooltip("Health ratio at which the critical health vignette starts appearing")]
        public float CriticalHealthRatio = 0.3f;
        [Tooltip("Multiplier to apply to the received damage")]
        public float DamageMultiplier = 1f;
        [Range(0, 1)]
        [Tooltip("Multiplier to apply to self damage")]
        public float SensibilityToSelfdamage = 0.5f;

        public UnityAction<float, GameObject> OnDamaged;
        public UnityAction<float> OnHealed;
        public UnityAction OnDie;

        public float CurrentHealth { get; set; }
        public bool CanPickup() => CurrentHealth < MaxHealth;

        public float GetRatio() => CurrentHealth / MaxHealth;
        public bool IsCritical() => GetRatio() <= CriticalHealthRatio;

        public PhotonView photonView;

        public bool invulnerable { get; private set; } = false;
        public void DisableInvulnerable()
        {
            if (photonView.IsMine) invulnerable = false;
        }

        public bool IsDead;
        public CauseDeath IsCauseDeath;
        public enum CauseDeath
        {
            Player,
            Enemy,
            Environment,
        }

        void Start()
        {
            CurrentHealth = MaxHealth;
            photonView = GetComponent<PhotonView>();
            StartCoroutine(PassiveRegeneration());
        }

        private IEnumerator PassiveRegeneration()
        {
            while (true)
            {
                yield return new WaitForSeconds(AsteroidsGame.PLAYER_PASSIVE_REGENERATION_INTERVAL);
                if (!IsDead) Heal(AsteroidsGame.PLAYER_PASSIVE_REGENERATION_AMOUNT);
            }
        }

        public void Heal(float healAmount)
        {
            float healthBefore = CurrentHealth;
            CurrentHealth += healAmount;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, MaxHealth);

            // call OnHeal action
            float trueHealAmount = CurrentHealth - healthBefore;
            if (trueHealAmount > 0f)
            {
                OnHealed?.Invoke(trueHealAmount);
            }
        }

        public void InflictDamage(float damage, bool isExplosionDamage, GameObject damageSource, out bool Dead)
        {
            var totalDamage = damage;

            // skip the crit multiplier if it's from an explosion
            if (!isExplosionDamage)
            {
                totalDamage *= DamageMultiplier;
            }

            // potentially reduce damages if inflicted by self
            if (gameObject == damageSource)
            {
                totalDamage *= SensibilityToSelfdamage;
            }

            // apply the damages
            TakeDamage(totalDamage, damageSource, out Dead);
        }

        public void TakeDamage(float damage, GameObject damageSource, out bool Dead)
        {
            if (invulnerable) damage = 0.01f;
            float healthBefore = CurrentHealth;
            CurrentHealth -= damage;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, MaxHealth);

            // call OnDamage action
            float trueDamageAmount = healthBefore - CurrentHealth;
            if (trueDamageAmount > 0f)
            {
                OnDamaged?.Invoke(trueDamageAmount, damageSource);
            }

            HandleDeath(out Dead);
        }

        public void Kill()
        {
            CurrentHealth = 0f;
            bool Dead;

            //call OnDamage action
            OnDamaged?.Invoke(MaxHealth, null);
            HandleDeath(out Dead);
        }

        void HandleDeath(out bool Dead)
        {
            Dead = false;
            // call OnDie action
            if (!IsDead && CurrentHealth <= 0f)
            {
                IsDead = true;
                Dead = IsDead;
                invulnerable = true;
                OnDie?.Invoke();
            }
        }
    }
}
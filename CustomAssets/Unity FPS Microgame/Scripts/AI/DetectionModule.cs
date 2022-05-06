using System.Linq;
using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Unity.FPS.AI
{
    public class DetectionModule : MonoBehaviour
    {
        public enum ControllMode
        {
            Drone,
            Humanoid
        }
        public ControllMode controllMode = ControllMode.Drone;

        public LayerMask IgnoryLayor;

        [Tooltip("The point representing the source of target-detection raycasts for the enemy AI")]
        public Transform DetectionSourcePoint;

        public bool IgnoreDetectionRange = true;
        public bool IsEnemySeeing = true;
        public bool IsSeeingThroughWalls = true;
        [Tooltip("The max distance at which the enemy can see targets")]
        public float DetectionRange = 100f;

        [Tooltip("Time before an enemy abandons a known target that it can't see anymore")]
        public float KnownTargetTimeout = 4f;

        [Tooltip("Optional animator for OnShoot animations")]
        public Animator Animator;

        public UnityAction onDetectedTarget;
        public UnityAction onLostTarget;

        public Actor KnownDetectedTarget { get; private set; }
        public bool IsTargetInAttackRange { get; private set; }
        public bool IsSeeingTarget { get; private set; }
        public bool HadKnownTarget { get; private set; }

        protected float TimeLastSeenTarget = Mathf.NegativeInfinity;

        ActorsManager m_ActorsManager;
        EnemyMobile m_EnemyMobile;

        const string k_AnimAttackParameter = "Attack";
        const string k_AnimOnDamagedParameter = "OnDamaged";

        private float m_StartDetectionRange;
        private Actor m_Actor;

        private void OnDestroy()
        {
            EventManager.RemoveListener<PlayerSpawnEvent>(OnPlayerSpawnEvent);
        }

        protected virtual void Start()
        {
            m_StartDetectionRange = DetectionRange;

            m_ActorsManager = ActorsManager.GetInstance();
            EventManager.AddListener<PlayerSpawnEvent>(OnPlayerSpawnEvent);
        }

        private void OnPlayerSpawnEvent(PlayerSpawnEvent evt)
        {

        }

        private bool FirstActivate = false;
        private void Activate(Actor actor)
        {
            FirstActivate = true;
            m_Actor = actor;
            m_EnemyMobile = m_Actor.GetComponent<EnemyMobile>();

            if (IsEnemySeeing)
            {             
                KnownDetectedTarget = m_ActorsManager.GetEnemyActor(actor);
                KnownDetectedTarget = m_ActorsManager.GetEnemyActors(actor).OrderBy(x => Vector3.Distance(transform.position, KnownDetectedTarget.AimPoint.position)).FirstOrDefault();
            }
            if (IgnoreDetectionRange) DetectionRange = 1000;
        }

        public void ResetDetectionTarget()
        {
            KnownDetectedTarget = null;
        }
        public float HasDistanceNearestForwardActor(float angleActors)
        {
            if (m_ActorsManager == null) m_ActorsManager = ActorsManager.GetInstance();

            List<Actor> actors = new List<Actor> ();
            actors.AddRange(m_ActorsManager.GetFriendlyActors(m_Actor).Where(a => a != m_Actor));

            for(int i = 0; i < actors.Count; i++)
            {
                float angle = HasAngleActor(actors[i]);

                if (angle > angleActors)
                {
                    actors.RemoveAt(i);
                }
            }

            return GetNearestActor(actors);
        }
        public float GetNearestActor(List<Actor> actors)
        {
            Actor nearestActor = actors.First();

            float minDistance = GetDistanceFromActor(nearestActor);
            float currentMinDistance = minDistance;
            for (int i = 0; i < actors.Count; i++)
            {
                currentMinDistance = GetDistanceFromActor(actors[i]);

                if (currentMinDistance < minDistance)
                {
                    minDistance = currentMinDistance;
                    nearestActor = actors[i];
                }
            }

            //Debug.Log(m_Actor.Nickname + " check distance " + nearestActor.Nickname + ": " + minDistance);
            return minDistance;
        }

        public float HasAngleActor(Actor actor)
        {
            if (m_Actor == null) return 0;
            float angle = Vector3.Angle(DetectionSourcePoint.forward, actor.AimPoint.position - DetectionSourcePoint.position);
            //Debug.Log(actor.Nickname + " angle " + angle.ToString() + " " + m_Actor.Nickname);

            return angle;
        }

        public float GetDistanceFromActor(Actor actor)
        {
            if (m_Actor == null) return 0;
            float distance = Vector3.Distance(DetectionSourcePoint.position, actor.AimPoint.position);
            //Debug.Log(actor.Nickname + " distance: " + distance + " / " + m_Actor.Nickname);

            return distance; 
        }

        public virtual void HandleTargetDetection(Actor actor, Collider[] selfColliders)
        {
            if (!FirstActivate) Activate(actor);

            // Handle known target detection timeout
            if (KnownDetectedTarget && !IsSeeingTarget && (Time.time - TimeLastSeenTarget) > KnownTargetTimeout)
            {
                KnownDetectedTarget = null;
            }

            IsSeeingTarget = false;

            // Find the closest visible hostile actor
            float sqrDetectionRange = DetectionRange * DetectionRange;
            float closestSqrDistance = Mathf.Infinity;
            foreach (Actor otherActor in m_ActorsManager.GetEnemyActors(actor))
            {
                if (otherActor.Affiliation != actor.Affiliation)
                {
                    float sqrDistance = (otherActor.transform.position - DetectionSourcePoint.position).sqrMagnitude;
                    if (sqrDistance < sqrDetectionRange && sqrDistance < closestSqrDistance)
                    {
                        // Check for obstructions
                        RaycastHit[] hits = Physics.RaycastAll(DetectionSourcePoint.position,
                            (otherActor.AimPoint.position - DetectionSourcePoint.position).normalized, DetectionRange,
                           ~IgnoryLayor);
                        RaycastHit closestValidHit = new RaycastHit();
                        closestValidHit.distance = Mathf.Infinity;
                        bool foundValidHit = false;
                        foreach (var hit in hits)
                        {
                            if (!selfColliders.Contains(hit.collider) && hit.distance < closestValidHit.distance)
                            {
                                closestValidHit = hit;
                                foundValidHit = true;
                            }
                        }

                        if (foundValidHit || IsSeeingThroughWalls)
                        {
                            Actor hitActor = foundValidHit ?
                                closestValidHit.collider.GetComponent<Actor>() :
                                m_ActorsManager.GetEnemyActors(actor).OrderBy
                                (x => Vector3.Distance(otherActor.transform.position, x.transform.position)).FirstOrDefault();

                            if (hitActor == otherActor)
                            {
                                IsSeeingTarget = true;
                                closestSqrDistance = sqrDistance;

                                TimeLastSeenTarget = Time.time;
                                KnownDetectedTarget = otherActor;
                            }
                        }
                    }
                }
            }

            IsTargetInAttackRange = KnownDetectedTarget != null &&
                                    Vector3.Distance(transform.position, KnownDetectedTarget.transform.position) <=
                                    m_EnemyMobile.DunamicAttackRange;

            // Detection events
            if (!HadKnownTarget &&
                KnownDetectedTarget != null)
            {
                OnDetect();
            }

            if (HadKnownTarget &&
                KnownDetectedTarget == null)
            {
                OnLostTarget();
            }

            // Remember if we already knew a target (for next frame)
            HadKnownTarget = KnownDetectedTarget != null;
        }

        public virtual void OnLostTarget() => onLostTarget?.Invoke();

        public virtual void OnDetect() => onDetectedTarget?.Invoke();


        private bool WaitOnDamaged;
        public virtual void OnDamaged(GameObject damageSource)
        {
            TimeLastSeenTarget = Time.time;

            if (controllMode == ControllMode.Drone && Animator)
            {
                //Animator.SetTrigger(k_AnimOnDamagedParameter);
            }
            if (WaitOnDamaged) return;
            StartCoroutine(WaitSetTargetDamaged(damageSource));
        }

        private IEnumerator WaitSetTargetDamaged(GameObject damageSource)
        {
            WaitOnDamaged = true;
            yield return new WaitForSeconds(KnownTargetTimeout);
            KnownDetectedTarget = damageSource.GetComponent<ProjectileBase>().OwnerActor;
            WaitOnDamaged = false;

        }

        public virtual void OnAttack()
        {
            if (controllMode == ControllMode.Drone && Animator)
            {
                //Animator.SetTrigger(k_AnimAttackParameter);
            }
        }
    }
}
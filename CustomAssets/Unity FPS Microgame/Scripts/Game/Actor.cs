using UnityEngine;
using Photon.Realtime;
using Photon.Pun;
using Unity.FPS.AI;
using System.Collections.Generic;

namespace Unity.FPS.Game
{
    // This class contains general information describing an actor (player or enemies).
    // It is mostly used for AI detection logic and determining if an actor is friend or foe
    public class Actor : MonoBehaviour
    {
        public string Nickname { get; private set; } = "Alex";

        /// <summary>
        /// Represents the affiliation (or team) of the actor. Actors of the same affiliation are friendly to each other
        /// </summary>
        /// 

        [Tooltip("Represents point where other actors will aim when they attack this actor")]
        public Transform AimPoint;

        //public string[] ListNickName = new string[] {"Alex", "Steve", "Aloha", "LegalDepartment", "Bot1234" };

        public int Affiliation { get; private set; } = 0;
        public List<int> EnemysAffiliation { get; private set; } = new List<int> { };
        public Player PhotonPlayer { get; private set; }

        public PhotonView PhotonView { get; private set; }

        public Health Health { get; private set; }
        public EnemyController EnemyController { get; private set; }
        public PlayerController PlayerController { get; private set; }

        public int CountTeams { get; private set; }

        private ActorsManager m_ActorsManager;

        public int GetRandomEnemyAffilition()
        {
            return EnemysAffiliation[Random.Range(0, EnemysAffiliation.Count)];
        }

        public void OnHitEnemy(bool hit)
        {
            if (PlayerController)
            {
                PlayerController.PlayerWeaponsManager.OnHitEnemy(hit);
            }
            if (EnemyController)
            {
                EnemyController.EnemyMobile.OnHitEnemy(hit);
            }
        }

        public void SetAffiliation(EnemyController bot, int Team)
        {
            EnemyController = GetComponent<EnemyController>();
            if (EnemyController == bot)
            {
                CountTeams = PhotonNetwork.CurrentRoom.CountTeams;
                Affiliation = Team;
                Nickname = "Bot " + Random.Range(1000, 10000);

                for (int i = 0; i < CountTeams; i++)
                {
                    if (i != Affiliation) EnemysAffiliation.Add(i);
                }
            }
        }

        public void SetAffiliation(PlayerController player)
        {
            PlayerController = GetComponent<PlayerController>();
            if (PlayerController == player)
            {
                CountTeams = PhotonNetwork.CurrentRoom.CountTeams;
                PhotonView = player.PhotonView;
                PhotonPlayer = PhotonView.Owner;
                Affiliation = PhotonPlayer.TeamNumber;
                Nickname = PhotonPlayer.NickName;

                for (int i = 0; i < CountTeams; i++)
                {
                    if (i != Affiliation) EnemysAffiliation.Add(i);
                }
            }
        }

        private void Start()
        {
            m_ActorsManager = ActorsManager.GetInstance();
            Health = GetComponent<Health>();

            m_ActorsManager.AddActor(this);
        }


        void OnDestroy()
        {
            // Unregister as an actor
            if (m_ActorsManager)
            {
                m_ActorsManager.Actors.Remove(this);
            }
        }
    }
}
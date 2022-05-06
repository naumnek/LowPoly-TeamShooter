using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Unity.FPS.Game
{
    public class ActorsManager : MonoBehaviour
    {
        public List<Actor> Actors { get; private set; }

        private Dictionary<int, List<Actor>> PlayerTeams = new Dictionary<int, List<Actor>> { };

        private static ActorsManager instance;

        public static ActorsManager GetInstance() => instance;

        public List<Actor> GetEnemyActors(Actor actor)
        {
            List<Actor> EnemyPlayers = new List<Actor>();
            for (int i = 0; i < actor.EnemysAffiliation.Count; i++)
            {
                EnemyPlayers.AddRange(
                    PlayerTeams[actor.EnemysAffiliation[i]]);
            }

            return EnemyPlayers;
        }
        public List<Actor> GetFriendlyActors(Actor actor)
        {
            List<Actor> FriendlyPlayers = PlayerTeams[actor.Affiliation];
            return FriendlyPlayers;
        }

        public Actor GetEnemyActor(Actor actor)
        {
            return GetEnemyActors(actor).FirstOrDefault();
        }

        public void AddActor(Actor actor)
        {
            Actors.Add(actor);
            if (!PlayerTeams.ContainsKey(actor.Affiliation)) PlayerTeams[actor.Affiliation] = new List<Actor> { };

            PlayerTeams[actor.Affiliation].Add(actor);
        }

        void Awake()
        {
            instance = this;
            Actors = new List<Actor>();
        }
    }
}

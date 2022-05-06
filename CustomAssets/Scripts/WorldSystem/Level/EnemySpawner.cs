using System.Collections.Generic;
using System.Linq;
using System.Collections;
using Unity.FPS.Game;
using UnityEngine;
using LowLevelGenerator.Scripts;
using System;
using Unity.FPS.AI;

namespace naumnek.FPS
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Completed Wave Message")]
        public string Title = "Wave completed";
        public float DelayBeforeDisplay = 1f;
        [Header("General")]
        public int MaxWaveLevel = 1;
        public float DelayStartWave = 5f;
        public ActorsManager ActorsManager;
        [Tooltip("Represents the affiliation (or team) of the actor. Actors of the same affiliation are friendly to each other")]
        public int EnemyAffiliation = 0;
        public int EnemyCount = 1;
        [Tooltip("The max distance at which the enemy can see targets")]
        public float DetectionRange = 200f;
        public Transform EnemyContainer;
        [Header("Options")]
        public EnemyDrop[] Items;
        public Spawner[] Spawners;
        [Header("Spawnpoint Options")]
        public int min_enemy = 1;
        public int max_enemy = 3;
        public int minX_enemy = 1;
        public int maxX_enemy = 8;
        public int minY_enemy = 1;
        public int maxY_enemy = 1;
        public int minZ_enemy = 1;
        public int maxZ_enemy = 8;
        [Header("Player")]
        public Transform emptyPlayer;
        public Transform m_BodyPlayer;
        public GameObject prefabPlayer;
        private List<SpawnSection> Spawnpoints = new List<SpawnSection>();
        private List<Transform> listEnemys = new List<Transform>();
        private System.Random ran = new System.Random();
        private static EnemySpawner instance;
        private int LastWaveLevel = 0;

        private void Start()
        {
            EventManager.AddListener<WaveCompletedEvent>(EnemysWaveLevel);
            EventManager.AddListener<EndLoadGameSceneEvent>(OnEndLoadGameScene);

            Spawnpoints.AddRange(GetComponentsInChildren<SpawnSection>());
            //List<int> Rangs = new List<int>();
            //for (int i = 0; i < SpawnSections.Count; i++) Rangs.Add(SpawnSections[i].Rank);

        }

        public string GetTitle() => Title + " - " + LastWaveLevel + "/" + MaxWaveLevel;

        private void OnEndLoadGameScene(EndLoadGameSceneEvent evt)
        {
            StartCoroutine(Initialize());
        }

        private void OnDestroy()
        {
            EventManager.RemoveListener<WaveCompletedEvent>(EnemysWaveLevel);
            EventManager.RemoveListener<EndLoadGameSceneEvent>(OnEndLoadGameScene);
        }

        private void EnemysWaveLevel(WaveCompletedEvent evt)
        {
            if (evt.BossKillCount >= MaxWaveLevel)
            {
                EventManager.Broadcast(Events.AllWaveCompletedEvent);
            }
            if (evt.BossKillCount > LastWaveLevel)
            {
                LastWaveLevel++;
                ActivateStartSpawnEnemys();
            }
        }

        private IEnumerator Initialize()
        {
            yield return new WaitForSeconds(DelayStartWave);
            ActivateStartSpawnEnemys();
        }

        private void ActivateStartSpawnEnemys()
        {
            List<int> l1 = new List<int> { 1, 2, 3 };
            int[] l2 = { 1, 2, 3 };
            for (int i = 0; i < Spawners.Length; i++)
            {
                if (Spawners[i].Rank == LastWaveLevel)
                {
                    Spawnpoints[ran.Next(0, Spawnpoints.Count)].ActivateEnemys(Spawners[i]);
                }
            }
        }

        public static EnemySpawner GetEnemySpawner()
        {
            return instance.GetComponent<EnemySpawner>();
        }

        /*public static void Initialize(Transform point)
        {
            instance.SetSpawnerPlayer(point);
            instance.InitializeSpawnSections();
        }

        private void InitializeSpawnSections()
        {
            int spawnRang = 0;
            for(int i = 0; i < SpawnSections.Count; i++)
            {
                if(SpawnSections[i].Rang == spawnRang)
                {

                }
            }
        }*/

        public static Transform ActivateSpawner(SpawnSection spawnerSection, Spawner spawner)
        {
            return instance.SpawnEnemys(spawnerSection, spawner);
        }
        public void SetSpawnerPlayer(Transform point)
        {
            listEnemys.Add(m_BodyPlayer);
            m_BodyPlayer.position = point.position;
            m_BodyPlayer.SetParent(EnemyContainer);
            m_BodyPlayer.gameObject.SetActive(true);
        }

        private Transform SpawnEnemys(SpawnSection spawnSection, Spawner spawner)
        {
            return NewEnemy(spawnSection, spawner);
        }

        public bool PlayerDetection(Transform spawnSection)
        {
            // Find the closest visible hostile actor
            float sqrDetectionRange = DetectionRange * DetectionRange;
            float closestSqrDistance = Mathf.Infinity;
            foreach (Actor otherActor in ActorsManager.Actors)
            {
                if (otherActor.Affiliation != EnemyAffiliation)
                {
                    float sqrDistance = (m_BodyPlayer.transform.position - spawnSection.position).sqrMagnitude;
                    if (sqrDistance < sqrDetectionRange && sqrDistance < closestSqrDistance)
                    {
                        // Check for obstructions
                        RaycastHit[] hits = Physics.RaycastAll(spawnSection.position,
                            (otherActor.AimPoint.position - spawnSection.position).normalized, DetectionRange,
                            -1, QueryTriggerInteraction.Ignore);
                        RaycastHit closestValidHit = new RaycastHit();
                        closestValidHit.distance = Mathf.Infinity;
                        bool foundValidHit = false;
                        foreach (var hit in hits)
                        {
                            if (hit.distance < closestValidHit.distance)
                            {
                                closestValidHit = hit;
                                foundValidHit = true;
                            }
                        }

                        if (foundValidHit)
                        {
                            Actor hitActor = closestValidHit.collider.GetComponentInParent<Actor>();
                            if (hitActor == otherActor)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        private Transform NewEnemy(SpawnSection spawnSection, Spawner spawner)
        {
            Transform sectionPosition = spawnSection.SpawnpointEnemy;
            if (PlayerDetection(spawnSection.transform))
            {
                print(spawnSection.name);
                List<SpawnSection> sections = new List<SpawnSection>();
                sections.AddRange(Spawnpoints.Where(s => s != spawnSection));
                for (int i = 0; i < sections.Count; i++)
                {
                    if(!PlayerDetection(sections[i].transform)) sectionPosition = sections[i].SpawnpointEnemy;
                }              
            }
            GameObject enemy = spawner.PrefabEnemys[ran.Next(0, spawner.PrefabEnemys.Count - 1)];
            float x = sectionPosition.position.x + ran.Next(minX_enemy, maxX_enemy);
            float y = sectionPosition.position.y + ran.Next(minY_enemy, maxY_enemy);
            float z = sectionPosition.position.z + ran.Next(minZ_enemy, maxZ_enemy);


            //print("Spawn: " + enemy.name + " - " + target.name + " - " + x + " - " + y + " - " + z + " - )");

            Transform obj = GameLogic.SpawnObject(enemy, sectionPosition, x, y, z);
            listEnemys.Add(obj);

            EnemyController enemyController = obj.GetComponent<EnemyController>();
            enemyController.ParentSpawnSection = spawnSection;

            EnemyDrop[] requiredDrop = Array.FindAll(Items, i => i.EnemyRank == enemyController.Rank);
            if (enemyController.Boss) requiredDrop = Array.FindAll(requiredDrop, r => r.DropStatus == EnemyDrop.Status.BossDrop);
            else requiredDrop = Array.FindAll(requiredDrop, r => r.DropStatus == EnemyDrop.Status.RegularDrop);
            int number_drop = ran.Next(0, requiredDrop.Length);
            enemyController.SetDrop(requiredDrop[number_drop]);

            return obj;
        }

        void RemoveEnemy(Transform target)
        {
            GameLogic.RemoveObject(ref listEnemys, target);
        }

        void Awake()
        {
            instance = this;
            EventManager.AddListener<EndLoadGameSceneEvent>(OnEndGeneration);
        }

        void OnEndGeneration(EndLoadGameSceneEvent evt)
        {
            
        }

        private void start1()
        {
            listEnemys.Add(GameLogic.SpawnObject(prefabPlayer, EnemyContainer, emptyPlayer.position.x, emptyPlayer.position.y, emptyPlayer.position.z));
            m_BodyPlayer = listEnemys.First();

        }
        private void start2()
        {

        }
    }


    [Serializable]
    public class Spawner
    {
        [Header("Options Spawn Enemys")]
        public int Rank = 0;
        public int SpawnCount = 1;
        public int EnemysPerSpawn = 1;
        public int DelaySpawnEnemy = 1;
        public int IntervalSpawnEnemy = 1;
        public List<GameObject> PrefabEnemys = new List<GameObject>();
    }

    [Serializable]
    public class EnemyDrop
    {
        public Status DropStatus = Status.RegularDrop;

        public enum Status
        {
            RegularDrop,
            BossDrop
        }

        [Tooltip("The object this enemy can drop when dying")]
        public GameObject LootPrefab;

        [Tooltip("The chance the object has to drop")]
        [Range(0, 1)]
        public float DropRate = 1f;
        [Tooltip("Rang enemy from which the object has to drop")]
        public int EnemyRank = 0;
    }
}

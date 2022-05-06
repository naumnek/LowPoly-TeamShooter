using System.Linq;
using LowLevelGenerator.Scripts.Helpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using naumnek.FPS;
using Unity.FPS.Game;
using Unity.FPS.AI;

namespace LowLevelGenerator.Scripts
{
    public class SpawnSection : MonoBehaviour
    {
        [Header("General")]
        public Transform SpawnpointEnemy;
        [Header("References")]
        public EnemySpawner enemySpawner;
        private List<Transform> AllEnemys = new List<Transform>();

        private Spawner Spawner;
        private int ActiveEnemys = 0;
        // Start is called before the first frame update
        void Start()
        {
            if (SpawnpointEnemy == null) SpawnpointEnemy = this.transform;
        }

        public void ActivateEnemys(Spawner spawner)
        {
            Spawner = spawner;

            StartCoroutine(DelayActiveEnemy());
        }
        private IEnumerator DelayActiveEnemy()
        {
            yield return new WaitForSeconds(Spawner.DelaySpawnEnemy);
            StartCoroutine(SetActiveEnemy());
        }

        private IEnumerator SetActiveEnemy()
        {
            ActiveEnemys++;

            for (int ii = 0; ii < Spawner.EnemysPerSpawn; ii++)
            {
                Transform obj = EnemySpawner.ActivateSpawner(this, Spawner);
                obj.GetComponent<EnemyController>().SetSpawnSection(this);
                AllEnemys.Add(obj);
            }
            yield return new WaitForSeconds(Spawner.IntervalSpawnEnemy);
            if(AllEnemys.Count < Spawner.SpawnCount) StartCoroutine(SetActiveEnemy());         
        }
    }
}

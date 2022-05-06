using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;
using System.Linq;
using Unity.FPS.Game;

namespace naumnek.FPS
{
    public class GameLogic : MonoBehaviour
    {
        [Header("Options Room")]
        public string Title = "All enemies destroyed";
        public float DelayBeforeDisplay = 0.5f;

        [Header("Other")]
        public Timer timer;
        EnemySpawner m_EnemySpawner;
        private static GameLogic instance;
        int EnemyKilled = 0;
        int EnemySpawned = 0;
        int WaveCompleted = 0;

        public static GameLogic GetGameLogic()
        {
            return instance.GetComponent<GameLogic>();
        }

        void Start()
        {
            instance = this;
            m_EnemySpawner = GetComponent<EnemySpawner>();
            EventManager.AddListener<WaveCompletedEvent>(OnWaveCompleted);
            EventManager.AddListener<EnemyKillEvent>(OnEnemyKill);
            EventManager.AddListener<AllWaveCompletedEvent>(OnAllWaveCompleted);
        }

        private void OnWaveCompleted(WaveCompletedEvent evt)
        {
            Statistics("Wave", "Completed");
            DisplayMessageEvent displayMessage = Events.DisplayMessageEvent;
            displayMessage.Message = m_EnemySpawner.GetTitle();
            displayMessage.DelayBeforeDisplay = m_EnemySpawner.DelayBeforeDisplay;
            EventManager.Broadcast(displayMessage);
        }


        private void OnAllWaveCompleted(AllWaveCompletedEvent evt)
        {
            DisplayMessageEvent displayMessage = Events.DisplayMessageEvent;
            displayMessage.Message = Title;
            displayMessage.DelayBeforeDisplay = DelayBeforeDisplay;
            EventManager.Broadcast(displayMessage);
            EventManager.Broadcast(Events.AllObjectivesCompletedEvent);
        }

        void OnEnemyKill(EnemyKillEvent evt)
        {
            //Statistics("Enemy", "Killed");
        }

        void Statistics(string Tag, string Name)
        {
            if (Tag == "Enemy" && Name == "Spawned") EnemySpawned++;
            if (Tag == "Enemy" && Name == "Killed")
            {
                EnemyKilled++;
                AllEnemysKilledMessage();
            }
            if (Tag == "Wave" && Name == "Completed")
            {
                WaveCompleted++;
            }
        }
        void AllEnemysKilledMessage()
        {

            if (EnemySpawned == EnemyKilled)
            {
                DisplayMessageEvent displayMessage = Events.DisplayMessageEvent;
                displayMessage.Message = Title;
                displayMessage.DelayBeforeDisplay = DelayBeforeDisplay;
                EventManager.Broadcast(displayMessage);
                EventManager.Broadcast(Events.AllObjectivesCompletedEvent);
            }
        }

        void WaveCompletedMessage()
        {
        }

        public static Transform SpawnObject(GameObject prefab, float x, float y)
        {
            return Instantiate(prefab, new Vector2(x, y), Quaternion.identity).transform;
        }

        public static void SpawnObject(ref List<Transform> list, GameObject prefab, float x, float y)
        {
            list.Add(Instantiate(prefab, new Vector2(x, y), Quaternion.identity).transform);
        }

        public static Transform SpawnObject(GameObject prefab, Transform parent, float x, float y, float z)
        {
            GameObject obj = Instantiate(prefab, parent);
            obj.transform.position = new Vector3(x, y, z);
            obj.transform.SetParent(parent);
            instance.Statistics(obj.tag,"Spawned");
            return obj.transform;
        }

        public static void SpawnObject(ref List<Transform> list, GameObject prefab, Transform parent, string name, float x, float y)
        {
            Transform parent2 = (new GameObject(name)).transform;
            parent2.position = new Vector2(x, y);
            Transform obj = Instantiate(prefab, new Vector2(0, 0), Quaternion.identity).transform;
            obj.SetParent(parent2, false);
            parent2.transform.SetParent(parent, false);
            list.Add(parent2);
        }

        public static void RemoveObject(ref List<Transform> list, Transform target)
        {
            list.Remove(target);
            Destroy(target.gameObject);
        }
        private void OnDestroy()
        {
            EventManager.RemoveListener<EnemyKillEvent>(OnEnemyKill);
        }
    }
}

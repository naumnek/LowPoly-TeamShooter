using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.FPS.Game;


namespace naumnek.FPS
{
    public class LoadManager : MonoBehaviour
    {
        public GameObject general;
        private static LoadManager instance;

        void Awake()
        {
            instance = this;
            EventManager.AddListener<EndLoadGameSceneEvent>(OnEndLoadGameScene);
        }

        private void OnDestroy()
        {
            EventManager.RemoveListener<EndLoadGameSceneEvent>(OnEndLoadGameScene);
        }

        // Start is called before the first frame update
        public void OnEndLoadGameScene(EndLoadGameSceneEvent evt)
        {
        }
    }
}


using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.UI;
using TMPro;
using Unity.FPS.Game;
using naumnek.Settings;
using Photon.Pun;
using Photon.Pun.Demo.Asteroids;
using System.Collections.Generic;
using System.Collections;

namespace naumnek.FPS
{
    public class FileManager : MonoBehaviour
    {
        [Header("General")]
        //PUBLIC
        public bool checkLoad;
        [Tooltip("Versions determines which scripts the file manager should use")]
        public string GameVersion = "fps_1";
        [Header("References")]
        public int LevelSeed;
        public float WaitLoadScene = 4f;
        public float BarFillSpeed = 0.5f;
        public float BarFillStartLoadScene = 0.5f;
        public GameObject LoadingCanvas;
        public Text ValueLoading;
        public Slider ValueLoadingBar;
        public GameObject loading;
        public string ResultEndGame = "None";
        //PRIVATE
        private string loadscene = "FirstLoadMenu";
        private GameObject Canvas;
        private MenuController mainMenu;
        private AsyncOperation loadingSceneOperation;
        private static FileManager instance;
        public static bool load = false;
        private Animator background_anim;
        System.Random ran = new System.Random();
        private SettingsManager m_SettingsManager;

        void Awake() 
        {
            instance = this;
            background_anim = loading.GetComponent<Animator>();
            SceneManager.sceneLoaded += OnLevelFinishedLoading;
            Debug.Log("ValueLoading: " + ValueLoading.name);
        }

        private void Start()
        {
            m_SettingsManager = SettingsManager.GetInstance();
        }

        void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
        {
            if (loadscene == "FirstLoadMenu") return;
            else
            {
                if (LevelSeed == 0) LevelSeed = ran.Next(Int32.MinValue, Int32.MaxValue);
            }
            LoadingCanvas.SetActive(false);
            IsVisibly = "Unvisibly";
        }

        public static FileManager GetFileManager()
        {
            return instance.GetComponent<FileManager>();
        }

        public static int GetSeed()
        {
            return instance.LevelSeed;
        }

        public static void EndGame(string scene, string result)
        {
            instance.ResultEndGame = result;
            instance.SwitchSceme(scene);
        }

        public static void LoadScene(string scene, int seed)
        {
            instance.LevelSeed = seed;
            instance.SwitchSceme(scene);
        }

        void SwitchSceme(string scene)
        {
            loadscene = scene;
            if (loadscene == "MainMenu")
            {

            }
            LoadingCanvas.SetActive(true);
            //Cursor.lockState = CursorLockMode.Locked;
            //Cursor.visible = false;
            IsVisibly = "Visibly";
            loadingSceneOperation = SceneManager.LoadSceneAsync(scene);
            loadingSceneOperation.allowSceneActivation = false;
            load = true;
        }

        public void EndLoadScene()
        {
            load = false;
            if (loadscene == "MainMenu")
            {
                GamePauseEvent gpe = Events.GamePauseEvent;
                gpe.ServerPause = false;
                EventManager.Broadcast(gpe);

                m_SettingsManager.ResetAllItemInfo();

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                EventManager.Broadcast(Events.EndLoadGameSceneEvent);
            }
        }

        public void StartLoadScene()
        {
            loadingSceneOperation.allowSceneActivation = true;
            load = false;
            EndLoadScene();
        }

        public void LoadMenu(bool active)
        {
            mainMenu = MenuController.GetMenuController();
            mainMenu.gameObject.SetActive(active);
            mainMenu.startMenu.SetActive(active);
        }

        private float time = 0f;
        private float timeLoadScreen = 0f;
        private string IsVisibly = "";
        private bool LoadingScene = true;

        void Update()
        {
            checkLoad = load;

            if (load)
            {
                ValueLoading.text = (Mathf.RoundToInt(loadingSceneOperation.progress * 100)).ToString() + "%";

                if (time < loadingSceneOperation.progress)
                {
                    time += Time.deltaTime * BarFillSpeed;
                    ValueLoadingBar.value = time;
                }
                if(time > BarFillStartLoadScene)
                {
                    StartCoroutine(WaitForStartLoadScene());
                }
            }
        }

        private IEnumerator WaitForStartLoadScene()
        {
            yield return new WaitForSeconds(WaitLoadScene);
            StartLoadScene();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnLevelFinishedLoading;
        }
    }

}



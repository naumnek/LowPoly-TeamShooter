using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace naumnek.FPS
{
    public class ScreenLoad : MonoBehaviour
    {
        public FileManager fileManager;

        private void Start()
        {
            if (fileManager == null) fileManager = FileManager.GetFileManager();
        }

        public void OnAnimationOver(string v)
        {
            switch (v)
            {
                case ("Unvisibily"):
                    fileManager.EndLoadScene();
                    break;
                case ("Visibily"):
                    fileManager.StartLoadScene();
                    break;
            }
        }
    }
}

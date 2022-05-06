using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using naumnek.FPS;
using UnityEngine.SceneManagement;

public class Conductor : MonoBehaviour
{
    public GameObject AllManagers;

    void Awake()
    {
        Conductor[] conductors = FindObjectsOfType<Conductor>();
        if (conductors.Length > 1)
        {
            Destroy(this.gameObject);
        }
        else
        {
            AllManagers.SetActive(true);
            DontDestroyOnLoad(this);
        }
    }
}

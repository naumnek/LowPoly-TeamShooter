using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ancestor : MonoBehaviour
{
    public GameObject Timer;
    private System.Random ran = new System.Random();
    private Timer t;
    public GameObject[] Foods;
    void Start()
    {
        Timer = GameObject.FindWithTag("Timer");
        t = Timer.gameObject.GetComponent<Timer>();
        Timer.gameObject.GetComponent<Timer>().enabled = true;
    }
    void Update()
    {
        try
        {
            Foods = GameObject.FindGameObjectsWithTag("Food");
        }
        catch
        {

        }
        finally
        {

        }
    }
}

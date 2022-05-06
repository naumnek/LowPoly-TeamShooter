using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Food : MonoBehaviour
{
    public GameObject Timer;
    private int lvl;
    public int quality;
    private int max_age;
    private int age;
    private System.Random ran = new System.Random();
    private Timer t;
    // Start is called before the first frame update
    
    void Start()
    {
        Timer = GameObject.FindWithTag("Timer");
        t = Timer.gameObject.GetComponent<Timer>();
        lvl = ran.Next(1, 10);
        max_age = ran.Next(lvl * 10, lvl * 1000);
        age = t.minutes;
        quality = ran.Next(lvl * 2, lvl * 3);
    }

    // Update is called once per frame
    public void Foods()
    {
        if(max_age <= t.minutes - age)
        {
            Destroy(this);
        }
    }
}

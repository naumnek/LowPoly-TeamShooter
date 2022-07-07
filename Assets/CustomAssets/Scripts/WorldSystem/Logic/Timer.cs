using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    public Text TimerText;

    private int min = 0;
    public float seconds = 0f;
    public int minutes = 0;
    public int hours = 0;
    public int days = 0;
    public int weeks = 0;
    public int months = 0;
    public int yaers = 0;
    private static Timer instance;
    // Start is called before the first frame update

    public static Timer GetInstance() => instance;

    private void Awake()
    {
        instance = this;
    }

    private void AllTime()
    {
        minutes = min;
        hours = minutes / 60;
        days = hours / 24;
        weeks = days / 7;
        months = days / 30;
        yaers = months / 12;
    }

    private string stringSeconds => (seconds < 10 ?
            "0" + seconds.ToString().Split(',')[0] :
            seconds.ToString().Split(',')[0]);
    private string stringMinutes => (min < 10 ?
            "0" + min.ToString() : min.ToString());

    // Update is called once per frame
    void Update()
    {
        seconds += Time.deltaTime; /* Вычитаем из 10 время кадра (оно в миллисекундах) */
        
        TimerText.text = stringMinutes + ":" + stringSeconds;

        if (seconds >= 60f) /* Время вышло пишем */
        {
            seconds = 0f; /* запускает опять таймер на 60,чтобы повторялось бесконечно */
            min += 1;
            if(min >= 60)
            {
                AllTime();
            }
        }
    }
}

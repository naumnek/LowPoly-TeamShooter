using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class DataManager : MonoBehaviour
{
    private static DataManager instance;


    public void setData(ref List<string> list, string target, string str)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Contains(target))
            {
                print("DataManager - Change: " + str + " from " + target);
                list[i] = list[i].Replace(list[i].Split(':')[1], str);
                return;
            }
        }
        return;
    }


    public string getStr(List<string> list, string target)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Contains(target))
            {
                return list[i].Split(':')[1];
            }
        }
        return "";
    }

    public int getInt(List<string> list, string target)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Contains(target))
            {
                return Convert.ToInt32(list[i].Split(':')[1]);
            }
        }
        return 0;
    }

    // Start is called before the first frame update
    void Start()
    {
        instance = this;
    }
}

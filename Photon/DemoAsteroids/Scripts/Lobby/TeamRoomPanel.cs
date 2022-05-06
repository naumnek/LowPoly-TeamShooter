using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TeamRoomPanel : MonoBehaviour
{
    public GameObject GridPlayers;
    public TMP_Text TeamNumberText;
    public GridLayoutGroup GridLayoutPlayers { get; private set; }

    // Start is called before the first frame update
    void Awake()
    {
        
        if (GridPlayers == null)
        {
            GridLayoutPlayers = GetComponentInChildren<GridLayoutGroup>();
            GridPlayers = GridLayoutPlayers.gameObject;
        }
        else GridLayoutPlayers = GridPlayers.GetComponent<GridLayoutGroup>();


        if (TeamNumberText == null) TeamNumberText = GetComponentInChildren<TMP_Text>();
    }
}

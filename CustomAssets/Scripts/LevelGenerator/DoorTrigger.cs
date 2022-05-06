using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LowLevelGenerator.Scripts
{
    public class DoorTrigger : MonoBehaviour
    {
        [HideInInspector]
        public DoorExit doorExit;
        public bool player;

        private void OnTriggerEnter(Collider trigger)
        {
            if (!player && trigger.tag == "Player") 
            {
                player = true;
                doorExit.OpenDoor(); 
            }
        }

        private void OnTriggerExit(Collider trigger)
        {
            if (player && trigger.tag == "Player")
            {
                player = false;
                doorExit.ClosedDoor(); 
            }
        }
    }
}

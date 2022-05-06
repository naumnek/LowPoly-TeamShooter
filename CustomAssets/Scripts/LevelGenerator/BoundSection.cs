using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace LowLevelGenerator.Scripts
{
    public class BoundSection : MonoBehaviour
    {
        public IEnumerable<Collider> GetColliders => GetComponentsInChildren<Collider>();

        public Collider MainBound;
        public Section ParentSection;
        bool player = false;

        private void Awake()
        {
            MainBound = GetComponent<Collider>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!player && other.tag == "Player")
            {
                player = true;
                ParentSection.OnTriggerPlayer(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (player && other.tag == "Player")
            {
                player = false;
                ParentSection.OnTriggerPlayer(false);
            }           
        }
    }
}

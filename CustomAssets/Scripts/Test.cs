using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using LowLevelGenerator.Scripts.Helpers;

namespace naumnek.FPS
{
    public class Test : MonoBehaviour
    {

        // Start is called before the first frame update
        private void OnTriggerEnter(Collider trigger)
        {
            print(trigger.name);
        }
    }
}
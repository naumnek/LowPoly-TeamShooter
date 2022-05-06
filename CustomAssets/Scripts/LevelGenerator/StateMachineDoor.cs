using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace LowLevelGenerator.Scripts
{
    public class StateMachineDoor : StateMachineBehaviour
    {
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if(!animator.GetBool("Open")) animator.SetBool("Closed", true);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.FPS.Gameplay;
using System.Linq;

namespace LowLevelGenerator.Scripts
{
    public class DoorExit : MonoBehaviour
    {
        public GameObject Structure;
        public DoorTrigger doorTrigger;
        public AudioSource sfx_door;
        public List<ParticleSystem> vfx_door = new List<ParticleSystem>();
        public float PlayerRecheckTime = 0.5f;
        [HideInInspector]
        public bool isClosing = false;
        [HideInInspector]
        public Animator anim;
        [HideInInspector]
        public List<Section> Sections = new List<Section>();
        public bool PlayerSectionMatched = false;

        bool EndOpened = true;

        private void Start()
        {
            anim = GetComponent<Animator>();
            doorTrigger.doorExit = this;
        }

        public void OpenDoor()
        {
            if (PlayerSectionMatched && doorTrigger.player && !isClosing && EndOpened)
            {
                Sections[0].SetActiveSection(this, true);
                isClosing = true;
                EndOpened = false;
                anim.SetBool("Open", true);
            }
        }

        public void ClosedDoor()
        {
            if(EndOpened)
            {
                anim.SetBool("Open", false);
            }
        }

        public void EndOpenedDoor()
        {
            EndOpened = true;
            if(!doorTrigger.player) ClosedDoor();
        }

        public void EndClosedDoor()
        {
            isClosing = false;
            Sections[0].SetActiveSection(this, false);
            OpenDoor();
        }

        public void PlayVFX()
        {
            for(int i = 0; i < vfx_door.Count;i++) vfx_door[i].Play();
        }


        public void PlaySFX()
        {
            sfx_door.Play();
        }
    }
}
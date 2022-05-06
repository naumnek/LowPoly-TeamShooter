using System.Linq;
using LowLevelGenerator.Scripts.Helpers;
using System.Collections.Generic;
using UnityEngine;
using naumnek.FPS;
using Unity.FPS.Game;
using Unity.FPS.AI;
using System.Collections;

namespace LowLevelGenerator.Scripts
{
    public class Section : MonoBehaviour
    {
        public bool Matched = true;

        /// <summary>
        /// Section tags
        /// </summary>
        public string[] Tags;

        /// <summary>
        /// Tags that this section can annex
        /// </summary>
        public string[] CreatesTags;

        /// <summary>
        /// Exits node in hierarchy
        /// </summary>
        public List<Transform> Exits = new List<Transform>();
        [HideInInspector]
        public int ExitsCompleted = 0;

        public BoundSection Bound;

        public GameObject ParentCollider;

        public GameObject Structure;

        public Transform Spawner;

        /// <summary>
        /// Chances of the section spawning a dead end
        /// </summary>
        public int DeadEndChance;

        [HideInInspector]
        public List<DoorExit> Doors = new List<DoorExit>();
        [HideInInspector]
        public List<DoorExit> FlankDoors = new List<DoorExit>();
        [HideInInspector]
        public List<GameObject> DeadEnds = new List<GameObject>();
        [HideInInspector]
        public List<Section> FlankSections = new List<Section>();
        [HideInInspector]
        public List<GameObject> ValidDeadEnds = new List<GameObject>();
        [HideInInspector]
        public List<Transform> TransformValidDeadEnds = new List<Transform>();
        [HideInInspector]
        public List<Transform> Enemys = new List<Transform>();

        public Section ParentSection;

        public bool TriggerPlayer { get; private set; } = false;

        public bool SectionGenerationMode { get; private set; } = false;

        Transform ContainerDoors;
        Transform ContainerDeadEnds;
        protected LevelGenerator m_LevelGenerator;
        protected int order;

        private void Awake()
        {
            Bound = GetComponentInChildren<BoundSection>();
            Bound.ParentSection = this;
        }

        public void Initialize(LevelGenerator levelGenerator, int sourceOrder)
        {
            m_LevelGenerator = levelGenerator;

            m_LevelGenerator.LevelSize++;
            m_LevelGenerator.RegisteredColliders.Add(Bound.MainBound);

            ContainerDoors = new GameObject("Doors").transform;
            ContainerDoors.SetParent(transform);
            ContainerDeadEnds = new GameObject("DeadEnds").transform;
            ContainerDeadEnds.SetParent(Structure.transform);

            /*if (!m_LevelGenerator.CustomStartGeneration)
            {
                if (Tags.First() == "spawn") EnemySpawner.InitializePlayer(Spawner);
                if (!m_LevelGenerator.DisableSectionsParser && Tags.First() == "room")
                {
                    Transform obj = EnemySpawner.ActivateSpawner(Spawner);
                    obj.GetComponent<EnemyController>().SpawnSection = this;
                    Enemys.Add(obj);
                }
            }*/

            order = sourceOrder + 1;

            GenerateAnnexes();
            
        }

        protected void GenerateAnnexes()
        {
            for (int i = 0;i < Exits.Count;i++)
            {
                if (m_LevelGenerator.LevelSize < m_LevelGenerator.MaxLevelSize && order < m_LevelGenerator.MaxAllowedOrder)
                {
                    if (DeadEndChance != 0 && RandomService.RollD100(DeadEndChance))
                    {
                        PlaceDeadEnd(Exits[i], true); 
                    }
                    else
                    {
                        StartCoroutine(GenerateSection(Exits[i]));
                    }
                }
                else 
                {
                    PlaceDeadEnd(Exits[i], true);                    
                }
                //if (i == Exits.Count - 1) m_LevelGenerator.RegisterNewSection(this);
            }           
        }

        public IEnumerator GenerateSection(Transform exit)
        {
            Section section = Instantiate(m_LevelGenerator.PickSectionWithTag(CreatesTags), exit);

            yield return new WaitForSeconds(m_LevelGenerator.WaitGenerateSection);

            if (m_LevelGenerator.LevelSize < m_LevelGenerator.MaxLevelSize && !m_LevelGenerator.IsSectionsIntersect(section.Bound, Bound)) // && 
            {
                section.transform.SetParent(m_LevelGenerator.SectionContainer);
                section.FlankSections.Add(this);
                section.ParentSection = this;
                FlankSections.Add(section);
                PlaceDoor(exit, section);
                section.Initialize(m_LevelGenerator, order);
            }
            else
            {
                Destroy(section.gameObject);
                PlaceDeadEnd(exit, false);
            }
        }

        public void RegenerationSection() //строим новые секции в подходящих точках
        {
            SectionGenerationMode = true;
            for (int i = 0; i < ValidDeadEnds.Count; i++)
            {
                if (i < m_LevelGenerator.MaxLevelSize - m_LevelGenerator.LevelSize)
                {
                    GameObject obj = ValidDeadEnds[i];
                    DeadEnds.Remove(obj);
                    ExitsCompleted--;

                    StartCoroutine(GenerateSection(TransformValidDeadEnds[i]));
                    TransformValidDeadEnds.Remove(TransformValidDeadEnds[i]);
                    ValidDeadEnds.Remove(obj);
                    Destroy(obj.gameObject);
                }
            }
            SectionGenerationMode = false;
        }

        bool EndGenerateAnnexes = false;

        //создаем на месте полученого Transform exit одну из стен в DeadEnds и заносим её в список DeadEndColliders 
        protected void PlaceDeadEnd(Transform exit, bool ValidDeadEnd)
        {
            ExitsCompleted++;
            GameObject obj = Instantiate(m_LevelGenerator.DeadEnds.PickOne(), exit);
            obj.transform.SetParent(ContainerDeadEnds);
            DeadEnds.Add(obj);
            if (ValidDeadEnd)
            {
                ValidDeadEnds.Add(obj);
                TransformValidDeadEnds.Add(exit);
            }
            if(!EndGenerateAnnexes && ExitsCompleted == Exits.Count)
            {
                EndGenerateAnnexes = true;
                m_LevelGenerator.RegisterNewSection(this);
            }
        }

        //создаем на месте полученого Transform exit одну из стен в DeadEnds и заносим её в список DeadEndColliders 
        protected void PlaceDoor(Transform exit, Section section)
        {
            ExitsCompleted++;
            DoorExit obj = Instantiate(m_LevelGenerator.Doors.PickOne(), exit);
            obj.transform.SetParent(ContainerDoors);
            Doors.Add(obj);
            obj.Sections.Add(this);
            obj.Sections.Add(FlankSections.Last());
            FlankDoors.Add(obj);
            section.FlankDoors.Add(obj);
            if(!EndGenerateAnnexes && ExitsCompleted == Exits.Count)
            {
                EndGenerateAnnexes = true;
                m_LevelGenerator.RegisterNewSection(this);
            }
        }

        //триггер на проверку игрока в области комнаты
        public void OnTriggerPlayer(bool trigger)
        {
            TriggerPlayer = trigger;
            if (trigger) //если игрок в комнате
            {
                for (int i = 0; i < FlankDoors.Count; i++)
                { //сообщаем об этом всем ближайшим комнатам
                    FlankDoors[i].PlayerSectionMatched = Matched;
                }
            }
        }

        public void OnEnemyInSectionKill(Transform enemy)
        {
            Enemys.Remove(enemy);
            if(Enemys.Count == 0 && !Matched)
            {
                Matched = true;
                for (int i = 0; i < FlankDoors.Count; i++)
                {
                    FlankDoors[i].PlayerSectionMatched = Matched;
                    FlankDoors[i].OpenDoor();
                }
            }
        }

        public void SetActiveSection(DoorExit door, bool action)
        {
            if (m_LevelGenerator.DisableSectionsParser) return;
            List<Section> EmptySections = new List<Section>();
            EmptySections.AddRange(door.Sections.Where(s => !s.TriggerPlayer)); 

            for (int i = 0; i < EmptySections.Count; i++)
            {
                if(action || EmptySections[i].FlankDoors.All(d => !d.isClosing)) EmptySections[i].Structure.SetActive(action);
                
                for (int ii = 0; ii < EmptySections[i].FlankDoors.Count; ii++)
                {                  
                    if (action || EmptySections[i].FlankDoors[ii].Sections.All(s => !s.TriggerPlayer && s.FlankDoors.All(d => !d.isClosing)))
                    {
                        EmptySections[i].FlankDoors[ii].Structure.SetActive(action);
                    }
                }
            }
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.FPS.Game;

public class CarController : MonoBehaviour
{
    [Tooltip("The distance at which the enemy considers that it has reached its current path destination point")]
    public bool Pause = true;
    public float PathReachingRadius = 2f;
    public PatrolPathCar PatrolPath;
    int m_PathDestinationNodeIndex;
    public NavMeshAgent NavMeshAgent { get; private set; }

    private void OnDestroy()
    {
        EventManager.RemoveListener<PlayerSpawnEvent>(OnPlayerSpawnEvent);
        EventManager.RemoveListener<GamePauseEvent>(OnGamePauseEvent);
    }

    private void Start()
    {
        EventManager.AddListener<PlayerSpawnEvent>(OnPlayerSpawnEvent);
        EventManager.AddListener<GamePauseEvent>(OnGamePauseEvent);

        if(!Pause) Activate();
    }

    private void Update()
    {
        if (!Pause)
        {
            UpdateCurrentAiState();
        }
    }

    private void OnPlayerSpawnEvent(PlayerSpawnEvent evt)
    {
        Activate();
    }
    private void OnGamePauseEvent(GamePauseEvent evt)
    {
        Pause = evt.ServerPause;
    }

    private void Activate()
    {
        NavMeshAgent = GetComponent<NavMeshAgent>();
        SetPathDestinationToClosestNode();
        Pause = false;
    }

    private void UpdateCurrentAiState()
    {
        UpdatePathDestination();
        SetNavDestination(GetDestinationOnPath());
    }

    bool IsPathValid()
    {
        return PatrolPath && PatrolPath.PathNodes.Count > 0;
    }

    public void ResetPathDestination()
    {
        m_PathDestinationNodeIndex = 0;
    }

    public void SetPathDestinationToClosestNode()
    {
        if (IsPathValid())
        {
            int closestPathNodeIndex = 0;
            for (int i = 0; i < PatrolPath.PathNodes.Count; i++)
            {
                float distanceToPathNode = PatrolPath.GetDistanceToNode(transform.position, i);
                if (distanceToPathNode < PatrolPath.GetDistanceToNode(transform.position, closestPathNodeIndex))
                {
                    closestPathNodeIndex = i;
                }
            }

            m_PathDestinationNodeIndex = closestPathNodeIndex;
        }
        else
        {
            m_PathDestinationNodeIndex = 0;
        }
    }

    public Vector3 GetDestinationOnPath()
    {
        if (IsPathValid())
        {
            return PatrolPath.GetPositionOfPathNode(m_PathDestinationNodeIndex);
        }
        else
        {
            return transform.position;
        }
    }

    public void SetNavDestination(Vector3 destination)
    {
        if (NavMeshAgent)
        {
            NavMeshAgent.SetDestination(destination);
        }
    }

    public void UpdatePathDestination(bool inverseOrder = false)
    {
        if (IsPathValid())
        {
            // Check if reached the path destination
            if ((transform.position - GetDestinationOnPath()).magnitude <= PathReachingRadius)
            {
                // increment path destination index
                m_PathDestinationNodeIndex =
                    inverseOrder ? (m_PathDestinationNodeIndex - 1) : (m_PathDestinationNodeIndex + 1);
                if (m_PathDestinationNodeIndex < 0)
                {
                    m_PathDestinationNodeIndex += PatrolPath.PathNodes.Count;
                }

                if (m_PathDestinationNodeIndex >= PatrolPath.PathNodes.Count)
                {
                    m_PathDestinationNodeIndex -= PatrolPath.PathNodes.Count;
                }
            }
        }
    }

}

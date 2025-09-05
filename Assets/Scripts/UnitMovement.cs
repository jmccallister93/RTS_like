using UnityEngine;
using UnityEngine.AI;

public enum MovementMode
{
    None,
    Move,        // Just move, ignore enemies
    AttackMove,  // Move but engage enemies
    Guard,
    Patrol
}

public class UnitMovement : MonoBehaviour, IPausable
{

    [Header("Movement Settings")]
    public LayerMask ground = 1;
    public float raycastDistance = 100f;

    private NavMeshAgent agent;
    public bool isCommandedtoMove;
    public MovementMode currentMode = MovementMode.None;

    // Pause snapshot
    private bool wasPausedWhileMoving;
    private Vector3 pausedDestination;
    private MovementMode pausedMode;

    // Patrol
    private Vector3 patrolStartPoint;
    private Vector3 patrolEndPoint;
    private bool movingToEndPoint = true;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        //PauseManager.Instance?.RegisterPausable(this); // optional helper
    }

    //private void OnDestroy()
    //{
    //    // Unregister from pause manager
    //    if (PauseManager.Instance != null)
    //    {
    //        PauseManager.Instance.UnregisterPausable(this);
    //    }
    //}

    private void Update()
    {
        if (isCommandedtoMove && agent != null)
        {
            if (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance)
            {
                if (currentMode == MovementMode.Patrol)
                {
                    movingToEndPoint = !movingToEndPoint;
                    Vector3 next = movingToEndPoint ? patrolEndPoint : patrolStartPoint;
                    agent.SetDestination(next);
                }
                else
                {
                    isCommandedtoMove = false;
                    currentMode = MovementMode.None;
                }
            }
        }
    }

    public void OnPause()
    {
        if (isCommandedtoMove && agent != null)
        {
            wasPausedWhileMoving = true;
            pausedDestination = agent.hasPath ? agent.destination : transform.position;
            pausedMode = currentMode;
        }
        else
        {
            wasPausedWhileMoving = false;
        }
    }

    public void OnResume()
    {
        if (wasPausedWhileMoving)
        {
            // Only restore if no queued commands will override
            if (!CommandQueue.Instance.HasQueuedCommandFor(gameObject))
            {
                isCommandedtoMove = true;
                currentMode = pausedMode;
            }

            wasPausedWhileMoving = false;
        }
    }

    // Public methods for other scripts to call
    public void MoveToPosition(Vector3 targetPosition, MovementMode mode = MovementMode.Move)
    {
        if (agent == null) return;

        if (NavMesh.SamplePosition(targetPosition, out var navHit, 2f, NavMesh.AllAreas))
        {
            // New order overrides current
            agent.ResetPath();
            isCommandedtoMove = true;
            currentMode = mode;
            agent.isStopped = false;
            agent.SetDestination(navHit.position);
        }
    }

    // Update the PatrolTo method
    public void StartPatrol(Vector3 patrolPoint)
    {
        patrolStartPoint = transform.position;
        patrolEndPoint = patrolPoint;
        movingToEndPoint = true;
        MoveToPosition(patrolEndPoint, MovementMode.Patrol);
    }

    public void StopMovement()
    {
        if (agent != null)
        {
            agent.ResetPath();
            isCommandedtoMove = false;
            currentMode = MovementMode.None;
        }
    }

    public bool IsMoving()
    {
        return isCommandedtoMove && agent != null && agent.hasPath;
    }

    public Vector3 GetDestination()
    {
        return agent != null ? agent.destination : transform.position;
    }

    public float GetRemainingDistance()
    {
        return agent != null ? agent.remainingDistance : 0f;
    }
}
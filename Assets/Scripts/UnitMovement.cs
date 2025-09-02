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

public class UnitMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public LayerMask ground = 1;
    public float raycastDistance = 100f;

    private NavMeshAgent agent;
    public bool isCommandedtoMove;
    public MovementMode currentMode = MovementMode.None;

    // Add patrol-specific fields
    private Vector3 patrolStartPoint;
    private Vector3 patrolEndPoint;
    private bool movingToEndPoint = true;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        // Only handle movement completion checking
        if (isCommandedtoMove && agent != null)
        {
            if (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance)
            {
                // Special handling for patrol mode
                if (currentMode == MovementMode.Patrol)
                {
                    // Switch patrol direction
                    movingToEndPoint = !movingToEndPoint;
                    Vector3 nextPoint = movingToEndPoint ? patrolEndPoint : patrolStartPoint;
                    agent.SetDestination(nextPoint);
                    // Keep patrol mode active
                }
                else
                {
                    isCommandedtoMove = false;
                    currentMode = MovementMode.None;
                }
            }
        }
    }

    // Public methods for other scripts to call
    public void MoveToPosition(Vector3 targetPosition, MovementMode mode = MovementMode.Move)
    {
        if (agent != null)
        {
            // Check if the target is on NavMesh
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(targetPosition, out navHit, 2f, NavMesh.AllAreas))
            {
                isCommandedtoMove = true;
                currentMode = mode;
                agent.SetDestination(navHit.position);
            }
        }
    }

    // Update the PatrolTo method
    public void StartPatrol(Vector3 patrolPoint)
    {
        patrolStartPoint = transform.position; // Current position
        patrolEndPoint = patrolPoint;          // Clicked position
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
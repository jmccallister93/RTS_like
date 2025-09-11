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

    [Header("Rotation Settings")]
    public bool enableInstantTurning = true;
    public float minVelocityForTurning = 0.1f; // Minimum velocity to trigger turning

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
    public bool patrolInterruptedByCombat = false;

    private Animator unitAnimator;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        unitAnimator = GetComponent<Animator>();

        // Disable NavMeshAgent's built-in rotation since we'll handle it manually
        if (agent != null && enableInstantTurning)
        {
            agent.updateRotation = false;
        }
    }

    private void Update()
    {
        if (isCommandedtoMove && agent != null)
        {
            // Handle instant turning to face movement direction
            if (enableInstantTurning)
            {
                HandleInstantTurning();
            }

            // NEW: Don't continue patrol automatically if interrupted by combat
            if (currentMode == MovementMode.Patrol && patrolInterruptedByCombat)
            {
                return; // Wait for combat to end before resuming patrol
            }

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

                    // Notify animator that movement is complete
                    if (unitAnimator != null)
                    {
                        unitAnimator.SetBool("isMoving", false);
                    }
                }
            }
        }
    }

    private void HandleInstantTurning()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        // Use desiredVelocity for more consistent direction, fallback to velocity
        Vector3 movementDirection = agent.desiredVelocity;
        if (movementDirection.magnitude < minVelocityForTurning)
        {
            movementDirection = agent.velocity;
        }

        // Only rotate if we have significant movement
        if (movementDirection.magnitude > minVelocityForTurning)
        {
            // Flatten the movement direction (remove Y component for ground-based units)
            movementDirection.y = 0;

            if (movementDirection != Vector3.zero)
            {
                // Calculate the rotation to face the movement direction
                Quaternion targetRotation = Quaternion.LookRotation(movementDirection);

                // Apply the rotation instantly
                transform.rotation = targetRotation;
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

            if (unitAnimator != null)
            {
                unitAnimator.SetBool("isMoving", true);
            }

            // Optional: Immediately face the target direction
            if (enableInstantTurning)
            {
                Vector3 initialDirection = (navHit.position - transform.position).normalized;
                initialDirection.y = 0;
                if (initialDirection != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(initialDirection);
                }
            }
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

    public void InterruptPatrolForCombat()
    {
        Debug.Log($"{name} InterruptPatrolForCombat called - currentMode: {currentMode}");
        if (currentMode == MovementMode.Patrol)
        {
            patrolInterruptedByCombat = true;
            isCommandedtoMove = false;

            if (agent != null && agent.isOnNavMesh)
            {
                agent.ResetPath();
                agent.isStopped = true;
            }
            Debug.Log($"{name} patrol interrupted for combat - patrolInterruptedByCombat: {patrolInterruptedByCombat}");
        }
        else
        {
            Debug.Log($"{name} NOT interrupting patrol - currentMode is {currentMode}, not Patrol");
        }
    }

    public void ResumePatrolAfterCombat()
    {
        Debug.Log($"{name} ResumePatrolAfterCombat called");

        if (patrolInterruptedByCombat)
        {
            patrolInterruptedByCombat = false;
            isCommandedtoMove = true;
            currentMode = MovementMode.Patrol;

            // NEW: Clear any lingering targets to prevent immediate re-detection
            var attackController = GetComponent<AttackController>();
            if (attackController != null)
            {
                attackController.targetToAttack = null;
            }

            Vector3 destination = movingToEndPoint ? patrolEndPoint : patrolStartPoint;

            if (agent != null && agent.isOnNavMesh && agent.enabled)
            {
                agent.isStopped = false;
                agent.SetDestination(destination);
            }

            if (unitAnimator != null)
            {
                unitAnimator.SetBool("isMoving", true);
            }

            Debug.Log($"{name} resuming patrol after combat - destination: {destination}");
        }
    }

    // NEW: Check if patrol is active
    public bool IsPatrolling()
    {
        return currentMode == MovementMode.Patrol;
    }
}
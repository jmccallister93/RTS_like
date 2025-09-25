using UnityEngine;
using UnityEngine.AI;

public class UnitMovingState : StateMachineBehaviour
{
    private NavMeshAgent agent;
    private UnitMovement unitMovement;
    private AttackController attackController;

    // Enemy detection for AttackMove
    private float targetCheckInterval = 0.1f;
    private float lastTargetCheck = 0f;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        agent = animator.GetComponent<NavMeshAgent>();
        unitMovement = animator.GetComponent<UnitMovement>();
        attackController = animator.GetComponent<AttackController>();

        // Reset target check timer
        lastTargetCheck = Time.time;


        if (agent != null && unitMovement != null)
        {
            agent.isStopped = false;
            if (unitMovement.isCommandedtoMove)
            {
                Vector3 destination = unitMovement.GetDestination();
                if (destination != Vector3.zero)
                {
                    agent.SetDestination(destination);
                }
            }
        }
        Debug.Log($"{animator.name} entered Movement state - Mode: {unitMovement?.currentMode}");
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (agent == null || unitMovement == null) return;


        // 1. Pause handling
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
        {
            agent.isStopped = true;
            return;
        }
        else
        {
            agent.isStopped = false;
        }

        // 1.5 PRIORITY: Check for ability usage
        if (animator.GetBool("isUsingAbility"))
        {
            var stateContext = animator.GetComponent<StateContext>();
            if (stateContext != null)
            {
                stateContext.SaveCurrentState(animator);
            }
            return; // Let animator transition to AbilityState
        }

        // 2. PRIORITY: Check for enemies during combat movement modes
        if (unitMovement.currentMode == MovementMode.AttackMove ||
            unitMovement.currentMode == MovementMode.Patrol)
        {
            // Check for targets periodically
            if (Time.time - lastTargetCheck >= targetCheckInterval)
            {
                lastTargetCheck = Time.time;

                if (CheckForEnemiesDuringMovement(animator))
                {
                    // Enemy found! 
                    Debug.Log($"{animator.name} found enemy during {unitMovement.currentMode} - transitioning to Follow");

                    // NEW: If we were patrolling, interrupt it for combat
                    if (unitMovement.currentMode == MovementMode.Patrol)
                    {
                        Debug.Log($"{animator.name} CALLING InterruptPatrolForCombat()");
                        unitMovement.InterruptPatrolForCombat();
                    }

                    //animator.SetBool("isMoving", false);
                    animator.SetBool("isFollowing", true);
                    return;
                }
            }
        }


        // 3. End moving if no command or reached destination
        if (!unitMovement.isCommandedtoMove ||
            (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance))
        {
            // Preserve patrol mode if interrupted for combat
            if (unitMovement.currentMode != MovementMode.Patrol || !unitMovement.patrolInterruptedByCombat)
            {
                unitMovement.isCommandedtoMove = false;
                unitMovement.currentMode = MovementMode.None;
            }

            // Clear the moving flag to return to Idle
            animator.SetBool("isMoving", false);

            //Debug.Log($"{animator.name} finished moving, returning to Idle - currentMode preserved: {unitMovement.currentMode}");
            return;
        }

        // 4. Keep destination in sync with UnitMovement
        Vector3 targetDestination = unitMovement.GetDestination();
        if (agent.destination != targetDestination && targetDestination != Vector3.zero)
        {
            agent.SetDestination(targetDestination);
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Only stop movement if we're transitioning to non-combat states
        // Don't stop if transitioning to Follow/Attack
        AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
        bool transitioningToCombat = animator.GetBool("isFollowing") || animator.GetBool("isAttacking");

        if (unitMovement != null && !unitMovement.isCommandedtoMove && !transitioningToCombat)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        Debug.Log($"{animator.name} exited Movement state");
    }

    /// <summary>
    /// Check for enemies during AttackMove - similar to AttackController logic
    /// </summary>
    private bool CheckForEnemiesDuringMovement(Animator animator)
    {
        if (attackController == null) return false;

        // First check if AttackController already has a target
        if (attackController.targetToAttack != null)
        {
            Unit targetUnit = attackController.targetToAttack.GetComponent<Unit>();
            if (targetUnit != null && targetUnit.IsAlive())
            {
                Debug.Log($"{animator.name} AttackController already has valid target: {attackController.targetToAttack.name}");
                return true;
            }
            else
            {
                // Clear invalid target
                attackController.targetToAttack = null;
            }
        }

        // If no target, scan for enemies manually
        return ScanForNearbyEnemies(animator);
    }

    /// <summary>
    /// Manual enemy scanning for AttackMove
    /// </summary>
    private bool ScanForNearbyEnemies(Animator animator)
    {
        float detectionRange = 10f; // Default range

        // Get detection range from sphere collider if available
        SphereCollider sphereCollider = animator.GetComponent<SphereCollider>();
        if (sphereCollider != null && sphereCollider.isTrigger)
        {
            detectionRange = sphereCollider.radius;
        }

        // Find all colliders within detection range
        Collider[] nearbyColliders = Physics.OverlapSphere(animator.transform.position, detectionRange);

        Transform nearestEnemy = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider collider in nearbyColliders)
        {
            // Skip ourselves
            if (collider.transform == animator.transform) continue;

            // Check if it's a valid target
            if (ShouldTarget(collider.gameObject, animator.gameObject))
            {
                float distance = Vector3.Distance(animator.transform.position, collider.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = collider.transform;
                }
            }
        }

        // Set the nearest enemy as target if found
        if (nearestEnemy != null)
        {
            attackController.targetToAttack = nearestEnemy;
            Debug.Log($"{animator.name} found enemy during AttackMove: {nearestEnemy.name} at distance {nearestDistance:F2}");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Check if we should target this object (copied from UnitIdleState logic)
    /// </summary>
    private bool ShouldTarget(GameObject potentialTarget, GameObject myUnit)
    {
        // Must have a Unit component and be alive
        Unit targetUnit = potentialTarget.GetComponent<Unit>();
        if (targetUnit == null || !targetUnit.IsAlive())
            return false;

        // Check team affiliation
        string myTag = myUnit.tag;
        string targetTag = potentialTarget.tag;

        // Player units should target Enemy units
        if (myTag == "Player" && targetTag == "Enemy")
            return true;

        // Enemy units should target Player units  
        if (myTag == "Enemy" && targetTag == "Player")
            return true;

        return false;
    }
}
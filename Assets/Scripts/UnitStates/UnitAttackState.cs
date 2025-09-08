using UnityEngine;
using UnityEngine.AI;

public class UnitAttackState : StateMachineBehaviour
{
    AttackController attackController;
    NavMeshAgent agent;
    UnitMovement unitMovement;
    public float attackingDistance = 1.5f;
    public float exitAttackDistance = 2.0f;
    public float attackCooldown = 1.0f;
    private float lastAttackTime;

    // Pause-related fields
    private bool wasPausedInThisState = false;
    private float pausedLastAttackTime;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        attackController = animator.transform.GetComponent<AttackController>();
        agent = animator.transform.GetComponent<NavMeshAgent>();
        unitMovement = animator.transform.GetComponent<UnitMovement>();
        wasPausedInThisState = false;

        if (attackController != null)
        {
            attackController.SetAttackStateMaterial();
        }

        // Stop the NavMeshAgent during attack
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        lastAttackTime = Time.time;
        Debug.Log($"{animator.name} entered Attack state");
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Check if game is paused - don't process AI logic while paused
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
        {
            // Store pause state info for resume
            if (!wasPausedInThisState)
            {
                wasPausedInThisState = true;
                pausedLastAttackTime = lastAttackTime;
            }
            return; // Don't process any logic while paused
        }

        // If we were paused and just resumed, restore timing
        if (wasPausedInThisState)
        {
            wasPausedInThisState = false;
            // Adjust last attack time to account for paused duration
            float pauseDuration = Time.time - pausedLastAttackTime;
            lastAttackTime = Time.time - (pausedLastAttackTime - lastAttackTime);
        }

        // PRIORITY 1: Check for ability usage
        if (animator.GetBool("isUsingAbility"))
        {
            var stateContext = animator.GetComponent<StateContext>();
            if (stateContext != null)
            {
                stateContext.SaveCurrentState(animator);
            }
            return; // Let animator transition to AbilityState
        }

        // PRIORITY 2: Check if player is commanding movement - override AI
        if (unitMovement != null && unitMovement.isCommandedtoMove)
        {
            Debug.Log($"{animator.name} player commanding movement - transitioning directly to Moving");

            // Clear attack target since player is overriding
            if (attackController != null)
            {
                attackController.targetToAttack = null;
            }

            // Transition directly to Moving state
            animator.SetBool("isAttacking", false);
            animator.SetBool("isFollowing", false);
            animator.SetBool("isMoving", true);
            return;
        }

        // PRIORITY 3: Check if target is null or destroyed
        if (attackController == null || attackController.targetToAttack == null)
        {
            Debug.Log($"{animator.name} no target - exiting attack to idle");
            ExitToIdle(animator);
            return;
        }

        // PRIORITY 4: Check if target is still alive
        Unit targetUnit = attackController.targetToAttack.GetComponent<Unit>();
        if (targetUnit == null || !targetUnit.IsAlive())
        {
            Debug.Log($"{animator.name} target dead or invalid - checking for other enemies");
            attackController.targetToAttack = null; // Clear the dead target

            // NEW: Check for other enemies before exiting combat
            if (CheckForOtherEnemies(animator))
            {
                Debug.Log($"{animator.name} found another enemy - transitioning to Follow");
                animator.SetBool("isAttacking", false);
                animator.SetBool("isFollowing", true);
                return;
            }

            // No other enemies found - exit to idle/patrol
            Debug.Log($"{animator.name} no other enemies - exiting to idle");
            ExitToIdle(animator);
            return;
        }

        // PRIORITY 5: Check distance - too far to attack
        float distanceFromTarget = Vector3.Distance(animator.transform.position, attackController.targetToAttack.position);

        if (distanceFromTarget > exitAttackDistance)
        {
            Debug.Log($"{animator.name} target too far ({distanceFromTarget:F2}) - switching to follow");
            animator.SetBool("isAttacking", false);
            animator.SetBool("isFollowing", true);
            return;
        }

        // PRIORITY 6: We're good to attack - look at target and attack
        Vector3 lookDirection = (attackController.targetToAttack.position - animator.transform.position).normalized;
        lookDirection.y = 0;
        if (lookDirection != Vector3.zero)
        {
            animator.transform.rotation = Quaternion.LookRotation(lookDirection);
        }

        // Attack with cooldown
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            PerformAttack(attackController.targetToAttack);
            lastAttackTime = Time.time;
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // SAFER: Re-enable NavMeshAgent when leaving attack state
        if (agent != null && agent.isOnNavMesh && agent.enabled)
        {
            agent.isStopped = false;
        }

        wasPausedInThisState = false;
        Debug.Log($"{animator.name} exited Attack state");
    }

    /// <summary>
    /// Check for other enemies when current target dies
    /// </summary>
    private bool CheckForOtherEnemies(Animator animator)
    {
        if (attackController == null) return false;

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
            return true;
        }

        return false;
    }

    /// <summary>
    /// Check if we should target this object
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

    /// <summary>
    /// Properly exit to idle state by clearing all animator bools
    /// </summary>
    private void ExitToIdle(Animator animator)
    {
        animator.SetBool("isAttacking", false);
        //animator.SetBool("isFollowing", false);

        if (unitMovement != null && unitMovement.patrolInterruptedByCombat)
        {
            unitMovement.ResumePatrolAfterCombat();
            animator.SetBool("isMoving", true);
        }
        else
        {
            animator.SetBool("isMoving", false);
        }
    }

    private void PerformAttack(Transform target)
    {
        Debug.Log($"{attackController.name} attacking {target.name}!");

        if (attackController != null)
        {
            attackController.DealDamage(target);
        }

        // Here you could add visual/audio effects
    }
}
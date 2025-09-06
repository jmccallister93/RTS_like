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
            // This prevents immediate attacking after resuming
            float pauseDuration = Time.time - pausedLastAttackTime;
            lastAttackTime = Time.time - (pausedLastAttackTime - lastAttackTime);
        }

        // PRIORITY 1: Check if player is commanding movement - override AI
        if (unitMovement != null && unitMovement.isCommandedtoMove)
        {
            Debug.Log($"{animator.name} player commanding movement - exiting attack");
            ExitToIdle(animator);
            return;
        }

        // PRIORITY 2: Check if target is null or destroyed
        if (attackController == null || attackController.targetToAttack == null)
        {
            Debug.Log($"{animator.name} no target - exiting attack to idle");
            ExitToIdle(animator);
            return;
        }

        // PRIORITY 3: Check if target is still alive
        Unit targetUnit = attackController.targetToAttack.GetComponent<Unit>();
        if (targetUnit == null || !targetUnit.IsAlive())
        {
            Debug.Log($"{animator.name} target dead - exiting attack to idle");
            attackController.targetToAttack = null; // Clear the dead target
            ExitToIdle(animator);
            return;
        }

        // PRIORITY 4: Check distance - too far to attack
        float distanceFromTarget = Vector3.Distance(animator.transform.position, attackController.targetToAttack.position);

        if (distanceFromTarget > exitAttackDistance)
        {
            Debug.Log($"{animator.name} target too far ({distanceFromTarget:F2}) - switching to follow");
            animator.SetBool("isAttacking", false);
            animator.SetBool("isFollowing", true);
            return;
        }

        // PRIORITY 5: We're good to attack - look at target and attack
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
        // Re-enable NavMeshAgent when leaving attack state
        if (agent != null)
        {
            agent.isStopped = false;
        }

        wasPausedInThisState = false;
        Debug.Log($"{animator.name} exited Attack state");
    }

    /// <summary>
    /// Properly exit to idle state by clearing all animator bools
    /// </summary>
    private void ExitToIdle(Animator animator)
    {
        animator.SetBool("isAttacking", false);
        animator.SetBool("isFollowing", false);
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
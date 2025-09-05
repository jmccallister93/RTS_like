using UnityEngine;
using UnityEngine.AI;

public class UnitFollowState : StateMachineBehaviour
{
    AttackController attackController;
    NavMeshAgent agent;
    UnitMovement unitMovement;
    public float attackingDistance = 1.5f;
    private bool hasSetDestination = false;
    private float originalStoppingDistance;

    // Pause-related fields
    private bool wasPausedInThisState = false;
    private Vector3 pausedDestination;
    private bool hadValidTarget = false;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        attackController = animator.transform.GetComponent<AttackController>();
        attackController.SetFollowStateMaterial();
        agent = animator.transform.GetComponent<NavMeshAgent>();
        unitMovement = animator.transform.GetComponent<UnitMovement>();
        hasSetDestination = false;
        wasPausedInThisState = false;

        originalStoppingDistance = agent.stoppingDistance;
        agent.stoppingDistance = attackingDistance * 0.5f;

        agent.enabled = true;
        agent.isStopped = false;

        Debug.Log($"Follow State Entered - Attack Distance: {attackingDistance}");
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
                hadValidTarget = attackController.targetToAttack != null;
                if (hadValidTarget && agent.hasPath)
                {
                    pausedDestination = agent.destination;
                }
            }
            return; // Don't process any logic while paused
        }

        // If we were paused and just resumed, restore state
        if (wasPausedInThisState)
        {
            wasPausedInThisState = false;

            // If we had a valid target when paused, restore the path
            if (hadValidTarget && attackController.targetToAttack != null)
            {
                hasSetDestination = false; // Force re-evaluation of destination
            }
        }

        // Add guard check
        if (unitMovement != null && unitMovement.currentMode == MovementMode.Guard)
        {
            // Check if we're chasing too far from guard position
            if (attackController.isGuarding)
            {
                float distanceFromGuardPoint = Vector3.Distance(
                    animator.transform.position,
                    attackController.guardPosition
                );

                if (distanceFromGuardPoint > attackController.guardRadius * 0.8f) // 80% of radius
                {
                    // Too far from guard point, return
                    agent.SetDestination(attackController.guardPosition);
                    attackController.targetToAttack = null;
                    animator.SetBool("isFollowing", false);
                    return;
                }
            }
        }

        // FIRST: Check if unit is commanded to move elsewhere - pause AI
        if (unitMovement != null && unitMovement.isCommandedtoMove &&
        unitMovement.currentMode == MovementMode.Move)
        {
            // Clear target and return to idle during pure movement
            attackController.targetToAttack = null;
            animator.SetBool("isFollowing", false);
            return;
        }

        // SECOND: Check if we should return to idle (no target)
        if (attackController.targetToAttack == null)
        {
            animator.SetBool("isFollowing", false);
            return;
        }

        // THIRD: Check if target is still alive
        Unit targetUnit = attackController.targetToAttack.GetComponent<Unit>();
        if (targetUnit != null && !targetUnit.IsAlive())
        {
            attackController.targetToAttack = null;
            animator.SetBool("isFollowing", false);
            return;
        }

        // FOURTH: Resume AI behavior - check distance for attack
        float distanceFromTarget = Vector3.Distance(animator.transform.position, attackController.targetToAttack.position);

        if (distanceFromTarget <= attackingDistance)
        {
            agent.isStopped = true;
            agent.ResetPath();
            animator.SetBool("isAttacking", true);
            return;
        }

        // FIFTH: Move toward target
        if (!hasSetDestination ||
            !agent.hasPath ||
            agent.isStopped ||
            Vector3.Distance(agent.destination, attackController.targetToAttack.position) > 1f)
        {
            agent.isStopped = false;
            agent.SetDestination(attackController.targetToAttack.position);
            hasSetDestination = true;
        }

        // Look at target
        Vector3 lookDirection = (attackController.targetToAttack.position - animator.transform.position).normalized;
        lookDirection.y = 0;
        if (lookDirection != Vector3.zero)
        {
            animator.transform.rotation = Quaternion.LookRotation(lookDirection);
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        hasSetDestination = false;
        wasPausedInThisState = false;
        hadValidTarget = false;

        if (agent != null)
        {
            agent.stoppingDistance = originalStoppingDistance;
        }
    }
}
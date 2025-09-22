using UnityEngine;
using UnityEngine.AI;

public class UnitFollowState : StateMachineBehaviour
{
    private AttackController attackController;
    private NavMeshAgent agent;
    private UnitMovement unitMovement;

    //Get attack range from AttackController
    private float attackingDistance;
    private bool hasSetDestination = false;
    private float originalStoppingDistance;

    // Pause state
    private bool wasPausedInThisState = false;
    private bool hadValidTarget = false;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        attackController = animator.GetComponent<AttackController>();
        agent = animator.GetComponent<NavMeshAgent>();
        unitMovement = animator.GetComponent<UnitMovement>();

        hasSetDestination = false;
        wasPausedInThisState = false;

        if (attackController != null)
        {
            attackingDistance = attackController.attackRange;
        }

        if (agent != null)
        {
            originalStoppingDistance = agent.stoppingDistance;
            agent.stoppingDistance = attackingDistance;
            agent.enabled = true;
            agent.isStopped = false;
        }
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
        {
            if (!wasPausedInThisState)
            {
                wasPausedInThisState = true;
                hadValidTarget = attackController != null && attackController.targetToAttack != null;
            }
            return;
        }
        else if (wasPausedInThisState)
        {
            wasPausedInThisState = false;
            hasSetDestination = false; 
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
            Debug.Log($"{animator.name} player commanding movement from Follow - transitioning to Moving");

            // Clear target since player is overriding
            if (attackController != null)
            {
                attackController.targetToAttack = null;
            }

            // Transition directly to Moving state
            animator.SetBool("isFollowing", false);
            animator.SetBool("isAttacking", false);
            animator.SetBool("isMoving", true);
            return;
        }

        if (attackController == null || attackController.targetToAttack == null)
        {
            ClearAndExit(animator);
            return;
        }

        // Check target alive
        Unit targetUnit = attackController.targetToAttack.GetComponent<Unit>();
        if (targetUnit == null || !targetUnit.IsAlive())
        {
            attackController.targetToAttack = null;
            ClearAndExit(animator);
            return;
        }

        // In attack range
        float distance = Vector3.Distance(animator.transform.position, attackController.targetToAttack.position);
        if (distance <= attackingDistance)
        {
            //Debug.Log($"{animator.name} in attack range of {attackController.targetToAttack.name}, switching to Attack state");
            if (agent != null)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
            animator.SetBool("isAttacking", true);
            return;
        }

        // Otherwise chase
        if (agent != null && (!hasSetDestination || !agent.hasPath || agent.isStopped ||
            Vector3.Distance(agent.destination, attackController.targetToAttack.position) > 1f))
        {
            agent.isStopped = false;
            agent.SetDestination(attackController.targetToAttack.position);
            hasSetDestination = true;
        }

        // Face target
        Vector3 lookDir = (attackController.targetToAttack.position - animator.transform.position).normalized;
        lookDir.y = 0;
        if (lookDir != Vector3.zero)
        {
            animator.transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        hasSetDestination = false;
        wasPausedInThisState = false;
        hadValidTarget = false;

        if (agent != null)
        {
            // NEW: Don't clear path/destination if player is commanding movement
            bool playerCommanding = unitMovement != null && unitMovement.isCommandedtoMove;

            if (!playerCommanding)
            {
                agent.isStopped = true;
                agent.ResetPath(); // Only clear path if NOT transitioning to player movement
            }

            agent.stoppingDistance = originalStoppingDistance;
        }

        Debug.Log($"{animator.name} exited Follow state");
    }

    private void ClearAndExit(Animator animator)
    {
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        // Always clear following flag first
        animator.SetBool("isFollowing", false);

        // NEW: Resume patrol if we were patrolling before combat
        if (unitMovement != null && unitMovement.IsPatrolling())
        {
            unitMovement.ResumePatrolAfterCombat();
            animator.SetBool("isMoving", true);
        }
    }
}

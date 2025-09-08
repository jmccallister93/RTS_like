using UnityEngine;

/// <summary>
/// Stores state context so units can return to what they were doing after ability use
/// </summary>
public class StateContext : MonoBehaviour
{
    [System.Serializable]
    public class SavedState
    {
        public string previousState;
        public Vector3 moveDestination;
        public MovementMode movementMode;
        public Transform attackTarget;
        public Vector3 guardPosition;
        public float guardRadius;
        public bool wasPatrolling;
        public Vector3 patrolEndPoint;
        public bool movingToEndPoint;
        public bool patrolInterrupted;
    }

    private SavedState savedState = new SavedState();
    private UnitMovement unitMovement;
    private AttackController attackController;

    private void Start()
    {
        unitMovement = GetComponent<UnitMovement>();
        attackController = GetComponent<AttackController>();
    }

    public void SaveCurrentState(Animator animator)
    {
        savedState = new SavedState();

        // Determine current state
        if (animator.GetBool("isAttacking"))
        {
            savedState.previousState = "Attack";
            if (attackController != null)
            {
                savedState.attackTarget = attackController.targetToAttack;
            }
        }
        else if (animator.GetBool("isFollowing"))
        {
            savedState.previousState = "Follow";
            if (attackController != null)
            {
                savedState.attackTarget = attackController.targetToAttack;
            }
        }
        else if (animator.GetBool("isMoving"))
        {
            savedState.previousState = "Moving";
            if (unitMovement != null)
            {
                savedState.moveDestination = unitMovement.GetDestination();
                savedState.movementMode = unitMovement.currentMode;

                // Special handling for patrol
                if (unitMovement.currentMode == MovementMode.Patrol)
                {
                    savedState.wasPatrolling = true;
                    //savedState.patrolEndPoint = unitMovement.patrolEndPoint;
                    //savedState.movingToEndPoint = unitMovement.movingToEndPoint;
                    savedState.patrolInterrupted = unitMovement.patrolInterruptedByCombat;
                }
            }
        }
        else
        {
            savedState.previousState = "Idle";
        }

        // Save guard information if applicable
        if (attackController != null && attackController.isGuarding)
        {
            savedState.guardPosition = attackController.guardPosition;
            savedState.guardRadius = attackController.guardRadius;
        }

        Debug.Log($"{name} saved state: {savedState.previousState}");
    }

    public void RestorePreviousState(Animator animator)
    {
        Debug.Log($"{name} restoring state: {savedState.previousState}");

        // Clear all animator bools first
        animator.SetBool("isMoving", false);
        animator.SetBool("isFollowing", false);
        animator.SetBool("isAttacking", false);

        switch (savedState.previousState)
        {
            case "Attack":
                RestoreAttackState(animator);
                break;
            case "Follow":
                RestoreFollowState(animator);
                break;
            case "Moving":
                RestoreMovingState(animator);
                break;
            case "Idle":
            default:
                RestoreIdleState(animator);
                break;
        }
    }

    private void RestoreAttackState(Animator animator)
    {
        if (savedState.attackTarget != null && attackController != null)
        {
            // Check if target is still alive
            Unit targetUnit = savedState.attackTarget.GetComponent<Unit>();
            if (targetUnit != null && targetUnit.IsAlive())
            {
                attackController.targetToAttack = savedState.attackTarget;
                animator.SetBool("isAttacking", true);
                Debug.Log($"{name} restored attack target: {savedState.attackTarget.name}");
                return;
            }
        }

        // Target dead or invalid - return to idle/patrol
        RestoreIdleState(animator);
    }

    private void RestoreFollowState(Animator animator)
    {
        if (savedState.attackTarget != null && attackController != null)
        {
            // Check if target is still alive and in range
            Unit targetUnit = savedState.attackTarget.GetComponent<Unit>();
            if (targetUnit != null && targetUnit.IsAlive())
            {
                attackController.targetToAttack = savedState.attackTarget;
                animator.SetBool("isFollowing", true);
                Debug.Log($"{name} restored follow target: {savedState.attackTarget.name}");
                return;
            }
        }

        // Target dead or invalid - return to idle/patrol
        RestoreIdleState(animator);
    }

    private void RestoreMovingState(Animator animator)
    {
        //if (unitMovement != null)
        //{
        //    if (savedState.wasPatrolling)
        //    {
        //        // Restore patrol state
        //        unitMovement.currentMode = MovementMode.Patrol;
        //        unitMovement.patrolEndPoint = savedState.patrolEndPoint;
        //        unitMovement.movingToEndPoint = savedState.movingToEndPoint;
        //        unitMovement.patrolInterruptedByCombat = savedState.patrolInterrupted;

        //        Vector3 destination = savedState.movingToEndPoint ? savedState.patrolEndPoint : unitMovement.patrolStartPoint;
        //        unitMovement.MoveToPosition(destination, MovementMode.Patrol);
        //        Debug.Log($"{name} restored patrol movement");
        //    }
        //    else if (savedState.moveDestination != Vector3.zero)
        //    {
        //        // Restore regular movement
        //        unitMovement.MoveToPosition(savedState.moveDestination, savedState.movementMode);
        //        Debug.Log($"{name} restored movement to: {savedState.moveDestination}");
        //    }
        //    else
        //    {
        //        RestoreIdleState(animator);
        //    }
        //}
        //else
        //{
        //    RestoreIdleState(animator);
        //}
    }

    private void RestoreIdleState(Animator animator)
    {
        // Check if we were patrolling before
        if (savedState.wasPatrolling && unitMovement != null)
        {
            unitMovement.ResumePatrolAfterCombat();
            animator.SetBool("isMoving", true);
            Debug.Log($"{name} restored to patrol after ability");
        }
        else
        {
            // Restore guard position if we had one
            if (savedState.guardPosition != Vector3.zero && attackController != null)
            {
                attackController.SetGuardPosition(savedState.guardPosition, savedState.guardRadius);
                Debug.Log($"{name} restored guard position");
            }

            Debug.Log($"{name} restored to idle state");
        }
    }
}
using UnityEngine;
using UnityEngine.AI;

public class UnitMovingState : StateMachineBehaviour
{
    private NavMeshAgent agent;
    private UnitMovement unitMovement;
    private AttackController attackController; 

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        agent = animator.GetComponent<NavMeshAgent>();
        unitMovement = animator.GetComponent<UnitMovement>();
        attackController = animator.GetComponent<AttackController>(); 

        // Set the moving state material
        if (attackController != null)
        {
            attackController.SetMovingStateMaterial();
        }

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
        Debug.Log($"{animator.name} entered Movement state");
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

        // 2. End moving if no command or reached destination
        if (!unitMovement.isCommandedtoMove ||
            (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance))
        {
            unitMovement.isCommandedtoMove = false;
            unitMovement.currentMode = MovementMode.None;

            // Clear the moving flag to return to Idle
            animator.SetBool("isMoving", false);

            Debug.Log($"{animator.name} finished moving, returning to Idle");
            return;
        }

        // 3. Keep destination in sync with UnitMovement
        Vector3 targetDestination = unitMovement.GetDestination();
        if (agent.destination != targetDestination && targetDestination != Vector3.zero)
        {
            agent.SetDestination(targetDestination);
        }
    }
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Only stop if movement was canceled
        if (unitMovement != null && !unitMovement.isCommandedtoMove)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        Debug.Log($"{animator.name} exited Movement state");
    }
}

using UnityEngine;
using UnityEngine.AI;

public class UnitUsingAbilityState : StateMachineBehaviour
{
    private NavMeshAgent agent;
    private UnitMovement unitMovement;
    private AttackController attackController;
    private AbilityManager abilityManager;

    // Ability execution
    private bool abilityStarted = false;
    private float abilityDuration = 0f;
    private float abilityStartTime = 0f;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        agent = animator.GetComponent<NavMeshAgent>();
        unitMovement = animator.GetComponent<UnitMovement>();
        attackController = animator.GetComponent<AttackController>();
        abilityManager = animator.GetComponent<AbilityManager>();

        abilityStarted = false;
        abilityStartTime = 0f;

        // Stop all movement immediately
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        // Store current state context before ability (this should be done by the triggering system)
        Debug.Log($"{animator.name} entered Ability state");
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Handle pause
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
        {
            return;
        }

        // Start ability on first update
        if (!abilityStarted)
        {
            StartAbility(animator);
        }

        // Check if ability is complete
        if (abilityStarted && Time.time - abilityStartTime >= abilityDuration)
        {
            CompleteAbility(animator);
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        abilityStarted = false;

        // Don't restart agent here - let the next state handle it
        Debug.Log($"{animator.name} exited Ability state");
    }

    private void StartAbility(Animator animator)
    {
        abilityStarted = true;
        abilityStartTime = Time.time;

        if (abilityManager != null)
        {
            //// Get ability duration from the ability controller
            //abilityDuration = abilityManager.GetCurrentAbilityDuration();

            //// Execute the ability
            //abilityManager.ExecuteCurrentAbility();

            Debug.Log($"{animator.name} started ability - duration: {abilityDuration}");
        }
        else
        {
            // Fallback duration if no ability controller
            abilityDuration = 1f;
            Debug.LogWarning($"{animator.name} no AbilityController found, using default duration");
        }
    }

    private void CompleteAbility(Animator animator)
    {
        Debug.Log($"{animator.name} ability completed - returning to previous state");

        // Clear ability flag - this transitions to Idle
        animator.SetBool("isUsingAbility", false);

        // Use StateContext to set the right flags for the next transition
        if (unitMovement != null)
        {
            var stateContext = unitMovement.GetComponent<StateContext>();
            if (stateContext != null)
            {
                // This will set the appropriate animator bools to transition from Idle
                stateContext.RestorePreviousState(animator);
                return;
            }
        }

        // Fallback - stay in idle
        Debug.Log($"{animator.name} no state context found, staying in Idle");
    }
}
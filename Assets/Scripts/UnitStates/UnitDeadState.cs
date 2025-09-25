using UnityEngine;
using UnityEngine.AI;

public class UnitDeadState : StateMachineBehaviour
{
    private bool hasExecutedDeathLogic = false;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (hasExecutedDeathLogic) return; // Prevent multiple executions

        Debug.Log($"{animator.name} entered Dead state");

        // Execute death logic once
        ExecuteDeathLogic(animator);
        hasExecutedDeathLogic = true;
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Dead units don't need to update anything
        // This state is essentially a "do nothing" state
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Dead units should never exit this state, but just in case
        Debug.Log($"{animator.name} exited Dead state (this shouldn't happen)");
    }

    private void ExecuteDeathLogic(Animator animator)
    {
        GameObject unit = animator.gameObject;

        // 0. FIRST: Notify all units that this unit is dead (broadcast cleanup)
        NotifyUnitsOfDeath(unit);

        // 1. Disable all colliders (so unit can't be targeted or selected)
        Collider[] colliders = unit.GetComponents<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }

        // 2. Disable NavMeshAgent to stop pathfinding
        NavMeshAgent agent = unit.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = false;
        }

        // 3. Disable movement component
        UnitMovement movement = unit.GetComponent<UnitMovement>();
        if (movement != null)
        {
            movement.enabled = false;
        }

        // 4. Disable attack controller
        AttackController attackController = unit.GetComponent<AttackController>();
        if (attackController != null)
        {
            attackController.enabled = false;

            Renderer renderer = unit.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Make the dead unit darker/grayer
                Material deadMaterial = new Material(Shader.Find("Standard"));
                deadMaterial.color = Color.gray;
                renderer.material = deadMaterial;
            }
            
        }

        // 5. Rotate the capsule 90 degrees to lay it on its side
        // Rotate around the X-axis to make it lay down
        unit.transform.rotation = Quaternion.Euler(90f, unit.transform.rotation.eulerAngles.y, unit.transform.rotation.eulerAngles.z);

        // 6. Disable health bar
        HealthTracker healthBar = unit.GetComponentInChildren<HealthTracker>();
        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(false);
        }

        // 7. Disable guard area display
        GuardAreaDisplay guardDisplay = unit.GetComponent<GuardAreaDisplay>();
        if (guardDisplay != null)
        {
            guardDisplay.enabled = false;
        }

        // 8. Change layer to a "DeadUnits" layer to prevent interference
        int deadLayer = LayerMask.NameToLayer("Default"); // Use Default layer or create a "DeadUnits" layer
        unit.layer = deadLayer;

        // 9. Remove the Unit component to ensure IsAlive() returns false permanently
        Unit unitComponent = unit.GetComponent<Unit>();
        if (unitComponent != null)
        {
            // Mark as permanently dead
            unitComponent.enabled = false;
        }

        Debug.Log($"{unit.name} death visuals applied: rotated, colliders disabled, components disabled");
    }

    /// <summary>
    /// Notify all living units that this unit has died so they can clear their targets
    /// </summary>
    private void NotifyUnitsOfDeath(GameObject deadUnit)
    {
        // Find all units in the scene and clear this dead unit as their target
        AttackController[] allAttackControllers = Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None);

        foreach (AttackController controller in allAttackControllers)
        {
            if (controller != null && controller.enabled && controller.targetToAttack != null)
            {
                if (controller.targetToAttack.gameObject == deadUnit)
                {
                    Debug.Log($"{controller.name} clearing dead target {deadUnit.name}");
                    controller.targetToAttack = null;

                    // Force the attacking unit back to idle state
                    Animator attackerAnimator = controller.GetComponent<Animator>();
                    if (attackerAnimator != null)
                    {
                        attackerAnimator.SetBool("isAttacking", false);
                        attackerAnimator.SetBool("isFollowing", false);
                    }

                    // Stop movement
                    UnitMovement movement = controller.GetComponent<UnitMovement>();
                    if (movement != null)
                    {
                        movement.StopMovement();
                    }
                }
            }
        }
    }
}
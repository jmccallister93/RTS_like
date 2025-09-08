using UnityEngine;

public class UnitIdleState : StateMachineBehaviour
{
    AttackController attackController;
    UnitMovement unitMovement;

    private float targetCheckInterval = 0.1f; // Check for targets every 0.1 seconds
    private float lastTargetCheck = 0f;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        attackController = animator.transform.GetComponent<AttackController>();
        unitMovement = animator.transform.GetComponent<UnitMovement>();

        // Clear all state parameters when entering Idle
        animator.SetBool("isMoving", false);
        animator.SetBool("isFollowing", false);
        animator.SetBool("isAttacking", false);

        if (attackController != null)
        {
            attackController.SetIdleStateMaterial();
        }

        lastTargetCheck = Time.time;
        Debug.Log($"{animator.name} entered Idle state");
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Don't do AI behavior while player is commanding movement
        if (unitMovement != null && unitMovement.isCommandedtoMove)
        {

            animator.SetBool("isMoving", true);
            return;
        }

        //CheckForTargets(animator);

        // Check for targets periodically (not every frame for performance)
        if (Time.time - lastTargetCheck >= targetCheckInterval)
        {
            lastTargetCheck = Time.time;
            CheckForTargets(animator);
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Debug.Log($"{animator.name} exited Idle state");
    }

    private void CheckForTargets(Animator animator)
    {
        if (attackController == null) return;

        // Check if we have a target from the AttackController
        if (attackController.targetToAttack != null)
        {
            // Make sure target is still alive and valid
            Unit targetUnit = attackController.targetToAttack.GetComponent<Unit>();
            if (targetUnit != null && targetUnit.IsAlive())
            {
                Debug.Log($"{animator.name} has target {attackController.targetToAttack.name}, switching to Follow state");
                animator.SetBool("isFollowing", true);
                return;
            }
            else
            {
                // Clear dead or invalid target
                Debug.Log($"{animator.name} clearing invalid target");
                attackController.targetToAttack = null;
            }
        }

        // If no target from AttackController, try to find one manually
        // This is a backup in case the trigger detection fails
        FindNearestEnemy(animator);
    }

    /// <summary>
    /// Backup target finding method in case trigger detection fails
    /// </summary>
    private void FindNearestEnemy(Animator animator)
    {
        if (attackController == null) return;

        float detectionRange = 10f; // Match the sphere collider radius

        // Get the sphere collider radius if available
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
            Debug.Log($"{animator.name} manually found target {nearestEnemy.name} at distance {nearestDistance:F2}");
            animator.SetBool("isFollowing", true);
        }
    }

  
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
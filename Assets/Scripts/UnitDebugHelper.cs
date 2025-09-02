using UnityEngine;

/// <summary>
/// Add this script to a unit temporarily to debug state machine issues
/// </summary>
public class UnitDebugHelper : MonoBehaviour
{
    [Header("Debug Info")]
    public bool showDebugInfo = true;
    public bool showAnimatorInfo = true;
    public bool showTargetInfo = true;

    private Animator animator;
    private AttackController attackController;
    private UnitMovement unitMovement;
    private Unit unit;

    private void Start()
    {
        animator = GetComponent<Animator>();
        attackController = GetComponent<AttackController>();
        unitMovement = GetComponent<UnitMovement>();
        unit = GetComponent<Unit>();
    }

    private void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        GUILayout.Label($"=== {gameObject.name} Debug ===");
        GUILayout.Label($"Tag: {gameObject.tag}");
        GUILayout.Label($"Layer: {gameObject.layer}");

        // Health info
        if (unit != null)
        {
            GUILayout.Label($"Health: {unit.GetCurrentHealth():F1}/{unit.GetMaxHealth():F1}");
            GUILayout.Label($"Alive: {unit.IsAlive()}");
        }

        // Movement info
        if (unitMovement != null)
        {
            GUILayout.Label($"Commanded to Move: {unitMovement.isCommandedtoMove}");
        }

        // Target info
        if (showTargetInfo && attackController != null)
        {
            if (attackController.targetToAttack != null)
            {
                float distance = Vector3.Distance(transform.position, attackController.targetToAttack.position);
                GUILayout.Label($"Target: {attackController.targetToAttack.name}");
                GUILayout.Label($"Target Distance: {distance:F2}");

                Unit targetUnit = attackController.targetToAttack.GetComponent<Unit>();
                if (targetUnit != null)
                {
                    GUILayout.Label($"Target Health: {targetUnit.GetCurrentHealth():F1}");
                    GUILayout.Label($"Target Alive: {targetUnit.IsAlive()}");
                }
            }
            else
            {
                GUILayout.Label("Target: None");
            }
        }

        // Animator info
        if (showAnimatorInfo && animator != null)
        {
            GUILayout.Label("=== Animator ===");

            // Get current state info
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            GUILayout.Label($"Current State: {stateInfo.IsName("Idle")} Idle");
            GUILayout.Label($"Current State: {stateInfo.IsName("Follow")} Follow");
            GUILayout.Label($"Current State: {stateInfo.IsName("Attack")} Attack");

            GUILayout.Label($"isFollowing: {animator.GetBool("isFollowing")}");
            GUILayout.Label($"isAttacking: {animator.GetBool("isAttacking")}");
        }

        // Collider info
        SphereCollider sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider != null)
        {
            GUILayout.Label($"Detection Radius: {sphereCollider.radius}");
            GUILayout.Label($"Is Trigger: {sphereCollider.isTrigger}");
        }

        GUILayout.EndArea();
    }

    // Manual testing buttons
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T) && attackController != null)
        {
            Debug.Log($"=== {gameObject.name} Manual Target Search ===");

            // Find all nearby units
            Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, 15f);
            Debug.Log($"Found {nearbyColliders.Length} nearby colliders");

            foreach (Collider col in nearbyColliders)
            {
                if (col.transform == transform) continue;

                Unit nearbyUnit = col.GetComponent<Unit>();
                if (nearbyUnit != null)
                {
                    float distance = Vector3.Distance(transform.position, col.transform.position);
                    Debug.Log($"  - {col.name} (Tag: {col.tag}, Distance: {distance:F2}, Alive: {nearbyUnit.IsAlive()})");
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.R) && animator != null)
        {
            Debug.Log($"=== {gameObject.name} Reset Animator ===");
            animator.SetBool("isFollowing", false);
            animator.SetBool("isAttacking", false);
        }
    }
}
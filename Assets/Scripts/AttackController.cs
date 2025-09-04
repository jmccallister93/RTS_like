using UnityEngine;

public class AttackController : MonoBehaviour
{
    public Transform targetToAttack;

    [Header("Materials")]
    public Material idleStateMaterial;
    public Material followStateMaterial;
    public Material attackStateMaterial;

    [Header("Attack Settings")]
    [SerializeField] private float attackDamage = 25f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float detectionRange = 10f; // For manual override if needed



    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    [Header("Guard Mode")]
    public bool isGuarding { get; private set; } = false;  // Public getter
    public Vector3 guardPosition { get; private set; }      // Public getter
    public float guardRadius { get; private set; } = 5f;    // Public getter // Adjustable guard area size

    private void Start()
    {
        // Make sure we have a sphere collider for detection
        SphereCollider detectionCollider = GetComponent<SphereCollider>();
        if (detectionCollider != null && detectionCollider.isTrigger)
        {
            detectionRange = detectionCollider.radius;
            if (showDebugLogs)
                Debug.Log($"{gameObject.name} detection range: {detectionRange}");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} missing trigger SphereCollider for enemy detection!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (showDebugLogs)
            Debug.Log($"{gameObject.name} detected {other.name} entering trigger");

        // Check if we're in pure Move mode
        UnitMovement movement = GetComponent<UnitMovement>();
        if (movement != null && movement.isCommandedtoMove &&
            movement.currentMode == MovementMode.Move)
        {
            // Ignore enemies during pure movement
            return;
        }

        if (isGuarding)
        {
            float distanceFromGuardPoint = Vector3.Distance(other.transform.position, guardPosition);
            if (distanceFromGuardPoint > guardRadius)
            {
                return; // Ignore enemies outside guard area
            }
        }

        // Only auto-target if we don't already have a target
        if (targetToAttack == null && ShouldAutoTarget(other.gameObject))
        {
            targetToAttack = other.transform;
            if (showDebugLogs)
                Debug.Log($"{gameObject.name} auto-targeting {other.name}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Only clear target if it's the one leaving
        if (targetToAttack != null && other.transform == targetToAttack)
        {
            // Check distance to make sure they're really out of range
            float distance = Vector3.Distance(transform.position, other.transform.position);
            if (distance > detectionRange * 1.1f) // Small buffer to prevent flickering
            {
                if (showDebugLogs)
                    Debug.Log($"{gameObject.name} lost target {other.name} (distance: {distance:F2})");
                targetToAttack = null;
            }
        }
    }

    // Update method to handle lost targets that might have been destroyed
    private void Update()
    {
        // Check if our target is still valid
        if (targetToAttack != null)
        {
            // Check if target was destroyed
            if (targetToAttack == null)
            {
                if (showDebugLogs)
                    Debug.Log($"{gameObject.name} target was destroyed");
                return;
            }

            // Check if target is still alive
            Unit targetUnit = targetToAttack.GetComponent<Unit>();
            if (targetUnit != null && !targetUnit.IsAlive())
            {
                if (showDebugLogs)
                    Debug.Log($"{gameObject.name} target {targetToAttack.name} died, clearing target");
                targetToAttack = null;
                return;
            }

            // Check if target is too far (in case OnTriggerExit didn't fire)
            if (targetToAttack != null)
            {
                float distance = Vector3.Distance(transform.position, targetToAttack.position);

              
            }

        }
        // If guarding, check if target is leaving guard area
        if (isGuarding && targetToAttack != null)
        {
            float distanceFromGuardPoint = Vector3.Distance(targetToAttack.position, guardPosition);
            if (distanceFromGuardPoint > guardRadius)
            {
                // Target left guard area, stop pursuing
                targetToAttack = null;

                // Return to guard position
                GetComponent<UnitMovement>()?.MoveToPosition(guardPosition, MovementMode.Guard);
            }
        }
    }

    public void SetGuardPosition(Vector3 position, float radius = 5f)
    {
        isGuarding = true;
        guardPosition = position;
        guardRadius = radius;

        // Move to guard position
        GetComponent<UnitMovement>()?.MoveToPosition(position, MovementMode.Guard);
    }

    public void ClearGuardPosition()
    {
        isGuarding = false;
    }

    /// <summary>
    /// Determines if we should automatically target this unit
    /// </summary>
    private bool ShouldAutoTarget(GameObject potentialTarget)
    {
        // Don't target ourselves
        if (potentialTarget == this.gameObject)
            return false;

        // Must have a Unit component
        Unit targetUnit = potentialTarget.GetComponent<Unit>();
        if (targetUnit == null || !targetUnit.IsAlive())
            return false;

        // Check team affiliation
        string myTag = this.gameObject.tag;
        string targetTag = potentialTarget.tag;

        if (showDebugLogs)
            Debug.Log($"{gameObject.name} ({myTag}) checking target {potentialTarget.name} ({targetTag})");

        // Player units should target Enemy units
        if (myTag == "Player" && targetTag == "Enemy")
            return true;

        // Enemy units should target Player units
        if (myTag == "Enemy" && targetTag == "Player")
            return true;

        return false; // Same team or untagged - don't auto-target
    }

    /// <summary>
    /// Manually set a target (called by UnitSelectionManager on right-click)
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        // Clear guard mode when manually targeting
        ClearGuardPosition();

        if (newTarget != null && ShouldAutoTarget(newTarget.gameObject))
        {
            targetToAttack = newTarget;

            // Clear any current movement commands and set the unit to follow the target
            UnitMovement movement = GetComponent<UnitMovement>();
            if (movement != null)
            {
                movement.StopMovement();
            }

            // Trigger the animator to start following
            Animator animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetBool("isFollowing", true);
            }

            if (showDebugLogs)
                Debug.Log($"{gameObject.name} manually targeting {newTarget.name}");
        }
    }

    /// <summary>
    /// Called by the attack state to deal damage
    /// </summary>
    public void DealDamage(Transform target)
    {
        if (target == null)
        {
            if (showDebugLogs)
                Debug.Log($"{gameObject.name} tried to attack null target");
            return;
        }

        Unit targetUnit = target.GetComponent<Unit>();
        if (targetUnit != null && targetUnit.IsAlive())
        {
            targetUnit.TakeDamage(attackDamage);
            if (showDebugLogs)
                Debug.Log($"{gameObject.name} dealt {attackDamage} damage to {target.name}");
        }
        else if (showDebugLogs)
        {
            Debug.Log($"{gameObject.name} tried to attack {target.name} but target is dead or not a unit");
        }
    }

    // Material setting methods with null checks
    public void SetIdleStateMaterial()
    {
        var renderer = GetComponent<Renderer>();
        if (renderer != null && idleStateMaterial != null)
            renderer.material = idleStateMaterial;
    }

    public void SetFollowStateMaterial()
    {
        var renderer = GetComponent<Renderer>();
        if (renderer != null && followStateMaterial != null)
            renderer.material = followStateMaterial;
    }

    public void SetAttackStateMaterial()
    {
        var renderer = GetComponent<Renderer>();
        if (renderer != null && attackStateMaterial != null)
            renderer.material = attackStateMaterial;
    }

    private void OnDrawGizmos()
    {
        // Detection range (large sphere)
        Gizmos.color = new Color(1f, 1f, 0f, 0.1f); // Transparent yellow
        Gizmos.DrawSphere(transform.position, detectionRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Attack range (smaller sphere)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 1.5f);

        // Follow range (medium sphere)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 2.0f);

        // Draw line to current target
        if (targetToAttack != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, targetToAttack.position);

            // Draw target indicator
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(targetToAttack.position + Vector3.up * 2f, Vector3.one * 0.5f);
        }
    }
}
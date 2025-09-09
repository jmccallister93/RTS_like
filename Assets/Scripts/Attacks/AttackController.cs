using UnityEngine;

public class AttackController : MonoBehaviour, IPausable
{
    public Transform targetToAttack;

    [Header("Materials")]
    public Material idleStateMaterial;
    public Material movingStateMaterial;
    public Material followStateMaterial;
    public Material attackStateMaterial;

    [Header("Attack Settings")]
    [SerializeField] private float attackDamage = 25f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float maxAttackRange = 2.5f; // Maximum range for attacking while holding position

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    [Header("Guard Mode")]
    public bool isGuarding { get; private set; } = false;
    public Vector3 guardPosition { get; private set; }
    public float guardRadius { get; private set; } = 5f;

    // Pause-related fields
    private Transform pausedTarget;
    private bool hadTargetWhenPaused;

    private UnitMovement movement;
    private Unit unitComponent; // NEW: Reference to Unit component

    private void Awake()
    {
        movement = GetComponent<UnitMovement>();
        unitComponent = GetComponent<Unit>(); // NEW: Get Unit component
    }

    private void Start()
    {
        var detectionCollider = GetComponent<SphereCollider>();
        if (detectionCollider != null && detectionCollider.isTrigger)
        {
            detectionRange = detectionCollider.radius;
            if (showDebugLogs) Debug.Log($"{name} detection range: {detectionRange}");
        }
        else
        {
            Debug.LogWarning($"{name} missing trigger SphereCollider for enemy detection!");
        }

        //PauseManager.Instance?.RegisterPausable(this); // optional if you keep these helpers; not required with centralized scan
    }

    //private void OnDestroy()
    //{
    //    // Unregister from pause manager
    //    if (PauseManager.Instance != null)
    //    {
    //        PauseManager.Instance.UnregisterPausable(this);
    //    }
    //}

    public void OnPause()
    {
        hadTargetWhenPaused = targetToAttack != null;
        pausedTarget = targetToAttack;
    }

    public void OnResume()
    {
        if (hadTargetWhenPaused && pausedTarget != null)
        {
            var unit = pausedTarget.GetComponent<Unit>();
            targetToAttack = (unit != null && unit.IsAlive()) ? pausedTarget : null;
        }
        hadTargetWhenPaused = false;
        pausedTarget = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (showDebugLogs) Debug.Log($"{name} detected {other.name} entering trigger");

        // NEW: Don't acquire targets while holding position
        if (unitComponent != null && unitComponent.IsHoldingPosition())
        {
            if (showDebugLogs) Debug.Log($"{name} is holding position, ignoring new target {other.name}");
            return;
        }

        // Ignore while in pure Move mode
        var movement = GetComponent<UnitMovement>();
        if (movement != null && movement.isCommandedtoMove && movement.currentMode == MovementMode.Move)
            return;

        if (isGuarding)
        {
            float d = Vector3.Distance(other.transform.position, guardPosition);
            if (d > guardRadius) return;
        }

        if (targetToAttack == null && ShouldAutoTarget(other.gameObject))
        {
            targetToAttack = other.transform;
            if (showDebugLogs) Debug.Log($"{name} auto-targeting {other.name}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (targetToAttack != null && other.transform == targetToAttack)
        {
            float distance = Vector3.Distance(transform.position, other.transform.position);
            if (distance > detectionRange * 1.1f)
            {
                if (showDebugLogs) Debug.Log($"{name} lost target {other.name} (distance: {distance:F2})");
                targetToAttack = null;
            }
        }
    }

    // Update method to handle lost targets that might have been destroyed
    private void Update()
    {
        if (movement != null && movement.currentMode == MovementMode.Move)
            return;

        // Check if current target is dead and clear it immediately
        if (targetToAttack != null)
        {
            var unit = targetToAttack.GetComponent<Unit>();
            if (unit == null || !unit.IsAlive())
            {
                if (showDebugLogs) Debug.Log($"{name} clearing dead/invalid target: {targetToAttack.name}");
                targetToAttack = null;

                // Force transition back to idle state
                var animator = GetComponent<Animator>();
                if (animator != null)
                {
                    animator.SetBool("isAttacking", false);
                    animator.SetBool("isFollowing", false);
                }

                movement.StopMovement();
                return;
            }

            // NEW: Handle hold position behavior with current target
            if (unitComponent != null && unitComponent.IsHoldingPosition())
            {
                float distanceToTarget = Vector3.Distance(transform.position, targetToAttack.position);

                // Only attack if target is within close range, don't chase
                if (distanceToTarget > maxAttackRange)
                {
                    if (showDebugLogs) Debug.Log($"{name} holding position - target {targetToAttack.name} moved out of attack range");
                    targetToAttack = null;

                    // Stop any movement and set to idle
                    var animator = GetComponent<Animator>();
                    if (animator != null)
                    {
                        animator.SetBool("isAttacking", false);
                        animator.SetBool("isFollowing", false);
                    }
                    movement.StopMovement();
                }
                else
                {
                    // Target is close enough - stop movement but continue attacking
                    movement.StopMovement();

                    var animator = GetComponent<Animator>();
                    if (animator != null)
                    {
                        animator.SetBool("isFollowing", false); // Don't follow while holding
                        // Let attacking animation continue if in range
                    }
                }
                return; // Don't process normal movement/guard logic while holding
            }
        }

        // Normal guard behavior (only if not holding position)
        if (isGuarding && targetToAttack != null && (unitComponent == null || !unitComponent.IsHoldingPosition()))
        {
            float d = Vector3.Distance(targetToAttack.position, guardPosition);
            if (d > guardRadius)
            {
                targetToAttack = null;
                GetComponent<UnitMovement>()?.MoveToPosition(guardPosition, MovementMode.Guard);
            }
        }
    }

    public void SetGuardPosition(Vector3 position, float radius = 5f)
    {
        isGuarding = true;
        guardPosition = position;
        guardRadius = radius;

        // NEW: Don't move to guard position if holding position
        if (unitComponent == null || !unitComponent.IsHoldingPosition())
        {
            GetComponent<UnitMovement>()?.MoveToPosition(position, MovementMode.Guard);
        }
    }

    public void ClearGuardPosition() => isGuarding = false;

    private bool ShouldAutoTarget(GameObject potentialTarget)
    {
        if (potentialTarget == gameObject) return false;

        var unit = potentialTarget.GetComponent<Unit>();
        if (unit == null || !unit.IsAlive()) return false; // This now checks isDead flag

        string myTag = tag;
        string theirTag = potentialTarget.tag;

        if (myTag == "Player" && theirTag == "Enemy") return true;
        if (myTag == "Enemy" && theirTag == "Player") return true;
        return false;
    }

    public void SetTarget(Transform newTarget)
    {
        // NEW: Don't set new targets while holding position (except for direct attack commands)
        if (unitComponent != null && unitComponent.IsHoldingPosition())
        {
            // Only allow targeting if the target is within attack range
            if (newTarget != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, newTarget.position);
                if (distanceToTarget > maxAttackRange)
                {
                    if (showDebugLogs) Debug.Log($"{name} holding position - ignoring target {newTarget.name} (too far: {distanceToTarget:F2})");
                    return;
                }
            }
        }

        ClearGuardPosition();

        if (newTarget != null && ShouldAutoTarget(newTarget.gameObject))
        {
            targetToAttack = newTarget;

            // NEW: Don't stop movement or set following if holding position
            if (unitComponent == null || !unitComponent.IsHoldingPosition())
            {
                movement.StopMovement();

                var animator = GetComponent<Animator>();
                if (animator) animator.SetBool("isFollowing", true);
            }

            if (showDebugLogs) Debug.Log($"{name} manually targeting {newTarget.name}");
        }
    }

    // NEW: Method to handle hold position state changes
    public void SetHoldPosition(bool holdPosition)
    {
        if (holdPosition)
        {
            // When entering hold position, stop movement and clear guard
            movement?.StopMovement();
            ClearGuardPosition();

            // If we have a target that's too far, clear it
            if (targetToAttack != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, targetToAttack.position);
                if (distanceToTarget > maxAttackRange)
                {
                    targetToAttack = null;

                    var animator = GetComponent<Animator>();
                    if (animator != null)
                    {
                        animator.SetBool("isAttacking", false);
                        animator.SetBool("isFollowing", false);
                    }
                }
            }

            if (showDebugLogs) Debug.Log($"{name} entering hold position mode");
        }
        else
        {
            if (showDebugLogs) Debug.Log($"{name} exiting hold position mode");
        }
    }

    public void DealDamage(Transform target)
    {
        if (target == null)
        {
            if (showDebugLogs) Debug.Log($"{name} tried to attack null target");
            return;
        }

        var u = target.GetComponent<Unit>();
        if (u != null && u.IsAlive())
        {
            u.TakeDamage(attackDamage);
            if (showDebugLogs) Debug.Log($"{name} dealt {attackDamage} to {target.name}");
        }
        else if (showDebugLogs)
        {
            Debug.Log($"{name} tried to attack {target.name} but target is dead or not a unit");
        }
    }

    // Material setting methods with null checks
    public void SetIdleStateMaterial()
    {
        var renderer = GetComponent<Renderer>();
        if (renderer != null && idleStateMaterial != null)
            renderer.material = idleStateMaterial;
    }

    public void SetMovingStateMaterial()
    {
        var renderer = GetComponent<Renderer>();
        if (renderer != null && movingStateMaterial != null)
            renderer.material = movingStateMaterial;
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

        // NEW: Draw max attack range for hold position (if holding)
        if (unitComponent != null && unitComponent.IsHoldingPosition())
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // Orange for hold position range
            Gizmos.DrawSphere(transform.position, maxAttackRange);
            Gizmos.color = Color.orange;
            Gizmos.DrawWireSphere(transform.position, maxAttackRange);
        }

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
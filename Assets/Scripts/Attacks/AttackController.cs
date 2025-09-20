using UnityEngine;

public class AttackController : MonoBehaviour, IPausable
{
    public Transform targetToAttack;

    [Header("Attack Settings")]
    [SerializeField] public float attackDamage = 25f;
    [SerializeField] public float attackRange = 2f;
    [SerializeField] public float detectionRange = 10f;

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
    private Unit unitComponent; 

    private void Awake()
    {
        movement = GetComponent<UnitMovement>();
        unitComponent = GetComponent<Unit>(); 
    }

    private void Start()
    {
        var detectionCollider = GetComponent<SphereCollider>();
        if (detectionCollider != null && detectionCollider.isTrigger)
        {
            detectionRange = detectionCollider.radius;
        }

    }


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

        // Don't acquire targets while holding position
        if (unitComponent != null && unitComponent.IsHoldingPosition()) return;

        // Ignore while in pure Move mode
        var movement = GetComponent<UnitMovement>();
        if (movement != null && movement.isCommandedtoMove && movement.currentMode == MovementMode.Move) return;

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
        if (movement != null && movement.currentMode == MovementMode.Move) return;

        // Check if current target is dead and clear it immediately
        if (targetToAttack != null)
        {
            var unit = targetToAttack.GetComponent<Unit>();
            if (unit == null || !unit.IsAlive())
            {
               
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

            // Handle hold position behavior with current target
            if (unitComponent != null && unitComponent.IsHoldingPosition())
            {
                float distanceToTarget = Vector3.Distance(transform.position, targetToAttack.position);

                // Only attack if target is within close range, don't chase
                if (distanceToTarget > attackRange)
                {
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
        if (unit == null || !unit.IsAlive()) return false; 

        string myTag = tag;
        string theirTag = potentialTarget.tag;

        if (myTag == "Player" && theirTag == "Enemy") return true;
        if (myTag == "Enemy" && theirTag == "Player") return true;
        return false;
    }

    public void SetTarget(Transform newTarget)
    {
        // Don't set new targets while holding position (except for direct attack commands)
        if (unitComponent != null && unitComponent.IsHoldingPosition())
        {
            // Only allow targeting if the target is within attack range
            if (newTarget != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, newTarget.position);
                if (distanceToTarget > attackRange) return;
            }
        }

        ClearGuardPosition();

        if (newTarget != null && ShouldAutoTarget(newTarget.gameObject))
        {
            targetToAttack = newTarget;

            // Don't stop movement or set following if holding position
            if (unitComponent == null || !unitComponent.IsHoldingPosition())
            {
                movement.StopMovement();

                var animator = GetComponent<Animator>();
                if (animator) animator.SetBool("isFollowing", true);
            }
        }
    }

    // Method to handle hold position state changes
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
                if (distanceToTarget > attackRange)
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
        }
    }

    public void DealDamage(Transform target)
    {
        if (target == null) return;

        var u = target.GetComponent<Unit>();
        if (u != null && u.IsAlive()) u.TakeDamage(attackDamage);  
    }

   
}
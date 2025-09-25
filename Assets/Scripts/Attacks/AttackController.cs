using UnityEngine;

public class AttackController : MonoBehaviour, IPausable
{
    public Transform targetToAttack;

    [Header("Attack Settings")]
    [SerializeField] public float attackRange = 2f;
    [SerializeField] public float detectionRange = 10f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    [Header("Guard Mode")]
    public bool isGuarding { get; private set; } = false;
    public Vector3 guardPosition { get; private set; }
    public float guardRadius { get; private set; } = 5f;

    // Component references
    private Transform pausedTarget;
    private bool hadTargetWhenPaused;
    private UnitMovement movement;
    private Unit unitComponent;
    private CharacterManager characterManager; // NEW: For damage values



    private void Awake()
    {
        movement = GetComponent<UnitMovement>();
        unitComponent = GetComponent<Unit>();
        characterManager = GetComponent<CharacterManager>(); // NEW
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
        if (unitComponent != null && unitComponent.IsHoldingPosition()) return;

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

                var animator = GetComponent<Animator>();
                if (animator != null)
                {
                    animator.SetBool("isAttacking", false);
                    animator.SetBool("isFollowing", false);
                }

                movement.StopMovement();
                return;
            }

            // Handle hold position behavior
            if (unitComponent != null && unitComponent.IsHoldingPosition())
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
                    movement.StopMovement();
                }
                else
                {
                    movement.StopMovement();

                    var animator = GetComponent<Animator>();
                    if (animator != null)
                    {
                        animator.SetBool("isFollowing", false);
                    }
                }
                return;
            }
        }

        // Normal guard behavior
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
        if (unitComponent != null && unitComponent.IsHoldingPosition())
        {
            if (newTarget != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, newTarget.position);
                if (distanceToTarget > attackRange) return;
            }
        }

        ClearGuardPosition();

        if (newTarget != null && ShouldAutoTarget(newTarget.gameObject))
        {
            // Stop all movement and clear any existing states
            if (movement != null)
            {
                movement.StopMovement(); // This clears isCommandedtoMove
            }

            // Set the target
            targetToAttack = newTarget;

            if (unitComponent == null || !unitComponent.IsHoldingPosition())
            {
                var animator = GetComponent<Animator>();
                if (animator != null)
                {
                    // Clear all movement-related animator states first
                    animator.SetBool("isMoving", false);
                    animator.SetBool("isAttacking", false);

                    // Then set follow state
                    animator.SetBool("isFollowing", true);

                    if (showDebugLogs)
                        Debug.Log($"{name} setting target to {newTarget.name}, transitioning to Follow state");
                }
            }
        }
        else if (showDebugLogs && newTarget != null)
        {
            Debug.Log($"{name} cannot target {newTarget.name} - invalid target or hold position preventing");
        }
    }

    public void SetHoldPosition(bool holdPosition)
    {
        if (holdPosition)
        {
            movement?.StopMovement();
            ClearGuardPosition();

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
        if (u != null && u.IsAlive())
        {
            // Use melee damage from CharacterManager if available
            float damage = characterManager?.MeleeDamage ?? 25f;
            u.TakeDamage(damage);
        }
    }
}
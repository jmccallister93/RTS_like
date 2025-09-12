using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class Unit : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("UI References")]
    [SerializeField] private HealthTracker healthBar;

    [Header("Death Settings")]
    [SerializeField] private float corpseLifetime = 10f;

    [Header("Damage Text")]
    [SerializeField] private GameObject floatingDamageTextPrefab;
    [SerializeField] private Transform floatingTextSpawnPoint;

    private UnitMovement unitMovement;
    private AttackController attackController;
    private GuardAreaDisplay guardAreaDisplay;
    private Animator unitAnimator;

    // Death state
    private bool isDead = false;

    // NEW: Hold position state
    private bool isHoldingPosition = false;

    void Start()
    {
        // Initialize components
        unitMovement = GetComponent<UnitMovement>();
        attackController = GetComponent<AttackController>();
        unitAnimator = GetComponent<Animator>();

        // Initialize health
        currentHealth = maxHealth;

        // Find and setup health bar if it exists
        if (healthBar == null)
        {
            healthBar = GetComponentInChildren<HealthTracker>();
        }
        if (healthBar != null)
        {
            healthBar.UpdateSliderValue(currentHealth, maxHealth);
        }

        //Initialize selection
        if (UnitSelectionManager.Instance != null)
        {
            UnitSelectionManager.Instance.allUnitsList.Add(this.gameObject);
        }

        guardAreaDisplay = GetComponent<GuardAreaDisplay>();
        if (guardAreaDisplay == null)
        {
            guardAreaDisplay = gameObject.AddComponent<GuardAreaDisplay>();
        }
        MoveToDisplay moveDisplay = GetComponent<MoveToDisplay>();
        if (moveDisplay == null)
        {
            moveDisplay = gameObject.AddComponent<MoveToDisplay>();
        }
    }

    private void OnDestroy()
    {
        // Remove from both lists when destroyed
        if (UnitSelectionManager.Instance != null)
        {
            UnitSelectionManager.Instance.RemoveUnitFromSelection(this.gameObject);
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (healthBar != null)
            healthBar.UpdateSliderValue(currentHealth, maxHealth);

        // Spawn floating damage text
        if (floatingDamageTextPrefab != null)
        {
            Vector3 spawnPos = floatingTextSpawnPoint != null
                ? floatingTextSpawnPoint.position
                : transform.position + Vector3.up * 2f;

            // Slight random horizontal jitter so multiple hits don’t overlap perfectly
            spawnPos += new Vector3(Random.Range(-0.2f, 0.2f), 0f, Random.Range(-0.2f, 0.2f));

            GameObject textObj = Instantiate(floatingDamageTextPrefab, spawnPos, Quaternion.identity);
            var fdt = textObj.GetComponent<FloatingDamageText>();
            if (fdt != null)
            {
                // You can pass a color override (e.g., Color.yellow for crits)
                fdt.Initialize(damageAmount);
            }
        }

        if (currentHealth <= 0)
            Die();

        Debug.Log($"{gameObject.name} took {damageAmount} damage. Health: {currentHealth}/{maxHealth}");
    }

    private void Die()
    {
        if (isDead) return; // Already dead

        Debug.Log($"{gameObject.name} has died!");

        isDead = true;

        // Remove from selection manager lists immediately
        if (UnitSelectionManager.Instance != null)
        {
            UnitSelectionManager.Instance.RemoveUnitFromSelection(this.gameObject);
        }

        // Clear any current target (so other units stop attacking this one)
        if (attackController != null)
        {
            attackController.targetToAttack = null;
        }

        // Trigger dead state in animator
        if (unitAnimator != null)
        {
            unitAnimator.SetBool("isDead", true);
        }

        // Start corpse cleanup timer
        //Invoke(nameof(DestroyCorpse), corpseLifetime);
    }

    //private void DestroyCorpse()
    //{
    //    Destroy(gameObject);
    //}

    // Public getter for other scripts to check health
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public bool IsAlive() => !isDead; // Updated to check isDead flag
    public bool IsDead() => isDead;

    // NEW: Public getter for hold position state
    public bool IsHoldingPosition() => isHoldingPosition;

    public void MoveTo(Vector3 targetPosition)
    {
        if (isDead || unitMovement == null) return;

        // Clear hold position state when given new move command
        ClearHoldPosition();

        if (attackController != null)
        {
            attackController.ClearGuardPosition();
            attackController.targetToAttack = null;
        }
        unitMovement.MoveToPosition(targetPosition);
    }

    public void AttackMoveTo(Vector3 targetPosition)
    {
        if (isDead || unitMovement == null) return;

        // Clear hold position state when given new attack move command
        ClearHoldPosition();

        if (attackController != null)
        {
            attackController.ClearGuardPosition();
        }
        unitMovement.MoveToPosition(targetPosition, MovementMode.AttackMove);
    }

    public void GuardPosition(Vector3 guardPosition, float radius = 5f)
    {
        if (isDead || attackController == null) return;

        // Clear hold position state when given new guard command
        ClearHoldPosition();

        attackController.SetGuardPosition(guardPosition, radius);
    }

    public void PatrolTo(Vector3 patrolPoint)
    {
        if (isDead || unitMovement == null) return;

        // Clear hold position state when given new patrol command
        ClearHoldPosition();

        if (attackController != null)
        {
            attackController.ClearGuardPosition();
        }
        unitMovement.StartPatrol(patrolPoint);
    }

    public void StopMovement()
    {
        if (isDead || unitMovement == null) return;

        // Clear hold position state - stop is different from hold
        ClearHoldPosition();

        unitMovement.StopMovement();
    }

    // NEW: Hold Position - stops movement and prevents attacking/following
    public void HoldPosition()
    {
        if (isDead) return;

        // Set hold position state
        isHoldingPosition = true;

        // Stop current movement
        if (unitMovement != null)
        {
            unitMovement.StopMovement();
        }

        // Clear attack target and guard position
        if (attackController != null)
        {
            attackController.ClearGuardPosition();
            attackController.targetToAttack = null;
            
        }
    }

    
    public void ClearHoldPosition()
    {
        if (isHoldingPosition)
        {
            Debug.Log($"{gameObject.name} is no longer holding position");
            isHoldingPosition = false;

            // Notify attack controller if needed
            if (attackController != null)
            {
                // attackController.SetHoldPosition(false);
            }
        }
    }
}
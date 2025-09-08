using System;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("UI References")]
    [SerializeField] private HealthTracker healthBar;

    [Header("Death Settings")]
    [SerializeField] private float corpseLifetime = 10f; // How long the corpse stays before being destroyed

    private UnitMovement unitMovement;
    private AttackController attackController;
    private GuardAreaDisplay guardAreaDisplay;
    private Animator unitAnimator;

    // Death state
    private bool isDead = false;

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
        if (isDead) return; // Can't damage dead units

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // Update health bar
        if (healthBar != null)
        {
            healthBar.UpdateSliderValue(currentHealth, maxHealth);
        }

        // Check if unit died
        if (currentHealth <= 0)
        {
            Die();
        }

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

    public void MoveTo(Vector3 targetPosition)
    {
        if (isDead || unitMovement == null) return;

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

        if (attackController != null)
        {
            attackController.ClearGuardPosition();
        }
        unitMovement.MoveToPosition(targetPosition, MovementMode.AttackMove);
    }

    public void GuardPosition(Vector3 guardPosition, float radius = 5f)
    {
        if (isDead || attackController == null) return;

        attackController.SetGuardPosition(guardPosition, radius);
    }

    public void PatrolTo(Vector3 patrolPoint)
    {
        if (isDead || unitMovement == null) return;

        if (attackController != null)
        {
            attackController.ClearGuardPosition();
        }
        unitMovement.StartPatrol(patrolPoint);
    }

    public void StopMovement()
    {
        if (isDead || unitMovement == null) return;

        unitMovement.StopMovement();
    }
}
using System;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [Header("UI References")]
    [SerializeField] private HealthTracker healthBar;
    private UnitMovement unitMovement;
    private AttackController attackController;
    private GuardAreaDisplay guardAreaDisplay;

    void Start()
    {
        // Initialize components
        unitMovement = GetComponent<UnitMovement>();
        attackController = GetComponent<AttackController>();

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
        Debug.Log($"{gameObject.name} has died!");

        // Remove from selection manager lists
        if (UnitSelectionManager.Instance != null)
        {
            UnitSelectionManager.Instance.RemoveUnitFromSelection(this.gameObject);
        }

        // Destroy the unit
        Destroy(gameObject);
    }

    // Public getter for other scripts to check health
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public bool IsAlive() => currentHealth > 0;

    public void MoveTo(Vector3 targetPosition)
    {
        if (unitMovement != null)
        {
            if (attackController != null)
            {
                attackController.ClearGuardPosition();
                attackController.targetToAttack = null;
            }
            unitMovement.MoveToPosition(targetPosition);
        }
    }

    public void AttackMoveTo(Vector3 targetPosition)
    {
        if (unitMovement != null)
        {
            attackController.ClearGuardPosition();
            unitMovement.MoveToPosition(targetPosition, MovementMode.AttackMove);
        }
    }

    public void GuardPosition(Vector3 guardPosition, float radius = 5f)
    {
        if (attackController != null)
        {
            attackController.SetGuardPosition(guardPosition, radius);
        }
    }

    public void PatrolTo(Vector3 patrolPoint)
    {
        if (unitMovement != null)
        {
            attackController.ClearGuardPosition();
            unitMovement.StartPatrol(patrolPoint);
        }
    }

    public void StopMovement()
    {
        if (unitMovement != null)
        {
            unitMovement.StopMovement();
        }
    }
}
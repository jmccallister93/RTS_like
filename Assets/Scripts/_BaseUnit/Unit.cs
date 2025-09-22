using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class Unit : MonoBehaviour
{
    //[Header("Health Settings")]
    //[SerializeField] private float maxHealth = 100f;
    //[SerializeField] private float currentHealth;

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
    private CharacterManager characterManager;
    private Animator unitAnimator;

    private bool isDead = false;

    private bool isHoldingPosition = false;

    void Start()
    {
        // Initialize components
        unitMovement = GetComponent<UnitMovement>();
        attackController = GetComponent<AttackController>();
        characterManager = GetComponent<CharacterManager>();
        unitAnimator = GetComponent<Animator>();

        // Initialize health
        //currentHealth = maxHealth;

        //// Find and setup health bar if it exists
        if (healthBar == null)
        {
            healthBar = GetComponentInChildren<HealthTracker>();
        }
        if (healthBar != null && characterManager != null)
        {
            healthBar.UpdateSliderValue(characterManager.Health, characterManager.Health);
        }

        // Subscribe to health changes from CharacterManager
        if (characterManager != null)
        {
            characterManager.OnHealthChanged += UpdateHealthUI;
            characterManager.OnDeath += HandleDeath;
        }

        //Initialize selection
        if (UnitSelectionManager.Instance != null) UnitSelectionManager.Instance.allUnitsList.Add(this.gameObject);

        guardAreaDisplay = GetComponent<GuardAreaDisplay>();
        if (guardAreaDisplay == null) guardAreaDisplay = gameObject.AddComponent<GuardAreaDisplay>();

        MoveToDisplay moveDisplay = GetComponent<MoveToDisplay>();
        if (moveDisplay == null) moveDisplay = gameObject.AddComponent<MoveToDisplay>();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (characterManager != null)
        {
            characterManager.OnHealthChanged -= UpdateHealthUI;
            characterManager.OnDeath -= HandleDeath;
        }

        // Remove from selection
        if (UnitSelectionManager.Instance != null) UnitSelectionManager.Instance.RemoveUnitFromSelection(this.gameObject);
    }

    private void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        if (healthBar != null)
        {
            healthBar.UpdateSliderValue(currentHealth, maxHealth);
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (characterManager == null || !IsAlive()) return;

        // Apply damage through CharacterManager
        characterManager.TakeDamage(damageAmount);

        // Spawn floating damage text
        if (floatingDamageTextPrefab != null)
        {
            Vector3 spawnPos = floatingTextSpawnPoint != null
                ? floatingTextSpawnPoint.position
                : transform.position + Vector3.up * 2f;

            spawnPos += new Vector3(Random.Range(-0.2f, 0.2f), 0f, Random.Range(-0.2f, 0.2f));

            GameObject textObj = Instantiate(floatingDamageTextPrefab, spawnPos, Quaternion.identity);
            var fdt = textObj.GetComponent<FloatingDamageText>();
            if (fdt != null)
            {
                fdt.Initialize(damageAmount);
            }
        }
    }

    private void HandleDeath()
    {
        // Remove from selection manager lists immediately
        if (UnitSelectionManager.Instance != null) UnitSelectionManager.Instance.RemoveUnitFromSelection(this.gameObject);

        // Clear any current target
        if (attackController != null) attackController.targetToAttack = null;

        // Trigger dead state in animator
        if (unitAnimator != null) unitAnimator.SetBool("isDead", true);
    }

    // Public getter for other scripts to check health
    public float GetCurrentHealth() => characterManager?.Health ?? 0f;
    public float GetMaxHealth() => characterManager?.MaxHealth ?? 100f;
    public bool IsAlive() => characterManager?.IsAlive ?? false;
    public bool IsDead() => !IsAlive();
    public bool IsHoldingPosition() => isHoldingPosition;

    public void MoveTo(Vector3 targetPosition)
    {
        if (isDead || unitMovement == null) return;

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

        ClearHoldPosition();

        attackController.SetGuardPosition(guardPosition, radius);
    }

    public void PatrolTo(Vector3 patrolPoint)
    {
        if (isDead || unitMovement == null) return;

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

        ClearHoldPosition();

        unitMovement.StopMovement();
    }

    public void HoldPosition()
    {
        if (isDead) return;

        isHoldingPosition = true;

        if (unitMovement != null) unitMovement.StopMovement();

        if (attackController != null)
        {
            attackController.ClearGuardPosition();
            attackController.targetToAttack = null;
            
        }
    }

    
    public void ClearHoldPosition()
    {
        if (isHoldingPosition) isHoldingPosition = false;
    }
}
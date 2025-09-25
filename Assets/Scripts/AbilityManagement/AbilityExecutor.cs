using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Handles ability execution and casting logic
/// </summary>
public class AbilityExecutor : MonoBehaviour
{
    private AbilityManager abilityManager;
    private UnitAbilityTracker unitTracker;
    private AbilityCooldownManager cooldownManager;
    private AbilityTargetingSystem targetingSystem;

    // Pause state
    private bool wasCastingWhenPaused;
    private IAbility pausedCastingAbility;
    private GameObject pausedCaster;

    // Events
    public event Action<GameObject, IAbility> OnAbilityUsed;
    public event Action<GameObject, IAbility> OnAbilityCooldownStarted;

    public bool IsCasting
    {
        get
        {
            for (int i = 0; i < 6; i++)
            {
                if (cooldownManager.IsAbilityCasting(i)) return true;
            }
            return false;
        }
    }

    public void Initialize(AbilityManager manager)
    {
        abilityManager = manager;
        unitTracker = manager.GetComponent<UnitAbilityTracker>();
        cooldownManager = manager.GetComponent<AbilityCooldownManager>();
        targetingSystem = manager.GetComponent<AbilityTargetingSystem>();
    }

    public bool TryExecuteAbility(IAbility ability)
    {
        if (ability == null || abilityManager.CurrentSelectedUnit == null) return false;
        if (!ability.CanUse(abilityManager.CurrentSelectedUnit.gameObject)) return false;

        targetingSystem.CancelTargeting();

        bool paused = PauseManager.Instance != null && PauseManager.Instance.IsPaused;

        switch (ability.TargetType)
        {
            case TargetType.Self:
                if (paused)
                {
                    CommandQueue.Instance.QueueCommand(
                        new AbilityCastCommand(abilityManager.CurrentSelectedUnit.gameObject, ability,
                            abilityManager.CurrentSelectedUnit.transform.position, abilityManager.CurrentSelectedUnit.gameObject));
                }
                else
                {
                    ExecuteAbility(ability, abilityManager.CurrentSelectedUnit.transform.position, abilityManager.CurrentSelectedUnit.gameObject);
                }
                return true;

            case TargetType.Ally:
            case TargetType.Enemy:
            case TargetType.Area:
            case TargetType.Path:
            case TargetType.Point:
                return targetingSystem.StartTargeting(ability);

            default:
                Debug.LogWarning($"Unknown target type: {ability.TargetType}");
                return false;
        }
    }

    public void ExecuteAbilityQueued(GameObject caster, IAbility ability, Vector3 targetPosition, GameObject target = null)
    {
        // Temporarily treat this caster as selected so slot/cooldowns map correctly
        var prevUnit = abilityManager.CurrentSelectedUnit;
        var prevTracker = unitTracker.CurrentSelectedUnit;

        // Set the caster as current for execution
        unitTracker.UpdateSelectedUnit(); // This might need a different approach

        try
        {
            ExecuteAbility(ability, targetPosition, target);
        }
        finally
        {
            // Restore previous selection if needed
        }
    }

    public void ExecuteAbility(IAbility ability, Vector3 targetPosition, GameObject target = null)
    {
        var caster = abilityManager.CurrentSelectedUnit.gameObject;

        if (caster == null)
        {
            Debug.LogError("currentSelectedUnit is null in ExecuteAbility!");
            return;
        }

        if (!ability.CanUse(caster))
        {
            Debug.LogWarning("CanUse failed in ExecuteAbility!");
            return;
        }

        // Get the ability slot for cooldown tracking
        int slotIndex = unitTracker.GetAbilitySlotIndex(ability);

        if (slotIndex == -1)
        {
            Debug.LogError("Ability not found in slots!");
            return;
        }

        // Start casting or execute immediately
        if (ability.CastTime > 0)
        {
            StartCoroutine(CastAbility(caster, ability, targetPosition, target, slotIndex));
        }
        else
        {
            ability.Execute(caster, targetPosition, target);
            cooldownManager.StartCooldown(slotIndex, ability);
            OnAbilityUsed?.Invoke(caster, ability);
            OnAbilityCooldownStarted?.Invoke(caster, ability);
        }
    }

    private IEnumerator CastAbility(GameObject caster, IAbility ability, Vector3 targetPosition, GameObject target, int slotIndex)
    {
        cooldownManager.SetCastingState(slotIndex);
        ability.StartCast(caster, targetPosition, target);

        float castTime = ability.CastTime;
        float elapsed = 0f;

        while (elapsed < castTime)
        {
            // Check if casting was interrupted
            if (!cooldownManager.IsAbilityCasting(slotIndex))
            {
                ability.Cancel(caster);
                yield break;
            }

            // Pause handling
            if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
            {
                yield return null;
                continue;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Complete the cast
        if (caster != null)
        {
            ability.Execute(caster, targetPosition, target);
            cooldownManager.StartCooldown(slotIndex, ability);
            OnAbilityUsed?.Invoke(caster, ability);
            OnAbilityCooldownStarted?.Invoke(caster, ability);
        }
        else
        {
            cooldownManager.ClearCastingState(slotIndex);
        }
    }

    public void CancelCasting(int slotIndex = -1)
    {
        if (slotIndex >= 0)
        {
            cooldownManager.ClearCastingState(slotIndex);
        }
        else
        {
            // Cancel all casting
            for (int i = 0; i < 6; i++)
            {
                if (cooldownManager.IsAbilityCasting(i))
                {
                    cooldownManager.ClearCastingState(i);
                    var ability = unitTracker.GetAbility(i);
                    if (ability != null && abilityManager.CurrentSelectedUnit != null)
                    {
                        ability.Cancel(abilityManager.CurrentSelectedUnit.gameObject);
                    }
                }
            }
        }
    }

    #region Pause System Integration

    public void OnPause()
    {
        // Store casting state if needed
        for (int i = 0; i < 6; i++)
        {
            if (cooldownManager.IsAbilityCasting(i))
            {
                wasCastingWhenPaused = true;
                pausedCastingAbility = unitTracker.GetAbility(i);
                pausedCaster = abilityManager.CurrentSelectedUnit.gameObject;
                break;
            }
        }
    }

    public void OnResume()
    {
        // Restore casting state if needed
        if (wasCastingWhenPaused)
        {
            // Handle resuming cast or cancel based on game rules
            wasCastingWhenPaused = false;
            pausedCastingAbility = null;
            pausedCaster = null;
        }
    }

    #endregion
}
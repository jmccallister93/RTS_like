using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages ability cooldowns and states
/// </summary>
[Serializable]
public struct CooldownInfo
{
    public float currentCooldown;
    public AbilityState state;
}

public class AbilityCooldownManager : MonoBehaviour
{
    private AbilityManager abilityManager;
    private UnitAbilityTracker unitTracker;

    // Per-unit cooldown tracking
    private Dictionary<GameObject, Dictionary<IAbility, float>> unitCooldowns = new Dictionary<GameObject, Dictionary<IAbility, float>>();

    public void Initialize(AbilityManager manager)
    {
        abilityManager = manager;
        unitTracker = manager.GetComponent<UnitAbilityTracker>();
    }

    public void UpdateCooldowns()
    {
        if (abilityManager.CurrentSelectedUnit == null) return;

        for (int i = 0; i < 6; i++)
        {
            var ability = unitTracker.GetAbility(i);
            if (ability == null) continue;

            var cooldownInfo = GetCooldownInfo(i);

            if (cooldownInfo.state == AbilityState.OnCooldown)
            {
                cooldownInfo.currentCooldown -= Time.deltaTime;

                if (cooldownInfo.currentCooldown <= 0)
                {
                    cooldownInfo.currentCooldown = 0;
                    cooldownInfo.state = AbilityState.Ready;
                }

                SetCooldownInfo(i, cooldownInfo);
            }

            // Update ability state based on conditions
            if (cooldownInfo.state == AbilityState.Ready && !ability.CanUse(abilityManager.CurrentSelectedUnit.gameObject))
            {
                cooldownInfo.state = AbilityState.Disabled;
                SetCooldownInfo(i, cooldownInfo);
            }
            else if (cooldownInfo.state == AbilityState.Disabled && ability.CanUse(abilityManager.CurrentSelectedUnit.gameObject))
            {
                cooldownInfo.state = AbilityState.Ready;
                SetCooldownInfo(i, cooldownInfo);
            }
        }
    }

    public void StartCooldown(int slotIndex, IAbility ability)
    {
        var cooldownInfo = new CooldownInfo
        {
            state = AbilityState.OnCooldown,
            currentCooldown = ability.Cooldown
        };

        SetCooldownInfo(slotIndex, cooldownInfo);

        // Track cooldown for this unit
        var currentUnit = abilityManager.CurrentSelectedUnit.gameObject;
        if (currentUnit != null)
        {
            if (!unitCooldowns.ContainsKey(currentUnit))
            {
                unitCooldowns[currentUnit] = new Dictionary<IAbility, float>();
            }
            unitCooldowns[currentUnit][ability] = ability.Cooldown;
        }
    }

    public void SetCastingState(int slotIndex)
    {
        var cooldownInfo = GetCooldownInfo(slotIndex);
        cooldownInfo.state = AbilityState.Casting;
        SetCooldownInfo(slotIndex, cooldownInfo);
    }

    public void ClearCastingState(int slotIndex)
    {
        var cooldownInfo = GetCooldownInfo(slotIndex);
        if (cooldownInfo.state == AbilityState.Casting)
        {
            cooldownInfo.state = AbilityState.Ready;
            SetCooldownInfo(slotIndex, cooldownInfo);
        }
    }

    public CooldownInfo GetCooldownInfo(int slotIndex)
    {
        return unitTracker.GetCooldownInfo(slotIndex);
    }

    private void SetCooldownInfo(int slotIndex, CooldownInfo info)
    {
        unitTracker.SetCooldownInfo(slotIndex, info);
    }

    public void LoadCooldownsForUnit(GameObject unit, Dictionary<IAbility, float> cooldowns)
    {
        if (unit == null || cooldowns == null) return;

        for (int i = 0; i < 6; i++)
        {
            var ability = unitTracker.GetAbility(i);
            if (ability != null && cooldowns.ContainsKey(ability))
            {
                var cooldownInfo = new CooldownInfo
                {
                    currentCooldown = cooldowns[ability],
                    state = cooldowns[ability] > 0 ? AbilityState.OnCooldown : AbilityState.Ready
                };
                SetCooldownInfo(i, cooldownInfo);
            }
        }
    }

    public Dictionary<IAbility, float> GetCooldownsForUnit(GameObject unit)
    {
        if (unitCooldowns.ContainsKey(unit))
        {
            return new Dictionary<IAbility, float>(unitCooldowns[unit]);
        }
        return new Dictionary<IAbility, float>();
    }

    public bool IsAbilityReady(int slotIndex)
    {
        var cooldownInfo = GetCooldownInfo(slotIndex);
        return cooldownInfo.state == AbilityState.Ready;
    }

    public bool IsAbilityCasting(int slotIndex)
    {
        var cooldownInfo = GetCooldownInfo(slotIndex);
        return cooldownInfo.state == AbilityState.Casting;
    }

    public float GetRemainingCooldown(int slotIndex)
    {
        var cooldownInfo = GetCooldownInfo(slotIndex);
        return cooldownInfo.currentCooldown;
    }
}
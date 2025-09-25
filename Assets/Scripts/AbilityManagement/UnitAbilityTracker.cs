using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks ability states per unit and manages unit selection changes
/// </summary>
public class UnitAbilityTracker : MonoBehaviour
{
    // Current state
    private GameObject currentSelectedUnit;
    public Unit CurrentSelectedUnit => currentSelectedUnit != null ? currentSelectedUnit.GetComponent<Unit>() : null;

    private AbilitySlot[] currentAbilitySlots = new AbilitySlot[6];

    // Per-unit storage
    private Dictionary<GameObject, AbilitySlot[]> unitAbilities = new Dictionary<GameObject, AbilitySlot[]>();

    private AbilityManager abilityManager;


    public void Initialize(AbilityManager manager)
    {
        abilityManager = manager;
        InitializeAbilitySlots();
    }

    private void InitializeAbilitySlots()
    {
        for (int i = 0; i < currentAbilitySlots.Length; i++)
        {
            currentAbilitySlots[i] = new AbilitySlot();
        }
    }

    public void UpdateSelectedUnit()
    {
        GameObject newSelectedUnit = GetCurrentSelectedUnit();

        if (newSelectedUnit != currentSelectedUnit)
        {
            // Selection changed
            if (currentSelectedUnit != null)
            {
                SaveUnitAbilityState(currentSelectedUnit);
            }

            currentSelectedUnit = newSelectedUnit;

            if (currentSelectedUnit != null)
            {
                LoadUnitAbilities(currentSelectedUnit);
            }
            else
            {
                ClearAbilities();
            }

            // Cancel any active targeting when selection changes
            abilityManager.CancelTargeting();
        }
    }

    private GameObject GetCurrentSelectedUnit()
    {
        if (UnitSelectionManager.Instance == null) return null;

        var selectedUnits = UnitSelectionManager.Instance.unitsSelected;

        // Only return a unit if exactly one is selected
        if (selectedUnits != null && selectedUnits.Count == 1)
        {
            return selectedUnits[0];
        }

        return null;
    }

    private void SaveUnitAbilityState(GameObject unit)
    {
        if (unit == null) return;

        // Save current ability slots for this unit
        AbilitySlot[] savedSlots = new AbilitySlot[6];
        for (int i = 0; i < currentAbilitySlots.Length; i++)
        {
            savedSlots[i] = new AbilitySlot();
            savedSlots[i].ability = currentAbilitySlots[i].ability;
            savedSlots[i].currentCooldown = currentAbilitySlots[i].currentCooldown;
            savedSlots[i].state = currentAbilitySlots[i].state;
        }
        unitAbilities[unit] = savedSlots;
    }

    private void LoadUnitAbilities(GameObject unit)
    {
        if (unit == null) return;

        // Check if we have saved abilities for this unit
        if (unitAbilities.ContainsKey(unit))
        {
            // Restore saved state
            var savedSlots = unitAbilities[unit];
            for (int i = 0; i < currentAbilitySlots.Length; i++)
            {
                currentAbilitySlots[i].ability = savedSlots[i].ability;
                currentAbilitySlots[i].currentCooldown = savedSlots[i].currentCooldown;
                currentAbilitySlots[i].state = savedSlots[i].state;
            }
        }
        else
        {
            // First time selecting this unit, load default abilities
            LoadDefaultAbilitiesForUnit(unit);
        }
    }

    private void LoadDefaultAbilitiesForUnit(GameObject unit)
    {
        // Clear current abilities
        for (int i = 0; i < currentAbilitySlots.Length; i++)
        {
            currentAbilitySlots[i].ability = null;
            currentAbilitySlots[i].currentCooldown = 0;
            currentAbilitySlots[i].state = AbilityState.Ready;
        }

        // Load abilities from UnitAbilities component
        var unitAbilities = unit.GetComponent<UnitAbilities>();
        if (unitAbilities != null)
        {
            for (int i = 0; i < unitAbilities.abilities.Length && i < currentAbilitySlots.Length; i++)
            {
                if (unitAbilities.abilities[i] != null)
                {
                    currentAbilitySlots[i].ability = unitAbilities.abilities[i];
                    currentAbilitySlots[i].state = AbilityState.Ready;
                }
            }
        }
    }

    private void ClearAbilities()
    {
        for (int i = 0; i < currentAbilitySlots.Length; i++)
        {
            currentAbilitySlots[i].ability = null;
            currentAbilitySlots[i].currentCooldown = 0;
            currentAbilitySlots[i].state = AbilityState.Ready;
        }
    }

    #region Public API

    public void SetAbilityForUnit(GameObject unit, int slotIndex, IAbility ability)
    {
        if (unit == currentSelectedUnit)
        {
            SetAbility(slotIndex, ability);
        }
        else
        {
            // Store for when this unit is selected
            if (!unitAbilities.ContainsKey(unit))
            {
                unitAbilities[unit] = new AbilitySlot[6];
                for (int i = 0; i < 6; i++)
                {
                    unitAbilities[unit][i] = new AbilitySlot();
                }
            }

            if (slotIndex >= 0 && slotIndex < 6)
            {
                unitAbilities[unit][slotIndex].ability = ability;
            }
        }
    }

    public void SetAbility(int slotIndex, IAbility ability)
    {
        if (slotIndex < 0 || slotIndex >= currentAbilitySlots.Length) return;

        currentAbilitySlots[slotIndex].ability = ability;
        currentAbilitySlots[slotIndex].state = AbilityState.Ready;
        currentAbilitySlots[slotIndex].currentCooldown = 0;
    }

    public IAbility GetAbility(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= currentAbilitySlots.Length) return null;
        return currentAbilitySlots[slotIndex].ability;
    }

    public CooldownInfo GetCooldownInfo(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= currentAbilitySlots.Length)
            return new CooldownInfo { currentCooldown = 0, state = AbilityState.Ready };

        return new CooldownInfo
        {
            currentCooldown = currentAbilitySlots[slotIndex].currentCooldown,
            state = currentAbilitySlots[slotIndex].state
        };
    }

    public void SetCooldownInfo(int slotIndex, CooldownInfo info)
    {
        if (slotIndex < 0 || slotIndex >= currentAbilitySlots.Length) return;

        currentAbilitySlots[slotIndex].currentCooldown = info.currentCooldown;
        currentAbilitySlots[slotIndex].state = info.state;
    }

    public int GetAbilitySlotIndex(IAbility ability)
    {
        for (int i = 0; i < currentAbilitySlots.Length; i++)
        {
            if (currentAbilitySlots[i].ability == ability)
                return i;
        }
        return -1;
    }

    #endregion
}
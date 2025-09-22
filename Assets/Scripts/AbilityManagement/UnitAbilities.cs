using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UnitAbilities : MonoBehaviour
{
    [Header("Ability Slots")]
    public AbilitySO[] abilities = new AbilitySO[6]; // 6 ability slots

    [Header("Available Abilities Pool")]
    public List<AbilitySO> availableAbilities = new List<AbilitySO>(); // Unlocked but not assigned

    public AbilitySO GetAbility(int index)
    {
        if (index < 0 || index >= abilities.Length) return null;
        return abilities[index];
    }

    public void SetAbility(int index, AbilitySO ability)
    {
        if (index < 0 || index >= abilities.Length) return;
        abilities[index] = ability;
        OnAbilityChanged?.Invoke(index, ability);
    }

    public void AddAvailableAbility(AbilitySO ability)
    {
        if (!availableAbilities.Contains(ability))
        {
            availableAbilities.Add(ability);
            OnAbilityAvailable?.Invoke(ability);
        }
    }

    public void RemoveAvailableAbility(AbilitySO ability)
    {
        availableAbilities.Remove(ability);
    }

    public bool HasAbility(AbilitySO ability)
    {
        return System.Array.Exists(abilities, a => a == ability) ||
               availableAbilities.Contains(ability);
    }

    // Events
    public System.Action<int, AbilitySO> OnAbilityChanged;
    public System.Action<AbilitySO> OnAbilityAvailable;
}

using UnityEngine;

public class StatManager : MonoBehaviour
{
    private StatBlock baseStats = new StatBlock();
    private StatBlock allocatedStats = new StatBlock();

    private int _statPoints;
    public int StatPoints => _statPoints;

    // Total stats (base + allocated)
    public int Strength => baseStats.strength + allocatedStats.strength;
    public int Dexterity => baseStats.dexterity + allocatedStats.dexterity;
    public int Mind => baseStats.mind + allocatedStats.mind;
    public int Constitution => baseStats.constitution + allocatedStats.constitution;
    public int Reflex => baseStats.reflex + allocatedStats.reflex;
    public int Willpower => baseStats.willpower + allocatedStats.willpower;
    public int Vitality => baseStats.vitality + allocatedStats.vitality;
    public int Mobility => baseStats.mobility + allocatedStats.mobility;
    public int Focus => baseStats.focus + allocatedStats.focus;

    public void SetBaseStats(StatBlock stats)
    {
        baseStats = new StatBlock
        {
            strength = stats.strength,
            dexterity = stats.dexterity,
            mind = stats.mind,
            constitution = stats.constitution,
            reflex = stats.reflex,
            willpower = stats.willpower,
            vitality = stats.vitality,
            mobility = stats.mobility,
            focus = stats.focus
        };
        OnStatChanged?.Invoke();
    }

    public void AddBaseStats(StatBlock stats)
    {
        baseStats.strength += stats.strength;
        baseStats.dexterity += stats.dexterity;
        baseStats.mind += stats.mind;
        baseStats.constitution += stats.constitution;
        baseStats.reflex += stats.reflex;
        baseStats.willpower += stats.willpower;
        baseStats.vitality += stats.vitality;
        baseStats.mobility += stats.mobility;
        baseStats.focus += stats.focus;
        OnStatChanged?.Invoke();
    }

    public void GainStatPoints(int amount)
    {
        _statPoints += amount;
        OnStatPointsChanged?.Invoke();
    }

    public bool CanAffordStat(int cost) => _statPoints >= cost;

    public void IncreaseStat(string statName, int cost)
    {
        if (!CanAffordStat(cost)) return;

        switch (statName.ToLower())
        {
            case "strength": allocatedStats.strength++; break;
            case "dexterity": allocatedStats.dexterity++; break;
            case "mind": allocatedStats.mind++; break;
            case "constitution": allocatedStats.constitution++; break;
            case "reflex": allocatedStats.reflex++; break;
            case "willpower": allocatedStats.willpower++; break;
            case "vitality": allocatedStats.vitality++; break;
            case "mobility": allocatedStats.mobility++; break;
            case "focus": allocatedStats.focus++; break;
            default:
                Debug.LogWarning($"Stat '{statName}' does not exist.");
                return;
        }

        _statPoints -= cost;
        OnStatPointsChanged?.Invoke();
        OnStatChanged?.Invoke();
    }

    // Events
    public System.Action OnStatPointsChanged;
    public System.Action OnStatChanged;
}
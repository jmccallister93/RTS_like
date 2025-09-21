using UnityEngine;

public class StatManager : MonoBehaviour
{
    // Damage stats
    private int _strength, _dexterity, _mind;
    // Defense stats
    private int _constitution, _reflex, _willpower;
    // Base stats
    private int _vitality, _mobility, _focus;

    private int _statPoints;

    public int StatPoints => _statPoints;
    public int Strength => _strength;
    public int Dexterity => _dexterity;
    public int Mind => _mind;
    public int Constitution => _constitution;
    public int Reflex => _reflex;
    public int Willpower => _willpower;
    public int Vitality => _vitality;
    public int Mobility => _mobility;
    public int Focus => _focus;

    private void Awake()
    {
        _strength = 1;
        _dexterity = 1;
        _mind = 1;
        _constitution = 1;
        _reflex = 1;
        _willpower = 1;
        _vitality = 1;
        _mobility = 1;
        _focus = 1;
    }

    public void GainStatPoints(int amount)
    {
        _statPoints += amount;
        OnStatPointsChanged?.Invoke();
    }

    public bool CanAffordStat(int cost)
    {
        return _statPoints >= cost;
    }

    public void IncreaseStat(string statName, int cost)
    {
        if (!CanAffordStat(cost)) return;
        switch (statName.ToLower())
        {
            case "strength":
                _strength++;
                break;
            case "dexterity":
                _dexterity++;
                break;
            case "mind":
                _mind++;
                break;
            case "constitution":
                _constitution++;
                break;
            case "reflex":
                _reflex++;
                break;
            case "willpower":
                _willpower++;
                break;
            case "vitality":
                _vitality++;
                break;
            case "mobility":
                _mobility++;
                break;
            case "focus":
                _focus++;
                break;
            default:
                Debug.LogWarning($"Stat '{statName}' does not exist.");
                return;
        }
        _statPoints -= cost;
        OnStatPointsChanged?.Invoke();
    }

    public delegate void StatPointsChangedAction();

    public event StatPointsChangedAction OnStatPointsChanged;







}

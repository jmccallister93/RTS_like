using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    [SerializeField] private StatManager statManager;

    private int _baseHealth = 10;
    private int _baseSpeed = 1;
    private int _baseAbilityModifier = 1;
    private int _baseMeleeDamage = 1;
    private int _baseRangedDamage = 1;
    private int _baseAbilityDamage = 1;
    private int _baseMeleeDefense = 1;
    private int _baseRangedDefense = 1;
    private int _baseAbilityDefense = 1;

    // Calculated values (with stat bonuses)
    private int _health, _speed, _abilityModifier;
    private int _meleeDamage, _rangedDamage, _abilityDamage;
    private int _meleeDefense, _rangedDefense, _abilityDefense;

    public int Health => _health;
    public int Speed => _speed;
    public int AbilityModifier => _abilityModifier;
    public int MeleeDamage => _meleeDamage;
    public int RangedDamage => _rangedDamage;
    public int AbilityDamage => _abilityDamage;
    public int MeleeDefense => _meleeDefense;
    public int RangedDefense => _rangedDefense;
    public int AbilityDefense => _abilityDefense;

    private void Awake()
    {
        // If StatManager is not assigned, try to find it on the same GameObject
        if (statManager == null)
            statManager = GetComponent<StatManager>();
    }

    private void Start()
    {
        // Subscribe to stat changes
        if (statManager != null)
        {
            statManager.OnStatChanged += RecalculateStats;
        }

        // Calculate initial stats
        RecalculateStats();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (statManager != null)
        {
            statManager.OnStatChanged -= RecalculateStats;
        }
    }

    private void RecalculateStats()
    {
        if (statManager == null) return;

        // Health modified by Constitution and Vitality
        _health = _baseHealth + statManager.Constitution + statManager.Vitality;

        // Speed modified by Mobility and Dexterity
        _speed = _baseSpeed + statManager.Mobility + statManager.Dexterity;

        // Ability Modifier affected by Mind and Focus
        _abilityModifier = _baseAbilityModifier + statManager.Mind + statManager.Focus;

        // Damage calculations (1:1 ratio as requested)
        _meleeDamage = _baseMeleeDamage + statManager.Strength;
        _rangedDamage = _baseRangedDamage + statManager.Dexterity;
        _abilityDamage = _baseAbilityDamage + statManager.Mind;

        // Defense calculations
        _meleeDefense = _baseMeleeDefense + statManager.Constitution;
        _rangedDefense = _baseRangedDefense + statManager.Reflex;
        _abilityDefense = _baseAbilityDefense + statManager.Willpower;

        // Notify that character stats have changed
        OnCharacterStatsChanged?.Invoke();
    }

    // Public method to manually recalculate if needed
    public void ForceRecalculateStats()
    {
        RecalculateStats();
    }

    // Event for when character stats change
    public delegate void CharacterStatsChangedAction();
    public event CharacterStatsChangedAction OnCharacterStatsChanged;

}

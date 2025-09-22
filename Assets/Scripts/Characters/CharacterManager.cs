using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    [SerializeField] private StatManager statManager;

    private int _baseMaxHealth = 10;
    private int _baseSpeed = 1;
    private int _baseAbilityModifier = 1;
    private int _baseMeleeDamage = 1;
    private int _baseRangedDamage = 1;
    private int _baseAbilityDamage = 1;
    private int _baseMeleeDefense = 1;
    private int _baseRangedDefense = 1;
    private int _baseAbilityDefense = 1;

    // Calculated values (with stat bonuses)
    private float _maxHealth, _currentHealth;
    private float _speed, _abilityModifier;
    private float _meleeDamage, _rangedDamage, _abilityDamage;
    private float _meleeDefense, _rangedDefense, _abilityDefense;

    private bool _isAlive = true;

    // Properties
    public float Health => _currentHealth;
    public float MaxHealth => _maxHealth;
    public float Speed => _speed;
    public float AbilityModifier => _abilityModifier;
    public float MeleeDamage => _meleeDamage;
    public float RangedDamage => _rangedDamage;
    public float AbilityDamage => _abilityDamage;
    public float MeleeDefense => _meleeDefense;
    public float RangedDefense => _rangedDefense;
    public float AbilityDefense => _abilityDefense;
    public bool IsAlive => _isAlive;

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
        _currentHealth = _maxHealth;
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

        float previousMaxHealth = _maxHealth;

        // Calculate all stats with stat bonuses
        _maxHealth = _baseMaxHealth + (statManager.Constitution * 5) + (statManager.Vitality * 3);
        _speed = _baseSpeed + statManager.Mobility + statManager.Dexterity;
        _abilityModifier = _baseAbilityModifier + statManager.Mind + statManager.Focus;

        // Damage calculations
        _meleeDamage = _baseMeleeDamage + statManager.Strength;
        _rangedDamage = _baseRangedDamage + statManager.Dexterity;
        _abilityDamage = _baseAbilityDamage + statManager.Mind;

        // Defense calculations
        _meleeDefense = _baseMeleeDefense + statManager.Constitution;
        _rangedDefense = _baseRangedDefense + statManager.Reflex;
        _abilityDefense = _baseAbilityDefense + statManager.Willpower;

        // Adjust current health if max health changed
        if (previousMaxHealth > 0 && _maxHealth != previousMaxHealth)
        {
            float healthPercentage = _currentHealth / previousMaxHealth;
            _currentHealth = _maxHealth * healthPercentage;
        }

        OnCharacterStatsChanged?.Invoke();
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    public void TakeDamage(float damageAmount)
    {
        if (!_isAlive) return;

        _currentHealth -= damageAmount;
        _currentHealth = Mathf.Clamp(_currentHealth, 0, _maxHealth);

        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float healAmount)
    {
        if (!_isAlive) return;

        _currentHealth += healAmount;
        _currentHealth = Mathf.Clamp(_currentHealth, 0, _maxHealth);

        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    private void Die()
    {
        if (!_isAlive) return;

        _isAlive = false;
        OnDeath?.Invoke();
    }

    public void ForceRecalculateStats()
    {
        RecalculateStats();
    }

    // Events
    public System.Action OnCharacterStatsChanged;
    public System.Action<float, float> OnHealthChanged; // current, max
    public System.Action OnDeath;
}


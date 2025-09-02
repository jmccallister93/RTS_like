using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for all unit abilities
/// </summary>
[System.Serializable]
public abstract class UnitAbility : ScriptableObject
{
    [Header("Base Ability Info")]
    public string abilityName;
    public string description;
    public Sprite icon;

    [Header("Ability Properties")]
    public float cooldownTime = 5f;
    public float range = 5f;
    public int resourceCost = 0;
    public AbilityTargetType targetType = AbilityTargetType.None;

    public abstract string DisplayName { get; }
    public virtual Sprite Icon => icon;

    /// <summary>
    /// Check if this ability can be used by the given unit
    /// </summary>
    public abstract bool CanUse(GameObject caster);

    /// <summary>
    /// Execute the ability
    /// </summary>
    public abstract void Execute(GameObject caster, Vector3 targetPosition);

    /// <summary>
    /// Execute the ability on a target
    /// </summary>
    public virtual void Execute(GameObject caster, GameObject target)
    {
        if (target != null)
        {
            Execute(caster, target.transform.position);
        }
    }

    /// <summary>
    /// Get visual feedback for this ability (particles, etc.)
    /// </summary>
    public virtual void PlayVisualEffects(GameObject caster, Vector3 targetPosition) { }
}

/// <summary>
/// Types of ability targeting
/// </summary>
public enum AbilityTargetType
{
    None,           // No targeting required (self-cast)
    Ground,         // Target a position on the ground
    Unit,           // Target a specific unit
    Area            // Area of effect around target position
}

/// <summary>
/// Component that manages abilities for a unit
/// </summary>
public class UnitAbilityManager : MonoBehaviour
{
    [Header("Unit Abilities")]
    [SerializeField] private List<UnitAbility> abilities = new List<UnitAbility>();

    [Header("Resources")]
    [SerializeField] private int currentMana = 100;
    [SerializeField] private int maxMana = 100;
    [SerializeField] private float manaRegenRate = 5f; // per second

    // Cooldown tracking
    private Dictionary<Type, float> abilityCooldowns = new Dictionary<Type, float>();

    private void Start()
    {
        // Initialize cooldowns
        foreach (UnitAbility ability in abilities)
        {
            if (ability != null)
            {
                abilityCooldowns[ability.GetType()] = 0f;
            }
        }
    }

    private void Update()
    {
        UpdateCooldowns();
        RegenerateMana();
    }

    /// <summary>
    /// Add an ability to this unit
    /// </summary>
    public void AddAbility(UnitAbility ability)
    {
        if (ability != null && !abilities.Contains(ability))
        {
            abilities.Add(ability);
            abilityCooldowns[ability.GetType()] = 0f;
        }
    }

    /// <summary>
    /// Remove an ability from this unit
    /// </summary>
    public void RemoveAbility(Type abilityType)
    {
        abilities.RemoveAll(a => a.GetType() == abilityType);
        if (abilityCooldowns.ContainsKey(abilityType))
        {
            abilityCooldowns.Remove(abilityType);
        }
    }

    /// <summary>
    /// Check if unit has a specific ability
    /// </summary>
    public bool HasAbility(Type abilityType)
    {
        return abilities.Exists(a => a.GetType() == abilityType);
    }

    /// <summary>
    /// Get all abilities this unit has
    /// </summary>
    public List<UnitAbility> GetAbilities()
    {
        return new List<UnitAbility>(abilities);
    }

    /// <summary>
    /// Check if an ability can be used right now
    /// </summary>
    public bool CanUseAbility(Type abilityType)
    {
        UnitAbility ability = abilities.Find(a => a.GetType() == abilityType);
        if (ability == null) return false;

        // Check cooldown
        if (GetCooldownTimeRemaining(abilityType) > 0) return false;

        // Check mana/resources
        if (currentMana < ability.resourceCost) return false;

        // Check ability-specific conditions
        if (!ability.CanUse(gameObject)) return false;

        return true;
    }

    /// <summary>
    /// Use an ability at a position
    /// </summary>
    public bool UseAbility(Type abilityType, Vector3 targetPosition)
    {
        if (!CanUseAbility(abilityType)) return false;

        UnitAbility ability = abilities.Find(a => a.GetType() == abilityType);
        if (ability == null) return false;

        // Consume resources
        currentMana -= ability.resourceCost;

        // Set cooldown
        abilityCooldowns[abilityType] = ability.cooldownTime;

        // Execute ability
        ability.Execute(gameObject, targetPosition);
        ability.PlayVisualEffects(gameObject, targetPosition);

        Debug.Log($"{gameObject.name} used ability: {ability.DisplayName}");
        return true;
    }

    /// <summary>
    /// Use an ability on a target
    /// </summary>
    public bool UseAbility(Type abilityType, GameObject target)
    {
        if (target != null)
        {
            return UseAbility(abilityType, target.transform.position);
        }
        return false;
    }

    /// <summary>
    /// Use an ability without a target (self-cast)
    /// </summary>
    public bool UseAbility(Type abilityType)
    {
        return UseAbility(abilityType, transform.position);
    }

    /// <summary>
    /// Get remaining cooldown time for an ability
    /// </summary>
    public float GetCooldownTimeRemaining(Type abilityType)
    {
        if (abilityCooldowns.ContainsKey(abilityType))
        {
            return Mathf.Max(0f, abilityCooldowns[abilityType]);
        }
        return 0f;
    }

    /// <summary>
    /// Get cooldown progress (0-1, where 1 = ready to use)
    /// </summary>
    public float GetCooldownProgress(Type abilityType)
    {
        UnitAbility ability = abilities.Find(a => a.GetType() == abilityType);
        if (ability == null) return 1f;

        float remaining = GetCooldownTimeRemaining(abilityType);
        if (remaining <= 0f) return 1f;

        return 1f - (remaining / ability.cooldownTime);
    }

    private void UpdateCooldowns()
    {
        var keys = new List<Type>(abilityCooldowns.Keys);
        foreach (Type abilityType in keys)
        {
            if (abilityCooldowns[abilityType] > 0)
            {
                abilityCooldowns[abilityType] -= Time.deltaTime;
            }
        }
    }

    private void RegenerateMana()
    {
        if (currentMana < maxMana)
        {
            currentMana = Mathf.Min(maxMana, currentMana + (int)(manaRegenRate * Time.deltaTime));
        }
    }

    // Public getters for UI
    public int GetCurrentMana() => currentMana;
    public int GetMaxMana() => maxMana;
}

// Example abilities - you can create more by inheriting from UnitAbility

/// <summary>
/// Example: Heal ability that restores health
/// </summary>
[CreateAssetMenu(fileName = "HealAbility", menuName = "Unit Abilities/Heal")]
public class HealAbility : UnitAbility
{
    [Header("Heal Settings")]
    public float healAmount = 50f;
    public bool canTargetAllies = true;
    public bool canSelfHeal = true;

    public override string DisplayName => "Heal";

    public override bool CanUse(GameObject caster)
    {
        Unit casterUnit = caster.GetComponent<Unit>();
        return casterUnit != null && casterUnit.IsAlive();
    }

    public override void Execute(GameObject caster, Vector3 targetPosition)
    {
        // Find units near target position
        Collider[] nearbyUnits = Physics.OverlapSphere(targetPosition, range);

        foreach (Collider col in nearbyUnits)
        {
            Unit unit = col.GetComponent<Unit>();
            if (unit != null && ShouldHealUnit(caster, col.gameObject))
            {
                float newHealth = unit.GetCurrentHealth() + healAmount;
                newHealth = Mathf.Min(newHealth, unit.GetMaxHealth());

                // Since we don't have a SetHealth method, we'll work with what we have
                // You might want to add a Heal method to the Unit class
                Debug.Log($"{col.name} healed for {healAmount} (would be at {newHealth} health)");
                break; // Only heal one unit for now
            }
        }
    }

    private bool ShouldHealUnit(GameObject caster, GameObject target)
    {
        if (target == caster) return canSelfHeal;

        // Check if it's an ally (same tag)
        if (canTargetAllies && caster.tag == target.tag) return true;

        return false;
    }
}

/// <summary>
/// Example: Speed boost ability
/// </summary>
[CreateAssetMenu(fileName = "SpeedBoostAbility", menuName = "Unit Abilities/Speed Boost")]
public class SpeedBoostAbility : UnitAbility
{
    [Header("Speed Boost Settings")]
    public float speedMultiplier = 1.5f;
    public float duration = 10f;

    public override string DisplayName => "Speed Boost";

    public override bool CanUse(GameObject caster)
    {
        return caster.GetComponent<UnityEngine.AI.NavMeshAgent>() != null;
    }

    public override void Execute(GameObject caster, Vector3 targetPosition)
    {
        SpeedBoostEffect effect = caster.GetComponent<SpeedBoostEffect>();
        if (effect == null)
        {
            effect = caster.AddComponent<SpeedBoostEffect>();
        }

        effect.ApplySpeedBoost(speedMultiplier, duration);
    }
}

/// <summary>
/// Component that handles temporary speed boost effects
/// </summary>
public class SpeedBoostEffect : MonoBehaviour
{
    private UnityEngine.AI.NavMeshAgent agent;
    private float originalSpeed;
    private float boostEndTime;
    private bool isBoosted = false;

    private void Start()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            originalSpeed = agent.speed;
        }
    }

    private void Update()
    {
        if (isBoosted && Time.time >= boostEndTime)
        {
            RemoveSpeedBoost();
        }
    }

    public void ApplySpeedBoost(float multiplier, float duration)
    {
        if (agent == null) return;

        // Remove existing boost first
        if (isBoosted)
        {
            RemoveSpeedBoost();
        }

        // Apply new boost
        agent.speed = originalSpeed * multiplier;
        boostEndTime = Time.time + duration;
        isBoosted = true;

        Debug.Log($"{gameObject.name} speed boosted to {agent.speed} for {duration} seconds");
    }

    private void RemoveSpeedBoost()
    {
        if (agent != null)
        {
            agent.speed = originalSpeed;
        }
        isBoosted = false;

        Debug.Log($"{gameObject.name} speed boost expired");
    }
}
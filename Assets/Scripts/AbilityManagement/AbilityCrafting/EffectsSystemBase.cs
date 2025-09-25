using UnityEngine;
using System.Collections.Generic;

// =========================
// EFFECT INTERFACES
// =========================

public interface IDamageEffect
{
    float CalculateDamage(GameObject caster, GameObject target);
    void ApplyDamage(GameObject caster, GameObject target, float damage);
}

public interface IHealEffect
{
    float CalculateHealing(GameObject caster, GameObject target);
    void ApplyHealing(GameObject caster, GameObject target, float healing);
}

public interface IBuffEffect
{
    void ApplyBuff(GameObject caster, GameObject target);
}

public interface IDebuffEffect
{
    void ApplyDebuff(GameObject caster, GameObject target);
}

// =========================
// STATUS EFFECT SYSTEM (Basic)
// =========================

[System.Serializable]
public class StatusEffect
{
    public string name;
    public float duration;
    public float tickInterval = 0f; // 0 means no ticking
    public Sprite icon;

    // For damage/heal over time
    public float tickValue = 0f;
    public bool isDamageOverTime = false;
    public bool isHealOverTime = false;

    // For stat modifications
    public bool modifiesStats = false;
    public float movementSpeedMultiplier = 1f;
    public float damageMultiplier = 1f;
    public float defenseMultiplier = 1f;

    // For control effects
    public bool stunned = false;
    public bool silenced = false;
    public bool feared = false;
}

// =========================
// DAMAGE EFFECT HELPERS
// =========================

public abstract class DamageEffectAbility : IDamageEffect
{
    [Header("Damage Settings")]
    public float baseDamage = 10f;
    public float damageMultiplier = 1f;
    public bool scaleWithCasterStats = true;

    public virtual float CalculateDamage(GameObject caster, GameObject target)
    {
        float totalDamage = baseDamage * damageMultiplier;

        if (scaleWithCasterStats)
        {
            var characterManager = caster.GetComponent<CharacterManager>();
            if (characterManager != null)
            {
                totalDamage += characterManager.MeleeDamage * (damageMultiplier - 1f);
            }
        }

        // Could add resistance calculations here
        return totalDamage;
    }

    public virtual void ApplyDamage(GameObject caster, GameObject target, float damage)
    {
        // Use the same path as auto attacks - go through Unit.TakeDamage()
        var targetUnit = target.GetComponent<Unit>();
        if (targetUnit != null && targetUnit.IsAlive())
        {
            targetUnit.TakeDamage(damage);
            Debug.Log($"{caster.name} dealt {damage} damage to {target.name}");
        }
    }
}

// =========================
// HEAL EFFECT HELPERS
// =========================

public abstract class HealEffectAbility : IHealEffect
{
    [Header("Healing Settings")]
    public float baseHealing = 10f;
    public float healingMultiplier = 1f;
    public bool scaleWithCasterStats = true;

    public virtual float CalculateHealing(GameObject caster, GameObject target)
    {
        float totalHealing = baseHealing * healingMultiplier;

        if (scaleWithCasterStats)
        {
            // Could scale with a "Healing Power" stat if you implement one
            var characterManager = caster.GetComponent<CharacterManager>();
            if (characterManager != null)
            {
                // For now, just use the base healing
            }
        }

        return totalHealing;
    }

    public virtual void ApplyHealing(GameObject caster, GameObject target, float healing)
    {
        // Use the same pattern as damage - go through Unit.Heal()
        var targetUnit = target.GetComponent<Unit>();
        if (targetUnit != null && targetUnit.IsAlive())
        {
            targetUnit.Heal(healing);
            Debug.Log($"{caster.name} healed {target.name} for {healing}");
        }
    }
}

// =========================
// BUFF/DEBUFF HELPERS
// =========================

public abstract class BuffEffectAbility : IBuffEffect
{
    [Header("Buff Settings")]
    public StatusEffect buffEffect;

    public virtual void ApplyBuff(GameObject caster, GameObject target)
    {
        var statusManager = target.GetComponent<StatusEffectManager>();
        if (statusManager != null)
        {
            statusManager.AddStatusEffect(buffEffect);
            Debug.Log($"{caster.name} applied {buffEffect.name} buff to {target.name}");
        }
    }
}

public abstract class DebuffEffectAbility : IDebuffEffect
{
    [Header("Debuff Settings")]
    public StatusEffect debuffEffect;

    public virtual void ApplyDebuff(GameObject caster, GameObject target)
    {
        var statusManager = target.GetComponent<StatusEffectManager>();
        if (statusManager != null)
        {
            statusManager.AddStatusEffect(debuffEffect);
            Debug.Log($"{caster.name} applied {debuffEffect.name} debuff to {target.name}");
        }
    }
}

// =========================
// COMBINED EFFECT HELPERS
// =========================

/// <summary>
/// Helper for abilities that deal damage and apply a debuff
/// </summary>
public abstract class DamageDebuffAbility : DamageEffectAbility, IDebuffEffect
{
    [Header("Debuff Settings")]
    public StatusEffect debuffEffect;

    public virtual void ApplyDebuff(GameObject caster, GameObject target)
    {
        var statusManager = target.GetComponent<StatusEffectManager>();
        if (statusManager != null)
        {
            statusManager.AddStatusEffect(debuffEffect);
        }
    }

    protected virtual void ApplyDamageAndDebuff(GameObject caster, GameObject target)
    {
        float damage = CalculateDamage(caster, target);
        ApplyDamage(caster, target, damage);
        ApplyDebuff(caster, target);
    }
}

/// <summary>
/// Helper for abilities that heal and apply a buff
/// </summary>
public abstract class HealBuffAbility : HealEffectAbility, IBuffEffect
{
    [Header("Buff Settings")]
    public StatusEffect buffEffect;

    public virtual void ApplyBuff(GameObject caster, GameObject target)
    {
        var statusManager = target.GetComponent<StatusEffectManager>();
        if (statusManager != null)
        {
            statusManager.AddStatusEffect(buffEffect);
        }
    }

    protected virtual void ApplyHealingAndBuff(GameObject caster, GameObject target)
    {
        float healing = CalculateHealing(caster, target);
        ApplyHealing(caster, target, healing);
        ApplyBuff(caster, target);
    }
}

// =========================
// RESOURCE CONSUMPTION HELPERS
// =========================

public static class ResourceConsumptionUtils
{
    public static bool ConsumeWarriorRage(GameObject caster, int rageCost)
    {
        var warriorClass = caster.GetComponent<WarriorClass>();
        return warriorClass != null && warriorClass.SpendResource(rageCost);
    }

    public static bool ConsumeMana(GameObject caster, int manaCost)
    {
        // Implement when you add mana system
        // var mageClass = caster.GetComponent<MageClass>();
        // return mageClass != null && mageClass.SpendResource(manaCost);
        return true; // Placeholder
    }

    public static bool ConsumeStamina(GameObject caster, int staminaCost)
    {
        // Implement when you add stamina system
        return true; // Placeholder
    }
}
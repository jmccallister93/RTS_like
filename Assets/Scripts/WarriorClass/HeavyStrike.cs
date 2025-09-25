using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Warrior/Heavy Strike")]
public class HeavyStrikeSO : SingleTargetEnemyAbility, IDamageEffect, IDebuffEffect
{
    [Header("Heavy Strike Settings")]
    public float damageMultiplier = 2.0f;
    public int rageCost = 0;

    [Header("Stun Effect")]
    public StatusEffect stunEffect;

    void OnEnable()
    {
        // Set up the stun effect if not configured
        if (stunEffect.name == null || stunEffect.name == "")
        {
            stunEffect.name = "Heavy Strike Stun";
            stunEffect.duration = 1.0f;
            stunEffect.stunned = true;
            stunEffect.modifiesStats = false;
        }
    }

    protected override void ExecuteOnTarget(GameObject caster, GameObject target)
    {
        // Deal damage
        float damage = CalculateDamage(caster, target);
        ApplyDamage(caster, target, damage);

        // Apply stun
        ApplyDebuff(caster, target);
    }

    protected override bool ConsumeResources(GameObject caster)
    {
        return ResourceConsumptionUtils.ConsumeWarriorRage(caster, rageCost);
    }

    // IDamageEffect implementation
    public float CalculateDamage(GameObject caster, GameObject target)
    {
        var characterManager = caster.GetComponent<CharacterManager>();
        float baseDamage = characterManager?.MeleeDamage ?? 10f;
        return baseDamage * damageMultiplier;
    }

    public void ApplyDamage(GameObject caster, GameObject target, float damage)
    {
        var unit = target.GetComponent<Unit>();
        if (unit != null)
        {
            unit.TakeDamage(damage);
            Debug.Log($"Heavy Strike: {caster.name} dealt {damage} damage to {target.name}");
        }
    }

    // IDebuffEffect implementation
    public void ApplyDebuff(GameObject caster, GameObject target)
    {
        var statusManager = target.GetComponent<StatusEffectManager>();
        if (statusManager != null)
        {
            statusManager.AddStatusEffect(stunEffect, caster);
            Debug.Log($"Heavy Strike: {target.name} is stunned for {stunEffect.duration} seconds");
        }
    }
}
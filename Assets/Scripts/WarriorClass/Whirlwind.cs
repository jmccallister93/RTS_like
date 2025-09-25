using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Warrior/Whirlwind")]
public class WhirlwindSO : SelfAreaAbility, IDamageEffect
{
    [Header("Whirlwind Settings")]
    public float damageMultiplier = 1.5f;
    public int rageCost = 10;

    void OnEnable()
    {
        // Configure as area effect
        areaRadius = 3f;
    }

    protected override void ExecuteAreaEffect(GameObject caster, Vector3 center, System.Collections.Generic.List<GameObject> targets)
    {
        Debug.Log($"Whirlwind hit {targets.Count} enemies");

        foreach (GameObject target in targets)
        {
            float damage = CalculateDamage(caster, target);
            ApplyDamage(caster, target, damage);
        }
    }

    protected override bool ConsumeResources(GameObject caster)
    {
        return ResourceConsumptionUtils.ConsumeWarriorRage(caster, rageCost);
    }

    protected override bool IsValidAreaTarget(GameObject caster, GameObject target)
    {
        if (target == caster) return false; // Don't hit self
        if (!AbilityTargetUtils.IsValidUnit(target)) return false;
        return AbilityTargetUtils.IsEnemy(caster, target);
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
        var characterManager = target.GetComponent<CharacterManager>();
        if (characterManager != null)
        {
            characterManager.TakeDamage(damage);
            Debug.Log($"Whirlwind: {caster.name} dealt {damage} damage to {target.name}");
        }
    }
}
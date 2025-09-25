using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Cleric/Heal")]
public class HealSO : SingleTargetAllyAbility, IHealEffect
{
    [Header("Heal Settings")]
    public float baseHealing = 25f;
    public int manaCost = 15;

    protected override void ExecuteOnTarget(GameObject caster, GameObject target)
    {
        float healing = CalculateHealing(caster, target);
        ApplyHealing(caster, target, healing);
    }

    protected override bool ConsumeResources(GameObject caster)
    {
        return ResourceConsumptionUtils.ConsumeMana(caster, manaCost);
    }

    // IHealEffect implementation
    public float CalculateHealing(GameObject caster, GameObject target)
    {
        return baseHealing; // Could scale with caster's healing power stat
    }

    public void ApplyHealing(GameObject caster, GameObject target, float healing)
    {
        var unit = target.GetComponent<Unit>();
        if (unit != null)
        {
            unit.Heal(healing);
            Debug.Log($"Heal: {caster.name} healed {target.name} for {healing}");
        }
    }
}
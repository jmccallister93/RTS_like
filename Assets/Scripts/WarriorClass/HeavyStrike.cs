using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Warrior/Heavy Strike")]
public class HeavyStrikeSO : AbilitySO
{
    [Header("Heavy Strike Settings")]
    public float damageMultiplier = 2.0f;
    public int rageCost = 0;
    public float stunDuration = 1.0f;

    public override bool CanUse(GameObject caster)
    {
        var unit = caster.GetComponent<Unit>();
        var warriorClass = caster.GetComponent<WarriorClass>();

        //return unit != null && unit.IsAlive() &&
        //       warriorClass != null && warriorClass.CanSpendResource(rageCost);
        bool canUse = unit != null && unit.IsAlive() &&
                  warriorClass != null && warriorClass.CanSpendResource(rageCost);

        //Debug.Log($"Heavy Strike CanUse: {canUse} - Unit: {unit != null}, Alive: {unit?.IsAlive()}, Warrior: {warriorClass != null}");

        return canUse;
    }

    public override void Execute(GameObject caster, Vector3 targetPosition, GameObject target = null)
    {
        Debug.Log($"Heavy Strike Execute called! Target: {target?.name}");

        if (target == null)
        {
            Debug.LogWarning("Heavy Strike: No target!");
            return;
        }

        var casterUnit = caster.GetComponent<Unit>();
        var targetUnit = target.GetComponent<Unit>();
        var characterManager = caster.GetComponent<CharacterManager>();
        var warriorClass = caster.GetComponent<WarriorClass>();

        if (casterUnit == null || targetUnit == null || !targetUnit.IsAlive()) return;
        if (warriorClass == null || !warriorClass.SpendResource(rageCost)) return;

        // Calculate damage
        float baseDamage = characterManager?.MeleeDamage ?? 10f;
        float totalDamage = baseDamage * damageMultiplier;

        // Apply rage multiplier
        if (warriorClass.IsRaging)
        {
            totalDamage *= warriorClass.GetRageDamageMultiplier();
        }

        // Deal damage
        targetUnit.TakeDamage(totalDamage);

        // Apply stun effect (you could implement a stun system)
        if (stunDuration > 0)
        {
            // For now, just log it - you could implement actual stun mechanics
            Debug.Log($"{target.name} is stunned for {stunDuration} seconds!");
        }

        Debug.Log($"{caster.name} heavy strikes {target.name} for {totalDamage:F1} damage!");

        lastCastTime = Time.time;
    }
}
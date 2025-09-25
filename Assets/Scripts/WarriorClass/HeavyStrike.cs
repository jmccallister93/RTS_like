using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Warrior/Heavy Strike")]
public class HeavyStrikeSO: AbilitySO
{
    [Header("Heavy Strike Settings")]
    public float damageMultiplier = 2.0f;
    public int rageCost = 0;
    public float stunDuration = 1.0f;

    public override bool CanUse(GameObject caster)
    {
        var unit = caster.GetComponent<Unit>();
        var warriorClass = caster.GetComponent<WarriorClass>();

        bool canUse = unit != null && unit.IsAlive() &&
                  warriorClass != null && warriorClass.CanSpendResource(rageCost);

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


        // Deal damage
        targetUnit.TakeDamage(totalDamage);

        lastCastTime = Time.time;
    }
}
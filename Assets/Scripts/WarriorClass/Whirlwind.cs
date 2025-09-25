using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Warrior/Whirlwind")]
public class WhirlwindSO : AbilitySO
{
    [Header("Whirlwind Settings")]
    public float damageMultiplier = 1.5f;
    public int rageCost = 0;

    private void OnEnable()
    {
        // Set up Whirlwind as an area effect ability
        targetType = TargetType.Area;
        areaRadius = 3f; // 3 unit radius around the caster
        useAreaRadiusForIndicator = true; // Use area radius for the indicator circle
        range = 0f; // Whirlwind is cast at caster position, so no range needed
    }

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
        Debug.Log($"Whirlwind Execute called!");

        var casterUnit = caster.GetComponent<Unit>();
        var characterManager = caster.GetComponent<CharacterManager>();
        var warriorClass = caster.GetComponent<WarriorClass>();

        if (casterUnit == null || !casterUnit.IsAlive()) return;
        if (warriorClass == null || !warriorClass.SpendResource(rageCost)) return;

        // Calculate damage
        float baseDamage = characterManager?.MeleeDamage ?? 10f;
        float totalDamage = baseDamage * damageMultiplier;

        // Use caster position as the center of the whirlwind
        Vector3 whirlwindCenter = caster.transform.position;

        // Apply area effect damage
        int enemiesHit = 0;
        ExecuteAreaEffect(caster, whirlwindCenter, areaRadius, (hitTarget) =>
        {
            var targetUnit = hitTarget.GetComponent<Unit>();
            if (targetUnit != null)
            {
                targetUnit.TakeDamage(totalDamage);
                enemiesHit++;
                Debug.Log($"Whirlwind hit {hitTarget.name} for {totalDamage} damage");
            }
        });

        Debug.Log($"Whirlwind hit {enemiesHit} enemies");
        lastCastTime = Time.time;

        // Optional: Add visual/audio effects here
        // PlayWhirlwindEffect(whirlwindCenter);
    }

    protected override bool IsValidAreaTarget(GameObject caster, GameObject target)
    {
        if (target == caster) return false; // Don't hit self

        // Only hit enemies
        var casterTag = caster.tag;
        var targetTag = target.tag;

        return targetTag != casterTag &&
               ((casterTag == "Player" && targetTag == "Enemy") ||
                (casterTag == "Enemy" && targetTag == "Player"));
    }

    // Optional: Method to play whirlwind visual effects
    private void PlayWhirlwindEffect(Vector3 center)
    {
        // You can instantiate particle effects, play sounds, etc. here
        // Example:
        // GameObject effect = Instantiate(whirlwindEffectPrefab, center, Quaternion.identity);
        // Destroy(effect, 2f);
    }
}
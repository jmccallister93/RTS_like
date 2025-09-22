using UnityEngine;

public class WarriorClass : RPGClass
{
    [Header("Warrior Specific")]
    public float rageMultiplier = 1.5f;
    public int rageDuration = 10;
    private bool isRaging = false;
    private Coroutine rageCoroutine;

    [Header("Warrior Passive Benefits")]
    public float healthPerLevel = 15f;
    public float damagePerStrength = 1.5f;

    protected override void InitializeClassResource()
    {
        resourceName = "Rage";
        maxResource = 100;
        currentResource = 0; // Start with no rage
        OnResourceChanged?.Invoke(currentResource, maxResource);
    }

    public override void OnLevelUp(int newLevel)
    {
        base.OnLevelUp(newLevel);

        // Warrior gets extra health per level
        if (characterManager != null)
        {
            characterManager.ForceRecalculateStats(); // This will include new Constitution from leveling
        }

        switch (newLevel)
        {
            case 5:
                maxResource += 20; // Increase max rage
                Debug.Log($"{classDefinition.className} - Rage capacity increased!");
                break;
            case 10:
                rageMultiplier += 0.25f; // Stronger rage
                Debug.Log($"{classDefinition.className} - Rage becomes more powerful!");
                break;
        }
    }

    public override void ApplyPassiveEffects()
    {
        // Warrior passive: Rage provides damage bonus
        if (isRaging && characterManager != null)
        {
            // This could modify damage calculations in CharacterManager
            // For now, we'll handle it when abilities are used
        }
    }

    public override void OnAbilityUsed(AbilitySO ability)
    {
        // Warriors gain rage when using combat abilities
        if (ability.Name.ToLower().Contains("attack") || ability.Name.ToLower().Contains("strike"))
        {
            RestoreResource(15);
        }
    }

    public void ActivateRage()
    {
        if (CanSpendResource(50) && !isRaging)
        {
            SpendResource(50);
            isRaging = true;

            if (rageCoroutine != null)
                StopCoroutine(rageCoroutine);

            rageCoroutine = StartCoroutine(RageCoroutine());
            Debug.Log($"{classDefinition.className} enters rage mode!");

            // Apply rage effects
            ApplyPassiveEffects();
        }
    }

    private System.Collections.IEnumerator RageCoroutine()
    {
        yield return new UnityEngine.WaitForSeconds(rageDuration);
        isRaging = false;
        Debug.Log($"{classDefinition.className} rage mode ended.");
    }

    public bool IsRaging => isRaging;
    public float GetRageDamageMultiplier() => isRaging ? rageMultiplier : 1f;
}
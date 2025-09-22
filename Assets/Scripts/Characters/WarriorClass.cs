using UnityEngine;

public class WarriorClass : RPGClass
{
    [Header("Warrior Specific")]
    public float rageMultiplier = 1.5f;
    public int rageDuration = 10;
    private bool isRaging = false;
    private Coroutine rageCoroutine;

    protected override void InitializeClassResource()
    {
        resourceName = "Rage";
        maxResource = 100;
        currentResource = 0; // Warriors start with no rage
        OnResourceChanged?.Invoke(currentResource, maxResource);
    }

    public override void OnLevelUp(int newLevel)
    {
        base.OnLevelUp(newLevel);

        // Warrior-specific level benefits
        switch (newLevel)
        {
            case 5:
                maxResource += 20; // Increase max rage
                Debug.Log("Rage capacity increased!");
                break;
            case 10:
                rageMultiplier += 0.25f; // Stronger rage
                Debug.Log("Rage becomes more powerful!");
                break;
        }
    }

    public override void ApplyPassiveEffects()
    {
        // Warrior passive: Gain rage when taking damage or dealing damage
        if (isRaging)
        {
            // Apply rage damage bonus through character manager
            // This would modify the damage calculation
        }
    }

    public override void OnAbilityUsed(AbilitySO ability)
    {
        // Warriors gain rage when using abilities
        if (ability.Name.Contains("Attack"))
        {
            RestoreResource(10);
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
            Debug.Log("Warrior enters rage mode!");
        }
    }

    private System.Collections.IEnumerator RageCoroutine()
    {
        yield return new UnityEngine.WaitForSeconds(rageDuration);
        isRaging = false;
        Debug.Log("Rage mode ended.");
    }
}
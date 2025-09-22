using System.Linq;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int currentExperience = 0;
    [SerializeField] private ClassDefinition classDefinition;

    public int CurrentLevel => currentLevel;
    public int CurrentExperience => currentExperience;
    public int ExperienceToNextLevel => GetExperienceRequiredForLevel(currentLevel + 1) - currentExperience;
    public ClassDefinition CurrentClass => classDefinition;

    private StatManager statManager;
    private SkillManager skillManager;
    private UnitAbilities unitAbilities;
    private RPGClass rpgClass;

    private void Awake()
    {
        statManager = GetComponent<StatManager>();
        skillManager = GetComponent<SkillManager>();
        unitAbilities = GetComponent<UnitAbilities>();
        rpgClass = GetComponent<RPGClass>();
    }

    public void GainExperience(int amount)
    {
        currentExperience += amount;
        CheckForLevelUp();
        OnExperienceGained?.Invoke(amount);
    }

    private void CheckForLevelUp()
    {
        int requiredExp = GetExperienceRequiredForLevel(currentLevel + 1);

        while (currentExperience >= requiredExp && currentLevel < 100)
        {
            LevelUp();
            requiredExp = GetExperienceRequiredForLevel(currentLevel + 1);
        }
    }

    private void LevelUp()
    {
        int previousLevel = currentLevel;
        currentLevel++;

        if (classDefinition != null)
        {
            // Award stat and skill points
            statManager?.GainStatPoints(classDefinition.statPointsPerLevel);
            skillManager?.GainSkillPoints(classDefinition.skillPointsPerLevel);

            // Apply automatic stat growth
            ApplyStatGrowth();

            // Check for new abilities
            CheckForAbilityUnlocks(previousLevel, currentLevel);
        }

        // Notify RPG class of level up
        rpgClass?.OnLevelUp(currentLevel);

        OnLevelUp?.Invoke(currentLevel);
    }

    private void ApplyStatGrowth()
    {
        if (classDefinition?.statGrowthPerLevel == null) return;

        var growth = classDefinition.statGrowthPerLevel;
        statManager?.AddBaseStats(growth);
    }

    private void CheckForAbilityUnlocks(int previousLevel, int newLevel)
    {
        if (classDefinition?.abilityUnlocks == null || unitAbilities == null) return;

        var newUnlocks = classDefinition.abilityUnlocks
            .Where(unlock => unlock.levelRequired > previousLevel && unlock.levelRequired <= newLevel)
            .ToList();

        foreach (var unlock in newUnlocks)
        {
            // Check skill requirements if any
            if (!string.IsNullOrEmpty(unlock.requiredSkill))
            {
                int skillLevel = skillManager?.GetSkillLevel(unlock.requiredSkill) ?? 0;
                if (skillLevel < unlock.requiredSkillLevel)
                    continue; // Skip this unlock, skill requirement not met
            }

            // Unlock the ability
            UnlockAbility(unlock);
        }
    }

    private void UnlockAbility(ClassAbilityUnlock unlock)
    {
        if (unlock.autoAssign && unlock.slotIndex >= 0)
        {
            // Auto-assign to specific slot
            unitAbilities.SetAbility(unlock.slotIndex, unlock.ability);

            // Update ability manager if this unit is selected
            if (AbilityManager.Instance != null)
            {
                AbilityManager.Instance.SetAbilityForUnit(gameObject, unlock.slotIndex, unlock.ability);
            }
        }
        else
        {
            // Add to available abilities pool (could be implemented in UnitAbilities)
            unitAbilities.AddAvailableAbility(unlock.ability);
        }

        OnAbilityUnlocked?.Invoke(unlock.ability, unlock.levelRequired);
        Debug.Log($"{classDefinition.className} unlocked: {unlock.ability.Name} at level {unlock.levelRequired}!");
    }

    private int GetExperienceRequiredForLevel(int level)
    {
        if (classDefinition?.experienceRequiredCurve == null)
            return level * 100; // Default formula

        return Mathf.RoundToInt(classDefinition.experienceRequiredCurve.Evaluate(level));
    }

    public void SetClass(ClassDefinition newClass)
    {
        classDefinition = newClass;
        ApplyClassStartingValues();
    }

    private void ApplyClassStartingValues()
    {
        if (classDefinition == null) return;

        // Apply starting stats
        if (classDefinition.startingStats != null)
        {
            statManager?.SetBaseStats(classDefinition.startingStats);
        }

        // Apply starting abilities
        if (classDefinition.startingAbilities != null && unitAbilities != null)
        {
            for (int i = 0; i < classDefinition.startingAbilities.Length; i++)
            {
                if (classDefinition.startingAbilities[i] != null)
                {
                    unitAbilities.SetAbility(i, classDefinition.startingAbilities[i]);
                }
            }
        }
    }

    // Events
    public System.Action<int> OnExperienceGained;
    public System.Action<int> OnLevelUp;
    public System.Action<AbilitySO, int> OnAbilityUnlocked;
}
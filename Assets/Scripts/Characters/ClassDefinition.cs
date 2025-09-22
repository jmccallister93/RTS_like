using UnityEngine;

[CreateAssetMenu(fileName = "New RPG Class", menuName = "RPG/Class Definition")]
public class ClassDefinition : ScriptableObject
{
    [Header("Basic Info")]
    public string className;
    public string description;
    public Sprite classIcon;

    [Header("Starting Stats")]
    public StatBlock startingStats;

    [Header("Stat Growth Per Level")]
    public StatBlock statGrowthPerLevel;

    [Header("Class Abilities")]
    public ClassAbilityUnlock[] abilityUnlocks; // Abilities unlocked at specific levels

    [Header("Progression")]
    public AnimationCurve experienceRequiredCurve; // X = level, Y = total exp needed
    public int statPointsPerLevel = 2;
    public int skillPointsPerLevel = 1;

    [Header("Starting Equipment/Abilities")]
    public AbilitySO[] startingAbilities = new AbilitySO[6]; // Default abilities for slots 0-5
}

[System.Serializable]
public class ClassAbilityUnlock
{
    public int levelRequired;
    public AbilitySO ability;
    public int slotIndex = -1; // -1 means add to available pool, specific number assigns to slot
    public bool autoAssign = false; // If true, automatically assigns to the specified slot

    [Header("Unlock Conditions (Optional)")]
    public string requiredSkill; // Optional skill requirement
    public int requiredSkillLevel;
}

[System.Serializable]
public class StatBlock
{
    public int strength;
    public int dexterity;
    public int mind;
    public int constitution;
    public int reflex;
    public int willpower;
    public int vitality;
    public int mobility;
    public int focus;
}
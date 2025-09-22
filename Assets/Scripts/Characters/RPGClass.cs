using System.Collections.Generic;
using UnityEngine;

public abstract class RPGClass : MonoBehaviour
{
    [SerializeField] protected ClassDefinition classDefinition;

    protected LevelManager levelManager;
    protected StatManager statManager;
    protected SkillManager skillManager;
    protected CharacterManager characterManager;
    protected Unit unit;
    protected UnitAbilities unitAbilities;

    // Class-specific resources (mana, rage, etc.)
    [Header("Class Resources")]
    [SerializeField] protected int currentResource;
    [SerializeField] protected int maxResource = 100;
    [SerializeField] protected string resourceName = "Mana";

    public int CurrentResource => currentResource;
    public int MaxResource => maxResource;
    public string ResourceName => resourceName;

    protected virtual void Awake()
    {
        // Get all required components
        levelManager = GetComponent<LevelManager>();
        statManager = GetComponent<StatManager>();
        skillManager = GetComponent<SkillManager>();
        characterManager = GetComponent<CharacterManager>();
        unit = GetComponent<Unit>();
        unitAbilities = GetComponent<UnitAbilities>();
    }

    protected virtual void Start()
    {
        InitializeClass();
    }

    protected virtual void InitializeClass()
    {
        if (classDefinition != null)
        {
            levelManager?.SetClass(classDefinition);
            InitializeClassResource();
        }
    }

    protected virtual void InitializeClassResource()
    {
        currentResource = maxResource;
        OnResourceChanged?.Invoke(currentResource, maxResource);
    }

    // Called by LevelManager when leveling up
    public virtual void OnLevelUp(int newLevel)
    {
        // Override in derived classes for level-specific behavior
        Debug.Log($"{classDefinition.className} reached level {newLevel}!");
    }

    // Resource management
    public virtual bool CanSpendResource(int amount)
    {
        return currentResource >= amount;
    }

    public virtual bool SpendResource(int amount)
    {
        if (!CanSpendResource(amount)) return false;

        currentResource -= amount;
        OnResourceChanged?.Invoke(currentResource, maxResource);
        return true;
    }

    public virtual void RestoreResource(int amount)
    {
        currentResource = Mathf.Min(currentResource + amount, maxResource);
        OnResourceChanged?.Invoke(currentResource, maxResource);
    }

    // Class-specific passive effects
    public abstract void ApplyPassiveEffects();

    // Called when abilities are used (can be overridden for class-specific reactions)
    public virtual void OnAbilityUsed(AbilitySO ability) { }

    // Events
    public System.Action<int, int> OnResourceChanged; // current, max
}


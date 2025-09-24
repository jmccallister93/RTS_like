using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum TargetType
{
    Self,           // No targeting needed, affects caster
    Ally,           // Single ally target
    Enemy,          // Single enemy target  
    Area,           // Area of effect at target location
    Path,           // Line/path from caster to target
    Point,           // Single point on ground
    None            // No valid target type
}

public enum AbilityState
{
    Ready,          // Available to use
    Casting,        // Currently being cast
    OnCooldown,     // Cooling down after use
    Disabled        // Cannot be used (no mana, conditions not met, etc.)
}

[Serializable]
public class AbilitySlot
{
    public IAbility ability;
    public Button uiButton;
    public Image iconImage;
    public Image cooldownOverlay;
    public Text cooldownText;
    public float currentCooldown;
    public AbilityState state = AbilityState.Ready;
}

public interface IAbility
{
    string Name { get; }
    string Description { get; }
    Sprite Icon { get; }
    float Cooldown { get; }
    float CastTime { get; }
    float Range { get; }
    TargetType TargetType { get; }
    AbilityState State { get; }
    Color PreviewColor { get; }

    bool CanUse(GameObject caster);
    void StartCast(GameObject caster, Vector3 targetPosition, GameObject target = null);
    void Execute(GameObject caster, Vector3 targetPosition, GameObject target = null);
    void Cancel(GameObject caster);
    GameObject GetPreviewObject(); // For area/path previews
}

public class AbilityManager : MonoBehaviour, IPausable, IRunWhenPaused
{
    [Header("Component References")]
    public AbilityUIManager uiManager;
    public AbilityTargetingSystem targetingSystem;
    public AbilityCooldownManager cooldownManager;
    public AbilityInputHandler inputHandler;
    public UnitAbilityTracker unitTracker;
    public AbilityExecutor executor;

    // Events
    public static event Action<GameObject, IAbility> OnAbilityUsed;
    public static event Action<GameObject, IAbility> OnAbilityCooldownStarted;

    // Singleton
    private static AbilityManager _instance;
    public static AbilityManager Instance => _instance;

    // Properties
    public bool IsTargeting => targetingSystem.IsTargeting;
    public bool IsCasting => executor.IsCasting;
    public GameObject CurrentSelectedUnit => unitTracker.CurrentSelectedUnit;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        InitializeComponents();
    }

    private void Start()
    {
        SetupComponents();
    }

    private void Update()
    {
        bool paused = PauseManager.Instance != null && PauseManager.Instance.IsPaused;

        // Update components (some run while paused, some don't)
        unitTracker.UpdateSelectedUnit();
        inputHandler.HandleInput();
        targetingSystem.HandleTargeting();

        if (!paused)
        {
            cooldownManager.UpdateCooldowns();
        }

        uiManager.UpdateUI();
    }

    private void InitializeComponents()
    {
        // Get or create components
        if (uiManager == null) uiManager = GetComponent<AbilityUIManager>();
        if (targetingSystem == null) targetingSystem = GetComponent<AbilityTargetingSystem>();
        if (cooldownManager == null) cooldownManager = GetComponent<AbilityCooldownManager>();
        if (inputHandler == null) inputHandler = GetComponent<AbilityInputHandler>();
        if (unitTracker == null) unitTracker = GetComponent<UnitAbilityTracker>();
        if (executor == null) executor = GetComponent<AbilityExecutor>();
    }

    private void SetupComponents()
    {
        // Initialize component references to each other
        uiManager.Initialize(this);
        targetingSystem.Initialize(this);
        cooldownManager.Initialize(this);
        inputHandler.Initialize(this);
        unitTracker.Initialize(this);
        executor.Initialize(this);

        // Subscribe to events
        executor.OnAbilityUsed += (caster, ability) => OnAbilityUsed?.Invoke(caster, ability);
        executor.OnAbilityCooldownStarted += (caster, ability) => OnAbilityCooldownStarted?.Invoke(caster, ability);
    }

    #region Public API

    public bool TryUseAbility(int slotIndex)
    {
        var ability = unitTracker.GetAbility(slotIndex);
        if (ability == null) return false;
        return TryUseAbility(ability);
    }

    public bool TryUseAbility(IAbility ability)
    {
        if (CurrentSelectedUnit == null || !ability.CanUse(CurrentSelectedUnit))
            return false;

        if (ability.TargetType != TargetType.None && ability.TargetType != TargetType.Self)
        {
            CursorManager.Instance.SetCursor("AbilityTargetingEnemy");
        }

        return executor.TryExecuteAbility(ability);
    }

    public void SetAbility(int slotIndex, IAbility ability)
    {
        unitTracker.SetAbility(slotIndex, ability);
    }
    public void SetAbilityForUnit(GameObject unit, int slotIndex, IAbility ability)
    {
        unitTracker.SetAbilityForUnit(unit, slotIndex, ability);
    }
    public IAbility GetAbility(int slotIndex)
    {
        return unitTracker.GetAbility(slotIndex);
    }

    public void CancelTargeting()
    {
        targetingSystem.CancelTargeting();
    }

    #endregion

    #region Pause System

    public void OnPause()
    {
        executor.OnPause();
        targetingSystem.OnPause();
    }

    public void OnResume()
    {
        executor.OnResume();
        targetingSystem.OnResume();
    }

    #endregion
}
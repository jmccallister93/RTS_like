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
    [Header("UI References")]
    public Canvas abilityCanvas;
    public GameObject hotbarPanel;
    public Button[] abilityButtons = new Button[6];
    public Image[] abilityIcons = new Image[6];
    public Image[] cooldownOverlays = new Image[6];
    public Text[] cooldownTexts = new Text[6];

    [Header("Targeting")]
    public LayerMask groundLayer = 1;
    public Material previewMaterial;
    public GameObject areaPreviewPrefab;
    public LineRenderer pathPreviewPrefab;

    [Header("Visual Feedback")]
    public Color readyColor = Color.white;
    public Color cooldownColor = Color.gray;
    public Color castingColor = Color.yellow;
    public Color disabledColor = Color.red;
    public Color noSelectionColor = Color.gray;

    [Header("Settings")]
    public bool showTooltips = true;

    // Core state
    private AbilitySlot[] abilitySlots = new AbilitySlot[6];
    private IAbility currentlyTargeting;
    private bool isTargeting = false;
    private GameObject previewObject;
    private Mouse mouse;
    private Keyboard keyboard;
    private Camera mainCamera;

    // Current selected unit tracking
    private GameObject currentSelectedUnit;
    private Dictionary<GameObject, AbilitySlot[]> unitAbilities = new Dictionary<GameObject, AbilitySlot[]>();
    private Dictionary<GameObject, Dictionary<IAbility, float>> unitCooldowns = new Dictionary<GameObject, Dictionary<IAbility, float>>();

    // Pause state
    private bool wasCastingWhenPaused;
    private IAbility pausedCastingAbility;
    private GameObject pausedCaster;

    public static event Action<GameObject, IAbility> OnAbilityUsed;
    public static event Action<GameObject, IAbility> OnAbilityCooldownStarted;

    private static AbilityManager _instance;
    public static AbilityManager Instance => _instance;
    private UnitAbilities currentUnitAbilities;

    private void Awake()
    {
        mouse = Mouse.current;
        keyboard = Keyboard.current;
        mainCamera = Camera.main;

        InitializeAbilitySlots();
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void Start()
    {
        SetupUI();
    }

    private void Update()
    {
        // Don't process input while paused (except this manager can run when paused for UI)
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
            return;

        UpdateSelectedUnit();
        HandleHotkeyInput();
        HandleTargeting();
        UpdateCooldowns();
        UpdateUI();
    }

    #region Initialization

    private void InitializeAbilitySlots()
    {
        for (int i = 0; i < abilitySlots.Length; i++)
        {
            abilitySlots[i] = new AbilitySlot();
        }
    }

    private void SetupUI()
    {
        // Ensure we have all UI references
        if (hotbarPanel == null)
        {
            Debug.LogWarning($"{name}: Hotbar panel not assigned!");
            return;
        }

        // Setup button listeners
        for (int i = 0; i < abilityButtons.Length; i++)
        {
            if (abilityButtons[i] != null)
            {
                int index = i; // Capture for lambda
                abilityButtons[i].onClick.AddListener(() => TryUseAbility(index));

                // Setup UI references in slot
                abilitySlots[i].uiButton = abilityButtons[i];
                abilitySlots[i].iconImage = abilityIcons[i];
                abilitySlots[i].cooldownOverlay = cooldownOverlays[i];
                abilitySlots[i].cooldownText = cooldownTexts[i];
            }
        }
    }

    #endregion

    #region Unit Selection Management
    public void SetCurrentUnit(GameObject unit)
    {
        currentUnitAbilities = unit.GetComponent<UnitAbilities>();
    }
    public void UseAbility(int slot, Vector3 targetPos, GameObject target = null)
    {
        if (currentUnitAbilities == null) return;

        AbilitySO ability = currentUnitAbilities.GetAbility(slot);
        if (ability == null) return;

        if (!ability.CanUse(currentUnitAbilities.gameObject)) return;

        ability.StartCast(currentUnitAbilities.gameObject, targetPos, target);
        ability.Execute(currentUnitAbilities.gameObject, targetPos, target);
    }
    private void UpdateSelectedUnit()
    {
        GameObject newSelectedUnit = GetCurrentSelectedUnit();

        if (newSelectedUnit != currentSelectedUnit)
        {
            // Selection changed
            if (currentSelectedUnit != null)
            {
                // Save current unit's state
                SaveUnitAbilityState(currentSelectedUnit);
            }

            currentSelectedUnit = newSelectedUnit;

            if (currentSelectedUnit != null)
            {
                // Load new unit's abilities
                LoadUnitAbilities(currentSelectedUnit);
            }
            else
            {
                // No unit selected, clear abilities
                ClearAbilities();
            }

            // Cancel any active targeting when selection changes
            CancelTargeting();
        }
    }

    private GameObject GetCurrentSelectedUnit()
    {
        if (UnitSelectionManager.Instance == null) return null;

        var selectedUnits = UnitSelectionManager.Instance.unitsSelected;

        // Only return a unit if exactly one is selected
        if (selectedUnits != null && selectedUnits.Count == 1)
        {
            return selectedUnits[0];
        }

        return null;
    }

    private void SaveUnitAbilityState(GameObject unit)
    {
        if (unit == null) return;

        // Save current ability slots for this unit
        AbilitySlot[] savedSlots = new AbilitySlot[6];
        for (int i = 0; i < abilitySlots.Length; i++)
        {
            savedSlots[i] = new AbilitySlot();
            savedSlots[i].ability = abilitySlots[i].ability;
            savedSlots[i].currentCooldown = abilitySlots[i].currentCooldown;
            savedSlots[i].state = abilitySlots[i].state;
        }
        unitAbilities[unit] = savedSlots;

        // Save cooldowns
        if (!unitCooldowns.ContainsKey(unit))
        {
            unitCooldowns[unit] = new Dictionary<IAbility, float>();
        }

        foreach (var slot in abilitySlots)
        {
            if (slot.ability != null)
            {
                unitCooldowns[unit][slot.ability] = slot.currentCooldown;
            }
        }
    }

    private void LoadUnitAbilities(GameObject unit)
    {
        if (unit == null) return;

        // Check if we have saved abilities for this unit
        if (unitAbilities.ContainsKey(unit))
        {
            // Restore saved state
            var savedSlots = unitAbilities[unit];
            for (int i = 0; i < abilitySlots.Length; i++)
            {
                abilitySlots[i].ability = savedSlots[i].ability;
                abilitySlots[i].currentCooldown = savedSlots[i].currentCooldown;
                abilitySlots[i].state = savedSlots[i].state;
            }
        }
        else
        {
            // First time selecting this unit, load default abilities
            LoadDefaultAbilitiesForUnit(unit);
        }

        // Restore cooldowns
        if (unitCooldowns.ContainsKey(unit))
        {
            var cooldowns = unitCooldowns[unit];
            foreach (var slot in abilitySlots)
            {
                if (slot.ability != null && cooldowns.ContainsKey(slot.ability))
                {
                    slot.currentCooldown = cooldowns[slot.ability];
                    slot.state = slot.currentCooldown > 0 ? AbilityState.OnCooldown : AbilityState.Ready;
                }
            }
        }
    }

    private void LoadDefaultAbilitiesForUnit(GameObject unit)
    {
        // Clear current abilities
        for (int i = 0; i < abilitySlots.Length; i++)
        {
            abilitySlots[i].ability = null;
            abilitySlots[i].currentCooldown = 0;
            abilitySlots[i].state = AbilityState.Ready;
        }

        // Load abilities from UnitAbilities component
        var unitAbilities = unit.GetComponent<UnitAbilities>();
        if (unitAbilities != null)
        {
            for (int i = 0; i < unitAbilities.abilities.Length && i < abilitySlots.Length; i++)
            {
                if (unitAbilities.abilities[i] != null)
                {
                    abilitySlots[i].ability = unitAbilities.abilities[i];
                    abilitySlots[i].state = AbilityState.Ready;
                }
            }
        }
    }

    private void ClearAbilities()
    {
        for (int i = 0; i < abilitySlots.Length; i++)
        {
            abilitySlots[i].ability = null;
            abilitySlots[i].currentCooldown = 0;
            abilitySlots[i].state = AbilityState.Ready;
        }
    }

    #endregion

    #region Public API

    public void SetAbilityForUnit(GameObject unit, int slotIndex, IAbility ability)
    {
        if (unit == currentSelectedUnit)
        {
            SetAbility(slotIndex, ability);
        }
        else
        {
            // Store for when this unit is selected
            if (!unitAbilities.ContainsKey(unit))
            {
                unitAbilities[unit] = new AbilitySlot[6];
                for (int i = 0; i < 6; i++)
                {
                    unitAbilities[unit][i] = new AbilitySlot();
                }
            }

            if (slotIndex >= 0 && slotIndex < 6)
            {
                unitAbilities[unit][slotIndex].ability = ability;
            }
        }
    }

    public void SetAbility(int slotIndex, IAbility ability)
    {
        if (slotIndex < 0 || slotIndex >= abilitySlots.Length) return;

        abilitySlots[slotIndex].ability = ability;
        abilitySlots[slotIndex].state = AbilityState.Ready;
        abilitySlots[slotIndex].currentCooldown = 0;
    }

    public IAbility GetAbility(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= abilitySlots.Length) return null;
        return abilitySlots[slotIndex].ability;
    }

    public bool TryUseAbility(int slotIndex)
    {
        // Don't allow abilities if no single unit is selected
        if (currentSelectedUnit == null) return false;

        if (slotIndex < 0 || slotIndex >= abilitySlots.Length) return false;

        var slot = abilitySlots[slotIndex];
        if (slot.ability == null) return false;

        return TryUseAbility(slot.ability);
    }

    public bool TryUseAbility(IAbility ability)
    {
        if (ability == null || currentSelectedUnit == null || !ability.CanUse(currentSelectedUnit)) return false;

        // Cancel any current targeting
        CancelTargeting();

        // Handle different targeting types
        switch (ability.TargetType)
        {
            case TargetType.Self:
                ExecuteAbility(ability, currentSelectedUnit.transform.position, currentSelectedUnit);
                return true;

            case TargetType.Ally:
            case TargetType.Enemy:
            case TargetType.Area:
            case TargetType.Path:
            case TargetType.Point:
                StartTargeting(ability);
                return true;

            default:
                return false;
        }
    }

    public void CancelTargeting()
    {
        if (isTargeting && currentlyTargeting != null && currentSelectedUnit != null)
        {
            currentlyTargeting.Cancel(currentSelectedUnit);
        }

        isTargeting = false;
        currentlyTargeting = null;
        DestroyPreviewObject();
    }

    #endregion

    #region Input Handling

    private void HandleHotkeyInput()
    {
        // Don't process hotkeys if no unit selected
        if (currentSelectedUnit == null) return;

        // Don't process hotkeys while typing
        if (EventSystem.current.currentSelectedGameObject != null)
        {
            var inputField = EventSystem.current.currentSelectedGameObject.GetComponent<InputField>();
            if (inputField != null) return;
        }

        // Handle number keys 1-6 for ability slots
        if (keyboard.digit1Key.wasPressedThisFrame) TryUseAbility(0);
        if (keyboard.digit2Key.wasPressedThisFrame) TryUseAbility(1);
        if (keyboard.digit3Key.wasPressedThisFrame) TryUseAbility(2);
        if (keyboard.digit4Key.wasPressedThisFrame) TryUseAbility(3);
        if (keyboard.digit5Key.wasPressedThisFrame) TryUseAbility(4);
        if (keyboard.digit6Key.wasPressedThisFrame) TryUseAbility(5);

        // ESC cancels targeting
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            CancelTargeting();
        }
    }

    private void HandleTargeting()
    {
        if (!isTargeting || currentlyTargeting == null || currentSelectedUnit == null) return;

        Vector3 mouseWorldPos = GetMouseWorldPosition();

        // Update preview
        UpdateTargetingPreview(mouseWorldPos);

        // Handle targeting completion
        if (mouse.leftButton.wasPressedThisFrame)
        {
            CompleteTargeting(mouseWorldPos);
        }
        else if (mouse.rightButton.wasPressedThisFrame)
        {
            CancelTargeting();
        }
    }

    #endregion

    #region Targeting System

    private void StartTargeting(IAbility ability)
    {
        isTargeting = true;
        currentlyTargeting = ability;

        // Create preview object if needed
        CreatePreviewObject(ability);

        // Change cursor or other UI feedback here
        // Example: Cursor.SetCursor(targetingCursor, Vector2.zero, CursorMode.Auto);
    }

    private void CompleteTargeting(Vector3 targetPosition)
    {
        if (currentlyTargeting == null || currentSelectedUnit == null) return;

        GameObject targetObject = null;

        // For unit targeting, check if we clicked on a valid target
        if (currentlyTargeting.TargetType == TargetType.Ally ||
            currentlyTargeting.TargetType == TargetType.Enemy)
        {
            targetObject = GetTargetAtPosition(targetPosition);

            if (targetObject == null || !IsValidTarget(targetObject, currentlyTargeting.TargetType))
            {
                // Invalid target, cancel
                CancelTargeting();
                return;
            }
        }

        // Check range
        float distance = Vector3.Distance(currentSelectedUnit.transform.position, targetPosition);
        if (distance > currentlyTargeting.Range)
        {
            // Out of range, could move closer or just cancel
            CancelTargeting();
            return;
        }

        // Execute the ability
        ExecuteAbility(currentlyTargeting, targetPosition, targetObject);

        // Clear targeting
        isTargeting = false;
        currentlyTargeting = null;
        DestroyPreviewObject();
    }

    private void CreatePreviewObject(IAbility ability)
    {
        var preview = ability.GetPreviewObject();
        if (preview != null)
        {
            previewObject = Instantiate(preview);
        }
        else
        {
            // Create default preview based on targeting type
            switch (ability.TargetType)
            {
                case TargetType.Area:
                    if (areaPreviewPrefab != null)
                    {
                        previewObject = Instantiate(areaPreviewPrefab);
                        var renderer = previewObject.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                            renderer.material.color = ability.PreviewColor;
                        }
                    }
                    break;

                case TargetType.Path:
                    if (pathPreviewPrefab != null)
                    {
                        previewObject = Instantiate(pathPreviewPrefab.gameObject);
                        var lineRenderer = previewObject.GetComponent<LineRenderer>();
                        if (lineRenderer != null)
                        {
                            lineRenderer.startColor = ability.PreviewColor;
                            lineRenderer.endColor = ability.PreviewColor;
                        }
                    }
                    break;
            }
        }
    }

    private void UpdateTargetingPreview(Vector3 mousePosition)
    {
        if (previewObject == null || currentSelectedUnit == null) return;

        switch (currentlyTargeting.TargetType)
        {
            case TargetType.Area:
                previewObject.transform.position = mousePosition;
                break;

            case TargetType.Path:
                var lineRenderer = previewObject.GetComponent<LineRenderer>();
                if (lineRenderer != null)
                {
                    lineRenderer.positionCount = 2;
                    lineRenderer.SetPosition(0, currentSelectedUnit.transform.position);
                    lineRenderer.SetPosition(1, mousePosition);
                }
                break;

            case TargetType.Point:
                // Could show a simple marker
                previewObject.transform.position = mousePosition;
                break;
        }
    }

    private void DestroyPreviewObject()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
        }
    }

    #endregion

    #region Ability Execution

    private void ExecuteAbility(IAbility ability, Vector3 targetPosition, GameObject target = null)
    {
        if (currentSelectedUnit == null || !ability.CanUse(currentSelectedUnit)) return;

        // Get the ability slot for cooldown tracking
        int slotIndex = GetAbilitySlotIndex(ability);
        if (slotIndex == -1) return;

        var slot = abilitySlots[slotIndex];

        // Start casting or execute immediately
        if (ability.CastTime > 0)
        {
            StartCoroutine(CastAbility(ability, targetPosition, target, slot));
        }
        else
        {
            ability.Execute(currentSelectedUnit, targetPosition, target);
            StartCooldown(slot, ability);
            OnAbilityUsed?.Invoke(currentSelectedUnit, ability);
        }
    }

    private IEnumerator CastAbility(IAbility ability, Vector3 targetPosition, GameObject target, AbilitySlot slot)
    {
        slot.state = AbilityState.Casting;
        ability.StartCast(currentSelectedUnit, targetPosition, target);

        float castTime = ability.CastTime;
        float elapsed = 0f;

        while (elapsed < castTime)
        {
            // Check if casting was interrupted (unit deselected, etc.)
            if (slot.state != AbilityState.Casting || currentSelectedUnit == null)
            {
                ability.Cancel(currentSelectedUnit);
                yield break;
            }

            // Pause handling
            if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
            {
                yield return null;
                continue;
            }

            elapsed += Time.deltaTime;

            // Update casting UI feedback here

            yield return null;
        }

        // Complete the cast
        if (currentSelectedUnit != null)
        {
            ability.Execute(currentSelectedUnit, targetPosition, target);
            StartCooldown(slot, ability);
            OnAbilityUsed?.Invoke(currentSelectedUnit, ability);
        }
    }

    private void StartCooldown(AbilitySlot slot, IAbility ability)
    {
        slot.state = AbilityState.OnCooldown;
        slot.currentCooldown = ability.Cooldown;

        if (currentSelectedUnit != null)
        {
            OnAbilityCooldownStarted?.Invoke(currentSelectedUnit, ability);
        }
    }

    #endregion

    #region Utility Methods

    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = mainCamera.ScreenPointToRay(mouse.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
        {
            return hit.point;
        }

        // Fallback to ground plane
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }

        return Vector3.zero;
    }

    private GameObject GetTargetAtPosition(Vector3 position)
    {
        Ray ray = mainCamera.ScreenPointToRay(mouse.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return hit.collider.gameObject;
        }

        return null;
    }

    private bool IsValidTarget(GameObject target, TargetType targetType)
    {
        if (target == null || currentSelectedUnit == null) return false;

        var targetUnit = target.GetComponent<Unit>();
        if (targetUnit == null || !targetUnit.IsAlive()) return false;

        switch (targetType)
        {
            case TargetType.Ally:
                return target.tag == currentSelectedUnit.tag;

            case TargetType.Enemy:
                return target.tag != currentSelectedUnit.tag &&
                       ((currentSelectedUnit.tag == "Player" && target.tag == "Enemy") ||
                        (currentSelectedUnit.tag == "Enemy" && target.tag == "Player"));

            default:
                return false;
        }
    }

    private int GetAbilitySlotIndex(IAbility ability)
    {
        for (int i = 0; i < abilitySlots.Length; i++)
        {
            if (abilitySlots[i].ability == ability)
                return i;
        }
        return -1;
    }

    #endregion

    #region UI Updates

    private void UpdateCooldowns()
    {
        if (currentSelectedUnit == null) return;

        for (int i = 0; i < abilitySlots.Length; i++)
        {
            var slot = abilitySlots[i];
            if (slot.ability == null) continue;

            if (slot.state == AbilityState.OnCooldown)
            {
                slot.currentCooldown -= Time.deltaTime;

                if (slot.currentCooldown <= 0)
                {
                    slot.currentCooldown = 0;
                    slot.state = AbilityState.Ready;
                }
            }

            // Update ability state based on conditions
            if (slot.state == AbilityState.Ready && !slot.ability.CanUse(currentSelectedUnit))
            {
                slot.state = AbilityState.Disabled;
            }
            else if (slot.state == AbilityState.Disabled && slot.ability.CanUse(currentSelectedUnit))
            {
                slot.state = AbilityState.Ready;
            }
        }
    }

    private void UpdateUI()
    {
        for (int i = 0; i < abilitySlots.Length; i++)
        {
            UpdateAbilitySlotUI(i);
        }
    }

    private void UpdateAbilitySlotUI(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= abilitySlots.Length) return;

        var slot = abilitySlots[slotIndex];
        bool hasUnitSelected = currentSelectedUnit != null;

        // Enable/disable button based on unit selection
        if (slot.uiButton != null)
        {
            slot.uiButton.interactable = hasUnitSelected && slot.ability != null;
        }

        // Update icon
        if (slot.iconImage != null)
        {
            if (slot.ability != null)
            {
                slot.iconImage.sprite = slot.ability.Icon;

                // Grey out icon if no unit selected or ability not usable
                if (!hasUnitSelected)
                {
                    slot.iconImage.color = noSelectionColor;
                }
                else
                {
                    slot.iconImage.color = GetStateColor(slot.state);
                }
            }
            else
            {
                slot.iconImage.sprite = null;
                slot.iconImage.color = Color.clear;
            }
        }

        // Update cooldown overlay
        if (slot.cooldownOverlay != null)
        {
            if (slot.state == AbilityState.OnCooldown && slot.ability != null && hasUnitSelected)
            {
                float fillAmount = slot.currentCooldown / slot.ability.Cooldown;
                slot.cooldownOverlay.fillAmount = fillAmount;
                slot.cooldownOverlay.gameObject.SetActive(true);
            }
            else
            {
                slot.cooldownOverlay.gameObject.SetActive(false);
            }
        }

        // Update cooldown text
        if (slot.cooldownText != null)
        {
            if (slot.state == AbilityState.OnCooldown && hasUnitSelected)
            {
                slot.cooldownText.text = Mathf.Ceil(slot.currentCooldown).ToString();
                slot.cooldownText.gameObject.SetActive(true);
            }
            else
            {
                slot.cooldownText.gameObject.SetActive(false);
            }
        }
    }

    private Color GetStateColor(AbilityState state)
    {
        switch (state)
        {
            case AbilityState.Ready:
                return readyColor;
            case AbilityState.Casting:
                return castingColor;
            case AbilityState.OnCooldown:
                return cooldownColor;
            case AbilityState.Disabled:
                return disabledColor;
            default:
                return readyColor;
        }
    }

    #endregion

    #region Pause System Integration

    public void OnPause()
    {
        // Store casting state if needed
        for (int i = 0; i < abilitySlots.Length; i++)
        {
            if (abilitySlots[i].state == AbilityState.Casting)
            {
                wasCastingWhenPaused = true;
                pausedCastingAbility = abilitySlots[i].ability;
                pausedCaster = currentSelectedUnit;
                break;
            }
        }
    }

    public void OnResume()
    {
        // Restore casting state if needed
        if (wasCastingWhenPaused)
        {
            // Handle resuming cast or cancel based on game rules
            wasCastingWhenPaused = false;
            pausedCastingAbility = null;
            pausedCaster = null;
        }
    }

    #endregion
}
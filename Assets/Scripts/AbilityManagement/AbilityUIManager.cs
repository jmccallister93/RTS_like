using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles all UI-related functionality for the ability system
/// </summary>
public class AbilityUIManager : MonoBehaviour
{
    [Header("UI References")]
    public Canvas abilityCanvas;
    public GameObject hotbarPanel;
    public Button[] abilityButtons = new Button[6];
    public Image[] abilityIcons = new Image[6];
    public Image[] cooldownOverlays = new Image[6];
    public Text[] cooldownTexts = new Text[6];

    [Header("Visual Feedback")]
    public Color readyColor = Color.white;
    public Color cooldownColor = Color.gray;
    public Color castingColor = Color.yellow;
    public Color disabledColor = Color.red;
    public Color noSelectionColor = Color.gray;

    [Header("Settings")]
    public bool showTooltips = true;

    private AbilityManager abilityManager;

    public void Initialize(AbilityManager manager)
    {
        abilityManager = manager;
        SetupUI();
    }

    private void SetupUI()
    {
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
                abilityButtons[i].onClick.AddListener(() => abilityManager.TryUseAbility(index));
            }
        }
    }

    public void UpdateUI()
    {
        for (int i = 0; i < 6; i++)
        {
            UpdateAbilitySlotUI(i);
        }
    }

    public void UpdateAbilitySlotUI(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 6) return;

        var ability = abilityManager.GetAbility(slotIndex);
        var cooldownInfo = abilityManager.GetComponent<AbilityCooldownManager>().GetCooldownInfo(slotIndex);
        bool hasUnitSelected = abilityManager.CurrentSelectedUnit != null;

        // Enable/disable button
        if (abilityButtons[slotIndex] != null)
        {
            abilityButtons[slotIndex].interactable = hasUnitSelected && ability != null;
        }

        // Update icon
        if (abilityIcons[slotIndex] != null)
        {
            if (ability != null)
            {
                abilityIcons[slotIndex].sprite = ability.Icon;

                if (!hasUnitSelected)
                {
                    abilityIcons[slotIndex].color = noSelectionColor;
                }
                else
                {
                    abilityIcons[slotIndex].color = GetStateColor(cooldownInfo.state);
                }
            }
            else
            {
                abilityIcons[slotIndex].sprite = null;
                abilityIcons[slotIndex].color = Color.clear;
            }
        }

        // Update cooldown overlay
        if (cooldownOverlays[slotIndex] != null)
        {
            if (cooldownInfo.state == AbilityState.OnCooldown && ability != null && hasUnitSelected)
            {
                float fillAmount = cooldownInfo.currentCooldown / ability.Cooldown;
                cooldownOverlays[slotIndex].fillAmount = fillAmount;
                cooldownOverlays[slotIndex].gameObject.SetActive(true);
            }
            else
            {
                cooldownOverlays[slotIndex].gameObject.SetActive(false);
            }
        }

        // Update cooldown text
        if (cooldownTexts[slotIndex] != null)
        {
            if (cooldownInfo.state == AbilityState.OnCooldown && hasUnitSelected)
            {
                cooldownTexts[slotIndex].text = Mathf.Ceil(cooldownInfo.currentCooldown).ToString();
                cooldownTexts[slotIndex].gameObject.SetActive(true);
            }
            else
            {
                cooldownTexts[slotIndex].gameObject.SetActive(false);
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
}
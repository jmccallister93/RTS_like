using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Handles keyboard input for abilities
/// </summary>
public class AbilityInputHandler : MonoBehaviour
{
    private Keyboard keyboard;
    private AbilityManager abilityManager;

    public void Initialize(AbilityManager manager)
    {
        abilityManager = manager;
        keyboard = Keyboard.current;
    }

    public void HandleInput()
    {
        HandleHotkeyInput();
    }

    private void HandleHotkeyInput()
    {
        // Don't process hotkeys if no unit selected
        if (abilityManager.CurrentSelectedUnit == null) return;

        // Don't process hotkeys while typing in input fields
        if (EventSystem.current.currentSelectedGameObject != null)
        {
            var inputField = EventSystem.current.currentSelectedGameObject.GetComponent<InputField>();
            if (inputField != null) return;
        }

        // Handle number keys 1-6 for ability slots
        if (keyboard.digit1Key.wasPressedThisFrame) abilityManager.TryUseAbility(0);
        if (keyboard.digit2Key.wasPressedThisFrame) abilityManager.TryUseAbility(1);
        if (keyboard.digit3Key.wasPressedThisFrame) abilityManager.TryUseAbility(2);
        if (keyboard.digit4Key.wasPressedThisFrame) abilityManager.TryUseAbility(3);
        if (keyboard.digit5Key.wasPressedThisFrame) abilityManager.TryUseAbility(4);
        if (keyboard.digit6Key.wasPressedThisFrame) abilityManager.TryUseAbility(5);

        // ESC cancels targeting
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            abilityManager.CancelTargeting();
        }
    }
}
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum CommandType
{
    Move,
    Guard,
    AttackMove,
    Patrol,
}

public class CommandManager : MonoBehaviour, IRunWhenPaused
{
    [Header("UI References")]
    public Button moveButton;
    public Button guardButton;
    public Button attackMoveButton;
    public Button patrolButton;
    public Button stopMovementButton;

    [Header("Visual Feedback")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;

    [Header("Guard Mode Settings")]
    public float guardAreaRadius = 5f;
    public Color guardAreaColor = new Color(0, 0.5f, 1f, 0.5f);
    private GameObject guardAreaIndicator;
    private LineRenderer guardCircleRenderer;

    private CommandType currentCommand = CommandType.Move;
    private Mouse mouse;
    private Keyboard keyboard;

    void Start()
    {
        // Initialize mouse reference
        mouse = Mouse.current;
        keyboard = Keyboard.current;
        OnGUI();

        // Set up button listeners
        moveButton.onClick.AddListener(() => SelectCommand(CommandType.Move));
        guardButton.onClick.AddListener(() => SelectCommand(CommandType.Guard));
        attackMoveButton.onClick.AddListener(() => SelectCommand(CommandType.AttackMove));
        patrolButton.onClick.AddListener(() => SelectCommand(CommandType.Patrol));
        stopMovementButton.onClick.AddListener(() => ExecuteStopCommand());

        // Start with Move command selected
        SelectCommand(CommandType.Move);

        // Create guard area indicator
        CreateGuardAreaIndicator();
    }

    void Update()
    {

        // Handle hotkey input first
        HandleHotkeyInput();
        // Don't process if clicking on UI
        if (mouse.leftButton.wasPressedThisFrame && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        List<GameObject> selectedUnits = UnitSelectionManager.Instance?.unitsSelected;
        bool hasSelectedUnits = selectedUnits != null && selectedUnits.Count > 0;

        // Show/hide guard area preview - works even when paused
        if (currentCommand == CommandType.Guard && hasSelectedUnits && !mouse.rightButton.isPressed)
        {
            ShowGuardAreaPreview();
        }
        else
        {
            HideGuardAreaPreview();
        }

        // Handle right-click commands
        if (mouse.rightButton.wasPressedThisFrame)
        {
            if (hasSelectedUnits)
            {
                // FIRST: Check if we clicked on an enemy unit
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                RaycastHit hit;

                // Check for enemy click first (using attackable layer)
                if (Physics.Raycast(ray, out hit, 100f, UnitSelectionManager.Instance.attackable))
                {
                    Debug.Log($"Raycast hit {hit.collider.gameObject.name} on layer {hit.collider.gameObject.layer}");

                    GameObject clickedObject = hit.collider.gameObject;

                    // Check if it's a valid enemy
                    if (IsValidEnemyTarget(clickedObject, selectedUnits))
                    {
                        // Create attack commands for each selected unit
                        foreach (GameObject unit in selectedUnits)
                        {
                            if (unit != null)
                            {
                                var attackCommand = new AttackTargetCommand(unit, hit.transform);

                                // Queue command if paused, execute immediately if not
                                if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
                                {
                                    CommandQueue.Instance.QueueCommand(attackCommand);
                                }
                                else
                                {
                                    attackCommand.Execute();
                                }
                            }
                        }

                        // Hide preview and return to Move command
                        HideGuardAreaPreview();
                        SelectCommand(CommandType.Move);
                        return; // Don't process normal movement
                    }
                }

                // If we didn't click on an enemy, execute normal command
                Vector3 targetPosition = GetMouseWorldPosition();
                ExecuteCommand(targetPosition, selectedUnits);

                // Hide preview after command
                HideGuardAreaPreview();
            }

            SelectCommand(CommandType.Move);
        }
    }

    void HandleHotkeyInput()
    {
        // Only process hotkeys if we're not typing in a text field
        if (EventSystem.current.currentSelectedGameObject != null)
        {
            var inputField = EventSystem.current.currentSelectedGameObject.GetComponent<InputField>();
            if (inputField != null)
            {
                return; // Don't process hotkeys while typing
            }
        }

        // Command hotkeys
        if (keyboard.zKey.wasPressedThisFrame)
        {
            SelectCommand(CommandType.Move);
        }
        else if (keyboard.xKey.wasPressedThisFrame)
        {
            SelectCommand(CommandType.Guard);
        }
        else if (keyboard.cKey.wasPressedThisFrame)
        {
            SelectCommand(CommandType.AttackMove);
        }
        else if (keyboard.vKey.wasPressedThisFrame)
        {
            SelectCommand(CommandType.Patrol);
        }
        else if (keyboard.fKey.wasPressedThisFrame)
        {
            ExecuteStopCommand();
        }

        // Alternative: Use H for Hold Position (same as stop)
        else if (keyboard.hKey.wasPressedThisFrame)
        {
            ExecuteStopCommand();
        }
    }
    void OnGUI()
    {
        // Only show during development or if you want persistent hotkey display
        if (Application.isEditor )
        {
            GUILayout.BeginArea(new Rect(10, 10, 200, 150));
            GUILayout.Label("Hotkeys:");
            GUILayout.Label("M - Move");
            GUILayout.Label("G - Guard");
            GUILayout.Label("A - Attack Move");
            GUILayout.Label("P - Patrol");
            GUILayout.Label("S - Stop");
            GUILayout.Label("H - Hold Position");
            GUILayout.EndArea();
        }
    }

    bool IsValidEnemyTarget(GameObject target, List<GameObject> selectedUnits)
    {
        if (target == null) return false;

        // Must have a Unit component
        Unit targetUnit = target.GetComponent<Unit>();
        if (targetUnit == null || !targetUnit.IsAlive()) return false;

        // Check if any selected unit can attack this target (different teams)
        foreach (GameObject selectedUnit in selectedUnits)
        {
            if (selectedUnit != null)
            {
                string selectedTag = selectedUnit.tag;
                string targetTag = target.tag;

                // Player units can target Enemy units
                if (selectedTag == "Player" && targetTag == "Enemy")
                    return true;
            }
        }

        return false;
    }

    void ExecuteStopCommand()
    {
        List<GameObject> selectedUnits = UnitSelectionManager.Instance?.unitsSelected;

        if (selectedUnits != null && selectedUnits.Count > 0)
        {
            foreach (GameObject unitObj in selectedUnits)
            {
                if (unitObj != null)
                {
                    var stopCommand = new StopCommand(unitObj);

                    // Queue command if paused, execute immediately if not
                    if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
                    {
                        CommandQueue.Instance.QueueCommand(stopCommand);
                    }
                    else
                    {
                        stopCommand.Execute();
                    }
                }
            }
        }

        // Return to Move command after stopping
        SelectCommand(CommandType.Move);
    }

    void CreateGuardAreaIndicator()
    {
        // Create preview indicator (different from unit's persistent circles)
        guardAreaIndicator = new GameObject("GuardAreaPreview");
        guardAreaIndicator.SetActive(false);

        guardCircleRenderer = guardAreaIndicator.AddComponent<LineRenderer>();
        guardCircleRenderer.material = new Material(Shader.Find("Sprites/Default"));

        // Make preview more visible/different color
        guardCircleRenderer.startColor = new Color(1f, 1f, 0f, 0.6f); // Yellow for preview
        guardCircleRenderer.endColor = new Color(1f, 1f, 0f, 0.6f);
        guardCircleRenderer.startWidth = 0.25f; // Slightly thicker for preview
        guardCircleRenderer.endWidth = 0.25f;
        guardCircleRenderer.useWorldSpace = true;

        // Create circle points
        int segments = 64;
        guardCircleRenderer.positionCount = segments + 1;

        float angle = 0f;
        for (int i = 0; i <= segments; i++)
        {
            float x = Mathf.Sin(angle * Mathf.Deg2Rad) * guardAreaRadius;
            float z = Mathf.Cos(angle * Mathf.Deg2Rad) * guardAreaRadius;
            guardCircleRenderer.SetPosition(i, new Vector3(x, 0.1f, z)); // Slightly above ground
            angle += 360f / segments;
        }
    }

    void ShowGuardAreaPreview()
    {
        if (guardAreaIndicator != null)
        {
            Vector3 mousePos = GetMouseWorldPosition();
            guardAreaIndicator.transform.position = mousePos;
            guardAreaIndicator.SetActive(true);

            // Update circle to be at mouse position
            int segments = guardCircleRenderer.positionCount - 1;
            float angle = 0f;
            for (int i = 0; i <= segments; i++)
            {
                float x = Mathf.Sin(angle * Mathf.Deg2Rad) * guardAreaRadius;
                float z = Mathf.Cos(angle * Mathf.Deg2Rad) * guardAreaRadius;
                guardCircleRenderer.SetPosition(i, mousePos + new Vector3(x, 0.1f, z));
                angle += 360f / segments;
            }
        }
    }

    void HideGuardAreaPreview()
    {
        if (guardAreaIndicator != null)
        {
            guardAreaIndicator.SetActive(false);
        }
    }

    void SelectCommand(CommandType command)
    {
        currentCommand = command;
        UpdateButtonVisuals();
    }

    void UpdateButtonVisuals()
    {
        // Reset all buttons to normal color
        moveButton.GetComponent<Image>().color = normalColor;
        guardButton.GetComponent<Image>().color = normalColor;
        attackMoveButton.GetComponent<Image>().color = normalColor;
        patrolButton.GetComponent<Image>().color = normalColor;
        stopMovementButton.GetComponent<Image>().color = normalColor;

        // Highlight selected command
        switch (currentCommand)
        {
            case CommandType.Move:
                moveButton.GetComponent<Image>().color = selectedColor;
                break;
            case CommandType.Guard:
                guardButton.GetComponent<Image>().color = selectedColor;
                break;
            case CommandType.AttackMove:
                attackMoveButton.GetComponent<Image>().color = selectedColor;
                break;
            case CommandType.Patrol:
                patrolButton.GetComponent<Image>().color = selectedColor;
                break;
        }
    }

    void ExecuteCommand(Vector3 targetPosition, List<GameObject> selectedUnits)
    {
        foreach (GameObject unitObj in selectedUnits)
        {
            if (unitObj != null)
            {
                ICommand command = null;

                switch (currentCommand)
                {
                    case CommandType.Move:
                        command = new MoveCommand(unitObj, targetPosition);
                        break;
                    case CommandType.Guard:
                        command = new GuardCommand(unitObj, targetPosition, guardAreaRadius);
                        break;
                    case CommandType.AttackMove:
                        command = new AttackMoveCommand(unitObj, targetPosition);
                        break;
                    case CommandType.Patrol:
                        command = new PatrolCommand(unitObj, targetPosition);
                        break;
                }

                if (command != null)
                {
                    // Queue command if paused, execute immediately if not
                    if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
                    {
                        CommandQueue.Instance.QueueCommand(command);
                    }
                    else
                    {
                        command.Execute();
                    }
                }
            }
        }
    }

    Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            return hit.point;
        }

        // Fallback: project onto a ground plane at y=0
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float distance;
        if (groundPlane.Raycast(ray, out distance))
        {
            return ray.GetPoint(distance);
        }

        return Vector3.zero;
    }
}
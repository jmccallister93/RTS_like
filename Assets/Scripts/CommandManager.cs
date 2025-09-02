using System.Collections.Generic;
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

public class CommandManager : MonoBehaviour
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
    public Color guardAreaColor = new Color(0, 0.5f, 1f, 0.5f); // Semi-transparent blue
    private GameObject guardAreaIndicator;
    private LineRenderer guardCircleRenderer;

    private CommandType currentCommand = CommandType.Move;
    private Mouse mouse;



    void Start()
    {
        // Initialize mouse reference
        mouse = Mouse.current;
        
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
        // Don't process if clicking on UI
        if (mouse.leftButton.wasPressedThisFrame && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        List<GameObject> selectedUnits = UnitSelectionManager.Instance?.unitsSelected;
        bool hasSelectedUnits = selectedUnits != null && selectedUnits.Count > 0;

        // Show/hide guard area preview - only if Guard is selected AND units are selected
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
                    GameObject clickedObject = hit.collider.gameObject;

                    // Check if it's a valid enemy
                    if (IsValidEnemyTarget(clickedObject, selectedUnits))
                    {
                        // Direct attack command - make all selected units attack this target
                        foreach (GameObject unit in selectedUnits)
                        {
                            if (unit != null)
                            {
                                AttackController attackController = unit.GetComponent<AttackController>();
                                if (attackController != null)
                                {
                                    attackController.SetTarget(hit.transform);
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
                    Unit unit = unitObj.GetComponent<Unit>();
                    if (unit != null)
                    {
                        unit.StopMovement();

                        // Also clear any attack targets
                        AttackController attackController = unitObj.GetComponent<AttackController>();
                        if (attackController != null)
                        {
                            attackController.targetToAttack = null;
                            attackController.ClearGuardPosition();
                        }
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
        if (guardAreaIndicator != null )
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
                Unit unit = unitObj.GetComponent<Unit>();
                if (unit != null)
                {
                    switch (currentCommand)
                    {
                        case CommandType.Move:
                            unit.MoveTo(targetPosition);
                            break;
                        case CommandType.Guard:
                            unit.GuardPosition(targetPosition);
                            break;
                        case CommandType.AttackMove:
                            unit.AttackMoveTo(targetPosition);
                            break;
                        case CommandType.Patrol:
                            unit.PatrolTo(targetPosition);
                            break;

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
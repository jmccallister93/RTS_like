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
    Hold,
    Ability1,
    Ability2,
    Ability3,
    Ability4,
}

public class CommandManager : MonoBehaviour, IRunWhenPaused
{
    [Header("UI References")]
    public Button moveButton;
    public Button guardButton;
    public Button attackMoveButton;
    public Button patrolButton;
    public Button stopMovementButton;
    public Button holdPositionButton;
    public Button abilityButton1;
    public Button abilityButton2;
    public Button abilityButton3;
    public Button abilityButton4;

    [Header("Visual Feedback")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;

    [Header("Guard Area")]
    public GuardAreaDisplay guardAreaDisplay;

    // For pending ability casts
    private AbilitySO pendingAbility;
    private GameObject pendingCaster;
    private bool isAwaitingTarget = false;

    private CommandType currentCommand = CommandType.Move;
    private Mouse mouse;
    private Keyboard keyboard;

    void Start()
    {
        mouse = Mouse.current;
        keyboard = Keyboard.current;

        // Set up button listeners
        moveButton.onClick.AddListener(() => SelectCommand(CommandType.Move));
        guardButton.onClick.AddListener(() => SelectCommand(CommandType.Guard));
        attackMoveButton.onClick.AddListener(() => SelectCommand(CommandType.AttackMove));
        patrolButton.onClick.AddListener(() => SelectCommand(CommandType.Patrol));
        stopMovementButton.onClick.AddListener(() => ExecuteStopCommand());
        holdPositionButton.onClick.AddListener(() => ExecuteHoldCommand());
        abilityButton1.onClick.AddListener(() => SelectCommand(CommandType.Ability1));
        abilityButton2.onClick.AddListener(() => SelectCommand(CommandType.Ability2));
        abilityButton3.onClick.AddListener(() => SelectCommand(CommandType.Ability3));
        abilityButton4.onClick.AddListener(() => SelectCommand(CommandType.Ability4));

        SelectCommand(CommandType.Move);
        guardAreaDisplay.CreateGuardAreaIndicator();
    }

    void Update()
    {
        HandleHotkeyInput();

        if (mouse.leftButton.wasPressedThisFrame && EventSystem.current.IsPointerOverGameObject())
            return;

        List<GameObject> selectedUnits = UnitSelectionManager.Instance?.unitsSelected;
        bool hasSelectedUnits = selectedUnits != null && selectedUnits.Count > 0;

        // Guard area preview
        if (currentCommand == CommandType.Guard && hasSelectedUnits && !mouse.rightButton.isPressed)
            guardAreaDisplay.ShowGuardAreaPreview();
        else
            guardAreaDisplay.HideGuardAreaPreview();

        // Right-click handling
        if (mouse.rightButton.wasPressedThisFrame && hasSelectedUnits)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit hit;

            // Enemy targeting
            if (Physics.Raycast(ray, out hit, 100f, UnitSelectionManager.Instance.attackable))
            {
                GameObject clickedObject = hit.collider.gameObject;
                if (IsValidEnemyTarget(clickedObject, selectedUnits))
                {
                    foreach (GameObject unit in selectedUnits)
                    {
                        var attackCommand = new AttackTargetCommand(unit, hit.transform);
                        ExecuteOrQueueCommand(attackCommand);
                    }

                    guardAreaDisplay.HideGuardAreaPreview();
                    SelectCommand(CommandType.Move);
                    return;
                }
            }

            // If an ability is awaiting target
            if (isAwaitingTarget && pendingAbility != null && pendingCaster != null)
            {
                Vector3 targetPosition = hit.point;
                GameObject clickedObject = hit.collider?.gameObject;

                pendingAbility.StartCast(pendingCaster, targetPosition, clickedObject);
                pendingAbility.Execute(pendingCaster, targetPosition, clickedObject);

                pendingAbility = null;
                pendingCaster = null;
                isAwaitingTarget = false;

                SelectCommand(CommandType.Move);
                return;
            }

            // Normal movement/command
            Vector3 destination = GetMouseWorldPosition();
            ExecuteCommand(destination, selectedUnits);
            guardAreaDisplay.HideGuardAreaPreview();
            SelectCommand(CommandType.Move);
        }
    }

    void HandleHotkeyInput()
    {
        if (EventSystem.current.currentSelectedGameObject != null &&
            EventSystem.current.currentSelectedGameObject.GetComponent<InputField>() != null)
            return;

        if (keyboard.zKey.wasPressedThisFrame) SelectCommand(CommandType.Move);
        else if (keyboard.xKey.wasPressedThisFrame) SelectCommand(CommandType.Guard);
        else if (keyboard.cKey.wasPressedThisFrame) SelectCommand(CommandType.AttackMove);
        else if (keyboard.vKey.wasPressedThisFrame) SelectCommand(CommandType.Patrol);
        else if (keyboard.bKey.wasPressedThisFrame) ExecuteHoldCommand();
        else if (keyboard.nKey.wasPressedThisFrame) ExecuteStopCommand();
        else if (keyboard.digit1Key.wasPressedThisFrame) SelectCommand(CommandType.Ability1);
        else if (keyboard.digit2Key.wasPressedThisFrame) SelectCommand(CommandType.Ability2);
        else if (keyboard.digit3Key.wasPressedThisFrame) SelectCommand(CommandType.Ability3);
        else if (keyboard.digit4Key.wasPressedThisFrame) SelectCommand(CommandType.Ability4);
    }

    bool IsValidEnemyTarget(GameObject target, List<GameObject> selectedUnits)
    {
        if (target == null) return false;
        Unit targetUnit = target.GetComponent<Unit>();
        if (targetUnit == null || !targetUnit.IsAlive()) return false;

        foreach (GameObject selectedUnit in selectedUnits)
        {
            if (selectedUnit.tag == "Player" && target.tag == "Enemy")
                return true;
        }
        return false;
    }

    void ExecuteStopCommand()
    {
        foreach (GameObject unitObj in UnitSelectionManager.Instance?.unitsSelected ?? new List<GameObject>())
        {
            var stopCommand = new StopCommand(unitObj);
            ExecuteOrQueueCommand(stopCommand);
        }
        SelectCommand(CommandType.Move);
    }

    void ExecuteHoldCommand()
    {
        foreach (GameObject unitObj in UnitSelectionManager.Instance?.unitsSelected ?? new List<GameObject>())
        {
            var holdCommand = new HoldCommand(unitObj);
            ExecuteOrQueueCommand(holdCommand);
        }
        SelectCommand(CommandType.Move);
    }

    void SelectCommand(CommandType command)
    {
        currentCommand = command;
        UpdateButtonVisuals();
    }

    void UpdateButtonVisuals()
    {
        // reset
        moveButton.image.color = normalColor;
        guardButton.image.color = normalColor;
        attackMoveButton.image.color = normalColor;
        patrolButton.image.color = normalColor;
        stopMovementButton.image.color = normalColor;
        holdPositionButton.image.color = normalColor;
        abilityButton1.image.color = normalColor;
        abilityButton2.image.color = normalColor;
        abilityButton3.image.color = normalColor;
        abilityButton4.image.color = normalColor;

        // highlight
        switch (currentCommand)
        {
            case CommandType.Move: moveButton.image.color = selectedColor; break;
            case CommandType.Guard: guardButton.image.color = selectedColor; break;
            case CommandType.AttackMove: attackMoveButton.image.color = selectedColor; break;
            case CommandType.Patrol: patrolButton.image.color = selectedColor; break;
            case CommandType.Hold: holdPositionButton.image.color = selectedColor; break;
            case CommandType.Ability1: abilityButton1.image.color = selectedColor; break;
            case CommandType.Ability2: abilityButton2.image.color = selectedColor; break;
            case CommandType.Ability3: abilityButton3.image.color = selectedColor; break;
            case CommandType.Ability4: abilityButton4.image.color = selectedColor; break;
        }
    }

    void ExecuteCommand(Vector3 targetPosition, List<GameObject> selectedUnits)
    {
        foreach (GameObject unitObj in selectedUnits)
        {
            if (unitObj == null) continue;
            ICommand command = null;

            switch (currentCommand)
            {
                case CommandType.Move:
                    command = new MoveCommand(unitObj, targetPosition);
                    break;
                case CommandType.Guard:
                    command = new GuardCommand(unitObj, targetPosition, guardAreaDisplay.guardAreaRadius);
                    break;
                case CommandType.AttackMove:
                    command = new AttackMoveCommand(unitObj, targetPosition);
                    break;
                case CommandType.Patrol:
                    command = new PatrolCommand(unitObj, targetPosition);
                    break;
                case CommandType.Hold:
                    command = new HoldCommand(unitObj);
                    break;

                case CommandType.Ability1:
                case CommandType.Ability2:
                case CommandType.Ability3:
                case CommandType.Ability4:
                    int slotIndex = (int)currentCommand - (int)CommandType.Ability1;
                    var unitAbilities = unitObj.GetComponent<UnitAbilities>();
                    var ability = unitAbilities?.GetAbility(slotIndex);

                    if (ability != null && ability.CanUse(unitObj))
                    {
                        if (ability.TargetType == TargetType.None || ability.TargetType == TargetType.Self)
                        {
                            ability.StartCast(unitObj, unitObj.transform.position, unitObj);
                            ability.Execute(unitObj, unitObj.transform.position, unitObj);
                        }
                        else
                        {
                            pendingAbility = ability;
                            pendingCaster = unitObj;
                            isAwaitingTarget = true;
                        }
                    }
                    break;
            }

            if (command != null) ExecuteOrQueueCommand(command);
        }
    }

    void ExecuteOrQueueCommand(ICommand command)
    {
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
            CommandQueue.Instance.QueueCommand(command);
        else
            command.Execute();
    }

    Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
            return hit.point;

        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float distance))
            return ray.GetPoint(distance);

        return Vector3.zero;
    }
}
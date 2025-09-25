using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using static UnityEngine.GraphicsBuffer;

public class UnitSelectionManager : MonoBehaviour
{
    public static UnitSelectionManager Instance { get; set; }

    public List<GameObject> allUnitsList = new List<GameObject>();
    public List<GameObject> unitsSelected = new List<GameObject>();

    [Header("Selection Circle Settings")]
    public Color selectionCircleColor = Color.green;
    public float selectionCircleRadius = 0.5f;
    public float selectionCircleWidth = 0.15f;

 
    // Dictionary to track selection circles for each unit
    private Dictionary<GameObject, LineRenderer> unitSelectionCircles = new Dictionary<GameObject, LineRenderer>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public LayerMask clickable = 1;
    public LayerMask ground = 1;
    public LayerMask attackable = 1;
    public GameObject groundMarker;
    public float raycastDistance = 100f;
    private Camera cam;
    private NavMeshAgent agent;
    private Mouse mouse;
    public bool attackCursorVisible = false;

    private void Start()
    {
        cam = Camera.main;
        mouse = Mouse.current;
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        CleanupNullUnits();

        // SKIP SELECTION IF ABILITY MANAGER IS TARGETING
        // ENHANCED: More comprehensive check for ability system state
        bool abilitySystemActive = AbilityManager.Instance != null &&
            (AbilityManager.Instance.IsTargeting ||
             AbilityManager.Instance.IsCasting ||
             AbilityManager.Instance.GetComponent<AbilityTargetingSystem>()?.IsTargeting == true);

        if (abilitySystemActive)
        {
            // Debug to verify this is working
            if (mouse.leftButton.wasPressedThisFrame)
            {
                Debug.Log("Left-click blocked: Ability system is active");
            }

            HandleAttackCursor();
            return;
        }

        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;

            Vector2 mousePosition = mouse.position.ReadValue();
            Ray ray = cam.ScreenPointToRay(mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, raycastDistance, clickable))
            {
                GameObject clickedObj = hit.collider.gameObject;
                if (clickedObj.GetComponent<Unit>() != null)
                {
                    if (Keyboard.current.leftShiftKey.isPressed)
                        MultiSelect(clickedObj);
                    else
                        SelectByClicking(clickedObj);
                }
                else
                {
                    if (!Keyboard.current.leftShiftKey.isPressed)
                        DeselectAll();
                }
            }
            else
            {
                if (!Keyboard.current.leftShiftKey.isPressed)
                    DeselectAll();
            }
        }

        // Handle attack cursor and right-click attacks
        HandleAttackCursor();
    }

    // Extract attack cursor logic into separate method
    private void HandleAttackCursor()
    {
        if (unitsSelected.Count > 0)
        {
            Vector2 mousePosition = mouse.position.ReadValue();
            Ray ray = cam.ScreenPointToRay(mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, raycastDistance, attackable))
            {
                // Only show attack cursor for valid enemy targets
                if (IsValidAttackTarget(hit.collider.gameObject))
                {
                    attackCursorVisible = true;

                    // Only process right-click attacks if not targeting abilities
                    if (mouse.rightButton.wasPressedThisFrame &&
                        (AbilityManager.Instance == null || !AbilityManager.Instance.IsTargeting))
                    {
                        Transform target = hit.transform;
                        foreach (GameObject unit in unitsSelected)
                        {
                            if (unit != null && unit.GetComponent<AttackController>())
                            {
                                unit.GetComponent<AttackController>().SetTarget(target);
                            }
                        }
                    }
                }
                else
                {
                    attackCursorVisible = false;
                }
            }
            else
            {
                attackCursorVisible = false;
            }
        }
    }

    private bool IsValidAttackTarget(GameObject target)
    {
        if (target == null) return false;

        // Must have a Unit component
        if (target.GetComponent<Unit>() == null) return false;

        // Check if any selected unit can target this (different team)
        foreach (GameObject selectedUnit in unitsSelected)
        {
            if (selectedUnit != null)
            {
                string selectedTag = selectedUnit.tag;
                string targetTag = target.tag;

                // Player units can target Enemy units
                if (selectedTag == "Player" && targetTag == "Enemy")
                    return true;

                // Enemy units can target Player units (if somehow an enemy gets selected)
                if (selectedTag == "Enemy" && targetTag == "Player")
                    return true;
            }
        }

        return false;
    }

    // Clean up any null references from destroyed units
    private void CleanupNullUnits()
    {
        unitsSelected.RemoveAll(unit => unit == null);
        allUnitsList.RemoveAll(unit => unit == null);

        // Clean up selection circles for destroyed units
        var keysToRemove = new List<GameObject>();
        foreach (var kvp in unitSelectionCircles)
        {
            if (kvp.Key == null)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value.gameObject);
                keysToRemove.Add(kvp.Key);
            }
        }
        foreach (var key in keysToRemove)
        {
            unitSelectionCircles.Remove(key);
        }
    }

    // Public method to remove a unit from selection (called when unit is destroyed)
    public void RemoveUnitFromSelection(GameObject unit)
    {
        if (unitsSelected.Contains(unit))
        {
            unitsSelected.Remove(unit);
        }
        if (allUnitsList.Contains(unit))
        {
            allUnitsList.Remove(unit);
        }

        // Remove selection circle
        RemoveSelectionCircle(unit);
    }

    private void MultiSelect(GameObject unit)
    {
        if (unitsSelected.Contains(unit))
        {
            unitsSelected.Remove(unit);
            SelectUnit(unit, false);
        }
        else
        {
            unitsSelected.Add(unit);
            SelectUnit(unit, true);
        }
    }

    public void DeselectAll()
    {
        // Create a copy to avoid modification during iteration
        var unitsToDeselect = new List<GameObject>(unitsSelected);

        foreach (GameObject unit in unitsToDeselect)
        {
            if (unit != null) // Check for null
            {
                SelectUnit(unit, false);
            }
        }

        if (groundMarker != null)
        {
            groundMarker.SetActive(false);
        }
        unitsSelected.Clear();
    }

    private void SelectByClicking(GameObject unit)
    {
        DeselectAll();
        unitsSelected.Add(unit);
        SelectUnit(unit, true);
    }

    private void TriggerSelectionIndicator(GameObject unit, bool isVisible)
    {
        if (unit != null && unit.transform.childCount > 0)
        {
            unit.transform.GetChild(0).gameObject.SetActive(isVisible);
        }
    }

    private void CreateSelectionCircle(GameObject unit)
    {
        if (unit == null || unitSelectionCircles.ContainsKey(unit))
            return;

        GameObject circleObj = new GameObject("SelectionCircle");
        circleObj.transform.SetParent(unit.transform);
        circleObj.transform.localPosition = Vector3.zero;

        LineRenderer circleRenderer = circleObj.AddComponent<LineRenderer>();
        circleRenderer.material = new Material(Shader.Find("Sprites/Default"));
        circleRenderer.startColor = selectionCircleColor;
        circleRenderer.endColor = selectionCircleColor;
        circleRenderer.startWidth = selectionCircleWidth;
        circleRenderer.endWidth = selectionCircleWidth;
        circleRenderer.useWorldSpace = false;

        // Create circle points
        int segments = 32; // More segments for smoother circle
        circleRenderer.positionCount = segments + 1;

        float angle = 0f;
        for (int i = 0; i <= segments; i++)
        {
            float x = Mathf.Sin(angle * Mathf.Deg2Rad) * selectionCircleRadius;
            float z = Mathf.Cos(angle * Mathf.Deg2Rad) * selectionCircleRadius;
            circleRenderer.SetPosition(i, new Vector3(x, 0.05f, z)); // Slightly above ground
            angle += 360f / segments;
        }

        unitSelectionCircles[unit] = circleRenderer;
    }

    private void RemoveSelectionCircle(GameObject unit)
    {
        if (unitSelectionCircles.ContainsKey(unit))
        {
            if (unitSelectionCircles[unit] != null)
            {
                Destroy(unitSelectionCircles[unit].gameObject);
            }
            unitSelectionCircles.Remove(unit);
        }
    }

    private void SelectUnit(GameObject unit, bool isSelected)
    {
        if (unit != null)
        {
            TriggerSelectionIndicator(unit, isSelected);

            // Handle selection circle
            if (isSelected)
            {
                CreateSelectionCircle(unit);
            }
            else
            {
                RemoveSelectionCircle(unit);
            }

            // Notify guard area display
            GuardAreaDisplay guardDisplay = unit.GetComponent<GuardAreaDisplay>();
            if (guardDisplay != null)
            {
                guardDisplay.SetSelected(isSelected);
            }
            MoveToDisplay moveDisplay = unit.GetComponent<MoveToDisplay>();
            if (moveDisplay != null)
            {
                moveDisplay.SetSelected(isSelected);
            }
        }
    }

    internal void DragSelect(GameObject unit)
    {
        if (unit != null && !unitsSelected.Contains(unit))
        {
            unitsSelected.Add(unit);
            SelectUnit(unit, true);
        }
    }


}
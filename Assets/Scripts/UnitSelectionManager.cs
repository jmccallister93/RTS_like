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

        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return; // Don't process selection when clicking UI
            }

            Vector2 mousePosition = mouse.position.ReadValue();
            Ray ray = cam.ScreenPointToRay(mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, raycastDistance, clickable))
            {
                if (Keyboard.current.leftShiftKey.isPressed)
                {
                    MultiSelect(hit.collider.gameObject);
                }
                else
                {
                    SelectByClicking(hit.collider.gameObject);
                }
            }
            else
            {
                if (!Keyboard.current.leftShiftKey.isPressed)
                {
                    DeselectAll();
                }
            }
        }

        

        if (unitsSelected.Count > 0 && AtleastOneOffensiveUnit(unitsSelected))
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
                    if (mouse.rightButton.wasPressedThisFrame)
                    {
                        Transform target = hit.transform;
                        foreach (GameObject unit in unitsSelected)
                        {
                            if (unit != null && unit.GetComponent<AttackController>())
                            {
                                // Use the improved SetTarget method
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

    //private bool CanSelectUnit(GameObject unit)
    //{
    //    // Only allow selection of units with "Player" tag
    //    return unit != null && unit.CompareTag("Player");
    //}


    // Clean up any null references from destroyed units
    private void CleanupNullUnits()
    {
        unitsSelected.RemoveAll(unit => unit == null);
        allUnitsList.RemoveAll(unit => unit == null);
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
    }

    private bool AtleastOneOffensiveUnit(List<GameObject> unitsSelected)
    {
        foreach (GameObject unit in unitsSelected)
        {
            if (unit != null && unit.GetComponent<AttackController>())
            {
                return true;
            }
        }
        return false;
    }

    private void MultiSelect(GameObject unit)
    {

        //if (!CanSelectUnit(unit)) return;

        if (unitsSelected.Contains(unit))
        {
            unitsSelected.Remove(unit);
            SelectUnit(unit, false); // Fixed: was calling EnableUnitMovement instead of SelectUnit
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

    private void EnableUnitMovement(GameObject unit, bool shouldMove)
    {
        if (unit != null)
        {
            var unitMovement = unit.GetComponent<UnitMovement>();
            if (unitMovement != null)
            {
                unitMovement.enabled = shouldMove;
            }
        }
    }

    private void TriggerSelectionIndicator(GameObject unit, bool isVisible)
    {
        if (unit != null && unit.transform.childCount > 0)
        {
            unit.transform.GetChild(0).gameObject.SetActive(isVisible);
        }
    }

    private void SelectUnit(GameObject unit, bool isSelected)
    {
        if (unit != null)
        {
            TriggerSelectionIndicator(unit, isSelected);
            //EnableUnitMovement(unit, isSelected);

            // Notify guard area display
            GuardAreaDisplay guardDisplay = unit.GetComponent<GuardAreaDisplay>();
            if (guardDisplay != null)
            {
                guardDisplay.SetSelected(isSelected);
            }
        }
    }

    internal void DragSelect(GameObject unit)
    {
        //if (!CanSelectUnit(unit)) return;

        if (unit != null && !unitsSelected.Contains(unit))
        {
            unitsSelected.Add(unit);
            SelectUnit(unit, true);
        }
    }
}
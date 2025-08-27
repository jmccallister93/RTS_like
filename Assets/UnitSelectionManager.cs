using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

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
    public GameObject groundMarker;
    public float raycastDistance = 100f;
    private Camera cam;
    private NavMeshAgent agent;
    private Mouse mouse;

    private void Start()
    {
        cam = Camera.main;
        mouse = Mouse.current;
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (mouse.leftButton.wasPressedThisFrame)
        {

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
                if (!Input.GetKey(KeyCode.LeftShift))
                {
                    DeselectAll();
                }
                
            }
        }

        if (mouse.rightButton.wasPressedThisFrame && unitsSelected.Count > 0)
        {

            Vector2 mousePosition = mouse.position.ReadValue();
            Ray ray = cam.ScreenPointToRay(mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, raycastDistance, ground))
            {
                groundMarker.transform.position = hit.point;
                groundMarker.SetActive(false);
                groundMarker.SetActive(true);



            }
        }
        }

    private void MultiSelect(GameObject unit)
    {
        if(unitsSelected.Contains(unit))
        {
            unitsSelected.Remove(unit);
            EnableUnitMovement(unit, false);
        }
        else
        {
            unitsSelected.Add(unit);
            TriggerSelectionIndicator(unit, true);
            EnableUnitMovement(unit, true);
        }
    }

    private void DeselectAll()
    {
        foreach (GameObject unit in unitsSelected)
        {
            EnableUnitMovement(unit, false);
            TriggerSelectionIndicator(unit, false);
        }
        groundMarker.SetActive(false);
        unitsSelected.Clear();
    }

    private void SelectByClicking(GameObject unit)
    {
        DeselectAll();

        unitsSelected.Add(unit);
        TriggerSelectionIndicator(unit, true);
        EnableUnitMovement(unit, true);
    }

    private void EnableUnitMovement(GameObject unit, bool shouldMove)
    {
        unit.GetComponent<UnitMovement>().enabled = shouldMove;
    }

    private void TriggerSelectionIndicator(GameObject unit, bool isVisible)
    {
        unit.transform.GetChild(0).gameObject.SetActive(isVisible);
    }
}

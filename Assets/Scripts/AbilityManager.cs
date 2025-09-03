using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;


public class AbilityManager : MonoBehaviour
{

    [Header("UI References")]
    public Button ability1Button;
    public Button ability2Button;
    public Button ability3Button;
    public Button ability4Button;
    public Button ability5Button;
    public Button ability6Button;

    [Header("Visual Feedback")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;

    private Mouse mouse;
    void Start()
    {
        mouse = Mouse.current;
        ability1Button.onClick.AddListener(() => OnAbilityButtonClicked(ability1Button));
        ability2Button.onClick.AddListener(() => OnAbilityButtonClicked(ability2Button));
        ability3Button.onClick.AddListener(() => OnAbilityButtonClicked(ability3Button));
        ability4Button.onClick.AddListener(() => OnAbilityButtonClicked(ability4Button));
        ability5Button.onClick.AddListener(() => OnAbilityButtonClicked(ability5Button));
        ability6Button.onClick.AddListener(() => OnAbilityButtonClicked(ability6Button));

    }


    // Update is called once per frame
    void Update()
    {
        if (mouse.leftButton.wasPressedThisFrame && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
    }

    private void OnAbilityButtonClicked(Button ability1Button)
    {
        
    }

    private void DisplayUnitAbility()
    {

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

    bool IsValidAllyTarget(GameObject target, List<GameObject> selectedUnits)
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
                if (selectedTag == "Player" && targetTag == "Player")
                    return true;
            }
        }

        return false;
    }

    bool IsValidAreaTarget(GameObject target, List<GameObject> selectedUnits)
    {
        return false;
    }

    bool IsValidSelfTarget(GameObject target, List<GameObject> selectedUnits)
    {
        return false;
    }

    bool IsAbilityOnCooldown()
    {
        return false;
    }

    bool IsAbilityInRange()
    {
        return false;
    }


    private void ExecuteAbility()
    {

    }

    private void CancelAbility()
    {
    }   

    void UpdateButtonVisuals()
    {
        // Reset all buttons to normal color
        ability1Button.image.color = normalColor;
        ability2Button.image.color = normalColor;
        ability3Button.image.color = normalColor;
        ability4Button.image.color = normalColor;
        ability5Button.image.color = normalColor;
        ability6Button.image.color = normalColor;
        // Highlight selected ability button
        // Example: if ability 1 is selected
        //ability1Button.image.color = selectedColor;
    }

    void ShowAbilityAreaPreview()
    {

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

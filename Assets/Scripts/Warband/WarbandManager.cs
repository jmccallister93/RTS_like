using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class WarbandManager : MonoBehaviour, IRunWhenPaused
{
    [Header("UI References")]
    public Button warbandMember1Button;
    public Button warbandMember2Button;
    public Button warbandMember3Button;
    public Button warbandMember4Button;
    public Button warbandMember5Button;
    public Button warbandMember6Button;
    public Button warbandMember7Button;

    [Header("Unit References")]
    [SerializeField] private List<GameObject> warbandUnits = new List<GameObject>();

    [Header("Camera Settings")]
    public Camera cam;
    public float cameraFollowSpeed = 5f;
    public Vector3 cameraOffset = new Vector3(0, 10, -8);
    public float cameraDistance = 10f;

    [Header("Visual Feedback")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;
    public Color deadColor = Color.red;

    private Mouse mouse;
    private List<Button> warbandButtons = new List<Button>();
    private bool isCameraFollowing = false;
    private GameObject targetUnit;

    void Start()
    {
        // Initialize camera reference
        if (cam == null)
            cam = Camera.main;

        mouse = Mouse.current;

        // Store buttons in a list for easier management
        warbandButtons.AddRange(new Button[] {
            warbandMember1Button,
            warbandMember2Button,
            warbandMember3Button,
            warbandMember4Button,
            warbandMember5Button,
            warbandMember6Button,
            warbandMember7Button
        });

        // Set up button listeners
        for (int i = 0; i < warbandButtons.Count; i++)
        {
            int index = i; // Capture the index for the lambda
            warbandButtons[i].onClick.AddListener(() => OnWarbandMemberButtonClicked(index));
        }

        // Auto-assign units if not manually assigned
        if (warbandUnits.Count == 0)
        {
            AutoAssignUnits();
        }

        // Initialize button states
        UpdateButtonStates();
    }

    void Update()
    {
        if (mouse.leftButton.wasPressedThisFrame)
        {
            // Check if the EventSystem has an actively selected UI element
            GameObject selectedUI = EventSystem.current.currentSelectedGameObject;

            if (selectedUI != null)
            {
                // A UI element was clicked → ignore world logic
                // ✅ Buttons will still fire because we didn’t cancel Update
            }
            else
            {
                // No UI element selected → handle world click
                HandleWorldClick();
            }
        }

        // Handle camera following
        if (isCameraFollowing && targetUnit != null)
        {
            FollowUnit(targetUnit);
        }

        // Update button states based on current selection
        UpdateButtonStates();
    }


    private void HandleWorldClick()
    {
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Example: hook into your UnitSelectionManager
            //Debug.Log("Clicked world object: " + hit.collider.name);
        }
    }

    /// <summary>
    /// Automatically assigns units from UnitSelectionManager's allUnitsList
    /// </summary>
    private void AutoAssignUnits()
    {
        // Find all GameObjects with "Player" tag in the scene
        GameObject[] playerUnits = GameObject.FindGameObjectsWithTag("Player");

        // Clear existing warband units
        warbandUnits.Clear();

        // Take up to 7 units (matching the number of buttons)
        int maxUnits = Mathf.Min(playerUnits.Length, warbandButtons.Count);

        for (int i = 0; i < maxUnits; i++)
        {
            GameObject unit = playerUnits[i];
            if (unit != null)
            {
                warbandUnits.Add(unit);
            }
        }

        // Fill remaining slots with null if we have fewer than 7 units
        while (warbandUnits.Count < warbandButtons.Count)
        {
            warbandUnits.Add(null);
        }

        Debug.Log($"Auto-assigned {maxUnits} player units to warband slots");
    }

    public void AutoAssignPlayerUnits()
    {
        AutoAssignUnits();
        UpdateButtonStates();
    }

    /// <summary>
    /// Manually assign a unit to a specific warband slot
    /// </summary>
    public void AssignUnitToSlot(GameObject unit, int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < warbandButtons.Count)
        {
            // Ensure the list is large enough
            while (warbandUnits.Count <= slotIndex)
            {
                warbandUnits.Add(null);
            }

            warbandUnits[slotIndex] = unit;
            UpdateButtonStates();
        }
    }

    /// <summary>
    /// Called when a warband button is clicked
    /// </summary>
    private void OnWarbandMemberButtonClicked(int memberIndex)
    {
        if (memberIndex >= 0 && memberIndex < warbandUnits.Count)
        {
            GameObject selectedUnit = warbandUnits[memberIndex];

            if (selectedUnit != null && IsMemberAlive(selectedUnit))
            {
                // Move camera to the unit
                MoveCameraToMemberPrecise(selectedUnit);

                // Select the unit in UnitSelectionManager
                SelectWarbandMember(selectedUnit);

                UpdateButtonStates();
            }
        }
    }



    /// <summary>
    /// Moves the camera to focus on a specific unit
    /// </summary>
    //private void MoveCameraToMember(GameObject unit)
    //{
    //    if (unit != null && cam != null)
    //    {
    //        // Get current camera state
    //        Vector3 currentPosition = cam.transform.position;
    //        Quaternion currentRotation = cam.transform.rotation;

    //        // Calculate where the camera is currently looking at based on its forward direction and distance
    //        Vector3 currentLookAtPoint = currentPosition + cam.transform.forward * cameraDistance;

    //        // Calculate the offset between where camera is looking and where the unit is
    //        Vector3 offset = unit.transform.position - currentLookAtPoint;

    //        // Move camera by this offset to center the unit while maintaining rotation and zoom
    //        Vector3 newPosition = currentPosition + offset;

    //        targetUnit = unit;
    //        isCameraFollowing = true;

    //        StartCoroutine(SmoothCameraMove(unit, newPosition, currentRotation));
    //    }
    //}
    private void MoveCameraToMemberPrecise(GameObject unit)
    {
        if (unit != null && cam != null)
        {
            // Get current camera state
            Vector3 currentPosition = cam.transform.position;
            Quaternion currentRotation = cam.transform.rotation;

            // Raycast from screen center to find where camera is currently looking
            Ray centerRay = cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0));

            // Find intersection with a plane at the unit's Y level
            Plane targetPlane = new Plane(Vector3.up, unit.transform.position);
            float distance;

            Vector3 currentCenterPoint;
            if (targetPlane.Raycast(centerRay, out distance))
            {
                currentCenterPoint = centerRay.GetPoint(distance);
            }
            else
            {
                // Fallback to forward direction method
                currentCenterPoint = currentPosition + cam.transform.forward * cameraDistance;
            }

            // Calculate offset and new position
            Vector3 offset = unit.transform.position - currentCenterPoint;
            Vector3 newPosition = currentPosition + offset;

            targetUnit = unit;
            isCameraFollowing = true;

            StartCoroutine(SmoothCameraMove(unit, newPosition, currentRotation));
        }
    }

    /// <summary>
    /// Smoothly moves camera to the target unit
    /// </summary>
    private System.Collections.IEnumerator SmoothCameraMove(GameObject unit, Vector3 targetPosition, Quaternion targetRotation)
    {
        Vector3 startPosition = cam.transform.position;
        Quaternion startRotation = cam.transform.rotation;

        float elapsedTime = 0;
        float duration = 1f / cameraFollowSpeed;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;

            // Smoothly interpolate position and rotation
            cam.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            cam.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, t);

            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        // Ensure we end up at exact target position and rotation
        cam.transform.position = targetPosition;
        cam.transform.rotation = targetRotation;
        isCameraFollowing = false;
    }

    /// <summary>
    /// Continuously follow a unit (for real-time following)
    /// </summary>
    private void FollowUnit(GameObject unit)
    {
        if (unit != null)
        {
            Vector3 targetPosition = unit.transform.position + cameraOffset;
            cam.transform.position = Vector3.Lerp(cam.transform.position, targetPosition, Time.deltaTime * cameraFollowSpeed);
            cam.transform.LookAt(unit.transform);
        }
    }

    /// <summary>
    /// Selects the warband member using UnitSelectionManager
    /// </summary>
    private void SelectWarbandMember(GameObject unit)
    {
        if (UnitSelectionManager.Instance != null)
        {
            // Check if Shift is held for multi-selection
            bool addToSelection = Keyboard.current.leftShiftKey.isPressed;

            if (!addToSelection)
            {
                // Clear current selection if not holding Shift
                UnitSelectionManager.Instance.DeselectAll();
            }

            // Add the unit to selection if not already selected
            if (!UnitSelectionManager.Instance.unitsSelected.Contains(unit))
            {
                UnitSelectionManager.Instance.unitsSelected.Add(unit);
                // Trigger selection visual feedback
                UnitSelectionManager.Instance.GetComponent<UnitSelectionManager>()
                    .GetType()
                    .GetMethod("SelectUnit", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.Invoke(UnitSelectionManager.Instance, new object[] { unit, true });
            }
        }
    }

    /// <summary>
    /// Updates the visual state of all warband buttons
    /// </summary>
    private void UpdateButtonStates()
    {
        for (int i = 0; i < warbandButtons.Count; i++)
        {
            if (warbandButtons[i] != null)
            {
                Color buttonColor = normalColor;

                if (i < warbandUnits.Count && warbandUnits[i] != null)
                {
                    GameObject unit = warbandUnits[i];

                    if (!IsMemberAlive(unit))
                    {
                        buttonColor = deadColor;
                    }
                    else if (UnitSelectionManager.Instance != null &&
                             UnitSelectionManager.Instance.unitsSelected.Contains(unit))
                    {
                        buttonColor = selectedColor;
                    }
                    else
                    {
                        buttonColor = normalColor;
                    }
                }
                else
                {
                    // No unit assigned to this slot
                    buttonColor = Color.gray;
                }

                // Apply the color to the button
                ColorBlock colors = warbandButtons[i].colors;
                colors.normalColor = buttonColor;
                colors.selectedColor = buttonColor; // ✅ add this
                colors.highlightedColor = buttonColor * 1.2f;
                colors.pressedColor = buttonColor * 0.8f;
                warbandButtons[i].colors = colors;

            }
        }
    }



    /// <summary>
    /// Checks if a warband member is alive
    /// </summary>
    private bool IsMemberAlive(GameObject unit)
    {
        if (unit == null) return false;

        // Check if the unit has a health component
        var healthComponent = unit.GetComponent<Unit>(); // Adjust based on your health component name
        if (healthComponent != null)
        {
            // Assuming your Unit script has a way to check if alive
            // You might need to adjust this based on your Unit implementation
            return unit.activeInHierarchy;
        }

        return unit.activeInHierarchy;
    }

    /// <summary>
    /// Public method to refresh the warband list (call when units are spawned/destroyed)
    /// </summary>
    public void RefreshWarbandList()
    {
        //AutoAssignUnits();
        UpdateButtonStates();
    }

    /// <summary>
    /// Get the unit assigned to a specific warband slot
    /// </summary>
    public GameObject GetWarbandUnit(int index)
    {
        if (index >= 0 && index < warbandUnits.Count)
        {
            return warbandUnits[index];
        }
        return null;
    }
}
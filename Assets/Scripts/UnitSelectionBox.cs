using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class UnitSelectionBox : MonoBehaviour
{
    Camera myCam;
    [SerializeField] RectTransform boxVisual;
    Rect selectionBox;
    Vector2 startPosition;
    Vector2 endPosition;
    private Mouse mouse;

    private void Start()
    {
        myCam = Camera.main;
        mouse = Mouse.current;
        startPosition = Vector2.zero;
        endPosition = Vector2.zero;
        DrawVisual();
    }

    private void Update()
    {
        // When Clicked
        if (mouse.leftButton.wasPressedThisFrame)
        {
            startPosition = mouse.position.ReadValue();
            // For selection the Units
            selectionBox = new Rect();
        }

        // When Dragging
        if (mouse.leftButton.isPressed)
        {
            if(boxVisual.rect.width > 0 || boxVisual.rect.height > 0)
            {
                UnitSelectionManager.Instance.DeselectAll();
                SelectUnits();
            }
             

            endPosition = mouse.position.ReadValue();
            DrawVisual();
            DrawSelection();
        }

        // When Releasing
        if (mouse.leftButton.wasReleasedThisFrame)
        {
            SelectUnits();
            startPosition = Vector2.zero;
            endPosition = Vector2.zero;
            DrawVisual();
        }
    }

    void DrawVisual()
    {
        // Calculate the starting and ending positions of the selection box.
        Vector2 boxStart = startPosition;
        Vector2 boxEnd = endPosition;

        // Calculate the center of the selection box.
        Vector2 boxCenter = (boxStart + boxEnd) / 2;

        // Set the position of the visual selection box based on its center.
        boxVisual.position = boxCenter;

        // Calculate the size of the selection box in both width and height.
        Vector2 boxSize = new Vector2(Mathf.Abs(boxStart.x - boxEnd.x), Mathf.Abs(boxStart.y - boxEnd.y));

        // Set the size of the visual selection box based on its calculated size.
        boxVisual.sizeDelta = boxSize;
    }

    void DrawSelection()
    {
        Vector2 mousePos = mouse.position.ReadValue();

        if (mousePos.x < startPosition.x)
        {
            selectionBox.xMin = mousePos.x;
            selectionBox.xMax = startPosition.x;
        }
        else
        {
            selectionBox.xMin = startPosition.x;
            selectionBox.xMax = mousePos.x;
        }

        if (mousePos.y < startPosition.y)
        {
            selectionBox.yMin = mousePos.y;
            selectionBox.yMax = startPosition.y;
        }
        else
        {
            selectionBox.yMin = startPosition.y;
            selectionBox.yMax = mousePos.y;
        }
    }

    void SelectUnits()
    {
        foreach (var unit in UnitSelectionManager.Instance.allUnitsList)
        {
            if (selectionBox.Contains(myCam.WorldToScreenPoint(unit.transform.position)))
            {
                UnitSelectionManager.Instance.DragSelect(unit);
            }
        }
    }
}
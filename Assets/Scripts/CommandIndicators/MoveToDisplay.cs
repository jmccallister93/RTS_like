using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class MoveToDisplay : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private bool isSelected = false;
    private NavMeshAgent agent;

   
    [Header("Line Settings")]
    public Color moveColor = Color.green;
    public Color guardColor = Color.blue;
    public Color patrolColor = Color.yellow;
    public Color attackMoveColor = Color.red;
    public float lineWidth = 0.1f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        CreateLineRenderer();
    }

    void Update()
    {
        bool shouldShow = isSelected && agent != null && agent.hasPath &&
                         agent.destination != transform.position;

        if (lineRenderer != null)
        {
            lineRenderer.enabled = shouldShow;
            if (shouldShow)
            {
                // Get movement type and set appropriate color
                UnitMovement movement = GetComponent<UnitMovement>();
                Color currentColor = GetColorForMovementMode(movement?.currentMode ?? MovementMode.Move);

                lineRenderer.startColor = currentColor;
                lineRenderer.endColor = currentColor;

                DrawLine(transform.position, agent.destination);
            }
        }
    }

    public void CreateLineRenderer()
    {
        GameObject lineObj = new GameObject("MoveLine");
        lineObj.transform.SetParent(transform);
        lineObj.transform.localPosition = Vector3.zero;

        lineRenderer = lineObj.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = moveColor;
        lineRenderer.endColor = moveColor;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;
    }

    private Color GetColorForMovementMode(MovementMode mode)
    {
        switch (mode)
        {
            case MovementMode.Move: return moveColor;
            case MovementMode.Guard: return guardColor;
            case MovementMode.Patrol: return patrolColor;
            case MovementMode.AttackMove: return attackMoveColor;
            default: return moveColor;
        }
    }

    private void DrawLine(Vector3 start, Vector3 end)
    {
        lineRenderer.SetPosition(0, start + Vector3.up * 0.1f);
        lineRenderer.SetPosition(1, end + Vector3.up * 0.1f);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
    }

    // These methods are no longer needed since we're reading directly from NavMesh
    // But keep them for backward compatibility if needed
    public void SetMoveTarget(Vector3 target) { /* No longer used */ }
    public void ClearMoveTarget() { /* No longer used */ }
}
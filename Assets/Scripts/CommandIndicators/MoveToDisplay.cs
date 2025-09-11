using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class MoveToDisplay : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private LineRenderer iconRenderer;
    private bool isSelected = false;
    private NavMeshAgent agent;
    private AttackController attackController;


    [Header("Line Settings")]
    public Color moveColor = Color.green;
    public Color guardColor = Color.blue;
    public Color patrolColor = Color.yellow;
    public Color attackMoveColor = Color.orange;
    public Color followEnemyColor = Color.red;
    public float lineWidth = 0.1f;

    [Header("Destination Icon Settings")]
    public bool showDestinationIcon = true;
    public float iconSize = 1f;
    public enum IconType { X, Circle, Arrow, Diamond }
    public IconType moveIconType = IconType.Circle;
    public IconType enemyIconType = IconType.X;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        attackController = GetComponent<AttackController>();
        CreateLineRenderer();
        CreateIconRenderer();
    }

    void Update()
    {
        if (!isSelected || agent == null)
        {
            if (lineRenderer != null)
                lineRenderer.enabled = false;
            HideDestinationIcon(); // Also hide icon when not selected
            return;
        }

        // Check if unit is following an enemy
        bool isFollowingEnemy = attackController != null &&
                               attackController.targetToAttack != null;

        // Check if unit is actually moving (not just has a destination)
        bool isActuallyMoving = agent.hasPath &&
                               agent.remainingDistance > agent.stoppingDistance &&
                               agent.velocity.sqrMagnitude > 0.01f; // Small threshold for velocity

        // Alternative: Use your UnitMovement component's IsMoving method
        // UnitMovement movement = GetComponent<UnitMovement>();
        // bool isActuallyMoving = movement != null && movement.IsMoving();

        // Show line if following enemy OR actually moving
        bool shouldShow = isFollowingEnemy || isActuallyMoving;

        if (lineRenderer != null)
        {
            lineRenderer.enabled = shouldShow;
            if (shouldShow)
            {
                Vector3 lineDestination = agent.destination; 
                Color lineColor;

                if (isFollowingEnemy)
                {
                    // Following an enemy - draw line to enemy position
                    //lineDestination = attackController.targetToAttack.position;
                    lineColor = followEnemyColor;
                }
                else
                {
                    // Regular movement - draw line to NavMesh destination
                    lineDestination = agent.destination;
                    UnitMovement movement = GetComponent<UnitMovement>();
                    lineColor = GetColorForMovementMode(movement?.currentMode ?? MovementMode.Move);
                }

                lineRenderer.startColor = lineColor;
                lineRenderer.endColor = lineColor;
                DrawLine(transform.position, lineDestination);

                if (showDestinationIcon)
                {
                    ShowDestinationIcon(lineDestination, isFollowingEnemy, lineColor);
                }
            }
            else
            {
                HideDestinationIcon(); // Hide icon when not showing line
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


    private void DrawLine(Vector3 start, Vector3 end)
    {
        lineRenderer.SetPosition(0, start + Vector3.up * 0.1f);
        lineRenderer.SetPosition(1, end + Vector3.up * 0.1f);
    }

    void CreateIconRenderer()
    {
        GameObject iconObj = new GameObject("DestinationIcon");
        iconObj.transform.SetParent(transform);

        iconRenderer = iconObj.AddComponent<LineRenderer>();
        iconRenderer.material = new Material(Shader.Find("Sprites/Default"));
        iconRenderer.startWidth = lineWidth * 1.5f;
        iconRenderer.endWidth = lineWidth * 1.5f;
        iconRenderer.useWorldSpace = true;
        iconRenderer.enabled = false;
    }

    void ShowDestinationIcon(Vector3 position, bool isEnemyTarget, Color iconColor)
    {
        if (iconRenderer == null) return;

        IconType iconType = isEnemyTarget ? enemyIconType : moveIconType;
        //Color iconColor = isEnemyTarget ? followEnemyColor : moveColor;

        iconRenderer.startColor = iconColor;  // Use the passed color
        iconRenderer.endColor = iconColor;    // Use the passed color
        iconRenderer.enabled = true;

        DrawIcon(position + Vector3.up * 0.15f, iconType);
    }

    void DrawIcon(Vector3 center, IconType iconType)
    {
        switch (iconType)
        {
            case IconType.X:
                DrawX(center);
                break;
            case IconType.Circle:
                DrawCircle(center);
                break;
           
        }
    }

    void DrawX(Vector3 center)
    {
        iconRenderer.positionCount = 5; // Two lines (4 points) + separator
        float half = iconSize * 0.5f;

        // First line of X
        iconRenderer.SetPosition(0, center + new Vector3(-half, 0, -half));
        iconRenderer.SetPosition(1, center + new Vector3(half, 0, half));

        // Break (duplicate point to create line break)
        iconRenderer.SetPosition(2, center + new Vector3(half, 0, half));

        // Second line of X
        iconRenderer.SetPosition(3, center + new Vector3(-half, 0, half));
        iconRenderer.SetPosition(4, center + new Vector3(half, 0, -half));
    }

    void DrawCircle(Vector3 center)
    {
        int segments = 16;
        iconRenderer.positionCount = segments + 1;

        float angle = 0f;
        for (int i = 0; i <= segments; i++)
        {
            float x = Mathf.Sin(angle * Mathf.Deg2Rad) * iconSize * 0.5f;
            float z = Mathf.Cos(angle * Mathf.Deg2Rad) * iconSize * 0.5f;
            iconRenderer.SetPosition(i, center + new Vector3(x, 0, z));
            angle += 360f / segments;
        }
    }



    void HideDestinationIcon()
    {
        if (iconRenderer != null)
            iconRenderer.enabled = false;
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


    public void SetSelected(bool selected)
    {
        isSelected = selected;
    }

}
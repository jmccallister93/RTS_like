using UnityEngine;
using UnityEngine.InputSystem;

public class AbilityIndicator : MonoBehaviour
{
    [Header("Render Settings")]
    public Material indicatorMaterial;
    public float lineWidth = 0.15f;
    public int circleSegments = 32;

    [Header("Colors")]
    public Color allyColor = new Color(0, 1f, 0, 0.4f);      // Green for ally abilities
    public Color enemyColor = new Color(1f, 0, 0, 0.4f);     // Red for enemy abilities  
    public Color areaColor = new Color(0, 0.5f, 1f, 0.4f);   // Blue for area abilities
    public Color pathColor = new Color(1f, 1f, 0, 0.4f);     // Yellow for path abilities

    // Renderers for different shapes
    private LineRenderer circleRenderer;
    private LineRenderer rectangleRenderer;
    private LineRenderer lineRenderer;

    // Current state
    private TargetType currentTargetType = TargetType.None;
    private IAbility currentAbility;
    private Camera playerCamera;
    private Mouse mouse;
    private bool isActive = false;

    // Ability parameters
    private float currentRange = 5f;
    private float currentWidth = 2f;  // For rectangle width or line thickness area
    private Vector3 startPosition;

    void Awake()
    {
        playerCamera = Camera.main;
        if (indicatorMaterial == null)
        {
            indicatorMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        CreateRenderers();
    }

    void CreateRenderers()
    {
        // Circle Renderer (for Area abilities)
        circleRenderer = CreateRenderer("CircleIndicator", circleSegments + 1);

        // Rectangle Renderer (for rectangular area abilities)
        rectangleRenderer = CreateRenderer("RectangleIndicator", 5); // 4 corners + close

        // Line Renderer (for path/line abilities)
        lineRenderer = CreateRenderer("LineIndicator", 2); // Start and end point
        lineRenderer.startWidth = lineWidth * 2f; // Make line abilities wider
        lineRenderer.endWidth = lineWidth * 2f;
    }

    LineRenderer CreateRenderer(string name, int positionCount)
    {
        GameObject rendererObj = new GameObject(name);
        rendererObj.transform.SetParent(transform);
        rendererObj.transform.localPosition = Vector3.zero;

        LineRenderer renderer = rendererObj.AddComponent<LineRenderer>();
        renderer.material = indicatorMaterial;
        renderer.startWidth = lineWidth;
        renderer.endWidth = lineWidth;
        renderer.useWorldSpace = true;
        renderer.positionCount = positionCount;
        renderer.enabled = false;

        return renderer;
    }

    public void ShowIndicator(IAbility ability, Vector3 casterPosition)
    {
        if (ability == null) return;

        currentAbility = ability;
        currentTargetType = ability.TargetType;
        currentRange = ability.Range;
        startPosition = casterPosition;
        isActive = true;

        // Hide all renderers first
        HideAllRenderers();

        // Set color based on target type
        Color indicatorColor = GetColorForTargetType(currentTargetType);

        // Show appropriate renderer
        switch (currentTargetType)
        {
            case TargetType.Area:
                circleRenderer.enabled = true;
                SetRendererColor(circleRenderer, indicatorColor);
                break;

            case TargetType.Point:
                // Use rectangle for point abilities (could be circle too)
                rectangleRenderer.enabled = true;
                SetRendererColor(rectangleRenderer, indicatorColor);
                currentWidth = 1f; // Small rectangle for point targeting
                break;

            case TargetType.Path:
                lineRenderer.enabled = true;
                SetRendererColor(lineRenderer, indicatorColor);
                break;

            //case TargetType.Enemy:
            //case TargetType.Ally:
            //    // Could show a small circle for single target abilities
            //    circleRenderer.enabled = true;
            //    SetRendererColor(circleRenderer, indicatorColor);
            //    currentRange = 1f; // Small indicator for single targets
            //    break;
        }
    }

    public void HideIndicator()
    {
        isActive = false;
        HideAllRenderers();
        currentAbility = null;
        currentTargetType = TargetType.None;
    }

    void HideAllRenderers()
    {
        circleRenderer.enabled = false;
        rectangleRenderer.enabled = false;
        lineRenderer.enabled = false;
    }

    void SetRendererColor(LineRenderer renderer, Color color)
    {
        renderer.startColor = color;
        renderer.endColor = color;
    }

    Color GetColorForTargetType(TargetType targetType)
    {
        return targetType switch
        {
            TargetType.Ally => allyColor,
            TargetType.Enemy => enemyColor,
            TargetType.Area => areaColor,
            TargetType.Path => pathColor,
            TargetType.Point => areaColor, // Use area color for point
            _ => areaColor
        };
    }

    void Update()
    {
        if (!isActive || currentAbility == null) return;

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        if (mouseWorldPos == Vector3.zero) return;

        switch (currentTargetType)
        {
            case TargetType.Area:
            case TargetType.Enemy:
            case TargetType.Ally:
                UpdateCircleIndicator(mouseWorldPos);
                break;

            case TargetType.Point:
                UpdateRectangleIndicator(mouseWorldPos);
                break;

            case TargetType.Path:
                UpdateLineIndicator(mouseWorldPos);
                break;
        }
    }

    void UpdateCircleIndicator(Vector3 center)
    {
        Vector3[] positions = new Vector3[circleSegments + 1];

        for (int i = 0; i <= circleSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / circleSegments;
            float x = Mathf.Cos(angle) * currentRange;
            float z = Mathf.Sin(angle) * currentRange;
            positions[i] = center + new Vector3(x, 0.1f, z); // Slightly above ground
        }

        circleRenderer.SetPositions(positions);
    }

    void UpdateRectangleIndicator(Vector3 center)
    {
        Vector3[] positions = new Vector3[5];
        float halfWidth = currentWidth * 0.5f;
        float halfHeight = currentRange * 0.5f;

        // Rectangle corners (clockwise)
        positions[0] = center + new Vector3(-halfWidth, 0.1f, -halfHeight);
        positions[1] = center + new Vector3(halfWidth, 0.1f, -halfHeight);
        positions[2] = center + new Vector3(halfWidth, 0.1f, halfHeight);
        positions[3] = center + new Vector3(-halfWidth, 0.1f, halfHeight);
        positions[4] = positions[0]; // Close the rectangle

        rectangleRenderer.SetPositions(positions);
    }

    void UpdateLineIndicator(Vector3 endPosition)
    {
        Vector3[] positions = new Vector3[2];
        positions[0] = startPosition + Vector3.up * 0.1f;
        positions[1] = endPosition + Vector3.up * 0.1f;

        lineRenderer.SetPositions(positions);
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
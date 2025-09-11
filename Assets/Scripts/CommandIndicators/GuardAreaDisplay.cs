using UnityEngine;
using UnityEngine.InputSystem;

public class GuardAreaDisplay : MonoBehaviour
{
    private LineRenderer circleRenderer;
    private AttackController attackController;
    private bool isSelected = false;

    [Header("Guard Mode Settings")]
    public float guardAreaRadius = 5f;
    public Color guardAreaColor = new Color(0, 0.5f, 1f, 0.5f);
    private GameObject guardAreaIndicator;
    private LineRenderer guardCircleRenderer;

    void Start()
    {
        attackController = GetComponent<AttackController>();
        CreateCircleRenderer();
    }

    void CreateCircleRenderer()
    {
        // Create a child GameObject for the circle
        GameObject circleObj = new GameObject("GuardCircle");
        circleObj.transform.SetParent(transform);
        circleObj.transform.localPosition = Vector3.zero;

        // Add and configure LineRenderer
        circleRenderer = circleObj.AddComponent<LineRenderer>();
        circleRenderer.material = new Material(Shader.Find("Sprites/Default"));
        circleRenderer.startColor = new Color(0, 0.5f, 1f, 0.3f); // Semi-transparent blue
        circleRenderer.endColor = new Color(0, 0.5f, 1f, 0.3f);
        circleRenderer.startWidth = 0.15f;
        circleRenderer.endWidth = 0.15f;
        circleRenderer.useWorldSpace = true;
        circleRenderer.enabled = false; // Start hidden
    }

    void Update()
    {
        //isSelected && 
        // Only show if unit is selected AND guarding
        bool shouldShow = attackController != null && attackController.isGuarding;

        if (circleRenderer != null)
        {
            circleRenderer.enabled = shouldShow;

            if (shouldShow)
            {
                DrawCircle(attackController.guardPosition, attackController.guardRadius);
            }
        }
    }

    void DrawCircle(Vector3 center, float radius)
    {
        int segments = 48;
        circleRenderer.positionCount = segments + 1;

        float angle = 0f;
        for (int i = 0; i <= segments; i++)
        {
            float x = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
            float z = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
            circleRenderer.SetPosition(i, center + new Vector3(x, 0.1f, z));
            angle += 360f / segments;
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
    }

    public void CreateGuardAreaIndicator()
    {
        // Create preview indicator (different from unit's persistent circles)
        guardAreaIndicator = new GameObject("GuardAreaPreview");
        guardAreaIndicator.SetActive(false);

        guardCircleRenderer = guardAreaIndicator.AddComponent<LineRenderer>();
        guardCircleRenderer.material = new Material(Shader.Find("Sprites/Default"));

        // Make preview more visible/different color
        guardCircleRenderer.startColor = new Color(1f, 1f, 0f, 0.6f); // Yellow for preview
        guardCircleRenderer.endColor = new Color(1f, 1f, 0f, 0.6f);
        guardCircleRenderer.startWidth = 0.25f; // Slightly thicker for preview
        guardCircleRenderer.endWidth = 0.25f;
        guardCircleRenderer.useWorldSpace = true;

        // Create circle points
        int segments = 64;
        guardCircleRenderer.positionCount = segments + 1;

        float angle = 0f;
        for (int i = 0; i <= segments; i++)
        {
            float x = Mathf.Sin(angle * Mathf.Deg2Rad) * guardAreaRadius;
            float z = Mathf.Cos(angle * Mathf.Deg2Rad) * guardAreaRadius;
            guardCircleRenderer.SetPosition(i, new Vector3(x, 0.1f, z)); // Slightly above ground
            angle += 360f / segments;
        }
    }

    public void ShowGuardAreaPreview()
    {
        if (guardAreaIndicator != null)
        {
            Vector3 mousePos = GetMouseWorldPosition();
            guardAreaIndicator.transform.position = mousePos;
            guardAreaIndicator.SetActive(true);

            // Update circle to be at mouse position
            int segments = guardCircleRenderer.positionCount - 1;
            float angle = 0f;
            for (int i = 0; i <= segments; i++)
            {
                float x = Mathf.Sin(angle * Mathf.Deg2Rad) * guardAreaRadius;
                float z = Mathf.Cos(angle * Mathf.Deg2Rad) * guardAreaRadius;
                guardCircleRenderer.SetPosition(i, mousePos + new Vector3(x, 0.1f, z));
                angle += 360f / segments;
            }
        }
    }

    public void HideGuardAreaPreview()
    {
        if (guardAreaIndicator != null)
        {
            guardAreaIndicator.SetActive(false);
        }
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
using UnityEngine;

public class GuardAreaDisplay : MonoBehaviour
{
    private LineRenderer circleRenderer;
    private AttackController attackController;
    private bool isSelected = false;

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
}
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class UnitHoverManager : MonoBehaviour
{
    [Header("Hover Circle Settings")]
    public Color playerHoverColor = Color.green;
    public Color enemyHoverColor = Color.red;
    public float hoverCircleRadius = 0.6f;
    public float hoverCircleWidth = 0.12f;
    public int circleSegments = 32;

    [Header("Raycast Settings")]
    public LayerMask unitLayerMask;
    public float raycastDistance = 100f;

    // Current hover state
    private GameObject currentlyHoveredUnit = null;
    private LineRenderer currentHoverCircle = null;
    private Camera mainCamera;
    private Mouse mouse;

    // Singleton pattern (optional)
    public static UnitHoverManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        unitLayerMask = LayerMask.GetMask("Clickable");
        mainCamera = Camera.main;
        mouse = Mouse.current;
    }

    private void Update()
    {
        HandleUnitHover();
    }

    private void HandleUnitHover()
    {
        // Skip hover detection if over UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            ClearCurrentHover();
            return;
        }

        // Skip hover detection if ability manager is targeting
        //if (AbilityManager.Instance != null &&
        //    (AbilityManager.Instance.IsTargeting || AbilityManager.Instance.IsCasting))
        //{
        //    ClearCurrentHover();
        //    return;
        //}

        // Get mouse world position and raycast
        Vector3 mouseWorldPos = RayCastManager.Instance.GetMouseWorldPosition();
        GameObject hoveredUnit = GetUnitUnderMouse();

        // Handle hover state changes
        if (hoveredUnit != currentlyHoveredUnit)
        {
            ClearCurrentHover();

            if (hoveredUnit != null && ShouldShowHoverForUnit(hoveredUnit))
            {
                ShowHoverCircle(hoveredUnit);
                currentlyHoveredUnit = hoveredUnit;
            }
        }
    }

    private GameObject GetUnitUnderMouse()
    {
        if (mouse == null || mainCamera == null) return null;

        Ray ray = mainCamera.ScreenPointToRay(mouse.position.ReadValue());
        
        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, unitLayerMask))
        {
            GameObject hitObject = hit.collider.gameObject;

            // Check if it has a Unit component
            if (hitObject.GetComponent<Unit>() != null)
            {
                return hitObject;
            }
        }

        return null;
    }

    private bool ShouldShowHoverForUnit(GameObject unit)
    {
        if (unit == null) return false;

        // Only show hover for Player and Enemy tagged units
        return unit.CompareTag("Player") || unit.CompareTag("Enemy");
    }

    private void ShowHoverCircle(GameObject unit)
    {
        if (unit == null) return;

        // Determine color based on unit tag
        Color circleColor = unit.CompareTag("Player") ? playerHoverColor : enemyHoverColor;

        // Create hover circle
        GameObject circleObj = new GameObject("UnitHoverCircle");
        circleObj.transform.SetParent(unit.transform);
        circleObj.transform.localPosition = Vector3.zero;

        LineRenderer circleRenderer = circleObj.AddComponent<LineRenderer>();
        circleRenderer.material = new Material(Shader.Find("Sprites/Default"));


        circleRenderer.startColor = circleColor;
        circleRenderer.endColor = circleColor;
        circleRenderer.startWidth = hoverCircleWidth;
        circleRenderer.endWidth = hoverCircleWidth;
        circleRenderer.useWorldSpace = false;
        circleRenderer.positionCount = circleSegments + 1;

        // Create circle points
        float angle = 0f;
        for (int i = 0; i <= circleSegments; i++)
        {
            float x = Mathf.Sin(angle * Mathf.Deg2Rad) * hoverCircleRadius;
            float z = Mathf.Cos(angle * Mathf.Deg2Rad) * hoverCircleRadius;
            circleRenderer.SetPosition(i, new Vector3(x, 0.05f, z));
            angle += 360f / circleSegments;
        }

        currentHoverCircle = circleRenderer;
    }

    private void ClearCurrentHover()
    {
        if (currentHoverCircle != null)
        {
            Destroy(currentHoverCircle.gameObject);
            currentHoverCircle = null;
        }
        currentlyHoveredUnit = null;
    }

    // Optional: Method to temporarily disable hover (useful for other systems)
    public void SetHoverEnabled(bool enabled)
    {
        this.enabled = enabled;
        if (!enabled)
        {
            ClearCurrentHover();
        }
    }

    // Clean up on destroy
    private void OnDestroy()
    {
        ClearCurrentHover();
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
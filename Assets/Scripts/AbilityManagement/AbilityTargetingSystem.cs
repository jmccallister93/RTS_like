using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// Handles ability targeting, previews, and targeting input
/// </summary>
public class AbilityTargetingSystem : MonoBehaviour
{
    [Header("Targeting")]
    public LayerMask groundLayer = 1;
    public Material previewMaterial;
    public GameObject areaPreviewPrefab;
    public LineRenderer pathPreviewPrefab;

    // State
    private IAbility currentlyTargeting;
    private bool isTargeting = false;
    private GameObject previewObject;
    private Mouse mouse;
    private Keyboard keyboard;
    private Camera mainCamera;
    private AbilityManager abilityManager;

    public bool IsTargeting => isTargeting;

    public void Initialize(AbilityManager manager)
    {
        abilityManager = manager;
        mouse = Mouse.current;
        keyboard = Keyboard.current;
        mainCamera = Camera.main;
    }

    public void HandleTargeting()
    {
        if (!isTargeting || currentlyTargeting == null) return;

        // Ignore UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        UpdateTargetingPreview(mouseWorldPos);

        if (mouse.leftButton.wasPressedThisFrame)
        {
            CompleteTargeting(mouseWorldPos);
        }
        else if (mouse.rightButton.wasPressedThisFrame)
        {
            CancelTargeting();
        }
    }

    public bool StartTargeting(IAbility ability)
    {
        if (abilityManager.CurrentSelectedUnit == null) return false;

        isTargeting = true;
        currentlyTargeting = ability;
        CreatePreviewObject(ability);
        return true;
    }

    public void CancelTargeting()
    {
        if (isTargeting && currentlyTargeting != null && abilityManager.CurrentSelectedUnit != null)
        {
            currentlyTargeting.Cancel(abilityManager.CurrentSelectedUnit);
        }

        isTargeting = false;
        currentlyTargeting = null;
        DestroyPreviewObject();
    }

    private void CompleteTargeting(Vector3 targetPosition)
    {
        if (currentlyTargeting == null || abilityManager.CurrentSelectedUnit == null) return;

        GameObject targetObject = null;
        if (currentlyTargeting.TargetType == TargetType.Ally || currentlyTargeting.TargetType == TargetType.Enemy)
        {
            targetObject = GetTargetAtPosition(targetPosition);
            if (targetObject == null || !IsValidTarget(targetObject, currentlyTargeting.TargetType))
            {
                CancelTargeting();
                return;
            }
        }

        // Range check
        float distance = Vector3.Distance(abilityManager.CurrentSelectedUnit.transform.position, targetPosition);
        if (distance > currentlyTargeting.Range)
        {
            CancelTargeting();
            return;
        }

        var caster = abilityManager.CurrentSelectedUnit;
        bool paused = PauseManager.Instance != null && PauseManager.Instance.IsPaused;

        if (paused)
        {
            CommandQueue.Instance.QueueCommand(
                new AbilityCastCommand(caster, currentlyTargeting, targetPosition, targetObject));
        }
        else
        {
            abilityManager.GetComponent<AbilityExecutor>().ExecuteAbility(currentlyTargeting, targetPosition, targetObject);
        }

        // Clean up
        isTargeting = false;
        currentlyTargeting = null;
        DestroyPreviewObject();
    }

    private bool IsValidTarget(GameObject target, TargetType targetType)
    {
        if (target == null || abilityManager.CurrentSelectedUnit == null) return false;

        var targetUnit = target.GetComponent<Unit>();
        if (targetUnit == null || !targetUnit.IsAlive()) return false;

        switch (targetType)
        {
            case TargetType.Ally:
                return target.tag == abilityManager.CurrentSelectedUnit.tag;

            case TargetType.Enemy:
                var casterTag = abilityManager.CurrentSelectedUnit.tag;
                return target.tag != casterTag &&
                       ((casterTag == "Player" && target.tag == "Enemy") ||
                        (casterTag == "Enemy" && target.tag == "Player"));

            default:
                return false;
        }
    }

    private void CreatePreviewObject(IAbility ability)
    {
        var preview = ability.GetPreviewObject();
        if (preview != null)
        {
            previewObject = Instantiate(preview);
        }
        else
        {
            // Create default preview based on targeting type
            switch (ability.TargetType)
            {
                case TargetType.Area:
                    if (areaPreviewPrefab != null)
                    {
                        previewObject = Instantiate(areaPreviewPrefab);
                        var renderer = previewObject.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                            renderer.material.color = ability.PreviewColor;
                        }
                    }
                    break;

                case TargetType.Path:
                    if (pathPreviewPrefab != null)
                    {
                        previewObject = Instantiate(pathPreviewPrefab.gameObject);
                        var lineRenderer = previewObject.GetComponent<LineRenderer>();
                        if (lineRenderer != null)
                        {
                            lineRenderer.startColor = ability.PreviewColor;
                            lineRenderer.endColor = ability.PreviewColor;
                        }
                    }
                    break;
            }
        }
    }

    private void UpdateTargetingPreview(Vector3 mousePosition)
    {
        if (previewObject == null || abilityManager.CurrentSelectedUnit == null) return;

        switch (currentlyTargeting.TargetType)
        {
            case TargetType.Area:
                previewObject.transform.position = mousePosition;
                break;

            case TargetType.Path:
                var lineRenderer = previewObject.GetComponent<LineRenderer>();
                if (lineRenderer != null)
                {
                    lineRenderer.positionCount = 2;
                    lineRenderer.SetPosition(0, abilityManager.CurrentSelectedUnit.transform.position);
                    lineRenderer.SetPosition(1, mousePosition);
                }
                break;

            case TargetType.Point:
                previewObject.transform.position = mousePosition;
                break;
        }
    }

    private void DestroyPreviewObject()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = mainCamera.ScreenPointToRay(mouse.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
        {
            return hit.point;
        }

        // Fallback to ground plane
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }

        return Vector3.zero;
    }

    private GameObject GetTargetAtPosition(Vector3 position)
    {
        Ray ray = mainCamera.ScreenPointToRay(mouse.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return hit.collider.gameObject;
        }

        return null;
    }

    public void OnPause()
    {
        // Handle pause logic if needed
    }

    public void OnResume()
    {
        // Handle resume logic if needed
    }
}
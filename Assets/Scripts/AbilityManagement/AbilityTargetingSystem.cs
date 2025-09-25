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

        Vector3 mouseWorldPos = RayCastManager.Instance.GetMouseWorldPosition(); 
        UpdateTargetingPreview(mouseWorldPos);

        if (mouse.rightButton.wasPressedThisFrame)
        {
            CompleteTargeting(mouseWorldPos);
        }
        else if (mouse.leftButton.wasPressedThisFrame)
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
            currentlyTargeting.Cancel(abilityManager.CurrentSelectedUnit.gameObject);
        }

        isTargeting = false;
        currentlyTargeting = null;
        DestroyPreviewObject();
    }

    public void CompleteTargeting(Vector3 targetPosition)
    {
        if (currentlyTargeting == null || abilityManager.CurrentSelectedUnit == null) return;

        GameObject targetObject = null;
        Transform targetTransform = null;

        if (currentlyTargeting.TargetType == TargetType.Ally || currentlyTargeting.TargetType == TargetType.Enemy)
        {
            targetObject = GetTargetAtPosition(targetPosition);
            if (targetObject == null || !IsValidTarget(targetObject, currentlyTargeting.TargetType))
            {
                CancelTargeting();
                return;
            }
            targetTransform = targetObject.transform;
        }

        var caster = abilityManager.CurrentSelectedUnit;
        float distance;

        if (targetTransform != null) // chasing a unit
        {
            distance = Vector3.Distance(caster.transform.position, targetTransform.position);
        }
        else // area or point targeting
        {
            distance = Vector3.Distance(caster.transform.position, targetPosition);
        }

        if (distance > currentlyTargeting.Range)
        {
            var abilityMove = new AbilityMoveCommand(
                caster.gameObject,
                currentlyTargeting,
                targetTransform,
                targetPosition
            );

            // Execute now if not paused; otherwise queue.
            if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
                CommandQueue.Instance.QueueCommand(abilityMove);
            else
                abilityMove.Execute();

            isTargeting = false;
            currentlyTargeting = null;
            DestroyPreviewObject();
            return;
        }


        // In range cast immediately
        bool paused = PauseManager.Instance != null && PauseManager.Instance.IsPaused;
        if (paused)
        {
            CommandQueue.Instance.QueueCommand(
                new AbilityCastCommand(caster.gameObject, currentlyTargeting, targetPosition, targetObject));
        }
        else
        {
            abilityManager.GetComponent<AbilityExecutor>()
                .ExecuteAbility(currentlyTargeting, targetPosition, targetObject);
        }

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
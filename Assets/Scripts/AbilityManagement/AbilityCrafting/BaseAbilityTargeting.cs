using UnityEngine;
using System.Collections.Generic;

// =========================
// SINGLE TARGET ABILITIES
// =========================

/// <summary>
/// Base class for abilities that target a single enemy
/// </summary>
public abstract class SingleTargetEnemyAbility : AbilitySO
{
    protected virtual void Awake()
    {
        targetType = TargetType.Enemy;
    }

    public override void Execute(GameObject caster, Vector3 targetPosition, GameObject target = null)
    {
        if (!ValidateTarget(caster, target)) return;
        if (!ConsumeResources(caster)) return;

        ExecuteOnTarget(caster, target);
        lastCastTime = Time.time;
    }

    protected virtual bool ValidateTarget(GameObject caster, GameObject target)
    {
        if (target == null)
        {
            Debug.LogWarning($"{name}: No target provided!");
            return false;
        }

        var characterManager = target.GetComponent<CharacterManager>();
        if (characterManager == null || !characterManager.IsAlive)
        {
            Debug.LogWarning($"{name}: Invalid or dead target!");
            return false;
        }

        // Validate it's an enemy
        return AbilityTargetUtils.IsEnemy(caster, target);
    }

    protected abstract void ExecuteOnTarget(GameObject caster, GameObject target);
    protected abstract bool ConsumeResources(GameObject caster);
}

/// <summary>
/// Base class for abilities that target a single ally
/// </summary>
public abstract class SingleTargetAllyAbility : AbilitySO
{
    protected virtual void Awake()
    {
        targetType = TargetType.Ally;
    }

    public override void Execute(GameObject caster, Vector3 targetPosition, GameObject target = null)
    {
        if (!ValidateTarget(caster, target)) return;
        if (!ConsumeResources(caster)) return;

        ExecuteOnTarget(caster, target);
        lastCastTime = Time.time;
    }

    protected virtual bool ValidateTarget(GameObject caster, GameObject target)
    {
        if (target == null)
        {
            Debug.LogWarning($"{name}: No target provided!");
            return false;
        }

        var characterManager = target.GetComponent<CharacterManager>();
        if (characterManager == null || !characterManager.IsAlive)
        {
            Debug.LogWarning($"{name}: Invalid or dead target!");
            return false;
        }

        // Validate it's an ally
        return AbilityTargetUtils.IsAlly(caster, target);
    }

    protected abstract void ExecuteOnTarget(GameObject caster, GameObject target);
    protected abstract bool ConsumeResources(GameObject caster);
}

// =========================
// SELF TARGET ABILITIES
// =========================

/// <summary>
/// Base class for abilities that target the caster only
/// </summary>
public abstract class SelfTargetAbility : AbilitySO
{
    protected virtual void Awake()
    {
        targetType = TargetType.Self;
        range = 0f;
    }

    public override void Execute(GameObject caster, Vector3 targetPosition, GameObject target = null)
    {
        if (!ValidateCaster(caster)) return;
        if (!ConsumeResources(caster)) return;

        ExecuteOnSelf(caster);
        lastCastTime = Time.time;
    }

    protected virtual bool ValidateCaster(GameObject caster)
    {
        var characterManager = caster.GetComponent<CharacterManager>();
        return characterManager != null && characterManager.IsAlive;
    }

    protected abstract void ExecuteOnSelf(GameObject caster);
    protected abstract bool ConsumeResources(GameObject caster);
}

/// <summary>
/// Base class for area abilities centered on the caster
/// </summary>
public abstract class SelfAreaAbility : AbilitySO
{
    protected virtual void Awake()
    {
        targetType = TargetType.Area;
        range = 0f; // Cast at caster position
        useAreaRadiusForIndicator = true;
    }

    public override void Execute(GameObject caster, Vector3 targetPosition, GameObject target = null)
    {
        if (!ValidateCaster(caster)) return;
        if (!ConsumeResources(caster)) return;

        Vector3 center = caster.transform.position;
        List<GameObject> targets = GetTargetsInArea(caster, center);

        if (targets.Count > 0)
        {
            ExecuteAreaEffect(caster, center, targets);
        }

        lastCastTime = Time.time;
    }

    protected virtual bool ValidateCaster(GameObject caster)
    {
        var characterManager = caster.GetComponent<CharacterManager>();
        return characterManager != null && characterManager.IsAlive;
    }

    protected virtual List<GameObject> GetTargetsInArea(GameObject caster, Vector3 center)
    {
        List<GameObject> validTargets = new List<GameObject>();
        Collider[] hitColliders = Physics.OverlapSphere(center, areaRadius);

        foreach (Collider hitCollider in hitColliders)
        {
            GameObject target = hitCollider.gameObject;
            if (IsValidAreaTarget(caster, target))
            {
                validTargets.Add(target);
            }
        }

        return validTargets;
    }

    protected abstract void ExecuteAreaEffect(GameObject caster, Vector3 center, List<GameObject> targets);
    protected abstract bool ConsumeResources(GameObject caster);
    protected abstract bool IsValidAreaTarget(GameObject caster, GameObject target);
}

// =========================
// GROUND TARGET ABILITIES
// =========================

/// <summary>
/// Base class for area abilities that target a location on the ground
/// </summary>
public abstract class GroundAreaAbility : AbilitySO
{
    public enum AreaShape
    {
        Circle,
        Square,
        Cone
    }

    [Header("Ground Area Settings")]
    public AreaShape areaShape = AreaShape.Circle;
    [Tooltip("For cone abilities - the angle in degrees")]
    public float coneAngle = 60f;

    protected virtual void Awake()
    {
        targetType = TargetType.Area;
        useAreaRadiusForIndicator = true;
    }

    public override void Execute(GameObject caster, Vector3 targetPosition, GameObject target = null)
    {
        if (!ValidateCaster(caster)) return;
        if (!ConsumeResources(caster)) return;

        List<GameObject> targets = GetTargetsInArea(caster, targetPosition);

        if (targets.Count > 0)
        {
            ExecuteAreaEffect(caster, targetPosition, targets);
        }

        lastCastTime = Time.time;
    }

    protected virtual bool ValidateCaster(GameObject caster)
    {
        var casterUnit = caster.GetComponent<Unit>();
        return casterUnit != null && casterUnit.IsAlive();
    }

    protected virtual List<GameObject> GetTargetsInArea(GameObject caster, Vector3 center)
    {
        List<GameObject> validTargets = new List<GameObject>();

        switch (areaShape)
        {
            case AreaShape.Circle:
                validTargets = GetCircularTargets(caster, center);
                break;
            case AreaShape.Square:
                validTargets = GetSquareTargets(caster, center);
                break;
            case AreaShape.Cone:
                validTargets = GetConeTargets(caster, center);
                break;
        }

        return validTargets;
    }

    protected virtual List<GameObject> GetCircularTargets(GameObject caster, Vector3 center)
    {
        List<GameObject> targets = new List<GameObject>();
        Collider[] hitColliders = Physics.OverlapSphere(center, areaRadius);

        foreach (Collider hitCollider in hitColliders)
        {
            if (IsValidAreaTarget(caster, hitCollider.gameObject))
            {
                targets.Add(hitCollider.gameObject);
            }
        }

        return targets;
    }

    protected virtual List<GameObject> GetSquareTargets(GameObject caster, Vector3 center)
    {
        List<GameObject> targets = new List<GameObject>();

        // Use a box overlap for square area
        Vector3 boxSize = new Vector3(areaRadius * 2, 2f, areaRadius * 2);
        Collider[] hitColliders = Physics.OverlapBox(center, boxSize / 2f, Quaternion.identity);

        foreach (Collider hitCollider in hitColliders)
        {
            if (IsValidAreaTarget(caster, hitCollider.gameObject))
            {
                targets.Add(hitCollider.gameObject);
            }
        }

        return targets;
    }

    protected virtual List<GameObject> GetConeTargets(GameObject caster, Vector3 center)
    {
        List<GameObject> targets = new List<GameObject>();

        // Direction from caster to target center
        Vector3 direction = (center - caster.transform.position).normalized;

        Collider[] hitColliders = Physics.OverlapSphere(caster.transform.position, areaRadius);

        foreach (Collider hitCollider in hitColliders)
        {
            if (!IsValidAreaTarget(caster, hitCollider.gameObject)) continue;

            // Check if target is within cone angle
            Vector3 toTarget = (hitCollider.transform.position - caster.transform.position).normalized;
            float angle = Vector3.Angle(direction, toTarget);

            if (angle <= coneAngle / 2f)
            {
                targets.Add(hitCollider.gameObject);
            }
        }

        return targets;
    }

    protected abstract void ExecuteAreaEffect(GameObject caster, Vector3 center, List<GameObject> targets);
    protected abstract bool ConsumeResources(GameObject caster);
    protected abstract bool IsValidAreaTarget(GameObject caster, GameObject target);
}

/// <summary>
/// Base class for path/line abilities that target from caster to a point
/// </summary>
public abstract class PathAbility : AbilitySO
{
    [Header("Path Settings")]
    public float pathWidth = 1f;

    protected virtual void Awake()
    {
        targetType = TargetType.Path;
    }

    public override void Execute(GameObject caster, Vector3 targetPosition, GameObject target = null)
    {
        if (!ValidateCaster(caster)) return;
        if (!ConsumeResources(caster)) return;

        List<GameObject> targets = GetTargetsInPath(caster, targetPosition);

        if (targets.Count > 0)
        {
            ExecutePathEffect(caster, targetPosition, targets);
        }

        lastCastTime = Time.time;
    }

    protected virtual bool ValidateCaster(GameObject caster)
    {
        var casterUnit = caster.GetComponent<Unit>();
        return casterUnit != null && casterUnit.IsAlive();
    }

    protected virtual List<GameObject> GetTargetsInPath(GameObject caster, Vector3 endPosition)
    {
        List<GameObject> targets = new List<GameObject>();

        Vector3 startPos = caster.transform.position;
        Vector3 direction = (endPosition - startPos).normalized;
        float distance = Vector3.Distance(startPos, endPosition);

        // Use a capsule collider to detect targets in the path
        RaycastHit[] hits = Physics.CapsuleCastAll(
            startPos,
            endPosition,
            pathWidth / 2f,
            direction,
            0f
        );

        foreach (RaycastHit hit in hits)
        {
            if (IsValidPathTarget(caster, hit.collider.gameObject))
            {
                targets.Add(hit.collider.gameObject);
            }
        }

        return targets;
    }

    protected abstract void ExecutePathEffect(GameObject caster, Vector3 endPosition, List<GameObject> targets);
    protected abstract bool ConsumeResources(GameObject caster);
    protected abstract bool IsValidPathTarget(GameObject caster, GameObject target);
}

// =========================
// UTILITY METHODS
// =========================

public static class AbilityTargetUtils
{
    public static bool IsEnemy(GameObject caster, GameObject target)
    {
        var casterTag = caster.tag;
        var targetTag = target.tag;

        return targetTag != casterTag &&
               ((casterTag == "Player" && targetTag == "Enemy") ||
                (casterTag == "Enemy" && targetTag == "Player"));
    }

    public static bool IsAlly(GameObject caster, GameObject target)
    {
        return caster.tag == target.tag;
    }

    public static bool IsValidUnit(GameObject target)
    {
        var characterManager = target.GetComponent<CharacterManager>();
        return characterManager != null && characterManager.IsAlive;
    }
}
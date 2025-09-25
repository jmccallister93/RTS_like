using UnityEngine;

public abstract class AbilitySO : ScriptableObject, IAbility
{
    [Header("General")]
    public string abilityName = "New Ability";
    [TextArea] public string description;
    public Sprite icon;
    public float cooldown = 5f;
    public float castTime = 0.5f;
    public float range = 10f;
    public TargetType targetType = TargetType.None;
    public Color previewColor = Color.white;

    [Header("Area Effect Settings")]
    [Tooltip("Radius for area effects - only used when targetType is Area")]
    public float areaRadius = 3f;
    [Tooltip("Use area radius for indicator instead of range")]
    public bool useAreaRadiusForIndicator = false;

    // Runtime state (not serialized)
    [HideInInspector] public float lastCastTime = -Mathf.Infinity;

    // IAbility interface implementation
    public string Name => abilityName;
    public string Description => description;
    public Sprite Icon => icon;
    public float Cooldown => cooldown;
    public float CastTime => castTime;
    public float Range => range;
    public TargetType TargetType => targetType;
    public Color PreviewColor => previewColor;
    public AbilityState State { get; set; } = AbilityState.Ready;

    // New property for area radius
    public float AreaRadius => areaRadius;
    public bool UseAreaRadiusForIndicator => useAreaRadiusForIndicator;

    public virtual bool CanUse(GameObject caster) => true;
    public virtual void StartCast(GameObject caster, Vector3 targetPosition, GameObject target = null) { }
    public abstract void Execute(GameObject caster, Vector3 targetPosition, GameObject target = null);
    public virtual void Cancel(GameObject caster) { }
    public virtual GameObject GetPreviewObject() => null;

    // Helper method for area effects
    protected virtual void ExecuteAreaEffect(GameObject caster, Vector3 centerPosition, float radius, System.Action<GameObject> effectAction)
    {
        Collider[] hitColliders = Physics.OverlapSphere(centerPosition, radius);

        foreach (Collider hitCollider in hitColliders)
        {
            var unit = hitCollider.GetComponent<Unit>();
            if (unit != null && unit.IsAlive() && IsValidAreaTarget(caster, hitCollider.gameObject))
            {
                effectAction(hitCollider.gameObject);
            }
        }
    }

    // Override this in derived classes to define what constitutes a valid target for area effects
    protected virtual bool IsValidAreaTarget(GameObject caster, GameObject target)
    {
        if (target == caster) return false; // Don't hit self by default

        var casterUnit = caster.GetComponent<Unit>();
        var targetUnit = target.GetComponent<Unit>();

        if (casterUnit == null || targetUnit == null) return false;

        // Default: hit enemies
        return caster.tag != target.tag;
    }
}
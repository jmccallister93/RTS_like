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


    public virtual bool CanUse(GameObject caster) => true;
    // manager enforces cooldowns, mana, conditions

    public virtual void StartCast(GameObject caster, Vector3 targetPosition, GameObject target = null) { }
    public abstract void Execute(GameObject caster, Vector3 targetPosition, GameObject target = null);
    public virtual void Cancel(GameObject caster) { }
    public virtual GameObject GetPreviewObject() => null;
}
using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Magic Missile")]
public class MagicMissileSO : AbilitySO
{
    [Header("Magic Missile Settings")]
    public GameObject projectilePrefab;
    public float damage = 25f;

    public override void Execute(GameObject caster, Vector3 targetPosition, GameObject target = null)
    {
        if (target == null || projectilePrefab == null) return;

        GameObject missile = Instantiate(
            projectilePrefab,
            caster.transform.position + Vector3.up,
            Quaternion.identity
        );

        var proj = missile.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.Init(target, damage);
        }

        lastCastTime = Time.time;
    }
}

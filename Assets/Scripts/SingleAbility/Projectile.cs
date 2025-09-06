using UnityEngine;

public class Projectile : MonoBehaviour
{
    private GameObject target;
    private float damage;
    public float speed = 10f;

    public void Init(GameObject target, float damage)
    {
        this.target = target;
        this.damage = damage;
    }

    private void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // Move toward target
        transform.position = Vector3.MoveTowards(transform.position, target.transform.position, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.transform.position) < 0.5f)
        {
            // Apply damage
            var unit = target.GetComponent<Unit>();
            if (unit != null)
            {
                unit.TakeDamage(damage);
            }

            Destroy(gameObject);
        }
    }
}

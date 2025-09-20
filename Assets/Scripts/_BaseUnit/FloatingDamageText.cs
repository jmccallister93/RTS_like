using UnityEngine;
using TMPro;

public class FloatingDamageText : MonoBehaviour
{
    public float floatSpeed = 2f;
    public float fadeDuration = 1f;

    private TextMeshProUGUI textMesh;
    private Color playerTagColor;
    private Color enemyTagColor;
    [SerializeField] private Camera targetCamera; // optional override

    void Awake()
    {
        textMesh = GetComponentInChildren<TextMeshProUGUI>();
        if (textMesh == null) { Debug.LogError("No TextMeshProUGUI found in children!"); }
        else { playerTagColor = textMesh.color; }


        if (targetCamera == null) targetCamera = Camera.main; // cache it once
    }

    public void Initialize(float damageAmount)
    {
        textMesh.text = damageAmount.ToString("0");

        // Check parent for tag and set color accordingly
        if (transform.parent != null)
        {
            if (transform.parent.CompareTag("Player"))
            {
                textMesh.color = Color.red;
            }
            else if (transform.parent.CompareTag("Enemy"))
            {
                textMesh.color = Color.white;
            }
            else
            {
                textMesh.color = playerTagColor; // fallback to default
            }
        }
        else
        {
            textMesh.color = playerTagColor; // fallback to default
        }

        Destroy(gameObject, fadeDuration);
    }

    void LateUpdate()
    {
        if (targetCamera)
        {
            // Face the same way the camera faces (perfect billboard)
            transform.rotation = Quaternion.LookRotation(
                targetCamera.transform.rotation * Vector3.forward,
                targetCamera.transform.rotation * Vector3.up
            );
        }

        // Move up
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        // Fade out
        var c = textMesh.color;
        c.a -= Time.deltaTime / fadeDuration;
        textMesh.color = c;
    }
}

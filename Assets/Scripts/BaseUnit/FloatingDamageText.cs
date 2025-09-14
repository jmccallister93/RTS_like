using UnityEngine;
using TMPro;

public class FloatingDamageText : MonoBehaviour
{
    public float floatSpeed = 2f;
    public float fadeDuration = 1f;

    private TextMeshProUGUI textMesh;
    private Color originalColor;

    void Awake()
    {
        textMesh = GetComponentInChildren<TextMeshProUGUI>();
        if (textMesh != null)
        {
            //if(Unit)
            originalColor = textMesh.color;
        }
        else
        {
            Debug.LogError("No TextMeshProUGUI found in children!");
        }
    }

    public void Initialize(float damageAmount)
    {
        textMesh.text = damageAmount.ToString("0"); // show as whole number
        originalColor.a = 1f;
        textMesh.color = originalColor;

        Destroy(gameObject, fadeDuration); // auto-cleanup
    }

    void Update()
    {
        // Move upward
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        // Fade out
        Color c = textMesh.color;
        c.a -= Time.deltaTime / fadeDuration;
        textMesh.color = c;
    }
}

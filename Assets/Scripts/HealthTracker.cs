using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HealthTracker : MonoBehaviour
{
    [Header("UI References")]
    public Slider HealthBarSlider;
    public Image sliderFill;

    [Header("Health Colors")]
    public Color healthyColor = Color.green;      // 60%+ health
    public Color warnedColor = Color.yellow;      // 30-60% health  
    public Color criticalColor = Color.red;       // Below 30% health

    [Header("Animation Settings")]
    public float smoothTransitionTime = 0.3f;    // How fast health changes animate
    public bool useColorTransition = true;       // Smooth color transitions

    private Coroutine smoothHealthChangeCoroutine;
    private Coroutine colorTransitionCoroutine;

    private void Start()
    {
        // Auto-find references if not assigned
        if (HealthBarSlider == null)
            HealthBarSlider = GetComponentInChildren<Slider>();

        if (sliderFill == null && HealthBarSlider != null)
            sliderFill = HealthBarSlider.fillRect?.GetComponent<Image>();

        // Set initial state
        if (HealthBarSlider != null)
        {
            HealthBarSlider.value = 1f; // Start at full health
            UpdateColor(1f); // Set to healthy color
        }
    }

    /// <summary>
    /// Call this method to update the health bar value and color
    /// </summary>
    public void UpdateSliderValue(float currentHealth, float maxHealth)
    {
        if (HealthBarSlider == null) return;

        // Calculate the health percentage
        float healthPercentage = Mathf.Clamp01(currentHealth / maxHealth);

        // Stop any existing smooth change coroutine
        if (smoothHealthChangeCoroutine != null)
        {
            StopCoroutine(smoothHealthChangeCoroutine);
        }

        // Start smooth health change animation
        smoothHealthChangeCoroutine = StartCoroutine(SmoothHealthChange(HealthBarSlider.value, healthPercentage, smoothTransitionTime));

        // Update the color based on health percentage
        UpdateColor(healthPercentage);
    }

    /// <summary>
    /// Immediately set health without animation
    /// </summary>
    public void SetHealthImmediate(float currentHealth, float maxHealth)
    {
        if (HealthBarSlider == null) return;

        float healthPercentage = Mathf.Clamp01(currentHealth / maxHealth);
        HealthBarSlider.value = healthPercentage;
        UpdateColor(healthPercentage);
    }

    /// <summary>
    /// Smoothly animate health bar changes
    /// </summary>
    private IEnumerator SmoothHealthChange(float startValue, float targetValue, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            // Use a smooth curve for more natural animation
            float smoothedT = Mathf.SmoothStep(0f, 1f, t);

            HealthBarSlider.value = Mathf.Lerp(startValue, targetValue, smoothedT);
            elapsedTime += Time.deltaTime;

            yield return null;
        }

        HealthBarSlider.value = targetValue;
        smoothHealthChangeCoroutine = null;
    }

    /// <summary>
    /// Update the health bar color based on health percentage
    /// </summary>
    private void UpdateColor(float healthPercentage)
    {
        if (sliderFill == null) return;

        Color targetColor;

        // Determine target color based on health thresholds
        if (healthPercentage >= 0.6f)
        {
            targetColor = healthyColor;
        }
        else if (healthPercentage >= 0.3f)
        {
            targetColor = warnedColor;
        }
        else
        {
            targetColor = criticalColor;
        }

        // Apply color change
        if (useColorTransition)
        {
            StartColorTransition(targetColor);
        }
        else
        {
            sliderFill.color = targetColor;
        }
    }

    /// <summary>
    /// Smoothly transition between colors
    /// </summary>
    private void StartColorTransition(Color targetColor)
    {
        if (colorTransitionCoroutine != null)
        {
            StopCoroutine(colorTransitionCoroutine);
        }

        colorTransitionCoroutine = StartCoroutine(SmoothColorTransition(sliderFill.color, targetColor, 0.2f));
    }

    /// <summary>
    /// Coroutine for smooth color transitions
    /// </summary>
    private IEnumerator SmoothColorTransition(Color startColor, Color targetColor, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            sliderFill.color = Color.Lerp(startColor, targetColor, t);
            elapsedTime += Time.deltaTime;

            yield return null;
        }

        sliderFill.color = targetColor;
        colorTransitionCoroutine = null;
    }

    /// <summary>
    /// Get current health percentage (0-1)
    /// </summary>
    public float GetHealthPercentage()
    {
        return HealthBarSlider != null ? HealthBarSlider.value : 0f;
    }

    /// <summary>
    /// Manually set colors in inspector or code
    /// </summary>
    public void SetHealthColors(Color healthy, Color warned, Color critical)
    {
        healthyColor = healthy;
        warnedColor = warned;
        criticalColor = critical;
    }

    /// <summary>
    /// For debugging - test different health values
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void TestHealthValue(float percentage)
    {
        if (Application.isPlaying)
        {
            UpdateSliderValue(percentage * 100f, 100f);
        }
    }
}
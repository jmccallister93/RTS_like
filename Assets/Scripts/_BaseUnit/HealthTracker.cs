using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HealthTracker : MonoBehaviour
{
    [Header("UI References")]
    public Slider HealthBarSlider;
    public Image sliderFill;
    public Slider damageIndicatorSlider; // New: White damage indicator
    public Image damageIndicatorFill;    // New: Fill image for damage indicator

    [Header("Health Colors")]
    public Color healthyColor = Color.green;      // 60%+ health
    public Color warnedColor = Color.yellow;      // 30-60% health  
    public Color criticalColor = Color.red;       // Below 30% health

    [Header("Animation Settings")]
    public float smoothTransitionTime = 0.3f;    // How fast health changes animate
    public bool useColorTransition = true;       // Smooth color transitions

    [Header("Damage Indicator Settings")]
    public Color damageIndicatorColor = Color.white;
    public float damageIndicatorDuration = 1f;   // How long damage indicator shows
    public AnimationCurve damageIndicatorFadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Shake Settings")]
    public float shakeIntensity = 0.5f;
    public float shakeDuration = 0.2f;
    public int shakeVibrations = 8;

    [Header("Destruction Settings")]
    public bool destroyOnZeroHealth = true;
    public float destroyDelay = 0.5f;                // Delay before destruction to allow effects to play
    public GameObject targetToDestroy;               // If null, destroys the parent GameObject
    public UnityEngine.Events.UnityEvent onZeroHealth; // Event triggered when health reaches 0

    private Coroutine smoothHealthChangeCoroutine;
    private Coroutine colorTransitionCoroutine;
    private Coroutine damageIndicatorCoroutine;
    private Coroutine shakeCoroutine;
    private float previousHealthPercentage = 1f;
    private Vector3 originalPosition;

    private void Start()
    {
        if (HealthBarSlider != null)
        {
            HealthBarSlider.interactable = false;   // Prevent player interaction
            HealthBarSlider.transition = Selectable.Transition.None; // Remove highlight animations
        }

        // Auto-find references if not assigned
        if (HealthBarSlider == null)
            HealthBarSlider = GetComponentInChildren<Slider>();

        if (sliderFill == null && HealthBarSlider != null)
            sliderFill = HealthBarSlider.fillRect?.GetComponent<Image>();

        // Setup damage indicator
        SetupDamageIndicator();

        // Store original position for shake effect
        originalPosition = transform.localPosition;

        // Setup target to destroy if not assigned
        if (targetToDestroy == null)
        {
            // Default to destroying this GameObject and its children
            targetToDestroy = gameObject;
        }

        // Set initial state
        if (HealthBarSlider != null)
        {
            HealthBarSlider.value = 1f; // Start at full health
            UpdateColor(1f); // Set to healthy color
            previousHealthPercentage = 1f;
        }
    }

    private void SetupDamageIndicator()
    {
        // If damage indicator slider isn't assigned, try to create or find one
        if (damageIndicatorSlider == null && HealthBarSlider != null)
        {
            // Look for existing damage indicator
            damageIndicatorSlider = transform.Find("DamageIndicator")?.GetComponent<Slider>();

            if (damageIndicatorSlider == null)
            {
                Debug.LogWarning("Damage Indicator Slider not found. Please assign it manually in the inspector or create one as a child object named 'DamageIndicator'");
            }
        }

        if (damageIndicatorSlider != null)
        {
            damageIndicatorSlider.interactable = false;
            damageIndicatorSlider.transition = Selectable.Transition.None;

            if (damageIndicatorFill == null)
                damageIndicatorFill = damageIndicatorSlider.fillRect?.GetComponent<Image>();

            if (damageIndicatorFill != null)
            {
                damageIndicatorFill.color = damageIndicatorColor;
            }

            // Initially hide the damage indicator
            damageIndicatorSlider.gameObject.SetActive(false);
        }
    }

    public void UpdateSliderValue(float currentHealth, float maxHealth)
    {
        if (HealthBarSlider == null) return;

        // Calculate the health percentage
        float healthPercentage = Mathf.Clamp01(currentHealth / maxHealth);

        // Check if this is damage (health decreased)
        bool tookDamage = healthPercentage < previousHealthPercentage;

        if (tookDamage)
        {
            // Show damage indicator
            ShowDamageIndicator(previousHealthPercentage, healthPercentage);

            // Trigger shake effect
            TriggerShake();
        }

        // Stop any existing smooth change coroutine
        if (smoothHealthChangeCoroutine != null)
        {
            StopCoroutine(smoothHealthChangeCoroutine);
        }

        // Start smooth health change animation
        smoothHealthChangeCoroutine = StartCoroutine(SmoothHealthChange(HealthBarSlider.value, healthPercentage, smoothTransitionTime));

        // Update the color based on health percentage
        UpdateColor(healthPercentage);

        // Check for zero health
        if (healthPercentage <= 0f && destroyOnZeroHealth)
        {
            HandleZeroHealth();
        }

        // Update previous health for next comparison
        previousHealthPercentage = healthPercentage;
    }

    private void ShowDamageIndicator(float fromHealth, float toHealth)
    {
        if (damageIndicatorSlider == null) return;

        // Stop any existing damage indicator animation
        if (damageIndicatorCoroutine != null)
        {
            StopCoroutine(damageIndicatorCoroutine);
        }

        // Set damage indicator to show the previous health amount
        damageIndicatorSlider.value = fromHealth;
        damageIndicatorSlider.gameObject.SetActive(true);

        // Start fade out animation
        damageIndicatorCoroutine = StartCoroutine(AnimateDamageIndicator());
    }

    private IEnumerator AnimateDamageIndicator()
    {
        float elapsedTime = 0f;
        Color startColor = damageIndicatorColor;

        while (elapsedTime < damageIndicatorDuration)
        {
            float t = elapsedTime / damageIndicatorDuration;
            float curveValue = damageIndicatorFadeCurve.Evaluate(t);

            if (damageIndicatorFill != null)
            {
                Color fadeColor = startColor;
                fadeColor.a = curveValue;
                damageIndicatorFill.color = fadeColor;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Hide the damage indicator
        damageIndicatorSlider.gameObject.SetActive(false);

        // Reset color for next use
        if (damageIndicatorFill != null)
        {
            damageIndicatorFill.color = damageIndicatorColor;
        }

        damageIndicatorCoroutine = null;
    }

    private void TriggerShake()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }

        shakeCoroutine = StartCoroutine(ShakeAnimation());
    }

    private void HandleZeroHealth()
    {
        // Trigger the zero health event
        onZeroHealth?.Invoke();

        // Start destruction coroutine
        StartCoroutine(DestroyAfterDelay());
    }

    private IEnumerator DestroyAfterDelay()
    {
        // Wait for the specified delay to allow effects to play out
        yield return new WaitForSeconds(destroyDelay);

        // Destroy the target GameObject
        if (targetToDestroy != null)
        {
            Destroy(targetToDestroy);
        }
        else
        {
            Debug.LogWarning("HealthTracker: No target assigned for destruction!");
        }
    }

    private IEnumerator ShakeAnimation()
    {
        float elapsedTime = 0f;

        while (elapsedTime < shakeDuration)
        {
            float t = elapsedTime / shakeDuration;

            // Calculate shake offset
            float shakeAmount = shakeIntensity * (1f - t); // Decrease intensity over time
            float shakeX = Mathf.Sin(t * shakeVibrations * Mathf.PI * 2) * shakeAmount;
            float shakeY = Mathf.Cos(t * shakeVibrations * Mathf.PI * 2) * shakeAmount * 0.5f;

            // Apply shake
            transform.localPosition = originalPosition + new Vector3(shakeX, shakeY, 0f);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Return to original position
        transform.localPosition = originalPosition;
        shakeCoroutine = null;
    }

    public void SetHealthImmediate(float currentHealth, float maxHealth)
    {
        if (HealthBarSlider == null) return;

        float healthPercentage = Mathf.Clamp01(currentHealth / maxHealth);
        HealthBarSlider.value = healthPercentage;
        UpdateColor(healthPercentage);
        previousHealthPercentage = healthPercentage;

        // Check for zero health
        if (healthPercentage <= 0f && destroyOnZeroHealth)
        {
            HandleZeroHealth();
        }
    }

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

    private void StartColorTransition(Color targetColor)
    {
        if (!isActiveAndEnabled) return;

        if (colorTransitionCoroutine != null)
            StopCoroutine(colorTransitionCoroutine);

        colorTransitionCoroutine = StartCoroutine(SmoothColorTransition(sliderFill.color, targetColor, 0.2f));
    }

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

    public float GetHealthPercentage()
    {
        return HealthBarSlider != null ? HealthBarSlider.value : 0f;
    }

    public void SetHealthColors(Color healthy, Color warned, Color critical)
    {
        healthyColor = healthy;
        warnedColor = warned;
        criticalColor = critical;
    }

    // Public method to manually trigger shake (useful for testing)
    public void TriggerManualShake()
    {
        TriggerShake();
    }

    // Public method to manually trigger zero health destruction
    public void TriggerZeroHealthDestruction()
    {
        if (destroyOnZeroHealth)
        {
            HandleZeroHealth();
        }
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void TestHealthValue(float percentage)
    {
        if (Application.isPlaying)
        {
            UpdateSliderValue(percentage * 100f, 100f);
        }
    }
}
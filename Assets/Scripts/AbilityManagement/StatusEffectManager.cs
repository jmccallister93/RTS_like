using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages status effects (buffs/debuffs) on a unit
/// Add this component to any Unit that can have status effects applied
/// </summary>
public class StatusEffectManager : MonoBehaviour
{
    [Header("Debug")]
    public bool showDebugInfo = true;

    private List<ActiveStatusEffect> activeEffects = new List<ActiveStatusEffect>();
    private Unit unit;
    private CharacterManager characterManager;

    // Events for UI and other systems
    public System.Action<StatusEffect> OnStatusEffectAdded;
    public System.Action<StatusEffect> OnStatusEffectRemoved;
    public System.Action<StatusEffect> OnStatusEffectTick;

    private class ActiveStatusEffect
    {
        public StatusEffect effect;
        public float remainingDuration;
        public float nextTickTime;
        public GameObject source; // Who applied this effect

        public ActiveStatusEffect(StatusEffect statusEffect, GameObject appliedBy)
        {
            effect = statusEffect;
            remainingDuration = statusEffect.duration;
            nextTickTime = Time.time + statusEffect.tickInterval;
            source = appliedBy;
        }
    }

    void Awake()
    {
        unit = GetComponent<Unit>();
        characterManager = GetComponent<CharacterManager>();
    }

    void Update()
    {
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused) return;

        UpdateStatusEffects();
    }

    private void UpdateStatusEffects()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            var activeEffect = activeEffects[i];

            // Handle ticking effects (DoT, HoT, etc.)
            if (activeEffect.effect.tickInterval > 0 && Time.time >= activeEffect.nextTickTime)
            {
                ProcessTick(activeEffect);
                activeEffect.nextTickTime = Time.time + activeEffect.effect.tickInterval;
            }

            // Update duration
            activeEffect.remainingDuration -= Time.deltaTime;

            // Remove expired effects
            if (activeEffect.remainingDuration <= 0)
            {
                RemoveStatusEffect(i);
            }
        }
    }

    public void AddStatusEffect(StatusEffect effect, GameObject appliedBy = null)
    {
        // Check if we already have this effect
        var existingEffect = activeEffects.FirstOrDefault(ae => ae.effect.name == effect.name);

        if (existingEffect != null)
        {
            // Refresh duration if same effect
            existingEffect.remainingDuration = effect.duration;
            if (showDebugInfo)
                Debug.Log($"Refreshed {effect.name} on {gameObject.name}");
        }
        else
        {
            // Add new effect
            var activeEffect = new ActiveStatusEffect(effect, appliedBy);
            activeEffects.Add(activeEffect);
            ApplyStatusEffectModifiers(effect, true);

            OnStatusEffectAdded?.Invoke(effect);

            if (showDebugInfo)
                Debug.Log($"Applied {effect.name} to {gameObject.name} for {effect.duration} seconds");
        }
    }

    public void RemoveStatusEffect(string effectName)
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (activeEffects[i].effect.name == effectName)
            {
                RemoveStatusEffect(i);
                break;
            }
        }
    }

    private void RemoveStatusEffect(int index)
    {
        if (index < 0 || index >= activeEffects.Count) return;

        var activeEffect = activeEffects[index];
        ApplyStatusEffectModifiers(activeEffect.effect, false); // Remove modifiers

        OnStatusEffectRemoved?.Invoke(activeEffect.effect);

        if (showDebugInfo)
            Debug.Log($"Removed {activeEffect.effect.name} from {gameObject.name}");

        activeEffects.RemoveAt(index);
    }

    private void ProcessTick(ActiveStatusEffect activeEffect)
    {
        var effect = activeEffect.effect;

        if (effect.isDamageOverTime && effect.tickValue > 0)
        {
            characterManager?.TakeDamage(effect.tickValue);
            if (showDebugInfo)
                Debug.Log($"{gameObject.name} took {effect.tickValue} damage from {effect.name}");
        }
        else if (effect.isHealOverTime && effect.tickValue > 0)
        {
            characterManager?.Heal(effect.tickValue);
            if (showDebugInfo)
                Debug.Log($"{gameObject.name} healed {effect.tickValue} from {effect.name}");
        }

        OnStatusEffectTick?.Invoke(effect);
    }

    private void ApplyStatusEffectModifiers(StatusEffect effect, bool applying)
    {
        if (!effect.modifiesStats) return;

        // You would implement actual stat modifications here
        // For example, if you have a character stats system:

        var characterManager = GetComponent<CharacterManager>();
        if (characterManager != null)
        {
            float multiplier = applying ? effect.movementSpeedMultiplier : (1f / effect.movementSpeedMultiplier);
            // characterManager.ModifyMovementSpeed(multiplier);

            // Similar for other stats...
        }

        // Handle control effects
        if (applying)
        {
            if (effect.stunned) ApplyStun();
            if (effect.silenced) ApplySilence();
            if (effect.feared) ApplyFear();
        }
        else
        {
            if (effect.stunned) RemoveStun();
            if (effect.silenced) RemoveSilence();
            if (effect.feared) RemoveFear();
        }
    }

    // Control effect methods (implement based on your needs)
    private void ApplyStun()
    {
        // Prevent movement and actions
        if (showDebugInfo) Debug.Log($"{gameObject.name} is stunned!");
    }

    private void RemoveStun()
    {
        // Restore movement and actions
        if (showDebugInfo) Debug.Log($"{gameObject.name} is no longer stunned!");
    }

    private void ApplySilence()
    {
        // Prevent ability use
        if (showDebugInfo) Debug.Log($"{gameObject.name} is silenced!");
    }

    private void RemoveSilence()
    {
        // Restore ability use
        if (showDebugInfo) Debug.Log($"{gameObject.name} is no longer silenced!");
    }

    private void ApplyFear()
    {
        // Force movement away from source or prevent actions
        if (showDebugInfo) Debug.Log($"{gameObject.name} is feared!");
    }

    private void RemoveFear()
    {
        // Restore normal behavior
        if (showDebugInfo) Debug.Log($"{gameObject.name} is no longer feared!");
    }

    // Query methods for other systems
    public bool HasStatusEffect(string effectName)
    {
        return activeEffects.Any(ae => ae.effect.name == effectName);
    }

    public bool IsStunned()
    {
        return activeEffects.Any(ae => ae.effect.stunned);
    }

    public bool IsSilenced()
    {
        return activeEffects.Any(ae => ae.effect.silenced);
    }

    public bool IsFeared()
    {
        return activeEffects.Any(ae => ae.effect.feared);
    }

    public List<StatusEffect> GetActiveEffects()
    {
        return activeEffects.Select(ae => ae.effect).ToList();
    }

    public float GetStatusEffectTimeRemaining(string effectName)
    {
        var activeEffect = activeEffects.FirstOrDefault(ae => ae.effect.name == effectName);
        return activeEffect?.remainingDuration ?? 0f;
    }

    // Clear all effects (useful for death, zone transitions, etc.)
    public void ClearAllEffects()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            RemoveStatusEffect(i);
        }
    }
}
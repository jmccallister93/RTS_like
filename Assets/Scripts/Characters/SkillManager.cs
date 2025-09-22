using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    private int _skillPoints;
    private Dictionary<string, int> skills = new Dictionary<string, int>();

    public int SkillPoints => _skillPoints;

    public void GainSkillPoints(int amount)
    {
        _skillPoints += amount;
        OnSkillPointsChanged?.Invoke();
    }

    public bool CanAffordSkill(int cost) => _skillPoints >= cost;

    public void UpgradeSkill(string skillName, int cost)
    {
        if (!CanAffordSkill(cost)) return;

        if (!skills.ContainsKey(skillName))
            skills[skillName] = 0;

        skills[skillName]++;
        _skillPoints -= cost;

        OnSkillPointsChanged?.Invoke();
        OnSkillUpgraded?.Invoke(skillName, skills[skillName]);
    }

    public int GetSkillLevel(string skillName)
    {
        return skills.ContainsKey(skillName) ? skills[skillName] : 0;
    }

    // Events
    public System.Action OnSkillPointsChanged;
    public System.Action<string, int> OnSkillUpgraded;
}
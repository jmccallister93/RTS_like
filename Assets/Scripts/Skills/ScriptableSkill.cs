using NUnit.Framework;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using System.Text;

namespace _Scripts.SkillSystem
{
    [CreateAssetMenu(fileName = "New Skill", menuName = "Skill System/New Skill ")]
    public class ScriptableSkill : ScriptableObject
    {
        public List<UpgradeData> UpgradeData = new List<UpgradeData>();
        public bool IsAbility;
        public string SkillName;
        public bool OverwriteDescription;
        [TextArea(1,4)] public string SkillDescription;
        public Sprite SkillIcon;
        public List<ScriptableSkill> PrerequisiteSkills = new List<ScriptableSkill>();
        public int SkillTier;
        public int Cost;

        private void OnValidate()
        {
            SkillName = name;
            if (UpgradeData.Count == 0) return;
            if(OverwriteDescription) return;
           

            GenerateDescription();
        }
        private void GenerateDescription()
        {
            if (IsAbility)
            {
                switch (UpgradeData[0].StatType)
                {
                    case StatTypes.DoubleJump:
                        SkillDescription = $"{SkillName} unlocks the ability to double jump.";
                        break;
                    case StatTypes.Dash:
                        SkillDescription = $"{SkillName} unlocks the ability to dash.";
                        break;
                    case StatTypes.Teleport:
                        SkillDescription = $"{SkillName} unlocks the ability to teleport.";
                        break;
                    default:
                        Debug.LogWarning("Ability skill has non-ability upgrade data.");
                        break;
                }
            }
            else
            {
               StringBuilder sb = new StringBuilder();
                sb.Append($"{SkillName} increases ");
                for (int i = 0; i < UpgradeData.Count; i++)
                {
                    sb.Append(UpgradeData[i].StatType.ToString());
                    sb.Append(" by ");
                    sb.Append(UpgradeData[i].SkillIncreaseAmount.ToString());
                    sb.Append(UpgradeData[i].IsPercentage ? "%" : " point(s)");
                    if ( i == UpgradeData.Count - 2)
                    {
                        sb.Append(" and ");
                    }
                    else
                    {
                        sb.Append(i < UpgradeData.Count - 1 ? ", " : ".");
                    }
                       
                    
                }

                SkillDescription = sb.ToString();
            }
        }
        }

    [System.Serializable]
    public class UpgradeData
    {
        public StatTypes StatType;
        public int SkillIncreaseAmount;
        public bool IsPercentage;

    }

    public enum StatTypes
    {
        Strength,
        Dexterity,
        Intelligence,
        Wisdom,
        Constitution,
        Charisma,
        DoubleJump,
        Dash,
        Teleport
    }
}



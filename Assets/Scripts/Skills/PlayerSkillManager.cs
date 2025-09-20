using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System;
using System.Linq;

namespace _Scripts.SkillSystem
{
    public class PlayerSkillManager : MonoBehaviour
    {
        private int _strength, _dexterity, _intelligence, _wisdom, _constitution, _charisma; //stats
        private int _doubleJump, _dash, _teleport; //unlockable abilities

        private int _skillPoints; //points to spend on skills

        public int Strength => _strength;
        public int Dexterity => _dexterity;
        public int Intelligence => _intelligence;
        public int Wisdom => _wisdom;
        public int Constitution => _constitution;
        public int Charisma => _charisma;
        public int SkillPoints => _skillPoints;

        public bool isDoubleJumpUnlocked => _doubleJump > 0;
        public bool isDashUnlocked => _dash > 0;
        public bool isTeleportUnlocked => _teleport > 0;

        public UnityAction OnSkillPointsChanged;

        private List<ScriptableSkill> _unlockedSkills = new List<ScriptableSkill>();

        private void Awake()
        {
            _strength = 10;
            _dexterity = 10;
            _intelligence = 10;
            _wisdom = 10;
            _constitution = 10;
            _charisma = 10;
            _doubleJump = 0;
            _dash = 0;
            _teleport = 0;
            _skillPoints = 10;
        }

        public void GainSkillPoint()
        {
            _skillPoints++;
            OnSkillPointsChanged?.Invoke();
        }

        public bool CanAffordSkill(ScriptableSkill skill)
        {
            return _skillPoints >= skill.Cost;
        }

        public void UnlockSkill(ScriptableSkill skill)
        {
            if (!CanAffordSkill(skill)) return;

            ModifyStats(skill);
            _unlockedSkills.Add(skill);
            _skillPoints -= skill.Cost;
            OnSkillPointsChanged?.Invoke();

        }

        private void ModifyStats(ScriptableSkill skill)
        {
            foreach (UpgradeData data in skill.UpgradeData)
            {

                switch (data.StatType)
                {
                    case StatTypes.Strength:
                        ModifyStats(ref _strength, data);
                        break;
                    case StatTypes.Dexterity:
                        ModifyStats(ref _dexterity, data);
                        break;
                    case StatTypes.Intelligence:
                        ModifyStats(ref _intelligence, data);
                        break;
                    case StatTypes.Wisdom:
                        ModifyStats(ref _wisdom, data);
                        break;
                    case StatTypes.Constitution:
                        ModifyStats(ref _constitution, data);
                        break;
                    case StatTypes.Charisma:
                        ModifyStats(ref _charisma, data);
                        break;
                    case StatTypes.DoubleJump:
                        ModifyStats(ref _doubleJump, data);
                        break;
                    case StatTypes.Dash:
                        ModifyStats(ref _dash, data);
                        break;
                    case StatTypes.Teleport:
                        ModifyStats(ref _teleport, data);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();

                }

            }
        }

        public bool IsSkillUnlocked(ScriptableSkill skill)
        {
            return _unlockedSkills.Contains(skill);
        }

        public bool PreReqMet(ScriptableSkill skill)
        {
            return skill.PrerequisiteSkills.Count == 0 || skill.PrerequisiteSkills.All(_unlockedSkills.Contains);
        }

        private void ModifyStats(ref int stat, UpgradeData data)
        {

            if (data.IsPercentage)
            {
                stat += (int)(stat * (data.SkillIncreaseAmount / 100f));
            }
            else
            {
                stat += data.SkillIncreaseAmount;
            }
        }
    }
}


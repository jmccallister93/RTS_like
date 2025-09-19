using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace _Scripts.SkillSystem
{
//Used to make a library for a specific class 
    [CreateAssetMenu(fileName = "New Skill Library", menuName = "Skill System/New Skill Library")]
    public class ScriptableSkillLibrary : ScriptableObject
    {
        public List<ScriptableSkill> SkillLibrary;

        public List<ScriptableSkill> GetSkillsOfTier(int tier)
        {
            return SkillLibrary.Where(skill => skill.SkillTier == tier).ToList();
        }
    }
}


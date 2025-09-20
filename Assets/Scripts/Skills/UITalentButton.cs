using _Scripts.SkillSystem;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
//using UnityEngine.UI;
using UnityEngine.UIElements;

public class UITalentButton 
{
    private Button _button;
    private ScriptableSkill _skill;
    private bool _isUnlocked = false;

    public static UnityAction<ScriptableSkill> OnSkillButtonClicked;

    public UITalentButton(Button assignedButton, ScriptableSkill assignedSkill)
    {
        _button = assignedButton;
        _button.clicked += OnClick;
        _skill = assignedSkill;
        if(assignedSkill.SkillIcon) _button.style.backgroundImage = new StyleBackground(assignedSkill.SkillIcon);
    }

    private void OnClick()
    {
        OnSkillButtonClicked?.Invoke(_skill);

    }
}

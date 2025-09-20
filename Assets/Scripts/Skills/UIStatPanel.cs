using System;
using UnityEngine;
using UnityEngine.UIElements;

public class UIStatPanel : MonoBehaviour
{
    private Label _strengthLabel, _dexterityLabel, _intelligenceLabel, _wisdomLabel, _charismaLabel, _constitutionLabel;
    private Label _doubleJumpLabel, _dashLabel, _teleportLabel;
    private Label _skillPointsLabel;

    private UIManager _uiManager;

    private void Awake()
    {
        _uiManager = GetComponent<UIManager>();
        if (_uiManager == null)
        {
            Debug.LogError("UIManager component not found on the GameObject.");
          
        }
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _uiManager.PlayerSkillManager.OnSkillPointsChanged += PopulateLabelText;
        GatherLabelReferences();
        PopulateLabelText();
    }

    private void PopulateLabelText()
    {

        _strengthLabel.text = $"STR - {_uiManager.PlayerSkillManager.Strength}";
        _dexterityLabel.text = $"DEX - {_uiManager.PlayerSkillManager.Dexterity}";
        _intelligenceLabel.text = $"INT - {_uiManager.PlayerSkillManager.Intelligence}";
        _wisdomLabel.text = $"WIS - {_uiManager.PlayerSkillManager.Wisdom}";
        _charismaLabel.text = $"CHA - {_uiManager.PlayerSkillManager.Charisma}";
        _constitutionLabel.text = $"CON - {_uiManager.PlayerSkillManager.Constitution}";

        _skillPointsLabel.text = $"Skill Points: {_uiManager.PlayerSkillManager.SkillPoints}";

        _doubleJumpLabel.text = $"Double Jump: {(_uiManager.PlayerSkillManager.isDoubleJumpUnlocked ? "Unlocked" : "Locked")}";
        _dashLabel.text = $"Dash: {(_uiManager.PlayerSkillManager.isDashUnlocked ? "Unlocked" : "Locked")}";
        _teleportLabel.text = $"Teleport: {(_uiManager.PlayerSkillManager.isTeleportUnlocked ? "Unlocked" : "Locked")}";
    }

    private void GatherLabelReferences()
    {
        _strengthLabel = _uiManager.UIDocument.rootVisualElement.Q<Label>("StatLabel_Strength");
        _dexterityLabel = _uiManager.UIDocument.rootVisualElement.Q<Label>("StatLabel_Dexterity");
        _intelligenceLabel = _uiManager.UIDocument.rootVisualElement.Q<Label>("StatLabel_Intelligence");
        _wisdomLabel = _uiManager.UIDocument.rootVisualElement.Q<Label>("StatLabel_Wisdom");
        _charismaLabel = _uiManager.UIDocument.rootVisualElement.Q<Label>("StatLabel_Charisma");
        _constitutionLabel = _uiManager.UIDocument.rootVisualElement.Q<Label>("StatLabel_Constitution");
        _doubleJumpLabel = _uiManager.UIDocument.rootVisualElement.Q<Label>("AbilityLabel_DoubleJump");
        _dashLabel = _uiManager.UIDocument.rootVisualElement.Q<Label>("AbilityLabel_Dash");
        _teleportLabel = _uiManager.UIDocument.rootVisualElement.Q<Label>("AbilityLabel_Teleport");
        _skillPointsLabel = _uiManager.UIDocument.rootVisualElement.Q<Label>("SkillPoints_Label");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

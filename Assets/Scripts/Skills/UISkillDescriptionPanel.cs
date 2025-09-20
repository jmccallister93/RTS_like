using _Scripts.SkillSystem;
using System;
using UnityEngine;
using UnityEngine.UIElements;

public class UISkillDescriptionPanel : MonoBehaviour
{
    private UIManager _uiManager;
    private ScriptableSkill _assignedSkill;
    private VisualElement _skillImage;
        private Label _skillNameLabel, _skillDescriptionLabel, _skillCostLabel, _skillPreReqLabel;
    private Button _purchaseSkillButton;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
   private void Awake()
    {
        _uiManager = GetComponent<UIManager>();
    }

   private void OnEnable()
    {
        UITalentButton.OnSkillButtonClicked += PopulateLabelText;
    }

   private void OnDisable()
    {
        UITalentButton.OnSkillButtonClicked -= PopulateLabelText;
        if (_purchaseSkillButton != null) _purchaseSkillButton.clicked -= PurchaseSkill;


    }



    private void Start()
    {
        GatherLabelReferences();
        var skill = _uiManager.SkillLibrary.GetSkillsOfTier(1)[0];
        PopulateLabelText(skill);
    }

    private void GatherLabelReferences()
    {
        _skillImage = _uiManager.UIDocument.rootVisualElement.Q<VisualElement>(name:"Icon");
        _skillNameLabel = _uiManager.UIDocument.rootVisualElement.Q<Label>(name: "SkillNameLabel");
        _skillDescriptionLabel = _uiManager.UIDocument.rootVisualElement.Q<Label>(name: "SkillDescriptionLabel");
        _skillCostLabel = _uiManager.UIDocument.rootVisualElement.Q<Label>(name: "SkillCost");
        _skillPreReqLabel = _uiManager.UIDocument.rootVisualElement.Q<Label>(name: "PreReqLabel");
        _purchaseSkillButton = _uiManager.UIDocument.rootVisualElement.Q<Button>(name: "PurchaseSkillButton");
        _purchaseSkillButton.clicked += PurchaseSkill;
        if (_skillImage == null) Debug.LogError("Icon not found!");
    }

    private void PurchaseSkill()
    {
        if (_uiManager.PlayerSkillManager.CanAffordSkill(_assignedSkill))
        {
            _uiManager.PlayerSkillManager.UnlockSkill(_assignedSkill);
            PopulateLabelText(_assignedSkill);
        }
    }

    private void PopulateLabelText(ScriptableSkill skill)
    {
        if(skill == null) return;

        _assignedSkill = skill;
        
        if(_assignedSkill.SkillIcon) _skillImage.style.backgroundImage = new StyleBackground(_assignedSkill.SkillIcon);

        _skillNameLabel.text = _assignedSkill.SkillName;
        _skillDescriptionLabel.text = _assignedSkill.SkillDescription;
        _skillCostLabel.text = $"Cost: {_assignedSkill.Cost} Skill Points";
        _skillPreReqLabel.text = _assignedSkill.PrerequisiteSkills.Count > 0 ? "Requires: " : "No Prerequisites";
        foreach (var preReq in _assignedSkill.PrerequisiteSkills)
        {
            _skillPreReqLabel.text += preReq.SkillName + ", ";

        }

        if(_uiManager.PlayerSkillManager.IsSkillUnlocked(_assignedSkill))
        {
            _purchaseSkillButton.text = "Unlocked";
            _purchaseSkillButton.SetEnabled(false);
        }
        else if (!_uiManager.PlayerSkillManager.PreReqMet(_assignedSkill))
        {
            _purchaseSkillButton.text = "Prerequisites Not Met";
            _purchaseSkillButton.SetEnabled(false);
        }
        else if (_uiManager.PlayerSkillManager.CanAffordSkill(_assignedSkill))
        {
            _purchaseSkillButton.text = "Purchase";
            _purchaseSkillButton.SetEnabled(true);
        }
        else
        {
            _purchaseSkillButton.text = "Insufficient Points";
            _purchaseSkillButton.SetEnabled(false);
        }
    }
}

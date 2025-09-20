using _Scripts.SkillSystem;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    private PlayerSkillManager _playerSkillManager;
    public PlayerSkillManager PlayerSkillManager => _playerSkillManager;

    private UIDocument _uiDocument;
    public UIDocument UIDocument => _uiDocument;

    [SerializeField] private VisualTreeAsset uiTalentButton;
    [SerializeField] private ScriptableSkillLibrary skillLibrary;
    public ScriptableSkillLibrary SkillLibrary => skillLibrary;
    private List<UITalentButton> _talentButtons = new List<UITalentButton>();

    private VisualElement _skillTopRow, _skillMiddleRow, _skillBottomRow;
    

    private void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
        _playerSkillManager = Object.FindFirstObjectByType<PlayerSkillManager>();
    }

    private void Start()
    {
        CreateSkillButtons();
    }

    private void CreateSkillButtons()
    {
        var root = _uiDocument.rootVisualElement;

        // Fixed: Correct variable names and UXML element names
        _skillTopRow = root.Q<VisualElement>("Skill_Row_One");
        _skillMiddleRow = root.Q<VisualElement>("Skill_Row_Two");
        _skillBottomRow = root.Q<VisualElement>("Skill_Row_Three");

        // Add null checks for debugging
        if (_skillTopRow == null) Debug.LogError("Skill_Row_One not found!");
        if (_skillMiddleRow == null) Debug.LogError("Skill_Row_Two not found!");
        if (_skillBottomRow == null) Debug.LogError("Skill_Row_Three not found!");

        SpawnButtons(_skillTopRow, skillLibrary.GetSkillsOfTier(1));
        SpawnButtons(_skillMiddleRow, skillLibrary.GetSkillsOfTier(2));
        SpawnButtons(_skillBottomRow, skillLibrary.GetSkillsOfTier(3));
    }

    private void SpawnButtons(VisualElement parent, List<ScriptableSkill> skills)
    {
        if (parent == null)
        {
            Debug.LogError("Parent VisualElement is null!");
            return;
        }

        foreach (var skill in skills)
        {
            Button clonedButton = uiTalentButton.CloneTree().Q<Button>();
            _talentButtons.Add(new UITalentButton(clonedButton, skill));
            parent.Add(clonedButton);
        }
    }
}
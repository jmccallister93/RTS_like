using UnityEngine;

public class LevelManager : MonoBehaviour
{
    private int _currentLevel;

    private int _currentExperience;
    private int _experienceToNextLevel;
    private const float ExperienceGrowthFactor = 1.5f;
    public int CurrentLevel => _currentLevel;
    public int CurrentExperience => _currentExperience;
    public int ExperienceToNextLevel => _experienceToNextLevel;
    public float ExperienceProgress => (float)_currentExperience / _experienceToNextLevel;
    public delegate void LevelUpAction(int newLevel);
    public event LevelUpAction OnLevelUp;
    public delegate void ExperienceGainedAction(int currentExperience, int experienceToNextLevel);
    public event ExperienceGainedAction OnExperienceGained;

    private void Awake()
    {
        _currentLevel = 1;
        _currentExperience = 0;
        _experienceToNextLevel = CalculateExperienceForNextLevel(_currentLevel);
    }
    public void GainExperience(int amount)
    {
        _currentExperience += amount;
        OnExperienceGained?.Invoke(_currentExperience, _experienceToNextLevel);
        while (_currentExperience >= _experienceToNextLevel)
        {
            LevelUp();
        }
    }
    private void LevelUp()
    {
        _currentExperience -= _experienceToNextLevel;
        _currentLevel++;
        _experienceToNextLevel = CalculateExperienceForNextLevel(_currentLevel);
        OnLevelUp?.Invoke(_currentLevel);
        OnExperienceGained?.Invoke(_currentExperience, _experienceToNextLevel);
    }
   
    private int CalculateExperienceForNextLevel(int level)
    {
        return Mathf.FloorToInt(100 * Mathf.Pow(ExperienceGrowthFactor, level - 1));
    }


}

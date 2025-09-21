using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    private int _health, _speed, _abilityModifier;
    private int _meleeDamage, _rangedDamage, _abilityDamage;
    private int _meleeDefense, _rangedDefense, _abilityDefense;

    public int Health => _health;
    public int Speed => _speed;
    public int AbilityModifier => _abilityModifier;
    public int MeleeDamage => _meleeDamage;
    public int RangedDamage => _rangedDamage;
    public int AbilityDamage => _abilityDamage;
    public int MeleeDefense => _meleeDefense;
    public int RangedDefense => _rangedDefense;
    public int AbilityDefense => _abilityDefense;
    private void Awake()
    {
        _health = 10;
        _speed = 1;
        _abilityModifier = 1;
        _meleeDamage = 1;
        _rangedDamage = 1;
        _abilityDamage = 1;
        _meleeDefense = 1;
        _rangedDefense = 1;
        _abilityDefense = 1;
    }



}

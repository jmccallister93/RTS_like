using UnityEngine;

public class UnitAbilities : MonoBehaviour
{
    public AbilitySO[] abilities; // drag ScriptableObject assets into here

    public AbilitySO GetAbility(int index)
    {
        if (index < 0 || index >= abilities.Length) return null;
        return abilities[index];
    }
}

using UnityEngine;

public static class InputBlocker
{
    // Gets reset every frame (start of AbilityManager.Update)
    public static bool ClickConsumedThisFrame = false;
}


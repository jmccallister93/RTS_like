using UnityEngine;

public class UnitIndicatorManager : MonoBehaviour
{
    [Header("Command Displays")]
    public MoveToDisplay moveToDisplay;
    public PatrolDisplay patrolDisplay;
    public AttackToDisplay attackToDisplay;
    public GuardAreaDisplay guardAreaDisplay;
    public HoldDisplay holdDisplay;

    void Awake()
    {
        // Make sure required indicators exist (either assigned in inspector or added dynamically)
        if (moveToDisplay == null) moveToDisplay = gameObject.AddComponent<MoveToDisplay>();
        if (attackToDisplay == null) attackToDisplay = gameObject.AddComponent<AttackToDisplay>();
        if (patrolDisplay == null) patrolDisplay = gameObject.AddComponent<PatrolDisplay>();
        if (guardAreaDisplay == null) guardAreaDisplay = gameObject.AddComponent<GuardAreaDisplay>();
        if (holdDisplay == null) holdDisplay = gameObject.AddComponent<HoldDisplay>();
    }

    //public void ShowMoveTo(Vector3 destination) => moveToDisplay.Show(destination);
    //public void HideMoveTo() => moveToDisplay.Hide();
    //public void ShowPatrol(Vector3 pointA, Vector3 pointB) => patrolDisplay.Show(pointA, pointB);
    //public void HidePatrol() => patrolDisplay.Hide();
    //public void ShowAttackTo(Vector3 targetPosition) => attackToDisplay.Show(targetPosition);
    //public void HideAttackTo() => attackToDisplay.Hide();
    //public void ShowHold() => holdDisplay.Show();
    //public void HideHold() => holdDisplay.Hide();
    //public void ShowGuardArea(Vector3 center, float radius) => guardAreaDisplay.Show(center, radius);
    //public void HideGuardArea() => guardAreaDisplay.Hide();

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

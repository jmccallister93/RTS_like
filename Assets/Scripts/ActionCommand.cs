//using UnityEngine;

///// <summary>
///// Base class for all action commands (Move, Attack, Patrol, etc.)
///// </summary>
//public abstract class ActionCommand
//{
//    public abstract string DisplayName { get; }
//    public abstract string Description { get; }
//    public abstract Sprite Icon { get; }

//    /// <summary>
//    /// Check if this command can be executed on the given unit
//    /// </summary>
//    public abstract bool CanExecuteOn(GameObject unit);

//    /// <summary>
//    /// Execute the command on a unit at the specified world position
//    /// </summary>
//    public abstract void Execute(GameObject unit, Vector3 targetPosition);

//    /// <summary>
//    /// Execute the command on a unit targeting another unit
//    /// </summary>
//    public virtual void Execute(GameObject unit, GameObject target)
//    {
//        if (target != null)
//        {
//            Execute(unit, target.transform.position);
//        }
//    }

//    /// <summary>
//    /// Get the cursor to display when this command is active
//    /// </summary>
//    public virtual CursorType GetCursorType() { return CursorType.Default; }
//}

///// <summary>
///// Cursor types for visual feedback
///// </summary>
//public enum CursorType
//{
//    Default,
//    Move,
//    Attack,
//    Patrol,
//    Guard,
//    Invalid
//}

///// <summary>
///// Basic move command - units move to target location
///// </summary>
//public class MoveCommand : ActionCommand
//{
//    public override string DisplayName => "Move";
//    public override string Description => "Move to target location";
//    public override Sprite Icon => Resources.Load<Sprite>("Icons/MoveIcon");

//    public override bool CanExecuteOn(GameObject unit)
//    {
//        return unit != null &&
//               unit.GetComponent<UnitMovement>() != null &&
//               unit.GetComponent<UnityEngine.AI.NavMeshAgent>() != null;
//    }

//    public override void Execute(GameObject unit, Vector3 targetPosition)
//    {
//        UnitMovement movement = unit.GetComponent<UnitMovement>();
//        UnityEngine.AI.NavMeshAgent agent = unit.GetComponent<UnityEngine.AI.NavMeshAgent>();

//        if (movement != null && agent != null)
//        {
//            // Clear any existing command behavior
//            UnitCommandBehavior commandBehavior = unit.GetComponent<UnitCommandBehavior>();
//            if (commandBehavior != null)
//            {
//                commandBehavior.ClearCommand();
//            }

//            // Clear any attack target
//            AttackController attackController = unit.GetComponent<AttackController>();
//            if (attackController != null)
//            {
//                attackController.targetToAttack = null;
//            }

//            // Set movement
//            movement.isCommandedtoMove = true;
//            UnityEngine.AI.NavMeshHit navHit;
//            if (UnityEngine.AI.NavMesh.SamplePosition(targetPosition, out navHit, 2f, UnityEngine.AI.NavMesh.AllAreas))
//            {
//                agent.SetDestination(navHit.position);
//                Debug.Log($"{unit.name} moving to {navHit.position}");
//            }
//        }
//    }

//    public override CursorType GetCursorType() => CursorType.Move;
//}

///// <summary>
///// Attack move command - units move to location but attack enemies on the way
///// </summary>
//public class AttackMoveCommand : ActionCommand
//{
//    public override string DisplayName => "Attack Move";
//    public override string Description => "Move to location, attacking enemies encountered";
//    public override Sprite Icon => Resources.Load<Sprite>("Icons/AttackMoveIcon");

//    public override bool CanExecuteOn(GameObject unit)
//    {
//        return unit != null &&
//               unit.GetComponent<UnitMovement>() != null &&
//               unit.GetComponent<AttackController>() != null;
//    }

//    public override void Execute(GameObject unit, Vector3 targetPosition)
//    {
//        // Set the unit to attack-move mode
//        UnitCommandBehavior commandBehavior = unit.GetComponent<UnitCommandBehavior>();
//        if (commandBehavior == null)
//        {
//            commandBehavior = unit.AddComponent<UnitCommandBehavior>();
//        }

//        commandBehavior.SetAttackMoveTarget(targetPosition);
//        Debug.Log($"{unit.name} attack-moving to {targetPosition}");
//    }

//    public override void Execute(GameObject unit, GameObject target)
//    {
//        if (target != null)
//        {
//            AttackController attackController = unit.GetComponent<AttackController>();
//            if (attackController != null)
//            {
//                attackController.SetTarget(target.transform);
//                Debug.Log($"{unit.name} ordered to attack {target.name}");
//            }
//        }
//    }

//    public override CursorType GetCursorType() => CursorType.Attack;
//}

///// <summary>
///// Patrol command - units move between waypoints
///// </summary>
//public class PatrolCommand : ActionCommand
//{
//    public override string DisplayName => "Patrol";
//    public override string Description => "Patrol between current position and target";
//    public override Sprite Icon => Resources.Load<Sprite>("Icons/PatrolIcon");

//    public override bool CanExecuteOn(GameObject unit)
//    {
//        return unit != null && unit.GetComponent<UnitMovement>() != null;
//    }

//    public override void Execute(GameObject unit, Vector3 targetPosition)
//    {
//        UnitCommandBehavior commandBehavior = unit.GetComponent<UnitCommandBehavior>();
//        if (commandBehavior == null)
//        {
//            commandBehavior = unit.AddComponent<UnitCommandBehavior>();
//        }

//        Vector3 startPosition = unit.transform.position;
//        commandBehavior.SetPatrolPoints(startPosition, targetPosition);
//        Debug.Log($"{unit.name} patrolling between {startPosition} and {targetPosition}");
//    }

//    public override CursorType GetCursorType() => CursorType.Patrol;
//}

///// <summary>
///// Guard command - units stay in defensive position, only engaging nearby enemies
///// </summary>
//public class GuardCommand : ActionCommand
//{
//    public override string DisplayName => "Guard";
//    public override string Description => "Guard this position, engaging nearby enemies";
//    public override Sprite Icon => Resources.Load<Sprite>("Icons/GuardIcon");

//    public override bool CanExecuteOn(GameObject unit)
//    {
//        return unit != null && unit.GetComponent<AttackController>() != null;
//    }

//    public override void Execute(GameObject unit, Vector3 targetPosition)
//    {
//        UnitCommandBehavior commandBehavior = unit.GetComponent<UnitCommandBehavior>();
//        if (commandBehavior == null)
//        {
//            commandBehavior = unit.AddComponent<UnitCommandBehavior>();
//        }

//        commandBehavior.SetGuardPosition(targetPosition);

//        // Move to guard position first
//        UnitMovement movement = unit.GetComponent<UnitMovement>();
//        UnityEngine.AI.NavMeshAgent agent = unit.GetComponent<UnityEngine.AI.NavMeshAgent>();
//        if (movement != null && agent != null)
//        {
//            movement.isCommandedtoMove = true;
//            UnityEngine.AI.NavMeshHit navHit;
//            if (UnityEngine.AI.NavMesh.SamplePosition(targetPosition, out navHit, 2f, UnityEngine.AI.NavMesh.AllAreas))
//            {
//                agent.SetDestination(navHit.position);
//            }
//        }

//        Debug.Log($"{unit.name} guarding position {targetPosition}");
//    }

//    public override CursorType GetCursorType() => CursorType.Guard;
//}

///// <summary>
///// Hold position command - units stop all movement and only attack enemies in range
///// </summary>
//public class HoldPositionCommand : ActionCommand
//{
//    public override string DisplayName => "Hold Position";
//    public override string Description => "Stop moving, only attack enemies in range";
//    public override Sprite Icon => Resources.Load<Sprite>("Icons/HoldIcon");

//    public override bool CanExecuteOn(GameObject unit)
//    {
//        return unit != null;
//    }

//    public override void Execute(GameObject unit, Vector3 targetPosition)
//    {
//        // Stop movement
//        UnitMovement movement = unit.GetComponent<UnitMovement>();
//        UnityEngine.AI.NavMeshAgent agent = unit.GetComponent<UnityEngine.AI.NavMeshAgent>();

//        if (movement != null)
//        {
//            movement.isCommandedtoMove = false;
//        }

//        if (agent != null)
//        {
//            agent.ResetPath();
//            agent.isStopped = true;
//        }

//        // Set hold position behavior
//        UnitCommandBehavior commandBehavior = unit.GetComponent<UnitCommandBehavior>();
//        if (commandBehavior == null)
//        {
//            commandBehavior = unit.AddComponent<UnitCommandBehavior>();
//        }

//        commandBehavior.SetHoldPosition();
//        Debug.Log($"{unit.name} holding position");
//    }

//    public override CursorType GetCursorType() => CursorType.Default;
//}
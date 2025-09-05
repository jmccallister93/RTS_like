using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Command Queue System for handling commands during pause
public class CommandQueue : MonoBehaviour
{
    public static CommandQueue Instance;

    private Queue<ICommand> queuedCommands = new Queue<ICommand>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void QueueCommand(ICommand command)
    {
        queuedCommands.Enqueue(command);
        Debug.Log($"Queued command: {command.GetType().Name}");
    }

    public void ExecuteQueuedCommands()
    {
        Debug.Log($"Executing {queuedCommands.Count} queued commands");

        while (queuedCommands.Count > 0)
        {
            ICommand command = queuedCommands.Dequeue();
            command.Execute();
        }
    }

    public void ClearQueue()
    {
        queuedCommands.Clear();
    }

    public int GetQueuedCommandCount()
    {
        return queuedCommands.Count;
    }

    public bool HasQueuedCommandFor(GameObject unit)
    {
        return queuedCommands.Any(cmd => cmd.TargetUnit == unit);
    }
}

// Command interface and implementations
public interface ICommand
{
    GameObject TargetUnit { get; }
    void Execute();
}

public class MoveCommand : ICommand
{
    private GameObject unit;
    private Vector3 targetPosition;

    public MoveCommand(GameObject unit, Vector3 targetPosition)
    {
        this.unit = unit;
        this.targetPosition = targetPosition;
    }
    public GameObject TargetUnit => unit;
    public void Execute()
    {
        if (unit != null)
        {
            Unit unitComponent = unit.GetComponent<Unit>();
            if (unitComponent != null)
            {
                unitComponent.MoveTo(targetPosition);
            }
        }
    }
}

public class AttackMoveCommand : ICommand
{
    private GameObject unit;
    private Vector3 targetPosition;

    public AttackMoveCommand(GameObject unit, Vector3 targetPosition)
    {
        this.unit = unit;
        this.targetPosition = targetPosition;
    }

    public GameObject TargetUnit => unit;
    public void Execute()
    {
        if (unit != null)
        {
            Unit unitComponent = unit.GetComponent<Unit>();
            if (unitComponent != null)
            {
                unitComponent.AttackMoveTo(targetPosition);
            }
        }
    }
}

public class GuardCommand : ICommand
{
    private GameObject unit;
    private Vector3 guardPosition;
    private float radius;

    public GuardCommand(GameObject unit, Vector3 guardPosition, float radius = 5f)
    {
        this.unit = unit;
        this.guardPosition = guardPosition;
        this.radius = radius;
    }
    public GameObject TargetUnit => unit;
    public void Execute()
    {
        if (unit != null)
        {
            Unit unitComponent = unit.GetComponent<Unit>();
            if (unitComponent != null)
            {
                unitComponent.GuardPosition(guardPosition, radius);
            }
        }
    }
}

public class PatrolCommand : ICommand
{
    private GameObject unit;
    private Vector3 patrolPoint;

    public PatrolCommand(GameObject unit, Vector3 patrolPoint)
    {
        this.unit = unit;
        this.patrolPoint = patrolPoint;
    }
    public GameObject TargetUnit => unit;
    public void Execute()
    {
        if (unit != null)
        {
            Unit unitComponent = unit.GetComponent<Unit>();
            if (unitComponent != null)
            {
                unitComponent.PatrolTo(patrolPoint);
            }
        }
    }
}

public class AttackTargetCommand : ICommand
{
    private GameObject unit;
    private Transform target;

    public AttackTargetCommand(GameObject unit, Transform target)
    {
        this.unit = unit;
        this.target = target;
    }
    public GameObject TargetUnit => unit;
    public void Execute()
    {
        if (unit != null && target != null)
        {
            AttackController attackController = unit.GetComponent<AttackController>();
            if (attackController != null)
            {
                attackController.SetTarget(target);
            }
        }
    }
}

public class StopCommand : ICommand
{
    private GameObject unit;

    public StopCommand(GameObject unit)
    {
        this.unit = unit;
    }
    public GameObject TargetUnit => unit;
    public void Execute()
    {
        if (unit != null)
        {
            Unit unitComponent = unit.GetComponent<Unit>();
            if (unitComponent != null)
            {
                unitComponent.StopMovement();
            }
        }
    }
}
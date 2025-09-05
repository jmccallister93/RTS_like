using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance;

    [Header("Pause Settings")]
    public bool IsPaused { get; private set; }
    public Key pauseKey = Key.Space;

    [Header("UI")]
    public GameObject pauseUI; // Assign your pause menu UI here

    // Events for other systems to subscribe to
    public static event Action OnGamePaused;
    public static event Action OnGameResumed;

    // Lists to track pausable objects
    private List<IPausable> pausableComponents = new List<IPausable>();
    private List<NavMeshAgent> pausedAgents = new List<NavMeshAgent>();
    private List<Animator> pausedAnimators = new List<Animator>();

    // Store agent states for resume
    private Dictionary<NavMeshAgent, AgentPauseData> agentPauseData = new Dictionary<NavMeshAgent, AgentPauseData>();
    private Dictionary<Animator, float> animatorSpeeds = new Dictionary<Animator, float>();

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

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard[pauseKey].wasPressedThisFrame)
        {
            Debug.Log("Space pressed!");
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (IsPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        if (IsPaused) return;

        IsPaused = true;
        Time.timeScale = 0f;

        // Show pause UI
        if (pauseUI != null)
            pauseUI.SetActive(true);

        // Pause all registered pausable components
        foreach (var pausable in pausableComponents)
        {
            pausable.OnPause();
        }

        // Pause all NavMeshAgents
        PauseAllNavMeshAgents();

        // Pause all Animators
        PauseAllAnimators();

        // Notify other systems
        OnGamePaused?.Invoke();

        Debug.Log("Game Paused");
    }

    public void ResumeGame()
    {
        if (!IsPaused) return;

        IsPaused = false;
        Time.timeScale = 1f;

        // Hide pause UI
        if (pauseUI != null)
            pauseUI.SetActive(false);

        // Execute queued commands first
        if (CommandQueue.Instance != null)
        {
            CommandQueue.Instance.ExecuteQueuedCommands();
        }

        // Resume all registered pausable components
        foreach (var pausable in pausableComponents)
        {
            pausable.OnResume();
        }

        // Resume all NavMeshAgents
        ResumeAllNavMeshAgents();

        // Resume all Animators
        ResumeAllAnimators();

        // Notify other systems
        OnGameResumed?.Invoke();

        Debug.Log("Game Resumed");
    }

    private void PauseAllNavMeshAgents()
    {
        NavMeshAgent[] agents = FindObjectsOfType<NavMeshAgent>();

        foreach (NavMeshAgent agent in agents)
        {
            if (agent.enabled && !pausedAgents.Contains(agent))
            {
                // Store agent state
                agentPauseData[agent] = new AgentPauseData
                {
                    destination = agent.destination,
                    hasPath = agent.hasPath,
                    velocity = agent.velocity,
                    isStopped = agent.isStopped
                };

                // Stop the agent
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                pausedAgents.Add(agent);
            }
        }
    }

    private void ResumeAllNavMeshAgents()
    {
        foreach (NavMeshAgent agent in pausedAgents)
        {
            if (agent != null && agentPauseData.ContainsKey(agent))
            {
                AgentPauseData data = agentPauseData[agent];

                // Restore agent state
                agent.isStopped = data.isStopped;

                if (data.hasPath)
                {
                    agent.SetDestination(data.destination);
                }
            }
        }

        pausedAgents.Clear();
        agentPauseData.Clear();
    }

    private void PauseAllAnimators()
    {
        Animator[] animators = FindObjectsOfType<Animator>();

        foreach (Animator animator in animators)
        {
            if (animator.enabled && !animatorSpeeds.ContainsKey(animator))
            {
                animatorSpeeds[animator] = animator.speed;
                animator.speed = 0f;
                pausedAnimators.Add(animator);
            }
        }
    }

    private void ResumeAllAnimators()
    {
        foreach (Animator animator in pausedAnimators)
        {
            if (animator != null && animatorSpeeds.ContainsKey(animator))
            {
                animator.speed = animatorSpeeds[animator];
            }
        }

        pausedAnimators.Clear();
        animatorSpeeds.Clear();
    }

    // Registration methods for pausable components
    public void RegisterPausable(IPausable pausable)
    {
        if (!pausableComponents.Contains(pausable))
        {
            pausableComponents.Add(pausable);
        }
    }

    public void UnregisterPausable(IPausable pausable)
    {
        pausableComponents.Remove(pausable);
    }
}

// Interface for pausable components
public interface IPausable
{
    void OnPause();
    void OnResume();
}

// Data structure to store NavMeshAgent state
[System.Serializable]
public class AgentPauseData
{
    public Vector3 destination;
    public bool hasPath;
    public Vector3 velocity;
    public bool isStopped;
}
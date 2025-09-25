using System;
using System.Collections.Generic;
using System.Linq;
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
    public GameObject pauseUI;

    public static event Action OnGamePaused;
    public static event Action OnGameResumed;

    // Track what we disable so we can re-enable only those
    private readonly List<Behaviour> disabledBehaviours = new();
    private readonly List<NavMeshAgent> pausedAgents = new();
    private readonly Dictionary<NavMeshAgent, AgentPauseData> agentPauseData = new();
    private readonly List<Animator> pausedAnimators = new();
    private readonly Dictionary<Animator, float> animatorSpeeds = new();

    private void Awake()
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

    private void Update()
    {
        var k = Keyboard.current;
        if (k != null && k[pauseKey].wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (IsPaused) ResumeGame();
        else PauseGame();
    }

    public void PauseGame()
    {
        if (IsPaused) return;
        IsPaused = true;
        Time.timeScale = 0f;

        if (pauseUI) pauseUI.SetActive(true);

        DisablePausables();      // disables + calls OnPause
        PauseAllNavMeshAgents();
        PauseAllAnimators();

        OnGamePaused?.Invoke();
        Debug.Log("Game Paused");
    }

    public void ResumeGame()
    {
        if (!IsPaused) return;
        
        IsPaused = false;
        Time.timeScale = 1f;

        if (pauseUI) pauseUI.SetActive(false);

        EnablePausables();       // re-enables + calls OnResume
        ResumeAllNavMeshAgents();
        ResumeAllAnimators();

        CommandQueue.Instance.ExecuteQueuedCommands();  

        OnGameResumed?.Invoke();
        Debug.Log("Game resumed");
    }


    private void DisablePausables()
    {
        disabledBehaviours.Clear();

        // Find all active, enabled MonoBehaviours implementing IPausable
        var pausableBehaviours = FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include,       // replaces "true"
            FindObjectsSortMode.None           // faster if you don’t need sorting
        )
        .Where(b => b.isActiveAndEnabled)
        .Where(b => b is IPausable)
        .ToList();

        foreach (var mb in pausableBehaviours)
        {
            // Skip PauseManager itself
            if (mb == this) continue;

            // Let them snapshot any state
            (mb as IPausable)?.OnPause();

            // Keep components that should run during pause (UI, command UI, camera tweeners)
            if (mb is IRunWhenPaused) continue;

            // Disable gameplay behaviours so their Update/Triggers stop firing
            mb.enabled = false;
            disabledBehaviours.Add(mb);
        }
    }

    private void EnablePausables()
    {
        // Re-enable what we disabled
        foreach (var b in disabledBehaviours)
        {
            if (b != null) b.enabled = true;
        }
        disabledBehaviours.Clear();

        // Call OnResume on all IPausable (both re-enabled ones and those that ran during pause)
        var pausableBehaviours = FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        )
        .Where(b => b is IPausable)
        .ToList();

        foreach (var mb in pausableBehaviours)
        {
            (mb as IPausable)?.OnResume();
        }
    }

    private void PauseAllNavMeshAgents()
    {
        pausedAgents.Clear();
        agentPauseData.Clear();

        foreach (var agent in FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None))
        {
            if (agent != null && agent.enabled)
            {
                agentPauseData[agent] = new AgentPauseData
                {
                    destination = agent.hasPath ? agent.destination : agent.transform.position,
                    hasPath = agent.hasPath,
                    velocity = agent.velocity,
                    isStopped = agent.isStopped
                };

                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                pausedAgents.Add(agent);
            }
        }
    }

    private void ResumeAllNavMeshAgents()
    {
        foreach (var agent in pausedAgents)
        {
            if (agent == null) continue;
            if (!agentPauseData.TryGetValue(agent, out var data)) continue;

            agent.isStopped = data.isStopped;
            if (data.hasPath)
            {
                agent.SetDestination(data.destination);
            }
        }
        pausedAgents.Clear();
        agentPauseData.Clear();
    }

    private void PauseAllAnimators()
    {
        pausedAnimators.Clear();
        animatorSpeeds.Clear();

        foreach (var animator in FindObjectsByType<Animator>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        ))
        {
            if (animator != null && animator.enabled && !animatorSpeeds.ContainsKey(animator))
            {
                animatorSpeeds[animator] = animator.speed;
                animator.speed = 0f;
                pausedAnimators.Add(animator);
            }
        }
    }

    private void ResumeAllAnimators()
    {
        foreach (var animator in pausedAnimators)
        {
            if (animator == null) continue;
            if (animatorSpeeds.TryGetValue(animator, out var sp))
            {
                animator.speed = sp;
            }
        }
        pausedAnimators.Clear();
        animatorSpeeds.Clear();
    }
}

[Serializable]
public class AgentPauseData
{
    public Vector3 destination;
    public bool hasPath;
    public Vector3 velocity;
    public bool isStopped;
}

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject gameOverPanel;
    public Text resultText;
    public Button tryAgainButton;
    public Button nextMatchButton;

    [Header("Game Settings")]
    public string gameMatchSceneName = "GameMatch";
    public float checkInterval = 0.5f; // How often to check for units

    private bool gameEnded = false;
    private int lastPlayerCount = -1;
    private int lastEnemyCount = -1;

    void Start()
    {
        Debug.Log("GameManager Start() called");

        // Hide game over panel initially
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // Start checking for units
        InvokeRepeating(nameof(CheckGameState), 1f, checkInterval);
    }


    void CheckGameState()
    {
        if (gameEnded) return;

        // Find all units with tags (including dead ones)
        GameObject[] allPlayerUnits = GameObject.FindGameObjectsWithTag("Player");
        GameObject[] allEnemyUnits = GameObject.FindGameObjectsWithTag("Enemy");

        // Filter to only alive units
        var alivePlayerUnits = allPlayerUnits.Where(unit => {
            Unit unitScript = unit.GetComponent<Unit>();
            return unitScript != null && unitScript.IsAlive();
        }).ToArray();

        var aliveEnemyUnits = allEnemyUnits.Where(unit => {
            Unit unitScript = unit.GetComponent<Unit>();
            return unitScript != null && unitScript.IsAlive();
        }).ToArray();

        int alivePlayerCount = alivePlayerUnits.Length;
        int aliveEnemyCount = aliveEnemyUnits.Length;

        // Log unit counts (only when they change to avoid spam)
        if (alivePlayerCount != lastPlayerCount || aliveEnemyCount != lastEnemyCount)
        {

            // Log when units die
            if (lastPlayerCount != -1 && alivePlayerCount < lastPlayerCount)
            {
                int playerDeaths = lastPlayerCount - alivePlayerCount;
            }

            if (lastEnemyCount != -1 && aliveEnemyCount < lastEnemyCount)
            {
                int enemyDeaths = lastEnemyCount - aliveEnemyCount;
            }

            lastPlayerCount = alivePlayerCount;
            lastEnemyCount = aliveEnemyCount;
        }

        // Check win condition (all enemies dead)
        if (aliveEnemyCount == 0 && allEnemyUnits.Length > 0) // Make sure there were enemies to begin with
        {
            EndGame(true); // Player wins
        }
        // Check lose condition (all players dead)  
        else if (alivePlayerCount == 0 && allPlayerUnits.Length > 0) // Make sure there were players to begin with
        {
            EndGame(false); // Player loses
        }
    }

    void EndGame(bool playerWon)
    {
        gameEnded = true;
        CancelInvoke(nameof(CheckGameState));

        // Show appropriate UI
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (resultText != null)
        {
            resultText.text = playerWon ? "Victory!" : "Defeat!";
            resultText.color = playerWon ? Color.green : Color.red;
        }

        // Setup button events HERE when UI is active
        if (playerWon)
        {
            if (tryAgainButton != null) tryAgainButton.gameObject.SetActive(false);
            if (nextMatchButton != null)
            {
                nextMatchButton.gameObject.SetActive(true);
                // Clear any existing listeners and add new one
                nextMatchButton.onClick.RemoveAllListeners();
                nextMatchButton.onClick.AddListener(NextMatch);
                Debug.Log("NextMatch listener added");
            }
        }
        else
        {
            if (tryAgainButton != null)
            {
                tryAgainButton.gameObject.SetActive(true);
                // Clear any existing listeners and add new one
                tryAgainButton.onClick.RemoveAllListeners();
                tryAgainButton.onClick.AddListener(RestartMatch);
                Debug.Log("RestartMatch listener added");
            }
            if (nextMatchButton != null) nextMatchButton.gameObject.SetActive(false);
        }
    }

    public void RestartMatch()
    {
        Debug.Log("Restarting match...");
        ReloadScene();
    }

    public void NextMatch()
    {
        Debug.Log("Loading next match...");
        ReloadScene();
    }

    private void ReloadScene()
    {
        Debug.Log($"Attempting to reload scene: {gameMatchSceneName}");

        // Reset time scale in case it was paused
        Time.timeScale = 1f;

        // Clear any lingering state
        gameEnded = false;

        // Try loading by scene name first
        if (!string.IsNullOrEmpty(gameMatchSceneName))
        {
            try
            {
                SceneManager.LoadScene(gameMatchSceneName);
                return;
            }
            catch (System.ArgumentException)
            {
                Debug.LogWarning($"Scene '{gameMatchSceneName}' not found in build settings. Trying to reload current scene.");
            }
        }

        // Fallback: reload the current scene
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);

        Debug.Log($"Scene reloaded: {currentScene.name}");
    }

    // Manual method to check current status (useful for debugging)
    [ContextMenu("Check Unit Status")]
    public void DebugUnitStatus()
    {
        GameObject[] allPlayerUnits = GameObject.FindGameObjectsWithTag("Player");
        GameObject[] allEnemyUnits = GameObject.FindGameObjectsWithTag("Enemy");

        int alivePlayerCount = 0;
        int aliveEnemyCount = 0;

        foreach (var unit in allPlayerUnits)
        {
            Unit unitScript = unit.GetComponent<Unit>();
            bool isAlive = unitScript != null && unitScript.IsAlive();
            if (isAlive) alivePlayerCount++;
            Debug.Log($"Player {unit.name}: {(isAlive ? "ALIVE" : "DEAD")}");
        }

        foreach (var unit in allEnemyUnits)
        {
            Unit unitScript = unit.GetComponent<Unit>();
            bool isAlive = unitScript != null && unitScript.IsAlive();
            if (isAlive) aliveEnemyCount++;
            Debug.Log($"Enemy {unit.name}: {(isAlive ? "ALIVE" : "DEAD")}");
        }
    }

    
}
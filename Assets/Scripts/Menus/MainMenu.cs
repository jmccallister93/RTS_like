using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Called when Play button is clicked
    public void PlayGame()
    {
        // Replace "GameMatch" with the exact name of your gameplay scene
        SceneManager.LoadScene("GameMatch");
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit(); // Won't quit in editor, but works in build
    }
}

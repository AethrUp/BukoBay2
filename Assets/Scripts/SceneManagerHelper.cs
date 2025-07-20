using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerHelper : MonoBehaviour
{
    // Function to load the gear setup scene
    public void OpenGearSetup()
    {
        // Debug.Log("Opening gear setup scene...");
        SceneManager.LoadScene("InventoryUI");
    }
    
    // Function to return to main scene (you can call this from the gear setup scene)
    public void ReturnToMain()
    {
        // Debug.Log("Returning to main scene...");
        // Replace "MainScene" with your actual main scene name
        SceneManager.LoadScene("MainScene");
    }
    
    // Generic function to load any scene by name
    public void LoadScene(string sceneName)
    {
        // Debug.Log($"Loading scene: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }
}
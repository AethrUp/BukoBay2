using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemCleaner : MonoBehaviour
{
    void Start()
    {
        CleanupEventSystems();
        CleanupAudioListeners();
    }
    
    void CleanupEventSystems()
    {
        EventSystem[] eventSystems = FindObjectsOfType<EventSystem>();
        Debug.Log($"Found {eventSystems.Length} EventSystems in scene");
        
        if (eventSystems.Length > 1)
        {
            // Keep the first one, destroy the rest
            for (int i = 1; i < eventSystems.Length; i++)
            {
                Debug.LogWarning($"Destroying duplicate EventSystem: {eventSystems[i].name}");
                Destroy(eventSystems[i].gameObject);
            }
        }
    }
    
    void CleanupAudioListeners()
    {
        AudioListener[] audioListeners = FindObjectsOfType<AudioListener>();
        Debug.Log($"Found {audioListeners.Length} AudioListeners in scene");
        
        AudioListener mainListener = null;
        
        // Find the main camera's audio listener or the first enabled one
        foreach (var listener in audioListeners)
        {
            if (listener.enabled && (listener.name.Contains("Main") || mainListener == null))
            {
                mainListener = listener;
                break;
            }
        }
        
        // Disable all others
        foreach (var listener in audioListeners)
        {
            if (listener != mainListener && listener.enabled)
            {
                Debug.LogWarning($"Disabling AudioListener on: {listener.name}");
                listener.enabled = false;
            }
        }
    }
}
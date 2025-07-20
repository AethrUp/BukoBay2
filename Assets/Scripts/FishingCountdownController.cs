using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class FishingCountdownController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject countdownPanel;
    public TextMeshProUGUI countdownText;
    public Slider countdownSlider;
    
    [Header("Timing Settings")]
    public float readyDuration = 2f;
    public float setDuration = 1.5f;
    public float goDuration = 1f;
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip readySound;
    public AudioClip setSound;
    public AudioClip goSound;
    public AudioClip castingSound;
    
    [Header("Game References")]
    public FishingManager fishingManager;
    public FishingUI fishingUI;
    
    public System.Action OnCountdownComplete;
    
    private bool isCountingDown = false;
    
    void Start()
    {
        if (countdownPanel != null)
            countdownPanel.SetActive(false);
    }
    
    public void StartCountdown()
    {
        if (isCountingDown)
        {
            // Debug.Log("Countdown already in progress!");
            return;
        }
        
        // Debug.Log("Starting fishing countdown...");
        StartCoroutine(CountdownSequence());
    }
    
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    IEnumerator CountdownSequence()
    {
        isCountingDown = true;
        
        countdownPanel.SetActive(true);
        
        // Calculate total duration
        float totalDuration = readyDuration + setDuration + goDuration;
        float elapsedTime = 0f;
        
        // Set initial slider to full
        if (countdownSlider != null)
            countdownSlider.value = 1f;
        
        // Phase 1: "Ready?"
        if (countdownText != null)
            countdownText.text = "Ready?";
        // Debug.Log("Countdown phase: Ready?");
        
        // Play ready sound
        PlaySound(readySound);
        
        while (elapsedTime < readyDuration)
        {
            elapsedTime += Time.deltaTime;
            
            // Update slider based on total progress
            if (countdownSlider != null)
            {
                countdownSlider.value = 1f - (elapsedTime / totalDuration);
            }
            
            yield return null;
        }
        
        // Phase 2: "Set!"
        if (countdownText != null)
            countdownText.text = "Set!";
        // Debug.Log("Countdown phase: Set!");
        
        // Play set sound
        PlaySound(setSound);
        
        while (elapsedTime < readyDuration + setDuration)
        {
            elapsedTime += Time.deltaTime;
            
            // Update slider based on total progress
            if (countdownSlider != null)
            {
                countdownSlider.value = 1f - (elapsedTime / totalDuration);
            }
            
            yield return null;
        }
        
        // Phase 3: "Go!"
        if (countdownText != null)
            countdownText.text = "Go!";
        // Debug.Log("Countdown phase: Go!");
        
        // Play go sound
        PlaySound(goSound);
        
        while (elapsedTime < totalDuration)
        {
            elapsedTime += Time.deltaTime;
            
            // Update slider based on total progress
            if (countdownSlider != null)
            {
                countdownSlider.value = 1f - (elapsedTime / totalDuration);
            }
            
            yield return null;
        }
        
        // Make sure slider is at 0 at the end
        if (countdownSlider != null)
            countdownSlider.value = 0f;
        
        // Play casting sound
        PlaySound(castingSound);
        
        countdownPanel.SetActive(false);
        
        if (OnCountdownComplete != null)
            OnCountdownComplete();
        
        if (GetComponent<FishingUI>() != null)
        {
            GetComponent<FishingUI>().StartFishingAfterCountdown();
        }
        
        isCountingDown = false;
        // Debug.Log("Countdown complete!");
}
}
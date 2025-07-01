using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TugOfWarStaminaBar : MonoBehaviour
{
    [Header("Bar References")]
    public Image playerBar;        // Blue bar that grows right
    public Image fishBar;          // Yellow bar that grows left
    public RectTransform barContainer; // Parent container for the bars
    
    [Header("Arrow References")]
    public Transform leftArrowContainer;  // Container for green arrows (player advantage)
    public Transform rightArrowContainer; // Container for red arrows (fish advantage)
    public GameObject arrowPrefab;        // Arrow sprite prefab
    
    [Header("Settings")]
    public Color playerBarColor = Color.blue;
    public Color fishBarColor = Color.yellow;
    public Color playerArrowColor = Color.green;
    public Color fishArrowColor = Color.red;
    public float blinkSpeed = 2f;         // How fast arrows blink when advantage > 5
    public int maxArrows = 10;            // Maximum arrows to show
    
    [Header("Current Values")]
    public int playerStamina = 100;
    public int fishStamina = 100;
    public int powerDifference = 0;       // Positive = player advantage, negative = fish advantage
    
    // Private variables
    private GameObject[] leftArrows;
    private GameObject[] rightArrows;
    private bool isBlinking = false;
    private Coroutine blinkCoroutine;
    
    void Start()
    {
        SetupBars();
        SetupArrows();
        UpdateDisplay();
    }
    
    void SetupBars()
    {
        // Setup player bar (blue, grows LEFT from center)
        if (playerBar != null)
        {
            playerBar.color = playerBarColor;
            playerBar.type = Image.Type.Filled;
            playerBar.fillMethod = Image.FillMethod.Horizontal;
            playerBar.fillOrigin = 1; // Fill from right (center) to left
            
            // Anchor to left half of container
            RectTransform playerRect = playerBar.rectTransform;
            playerRect.anchorMin = new Vector2(0f, 0);   // Start from left edge
            playerRect.anchorMax = new Vector2(0.5f, 1); // Extend to center
            playerRect.offsetMin = Vector2.zero;
            playerRect.offsetMax = Vector2.zero;
        }
        
        // Setup fish bar (yellow, grows RIGHT from center)  
        if (fishBar != null)
        {
            fishBar.color = fishBarColor;
            fishBar.type = Image.Type.Filled;
            fishBar.fillMethod = Image.FillMethod.Horizontal;
            fishBar.fillOrigin = 0; // Fill from left (center) to right
            
            // Anchor to right half of container
            RectTransform fishRect = fishBar.rectTransform;
            fishRect.anchorMin = new Vector2(0.5f, 0); // Start from center
            fishRect.anchorMax = new Vector2(1f, 1);   // Extend to right edge
            fishRect.offsetMin = Vector2.zero;
            fishRect.offsetMax = Vector2.zero;
        }
    }
    
    void SetupArrows()
    {
        // Create arrow pools
        leftArrows = new GameObject[maxArrows];
        rightArrows = new GameObject[maxArrows];
        
        // Setup left arrows (player advantage)
        if (leftArrowContainer != null && arrowPrefab != null)
        {
            for (int i = 0; i < maxArrows; i++)
            {
                GameObject arrow = Instantiate(arrowPrefab, leftArrowContainer);
                Image arrowImage = arrow.GetComponent<Image>();
                if (arrowImage != null)
                {
                    arrowImage.color = playerArrowColor;
                }
                arrow.SetActive(false);
                leftArrows[i] = arrow;
            }
        }
        
        // Setup right arrows (fish advantage)
        if (rightArrowContainer != null && arrowPrefab != null)
        {
            for (int i = 0; i < maxArrows; i++)
            {
                GameObject arrow = Instantiate(arrowPrefab, rightArrowContainer);
                Image arrowImage = arrow.GetComponent<Image>();
                if (arrowImage != null)
                {
                    arrowImage.color = fishArrowColor;
                    // Flip the arrow to point right
                    arrow.transform.localScale = new Vector3(-1, 1, 1);
                }
                arrow.SetActive(false);
                rightArrows[i] = arrow;
            }
        }
    }
    
    public void UpdateStamina(int newPlayerStamina, int newFishStamina)
    {
        playerStamina = Mathf.Clamp(newPlayerStamina, 0, 100);
        fishStamina = Mathf.Clamp(newFishStamina, 0, 100);
        UpdateDisplay();
    }
    
    public void UpdatePowerDifference(int newPowerDifference)
    {
        powerDifference = newPowerDifference;
        UpdateDisplay();
    }
    
    public void UpdateAll(int newPlayerStamina, int newFishStamina, int newPowerDifference)
    {
        playerStamina = Mathf.Clamp(newPlayerStamina, 0, 100);
        fishStamina = Mathf.Clamp(newFishStamina, 0, 100);
        powerDifference = newPowerDifference;
        UpdateDisplay();
    }
    
    void UpdateDisplay()
    {
        UpdateBars();
        UpdateArrows();
    }
    
    void UpdateBars()
    {
        if (playerBar != null)
        {
            // Player bar grows as FISH loses stamina (player is winning)
            float playerWinning = (100f - fishStamina) / 100f;
            playerBar.fillAmount = playerWinning;
        }
        
        if (fishBar != null)
        {
            // Fish bar grows as PLAYER loses stamina (fish is winning)
            float fishWinning = (100f - playerStamina) / 100f;
            fishBar.fillAmount = fishWinning;
        }
    }
    
    void UpdateArrows()
    {
        // Hide all arrows first
        HideAllArrows();
        
        // Determine which arrows to show based on power difference
        if (powerDifference > 0)
        {
            // Player has advantage - show green arrows on left
            ShowLeftArrows(Mathf.Min(Mathf.Abs(powerDifference), maxArrows));
        }
        else if (powerDifference < 0)
        {
            // Fish has advantage - show red arrows on right
            ShowRightArrows(Mathf.Min(Mathf.Abs(powerDifference), maxArrows));
        }
        
        // Handle blinking for large advantages
        bool shouldBlink = Mathf.Abs(powerDifference) > 5;
        if (shouldBlink && !isBlinking)
        {
            StartBlinking();
        }
        else if (!shouldBlink && isBlinking)
        {
            StopBlinking();
        }
    }
    
    void HideAllArrows()
    {
        // Hide left arrows
        if (leftArrows != null)
        {
            for (int i = 0; i < leftArrows.Length; i++)
            {
                if (leftArrows[i] != null)
                    leftArrows[i].SetActive(false);
            }
        }
        
        // Hide right arrows
        if (rightArrows != null)
        {
            for (int i = 0; i < rightArrows.Length; i++)
            {
                if (rightArrows[i] != null)
                    rightArrows[i].SetActive(false);
            }
        }
    }
    
    void ShowLeftArrows(int count)
    {
        if (leftArrows == null) return;
        
        for (int i = 0; i < count && i < leftArrows.Length; i++)
        {
            if (leftArrows[i] != null)
                leftArrows[i].SetActive(true);
        }
    }
    
    void ShowRightArrows(int count)
    {
        if (rightArrows == null) return;
        
        for (int i = 0; i < count && i < rightArrows.Length; i++)
        {
            if (rightArrows[i] != null)
                rightArrows[i].SetActive(true);
        }
    }
    
    void StartBlinking()
    {
        if (isBlinking) return;
        
        isBlinking = true;
        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);
        
        blinkCoroutine = StartCoroutine(BlinkArrows());
    }
    
    void StopBlinking()
    {
        isBlinking = false;
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        
        // Make sure arrows are visible
        SetArrowsVisible(true);
    }
    
    IEnumerator BlinkArrows()
    {
        while (isBlinking)
        {
            SetArrowsVisible(false);
            yield return new WaitForSeconds(1f / blinkSpeed / 2f);
            
            SetArrowsVisible(true);
            yield return new WaitForSeconds(1f / blinkSpeed / 2f);
        }
    }
    
    void SetArrowsVisible(bool visible)
    {
        // Set visibility for active left arrows
        if (leftArrows != null)
        {
            for (int i = 0; i < leftArrows.Length; i++)
            {
                if (leftArrows[i] != null && leftArrows[i].activeSelf)
                {
                    Image arrowImage = leftArrows[i].GetComponent<Image>();
                    if (arrowImage != null)
                        arrowImage.color = visible ? playerArrowColor : Color.clear;
                }
            }
        }
        
        // Set visibility for active right arrows
        if (rightArrows != null)
        {
            for (int i = 0; i < rightArrows.Length; i++)
            {
                if (rightArrows[i] != null && rightArrows[i].activeSelf)
                {
                    Image arrowImage = rightArrows[i].GetComponent<Image>();
                    if (arrowImage != null)
                        arrowImage.color = visible ? fishArrowColor : Color.clear;
                }
            }
        }
    }
    
    // Public methods for testing in inspector
    [ContextMenu("Test Player Advantage")]
    public void TestPlayerAdvantage()
    {
        UpdateAll(80, 40, 7); // Player winning with 7 power advantage
    }
    
    [ContextMenu("Test Fish Advantage")]
    public void TestFishAdvantage()
    {
        UpdateAll(30, 90, -4); // Fish winning with 4 power advantage
    }
    
    [ContextMenu("Test Close Battle")]
    public void TestCloseBattle()
    {
        UpdateAll(60, 65, 1); // Close battle with small player advantage
    }
    
    [ContextMenu("Test Blinking")]
    public void TestBlinking()
    {
        UpdateAll(90, 20, 8); // Large advantage that should blink
    }
}
using UnityEngine;
using UnityEngine.UI;

public class HittingTimingBar : MonoBehaviour
{
    [Header("Timing Bar Elements")]
    public Image backgroundBar;
    public Image sweetSpotArea;
    public Image movingIndicator;
    
    [Header("Timing Settings")]
    public float barSpeed = 2f;                    // Speed of the moving indicator
    public float sweetSpotSize = 0.2f;             // Size of sweet spot (0-1 range)
    public Color sweetSpotColor = Color.green;
    public Color indicatorColor = Color.white;
    public Color backgroundColor = Color.gray;
    
    [Header("Sweet Spot Position")]
    [Range(0f, 1f)]
    public float sweetSpotCenter = 0.5f;           // Where the sweet spot is centered (0-1 range)
    
    // Private variables
    private float currentPosition = 0f;
    private bool movingRight = true;
    private bool isActive = false;
    private HittingInteractionManager hitManager;
    
    // Properties
    public bool IsInSweetSpot => IsIndicatorInSweetSpot();
    public bool IsActive => isActive;
    
    void Awake()
    {
        SetupVisuals();
    }
    
    void SetupVisuals()
    {
        // Set up colors
        if (backgroundBar != null)
            backgroundBar.color = backgroundColor;
        
        if (sweetSpotArea != null)
            sweetSpotArea.color = sweetSpotColor;
        
        if (movingIndicator != null)
            movingIndicator.color = indicatorColor;
        
        // Position sweet spot
        UpdateSweetSpotPosition();
        
        // Initially hide the timing bar
        gameObject.SetActive(false);
    }
    
    public void Initialize(HittingInteractionManager manager)
    {
        hitManager = manager;
        Debug.Log("HittingTimingBar initialized");
    }
    
    public void StartTiming()
    {
        isActive = true;
        currentPosition = 0f;
        movingRight = true;
        gameObject.SetActive(true);
        UpdateIndicatorPosition();
        
        Debug.Log("Timing bar started");
    }
    
    public void StopTiming()
    {
        isActive = false;
        gameObject.SetActive(false);
        
        Debug.Log("Timing bar stopped");
    }
    
    void Update()
    {
        if (isActive)
        {
            UpdateIndicatorMovement();
        }
    }
    
    void UpdateIndicatorMovement()
    {
        // Move the indicator back and forth
        float moveAmount = barSpeed * Time.deltaTime;
        
        if (movingRight)
        {
            currentPosition += moveAmount;
            if (currentPosition >= 1f)
            {
                currentPosition = 1f;
                movingRight = false;
            }
        }
        else
        {
            currentPosition -= moveAmount;
            if (currentPosition <= 0f)
            {
                currentPosition = 0f;
                movingRight = true;
            }
        }
        
        UpdateIndicatorPosition();
    }
    
    void UpdateIndicatorPosition()
    {
        if (movingIndicator != null)
        {
            RectTransform indicatorRect = movingIndicator.GetComponent<RectTransform>();
            if (indicatorRect != null)
            {
                // Position indicator along the bar (0 = left, 1 = right)
                float barWidth = GetComponent<RectTransform>().rect.width;
                float xPos = (currentPosition - 0.5f) * barWidth;
                indicatorRect.anchoredPosition = new Vector2(xPos, 0);
            }
        }
    }
    
    void UpdateSweetSpotPosition()
    {
        if (sweetSpotArea != null)
        {
            RectTransform sweetSpotRect = sweetSpotArea.GetComponent<RectTransform>();
            if (sweetSpotRect != null)
            {
                // Position and size the sweet spot
                float barWidth = GetComponent<RectTransform>().rect.width;
                float sweetSpotWidth = barWidth * sweetSpotSize;
                float xPos = (sweetSpotCenter - 0.5f) * barWidth;
                
                sweetSpotRect.sizeDelta = new Vector2(sweetSpotWidth, sweetSpotRect.sizeDelta.y);
                sweetSpotRect.anchoredPosition = new Vector2(xPos, 0);
            }
        }
    }
    
    bool IsIndicatorInSweetSpot()
    {
        float sweetSpotMin = sweetSpotCenter - (sweetSpotSize / 2f);
        float sweetSpotMax = sweetSpotCenter + (sweetSpotSize / 2f);
        
        return currentPosition >= sweetSpotMin && currentPosition <= sweetSpotMax;
    }
    
    public float GetTimingAccuracy()
    {
        // Returns how close to perfect timing (0 = terrible, 1 = perfect)
        float distanceFromCenter = Mathf.Abs(currentPosition - sweetSpotCenter);
        float maxDistance = sweetSpotSize / 2f;
        
        if (distanceFromCenter <= maxDistance)
        {
            return 1f - (distanceFromCenter / maxDistance);
        }
        
        return 0f;
    }
    
    // Called when the component is reset in the editor
    void OnValidate()
    {
        if (Application.isPlaying)
            return;
        
        // Clamp values
        sweetSpotCenter = Mathf.Clamp01(sweetSpotCenter);
        sweetSpotSize = Mathf.Clamp(sweetSpotSize, 0.05f, 1f);
        
        // Update visuals in editor
        if (sweetSpotArea != null)
            UpdateSweetSpotPosition();
    }
}
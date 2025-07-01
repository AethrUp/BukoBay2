using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class DepthEffectsDisplay : MonoBehaviour
{
    [Header("Display Settings")]
    public Color positiveColor = new Color(0.2f, 0.6f, 1f, 1f); // Blue
    public Color negativeColor = new Color(1f, 0.6f, 0.2f, 1f); // Orange
    public Color neutralColor = new Color(0.8f, 0.8f, 0.8f, 1f); // Gray
    public Color textColor = Color.white;
    
    [Header("Box Settings")]
    public float boxWidth = 40f;
    public float boxHeight = 30f;
    public float spacing = 2f;
    
    private List<GameObject> depthBoxes = new List<GameObject>();
    private List<Image> depthBackgrounds = new List<Image>();
    private List<TextMeshProUGUI> depthTexts = new List<TextMeshProUGUI>();
    
    void Awake()
    {
        CreateDepthBoxes();
    }
    
    void CreateDepthBoxes()
    {
        // Clear any existing boxes
        ClearBoxes();
        
        // Create 9 boxes for depths 1-9
        for (int i = 1; i <= 9; i++)
        {
            GameObject box = CreateDepthBox(i);
            depthBoxes.Add(box);
        }
    }
    
    GameObject CreateDepthBox(int depthNumber)
    {
        // Create the box container
        GameObject boxObj = new GameObject($"DepthBox_{depthNumber}");
        boxObj.transform.SetParent(transform, false);
        
        // Add RectTransform for UI positioning
        RectTransform boxRect = boxObj.AddComponent<RectTransform>();
        boxRect.sizeDelta = new Vector2(boxWidth, boxHeight);
        boxRect.anchorMin = new Vector2(0, 0.5f);
        boxRect.anchorMax = new Vector2(0, 0.5f);
        boxRect.pivot = new Vector2(0, 0.5f);
        
        // Position the box (left to right)
        float xPos = (depthNumber - 1) * (boxWidth + spacing);
        boxRect.anchoredPosition = new Vector2(xPos, 0);
        
        // Add background image
        Image backgroundImage = boxObj.AddComponent<Image>();
        backgroundImage.color = neutralColor;
        depthBackgrounds.Add(backgroundImage);
        
        // Create text child object
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(boxObj.transform, false);
        
        // Add text component
        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = "0";
        textComponent.fontSize = 16;
        textComponent.color = textColor;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.fontStyle = FontStyles.Bold;
        
        // Position text to fill the box
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        depthTexts.Add(textComponent);
        
        return boxObj;
    }
    
    public void DisplayGearDepthEffects(GearCard gearCard)
    {
        if (gearCard == null)
        {
            ClearDisplay();
            return;
        }
        
        // Update each depth box
        for (int depth = 1; depth <= 9; depth++)
        {
            int effect = gearCard.GetDepthEffect(depth);
            UpdateDepthBox(depth - 1, effect); // Convert to 0-based index
        }
    }
    
    void UpdateDepthBox(int boxIndex, int effectValue)
    {
        if (boxIndex < 0 || boxIndex >= depthBackgrounds.Count) return;
        
        // Update text
        if (boxIndex < depthTexts.Count)
        {
            depthTexts[boxIndex].text = effectValue.ToString();
        }
        
        // Update background color based on value
        if (boxIndex < depthBackgrounds.Count)
        {
            Color bgColor;
            if (effectValue > 0)
                bgColor = positiveColor;
            else if (effectValue < 0)
                bgColor = negativeColor;
            else
                bgColor = neutralColor;
            
            depthBackgrounds[boxIndex].color = bgColor;
        }
    }
    
    public void ClearDisplay()
    {
        // Reset all boxes to neutral
        for (int i = 0; i < 9; i++)
        {
            UpdateDepthBox(i, 0);
        }
    }
    
    void ClearBoxes()
    {
        // Destroy existing boxes
        foreach (GameObject box in depthBoxes)
        {
            if (box != null)
                DestroyImmediate(box);
        }
        
        depthBoxes.Clear();
        depthBackgrounds.Clear();
        depthTexts.Clear();
    }
    
    // Method to manually set the parent width (useful for layout)
    public void SetContainerWidth()
    {
        float totalWidth = (9 * boxWidth) + (8 * spacing);
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(totalWidth, boxHeight);
        }
    }
    
    // Test method to show sample data
    [ContextMenu("Test Display")]
    void TestDisplay()
    {
        // Create some test data
        for (int i = 0; i < 9; i++)
        {
            int testValue = Random.Range(-3, 4);
            UpdateDepthBox(i, testValue);
        }
    }
}
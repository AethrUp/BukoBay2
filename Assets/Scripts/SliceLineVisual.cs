using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SliceLineVisual : MonoBehaviour
{
    [Header("Line Settings")]
    public LineRenderer lineRenderer;
    public Image backgroundImage;
    
    [Header("Colors")]
    public Color incompleteColor = Color.white;
    public Color completedColor = Color.green;
    public Color progressColor = Color.yellow;
    
    private Vector2[] linePoints;
    private bool[] pointsCompleted;
    private int lastCompletedIndex = -1;
    private List<GameObject> progressIndicators = new List<GameObject>();
    
    void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
        
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
    }
    
    public void SetupLine(Vector2[] points)
    {
        linePoints = points;
        pointsCompleted = new bool[points.Length];
        
        // Convert 2D points to 3D for LineRenderer
        Vector3[] linePositions = new Vector3[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            linePositions[i] = new Vector3(points[i].x, points[i].y, 0);
        }
        
        // Set up the LineRenderer
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = points.Length;
            lineRenderer.SetPositions(linePositions);
            lineRenderer.useWorldSpace = false;
            
            // FIX THE LAYERING ISSUE
            lineRenderer.sortingLayerName = "UI";
            lineRenderer.sortingOrder = 1000; // Put it in front of everything
            
            // Make it visible but not too thick
            lineRenderer.startWidth = 1f;
            lineRenderer.endWidth = 1f;
            
            // Set initial color
            if (lineRenderer.material != null)
            {
                lineRenderer.material.color = incompleteColor;
            }
        }
        
        Debug.Log($"SliceLineVisual setup with {points.Length} points");
    }
    
    public void UpdateProgress(int completedPointIndex)
    {
        if (completedPointIndex < 0 || completedPointIndex >= pointsCompleted.Length) return;
        
        pointsCompleted[completedPointIndex] = true;
        lastCompletedIndex = Mathf.Max(lastCompletedIndex, completedPointIndex);
        
        // Update visual feedback
        UpdateLineColors();
        
        Debug.Log($"Updated line progress: point {completedPointIndex} completed");
    }
    
    void UpdateLineColors()
    {
        if (lineRenderer == null || lineRenderer.material == null) return;
        
        // Calculate completion percentage
        float completionPercentage = GetCompletionPercentage();
        
        // Update overall line color based on progress
        if (completionPercentage >= 0.8f) // Near completion
        {
            lineRenderer.material.color = completedColor;
        }
        else if (completionPercentage > 0.2f) // Some progress
        {
            lineRenderer.material.color = progressColor;
        }
        else
        {
            lineRenderer.material.color = incompleteColor;
        }
        
        // Create visual indicators for completed sections
        UpdateProgressIndicators();
    }
    
    void UpdateProgressIndicators()
    {
        // Clear existing indicators
        ClearProgressIndicators();
        
        // Create small UI indicators for completed points
        for (int i = 0; i < pointsCompleted.Length; i++)
        {
            if (pointsCompleted[i])
            {
                CreateProgressIndicator(linePoints[i]);
            }
        }
    }
    
    void CreateProgressIndicator(Vector2 position)
{
    // Create a small green circle to show completed points
    GameObject indicator = new GameObject("ProgressIndicator");
    
    // Parent it directly to this slice line object
    indicator.transform.SetParent(transform, false);
    
    // Add UI Image component
    Image indicatorImage = indicator.AddComponent<Image>();
    indicatorImage.color = Color.green;
    
    // Position and size the indicator
    RectTransform indicatorRect = indicator.GetComponent<RectTransform>();
    
    // Use the exact same local position as the line point
    indicatorRect.anchoredPosition = position;
    indicatorRect.sizeDelta = new Vector2(12f, 12f);
    indicatorRect.anchorMin = new Vector2(0.5f, 0.5f);
    indicatorRect.anchorMax = new Vector2(0.5f, 0.5f);
    indicatorRect.pivot = new Vector2(0.5f, 0.5f);
    
    // IMPORTANT: Don't set rotation - it inherits from parent automatically
    // Since it's parented to the rotated slice line, it will rotate with it
    
    // Make sure it renders above the slice line
    Canvas indicatorCanvas = indicator.AddComponent<Canvas>();
    indicatorCanvas.overrideSorting = true;
    indicatorCanvas.sortingLayerName = "UI";
    indicatorCanvas.sortingOrder = 1001; // Higher than the line renderer (1000)
    
    // Add to progress indicators list for cleanup
    progressIndicators.Add(indicator);
    
    Debug.Log($"Created progress indicator at local position {position}, inheriting parent rotation");
}
    
    void ClearProgressIndicators()
    {
        if (progressIndicators != null)
        {
            foreach (GameObject indicator in progressIndicators)
            {
                if (indicator != null)
                    DestroyImmediate(indicator);
            }
            progressIndicators.Clear();
        }
    }
    
    public float GetCompletionPercentage()
    {
        if (pointsCompleted == null || pointsCompleted.Length == 0) return 0f;
        
        int completedCount = 0;
        foreach (bool completed in pointsCompleted)
        {
            if (completed) completedCount++;
        }
        
        return (float)completedCount / pointsCompleted.Length;
    }
    
    public void MarkAsCompleted()
{
    if (lineRenderer != null && lineRenderer.material != null)
    {
        lineRenderer.material.color = completedColor;
    }
    
    // Clean up progress indicators immediately when slice is done
    ClearProgressIndicators();
    
    Debug.Log("Slice line marked as completed - indicators cleared");
}
}
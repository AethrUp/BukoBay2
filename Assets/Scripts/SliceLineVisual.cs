using UnityEngine;
using UnityEngine.UI;

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
        
        // ENSURE IT'S SET UP FOR UI
        lineRenderer.useWorldSpace = false; // Add this line
        
        // Set initial color using material
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
        
        // Simple approach: change the whole line color based on progress
        float completionPercentage = GetCompletionPercentage();
        
        if (completionPercentage >= 0.8f) // 80% threshold
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
        
        Debug.Log("Slice line marked as completed");
    }
}
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SprayAreaVisual : MonoBehaviour
{
    [Header("Visual Settings")]
    public Image backgroundImage;
    public Image fillImage;
    
    [Header("Colors")]
    public Color emptyColor = new Color(1f, 1f, 1f, 0.3f);  // Semi-transparent white
    public Color filledColor = new Color(0f, 0.8f, 1f, 0.8f); // Blue
    public Color completedColor = Color.green;
    
    [Header("Fill Display")]
    public Transform fillContainer;
    public GameObject fillCellPrefab; // Keep this for backward compatibility but we won't use it
    
    private int gridResolution;
    private GameObject[,] fillCells;
    private int filledCellCount = 0;
    private int totalCells = 0;
    
    void Awake()
    {
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
        
        if (fillContainer == null)
            fillContainer = transform;
    }
    
    void Start()
    {
        // Set initial appearance
        if (backgroundImage != null)
        {
            backgroundImage.color = emptyColor;
        }
    }
    
    public void InitializeGrid(int resolution, float areaWidth, float areaHeight)
    {
        // Ensure component is properly initialized first
        if (fillContainer == null)
        {
            // Force initialization if Awake hasn't been called yet
            if (fillContainer == null)
                fillContainer = transform;
                
            // Try to find the FillContainer child if it exists
            Transform fillContainerChild = transform.Find("FillContainer");
            if (fillContainerChild != null)
                fillContainer = fillContainerChild;
        }
        
        gridResolution = resolution;
        totalCells = resolution * resolution;
        
        // Clear any existing fill cells BEFORE creating the new array
        ClearFillCells();
        
        // Create new array AFTER clearing
        fillCells = new GameObject[resolution, resolution];
        
        // Create fill cells grid without using the problematic prefab
        CreateFillGridDirect(areaWidth, areaHeight);
        
        Debug.Log($"SprayAreaVisual initialized with {resolution}x{resolution} grid (direct creation)");
    }
    
    void CreateFillGridDirect(float areaWidth, float areaHeight)
    {
        // Ensure fillContainer is initialized
        if (fillContainer == null)
        {
            fillContainer = transform;
            Debug.LogWarning("SprayAreaVisual: fillContainer was null, using transform as fallback.");
        }
        
        if (fillContainer == null)
        {
            Debug.LogError("SprayAreaVisual: fillContainer is still null after fallback! Cannot create fill grid.");
            return;
        }
        
        if (gridResolution <= 0)
        {
            Debug.LogError("SprayAreaVisual: Invalid grid resolution!");
            return;
        }
        
        float cellWidth = areaWidth / gridResolution;
        float cellHeight = areaHeight / gridResolution;
        
        Debug.Log($"SprayAreaVisual: Creating grid {gridResolution}x{gridResolution} with cell size {cellWidth}x{cellHeight}");
        
        for (int x = 0; x < gridResolution; x++)
        {
            for (int y = 0; y < gridResolution; y++)
            {
                try
                {
                    // Create fill cell directly without using a prefab
                    GameObject fillCell = new GameObject($"FillCell_{x}_{y}");
                    if (fillCell == null)
                    {
                        Debug.LogError($"SprayAreaVisual: Failed to create GameObject for cell [{x},{y}]");
                        fillCells[x, y] = null;
                        continue;
                    }
                    
                    // Set parent
                    if (fillContainer == null)
                    {
                        Debug.LogError($"SprayAreaVisual: fillContainer is null when setting parent for cell [{x},{y}]");
                        Destroy(fillCell);
                        fillCells[x, y] = null;
                        continue;
                    }
                    fillCell.transform.SetParent(fillContainer, false);
                    
                    // Add RectTransform
                    RectTransform cellRect = fillCell.AddComponent<RectTransform>();
                    if (cellRect == null)
                    {
                        Debug.LogError($"SprayAreaVisual: Failed to add RectTransform to cell [{x},{y}]");
                        Destroy(fillCell);
                        fillCells[x, y] = null;
                        continue;
                    }
                    cellRect.sizeDelta = new Vector2(cellWidth, cellHeight);
                    
                    // Calculate position (center the grid)
                    float posX = (x * cellWidth) - (areaWidth / 2) + (cellWidth / 2);
                    float posY = (y * cellHeight) - (areaHeight / 2) + (cellHeight / 2);
                    cellRect.anchoredPosition = new Vector2(posX, posY);
                    
                    // Set anchors and pivot
                    cellRect.anchorMin = new Vector2(0.5f, 0.5f);
                    cellRect.anchorMax = new Vector2(0.5f, 0.5f);
                    cellRect.pivot = new Vector2(0.5f, 0.5f);
                    
                    // Add Image component
                    Image cellImage = fillCell.AddComponent<Image>();
                    if (cellImage == null)
                    {
                        Debug.LogError($"SprayAreaVisual: Failed to add Image component to cell [{x},{y}]");
                        Destroy(fillCell);
                        fillCells[x, y] = null;
                        continue;
                    }
                    cellImage.color = Color.clear; // Start invisible
                    
                    // Store reference
                    fillCells[x, y] = fillCell;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"SprayAreaVisual: Error creating fill cell at [{x},{y}]: {e.Message}\nStack trace: {e.StackTrace}");
                    if (fillCells != null && x < fillCells.GetLength(0) && y < fillCells.GetLength(1))
                    {
                        fillCells[x, y] = null;
                    }
                }
            }
        }
        
        Debug.Log($"SprayAreaVisual: Created {gridResolution * gridResolution} fill cells directly");
    }
    
    public void FillGridCell(int gridX, int gridY)
    {
        if (fillCells == null || gridX < 0 || gridX >= gridResolution || gridY < 0 || gridY >= gridResolution)
            return;
        
        GameObject fillCell = fillCells[gridX, gridY];
        if (fillCell != null)
        {
            Image cellImage = fillCell.GetComponent<Image>();
            if (cellImage != null && cellImage.color == Color.clear)
            {
                // Fill this cell
                cellImage.color = filledColor;
                filledCellCount++;
                
                // Update overall progress
                UpdateProgressVisual();
            }
        }
    }
    
    void UpdateProgressVisual()
    {
        // Background color changes removed to improve spray visibility
        // The individual fill cells provide enough visual feedback
    }
    
    public void MarkAsCompleted()
    {
        // Mark all cells as completed
        if (fillCells != null)
        {
            for (int x = 0; x < gridResolution; x++)
            {
                for (int y = 0; y < gridResolution; y++)
                {
                    if (fillCells[x, y] != null)
                    {
                        Image cellImage = fillCells[x, y].GetComponent<Image>();
                        if (cellImage != null)
                        {
                            cellImage.color = completedColor;
                        }
                    }
                }
            }
        }
        
        // Update background to completed color
        if (backgroundImage != null)
        {
            backgroundImage.color = completedColor;
        }
        
        Debug.Log("Spray area marked as completed");
    }
    
    public float GetCompletionPercentage()
    {
        if (totalCells == 0) return 0f;
        return (float)filledCellCount / totalCells;
    }
    
    void ClearFillCells()
    {
        if (fillCells != null)
        {
            for (int x = 0; x < fillCells.GetLength(0); x++)
            {
                for (int y = 0; y < fillCells.GetLength(1); y++)
                {
                    if (fillCells[x, y] != null)
                    {
                        if (Application.isPlaying)
                            Destroy(fillCells[x, y]);
                        else
                            DestroyImmediate(fillCells[x, y]);
                    }
                }
            }
        }
        
        // Don't set fillCells to null here - let InitializeGrid create the new array
        filledCellCount = 0;
        // Don't reset totalCells here - InitializeGrid will set it
    }
    
    void OnDestroy()
    {
        ClearFillCells();
        fillCells = null;
        totalCells = 0;
    }
}
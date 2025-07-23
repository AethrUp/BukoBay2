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
    public GameObject fillCellPrefab;
    
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
        gridResolution = resolution;
        totalCells = resolution * resolution;
        fillCells = new GameObject[resolution, resolution];
        
        // Clear any existing fill cells
        ClearFillCells();
        
        // Create fill cells grid
        CreateFillGrid(areaWidth, areaHeight);
        
        Debug.Log($"SprayAreaVisual initialized with {resolution}x{resolution} grid");
    }
    
    void CreateFillGrid(float areaWidth, float areaHeight)
    {
        if (fillCellPrefab == null)
        {
            Debug.LogWarning("Fill cell prefab not assigned - using simple color change");
            return;
        }
        
        float cellWidth = areaWidth / gridResolution;
        float cellHeight = areaHeight / gridResolution;
        
        for (int x = 0; x < gridResolution; x++)
        {
            for (int y = 0; y < gridResolution; y++)
            {
                // Create fill cell
                GameObject fillCell = Instantiate(fillCellPrefab, fillContainer);
                
                // Position the cell
                RectTransform cellRect = fillCell.GetComponent<RectTransform>();
                if (cellRect != null)
                {
                    cellRect.sizeDelta = new Vector2(cellWidth, cellHeight);
                    
                    // Calculate position (center the grid)
                    float posX = (x * cellWidth) - (areaWidth / 2) + (cellWidth / 2);
                    float posY = (y * cellHeight) - (areaHeight / 2) + (cellHeight / 2);
                    cellRect.anchoredPosition = new Vector2(posX, posY);
                    
                    // Set anchors and pivot
                    cellRect.anchorMin = new Vector2(0.5f, 0.5f);
                    cellRect.anchorMax = new Vector2(0.5f, 0.5f);
                    cellRect.pivot = new Vector2(0.5f, 0.5f);
                }
                
                // Initialize as empty (invisible)
                Image cellImage = fillCell.GetComponent<Image>();
                if (cellImage != null)
                {
                    cellImage.color = Color.clear;
                }
                
                // Store reference
                fillCells[x, y] = fillCell;
            }
        }
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
        if (backgroundImage == null) return;
        
        // Update background color based on progress
        float progress = (float)filledCellCount / totalCells;
        
        if (progress < 0.3f)
        {
            // Early stage - mostly empty
            backgroundImage.color = Color.Lerp(emptyColor, filledColor, progress * 3f);
        }
        else if (progress < 0.8f)
        {
            // Mid stage - filling up
            backgroundImage.color = filledColor;
        }
        else
        {
            // Near completion - getting ready to complete
            backgroundImage.color = Color.Lerp(filledColor, completedColor, (progress - 0.8f) * 5f);
        }
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
                        DestroyImmediate(fillCells[x, y]);
                    }
                }
            }
        }
        
        fillCells = null;
        filledCellCount = 0;
        totalCells = 0;
    }
    
    void OnDestroy()
    {
        ClearFillCells();
    }
}
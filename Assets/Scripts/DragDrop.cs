using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardDragDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Card References")]
    public GearCard gearCard;
    public ActionCard actionCard;
    
    [Header("Drag Settings")]
    public Canvas canvas;
    public GraphicRaycaster raycaster;
    
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;
    private Transform originalParent;
    private InventoryDisplay inventoryDisplay;
    
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        // Add CanvasGroup if it doesn't exist
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Find the canvas and raycaster automatically
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }
        
        if (raycaster == null)
        {
            raycaster = canvas.GetComponent<GraphicRaycaster>();
        }
        
        // Find the inventory display
        inventoryDisplay = FindFirstObjectByType<InventoryDisplay>();
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        // Only allow dragging gear cards, not action cards (for now)
        if (gearCard == null) return;
        
        // Store original position and parent
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
        
        // Make the card semi-transparent while dragging
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
        
        // Move to top of hierarchy so it renders on top
        transform.SetParent(canvas.transform, true);
        
        Debug.Log($"Started dragging {gearCard.gearName}");
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (gearCard == null) return;
        
        // Move the card with the mouse/touch
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        if (gearCard == null) return;
        
        // Restore appearance
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        
        // Check what we dropped on
        DropZone dropZone = GetDropZone(eventData);
        
        if (dropZone != null && dropZone.CanAcceptCard(gearCard))
        {
            // Valid drop - handle the gear movement
            HandleGearMove(dropZone);
            Debug.Log($"Dropped {gearCard.gearName} on {dropZone.zoneName}");
        }
        else
        {
            // Invalid drop - return to original position
            ReturnToOriginalPosition();
            Debug.Log($"Invalid drop for {gearCard.gearName}, returning to original position");
        }
        
        // Refresh the inventory display after a short delay
        if (inventoryDisplay != null)
        {
            StartCoroutine(RefreshAfterDrop());
        }
    }
    
    System.Collections.IEnumerator RefreshAfterDrop()
    {
        yield return new WaitForEndOfFrame();
        inventoryDisplay.RefreshDisplay();
    }
    
    DropZone GetDropZone(PointerEventData eventData)
    {
        // Raycast to find what we're over
        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        raycaster.Raycast(eventData, raycastResults);
        
        foreach (var result in raycastResults)
        {
            DropZone dropZone = result.gameObject.GetComponent<DropZone>();
            if (dropZone != null)
            {
                return dropZone;
            }
        }
        
        return null;
    }
    
    void HandleGearMove(DropZone dropZone)
    {
        PlayerInventory inventory = inventoryDisplay.playerInventory;
        
        // Remove from current location
        RemoveFromCurrentLocation();
        
        // Add to new location
        dropZone.AcceptCard(gearCard);
        
        // Return to original parent temporarily to fix positioning
        transform.SetParent(originalParent, true);
    }
    
    void RemoveFromCurrentLocation()
    {
        PlayerInventory inventory = inventoryDisplay.playerInventory;
        
        // Check equipped slots
        if (inventory.equippedRod == gearCard) inventory.equippedRod = null;
        else if (inventory.equippedReel == gearCard) inventory.equippedReel = null;
        else if (inventory.equippedLine == gearCard) inventory.equippedLine = null;
        else if (inventory.equippedLure == gearCard) inventory.equippedLure = null;
        else if (inventory.equippedBait == gearCard) inventory.equippedBait = null;
        else if (inventory.equippedExtra1 == gearCard) inventory.equippedExtra1 = null;
        else if (inventory.equippedExtra2 == gearCard) inventory.equippedExtra2 = null;
        
        // Check tackle box
        else if (inventory.extraGear.Contains(gearCard))
        {
            inventory.extraGear.Remove(gearCard);
        }
    }
    
    void ReturnToOriginalPosition()
    {
        // Return to original parent and position
        transform.SetParent(originalParent, true);
        rectTransform.anchoredPosition = originalPosition;
    }
}
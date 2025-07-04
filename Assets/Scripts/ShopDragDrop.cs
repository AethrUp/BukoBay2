using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ShopItemDragDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Shop Item References")]
    public ShopItem shopItem;
    
    [Header("Drag Settings")]
    public Canvas canvas;
    public GraphicRaycaster raycaster;
    
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;
    private Transform originalParent;
    
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
        
        if (raycaster == null && canvas != null)
        {
            raycaster = canvas.GetComponent<GraphicRaycaster>();
        }
        
        // Get shop item data from the ShopItemData component
        ShopItemData shopData = GetComponent<ShopItemData>();
        if (shopData != null)
        {
            shopItem = shopData.shopItem;
        }
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"OnBeginDrag called for item: {(shopItem != null ? shopItem.itemName : "NULL")}");
        
        if (shopItem == null) 
        {
            Debug.LogError("ShopItem is null, cannot drag!");
            return;
        }
        
        if (rectTransform == null)
        {
            Debug.LogError("RectTransform is null!");
            return;
        }
        
        if (canvasGroup == null)
        {
            Debug.LogError("CanvasGroup is null!");
            return;
        }
        
        // Store original position and parent
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
        
        Debug.Log($"Stored original position: {originalPosition}, parent: {originalParent.name}");
        
        // Make the card semi-transparent while dragging
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
        
        // Move to top of hierarchy so it renders on top
        if (canvas != null)
        {
            transform.SetParent(canvas.transform, true);
            Debug.Log($"Moved to canvas: {canvas.name}");
        }
        else
        {
            Debug.LogError("Canvas is null, cannot move to top!");
        }
        
        Debug.Log($"Started dragging shop item: {shopItem.itemName}");
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (shopItem == null) 
        {
            Debug.LogError("OnDrag: ShopItem is null!");
            return;
        }
        
        if (rectTransform == null)
        {
            Debug.LogError("OnDrag: RectTransform is null!");
            return;
        }
        
        if (canvas == null)
        {
            Debug.LogError("OnDrag: Canvas is null!");
            return;
        }
        
        // Move the card with the mouse/touch
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        if (shopItem == null) return;
        
        // Restore appearance
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        
        // Check what we dropped on
        PurchaseDropZone purchaseZone = GetPurchaseDropZone(eventData);
        
        if (purchaseZone != null)
        {
            Debug.Log($"Dropped {shopItem.itemName} on purchase zone");
            purchaseZone.OnDrop(eventData);
        }
        else
        {
            // Invalid drop - return to original position
            ReturnToOriginalPosition();
            Debug.Log($"Invalid drop for {shopItem.itemName}, returning to original position");
        }
    }
    
    PurchaseDropZone GetPurchaseDropZone(PointerEventData eventData)
    {
        // Raycast to find what we're over
        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        raycaster.Raycast(eventData, raycastResults);
        
        Debug.Log($"Shop item raycast found {raycastResults.Count} results");
        
        foreach (var raycastResult in raycastResults)
        {
            Debug.Log($"Shop raycast hit: {raycastResult.gameObject.name}");
            PurchaseDropZone purchaseZone = raycastResult.gameObject.GetComponent<PurchaseDropZone>();
            if (purchaseZone != null)
            {
                Debug.Log("Found PurchaseDropZone component!");
                return purchaseZone;
            }
        }
        
        Debug.Log("No PurchaseDropZone found in raycast results");
        return null;
    }
    
    void ReturnToOriginalPosition()
    {
        // Return to original parent and position
        transform.SetParent(originalParent, true);
        rectTransform.anchoredPosition = originalPosition;
    }
}
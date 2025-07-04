using UnityEngine;
using UnityEngine.EventSystems;

public class PurchaseDropZone : MonoBehaviour, IDropHandler
{
    [Header("Purchase Settings")]
    public ShopManager shopManager;
    
    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("PurchaseDropZone.OnDrop called!");
        
        // Get the dragged object
        GameObject draggedObject = eventData.pointerDrag;
        
        if (draggedObject == null)
        {
            Debug.Log("No dragged object");
            return;
        }
        
        // Check if it has shop item data
        ShopItemData shopData = draggedObject.GetComponent<ShopItemData>();
        if (shopData == null || shopData.shopItem == null)
        {
            Debug.Log("No shop item data found");
            return;
        }
        
        // Check if it's draggable (has CardDragDrop)
        CardDragDrop dragDrop = draggedObject.GetComponent<CardDragDrop>();
        if (dragDrop == null)
        {
            Debug.Log("No CardDragDrop component found");
            return;
        }
        
        Debug.Log($"Attempting to purchase {shopData.shopItem.itemName}");
        
        // Try to purchase the item
        if (shopManager != null)
        {
            bool success = shopManager.PurchaseItem(shopData.shopItem, draggedObject);
            if (!success)
            {
                // Return to original position if purchase failed
                ReturnToOriginalPosition(draggedObject);
            }
        }
        else
        {
            Debug.LogError("No ShopManager assigned to PurchaseDropZone!");
            ReturnToOriginalPosition(draggedObject);
        }
    }
    
    void ReturnToOriginalPosition(GameObject draggedObject)
    {
        // This is a simple return - the CardDragDrop should handle this better
        // but for now this prevents the item from disappearing
        Debug.Log("Returning item to original position");
    }
}
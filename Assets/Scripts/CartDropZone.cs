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
            Debug.Log("No shop item data found on dragged object");
            return;
        }
        
        // Check if it has the shop drag component
        ShopItemDragDrop shopDragDrop = draggedObject.GetComponent<ShopItemDragDrop>();
        if (shopDragDrop == null)
        {
            Debug.Log("No ShopItemDragDrop component found");
            return;
        }
        
        Debug.Log($"Valid shop item dropped: {shopData.shopItem.itemName}");
        
        // Add item to cart instead of purchasing immediately
        if (shopManager != null)
        {
            bool addedToCart = shopManager.AddToCart(shopData.shopItem, draggedObject);
            if (!addedToCart)
            {
                Debug.Log($"Failed to add {shopData.shopItem.itemName} to cart");
                // Return the card to its original position using SendMessage
                draggedObject.SendMessage("ReturnToOriginalPosition", SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                Debug.Log($"Successfully added {shopData.shopItem.itemName} to cart");
            }
        }
        else
        {
            Debug.LogError("No ShopManager assigned to PurchaseDropZone!");
            // Return the card to its original position using SendMessage
            draggedObject.SendMessage("ReturnToOriginalPosition", SendMessageOptions.DontRequireReceiver);
        }
    }
}
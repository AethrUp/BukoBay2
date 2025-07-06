using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CartItemDragDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Drag Settings")]
    public Canvas canvas;
    public GraphicRaycaster raycaster;
    
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;
    private Transform originalParent;
    private int originalSiblingIndex;
    
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }
    
    void Start()
    {
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }
        
        if (raycaster == null && canvas != null)
        {
            raycaster = canvas.GetComponent<GraphicRaycaster>();
        }
        
        if (GetComponent<Image>() == null)
        {
            Image img = gameObject.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.01f);
        }
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
        
        if (canvas != null)
        {
            transform.SetParent(canvas.transform, true);
        }
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (rectTransform != null && canvas != null)
        {
            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        
        bool droppedOnCart = IsDroppedOnCart(eventData);
        
        if (droppedOnCart)
        {
            ReturnToCartPosition();
        }
        else
        {
            RemoveFromCart();
        }
    }
    
    bool IsDroppedOnCart(PointerEventData eventData)
    {
        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        raycaster.Raycast(eventData, raycastResults);
        
        foreach (var raycastResult in raycastResults)
        {
            Transform current = raycastResult.gameObject.transform;
            while (current != null)
            {
                if (current.name.Contains("Cart") || current.name.Contains("cart"))
                {
                    return true;
                }
                current = current.parent;
            }
        }
        
        return false;
    }
    
    void RemoveFromCart()
    {
        ShopManager shopManager = FindFirstObjectByType<ShopManager>();
        if (shopManager != null)
        {
            ShopItemData shopData = GetComponent<ShopItemData>();
            if (shopData != null && shopData.shopItem != null)
            {
                shopManager.RemoveFromCart(shopData.shopItem, gameObject);
            }
        }
    }
    
    void ReturnToCartPosition()
    {
        if (originalParent != null)
        {
            transform.SetParent(originalParent, true);
            transform.SetSiblingIndex(originalSiblingIndex);
            
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = originalPosition;
            }
        }
    }
}
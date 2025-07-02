using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class ActionCardDropZone : MonoBehaviour, IDropHandler
{
    [Header("Drop Zone Settings")]
    public bool targetsPlayer = true; // true = helps player, false = targets fish
    
    [Header("Game References")]
    public FishingManager fishingManager;
    public InteractivePhaseUI interactiveUI;
    
    [Header("Visual Settings")]
    public float cardSpacing = 10f;
    
    private List<GameObject> playedCards = new List<GameObject>();
    
    public void OnDrop(PointerEventData eventData)
    {
        // Get the dragged object
        GameObject draggedObject = eventData.pointerDrag;
        
        if (draggedObject == null) return;
        
        // Check if it's an action card
        CardDragDrop dragDrop = draggedObject.GetComponent<CardDragDrop>();
        if (dragDrop == null || dragDrop.actionCard == null) return;
        
        // Check if we're in the interactive phase
        if (fishingManager == null || !fishingManager.isInteractionPhase)
        {
            Debug.LogWarning("Cannot play action cards - not in interactive phase!");
            return;
        }
        
        ActionCard actionCard = dragDrop.actionCard;
        
        // Check if this card can target what this drop zone affects
        if (targetsPlayer && !actionCard.canTargetPlayer)
        {
            Debug.LogWarning($"{actionCard.actionName} cannot target players!");
            return;
        }
        
        if (!targetsPlayer && !actionCard.canTargetFish)
        {
            Debug.LogWarning($"{actionCard.actionName} cannot target fish!");
            return;
        }
        
        Debug.Log($"Playing {actionCard.actionName} on {(targetsPlayer ? "player" : "fish")}");
        
        // Play the action card through the fishing manager
        bool success = fishingManager.PlayActionCard(actionCard, targetsPlayer);
        
        if (success)
        {
            // Remove from player inventory
            RemoveFromPlayerInventory(actionCard);
            
            // Move the card to this panel
            MoveCardToPanel(draggedObject);
            
            // Update the drag component so it can't be dragged again
            dragDrop.actionCard = null;
            dragDrop.enabled = false;
        }
    }
    
    void RemoveFromPlayerInventory(ActionCard actionCard)
    {
        // Find the player inventory and remove the card
        PlayerInventory playerInventory = FindFirstObjectByType<PlayerInventory>();
        if (playerInventory != null && playerInventory.actionCards.Contains(actionCard))
        {
            playerInventory.actionCards.Remove(actionCard);
            Debug.Log($"Removed {actionCard.actionName} from player inventory");
        }
    }
    
    void MoveCardToPanel(GameObject cardObject)
    {
        // Move the card to this panel
        cardObject.transform.SetParent(transform, false);
        
        // Position it in a grid within this panel
        RectTransform cardRect = cardObject.GetComponent<RectTransform>();
        
        // Calculate position based on how many cards are already here
        int cardIndex = playedCards.Count;
        Vector2 newPosition = new Vector2(cardIndex * (cardRect.sizeDelta.x + cardSpacing), 0);
        
        cardRect.anchoredPosition = newPosition;
        cardRect.anchorMin = Vector2.zero;
        cardRect.anchorMax = Vector2.zero;
        cardRect.pivot = Vector2.zero;
        
        // Add to our list of played cards
        playedCards.Add(cardObject);
        
        Debug.Log($"Moved card to {(targetsPlayer ? "player" : "fish")} panel at position {cardIndex}");
    }
    
    // Clear all played cards (call this when the interactive phase ends)
    public void ClearPlayedCards()
    {
        foreach (GameObject card in playedCards)
        {
            if (card != null)
            {
                Destroy(card);
            }
        }
        playedCards.Clear();
    }
}
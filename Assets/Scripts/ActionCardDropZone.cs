using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;  // ADD THIS LINE
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

    [Header("Played Card Prefab")]
    public GameObject playedCardPrefab;  // Add this line

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
        Debug.Log("=== MoveCardToPanel called ==="); // ADD THIS LINE

        // Get the action card data before we destroy the original
        CardDragDrop dragDrop = cardObject.GetComponent<CardDragDrop>();
        ActionCard actionCard = dragDrop.actionCard;

        // Create the new circle prefab at a random position in the panel
        Vector2 randomPos = new Vector2(Random.Range(-100f, 100f), Random.Range(-50f, 50f));
        GameObject circleCard = Instantiate(playedCardPrefab, transform, false);
        circleCard.transform.localPosition = randomPos;

        // Set up the circle card
        SetupCircleCard(circleCard, actionCard);

        // Add to our list of played cards
        playedCards.Add(circleCard);

        // Destroy the original full-size card
        Destroy(cardObject);

        // Wake up all cards so they rearrange
        PlayedCardPhysics.WakeAllCardsInPanel(transform);

        Debug.Log($"Created circle card for {actionCard.actionName}");
    }

    void SetupCircleCard(GameObject circleCard, ActionCard actionCard)
{
    Debug.Log($"Setting up circle card for: {actionCard.actionName}");
    
    // Set the item image (MainArt is inside Mask, and we ARE the PlayedAction object)
    Transform mainArtTransform = circleCard.transform.Find("Mask/MainArt");
    Debug.Log($"MainArt found: {mainArtTransform != null}");
    
    if (mainArtTransform != null)
    {
        Image itemImage = mainArtTransform.GetComponent<Image>();
        Debug.Log($"Image component found: {itemImage != null}");
        
        if (itemImage != null && actionCard.actionImage != null)
        {
            itemImage.sprite = actionCard.actionImage;
            Debug.Log($"Assigned sprite: {actionCard.actionImage.name}");
        }
    }
    
    // Set FRAME color (FrameTop is also inside Mask)
    Transform frameTransform = circleCard.transform.Find("Mask/FrameTop");
    if (frameTransform != null)
    {
        Image frameImage = frameTransform.GetComponent<Image>();
        if (frameImage != null)
        {
            frameImage.color = Color.blue;
        }
    }
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
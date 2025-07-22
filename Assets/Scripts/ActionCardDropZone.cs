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
    
    [Header("Hitting System")]
    public HittingInteractionManager hittingManager;
    
    private readonly string[] hittingActionCards = {
        "Apoco-Sluggie Boy",
        "Bonkman",
        "Lil Sluggie Boy",
        "Otsuchii SS",
        "Sluggie Boy",
        "Udar 98"
    };

    [Header("Shooting System")]
    public ShootingInteractionManager shootingManager;
    
    private readonly string[] shootingActionCards = {
        "Aries Javeline",
        "CosmoRocket",
        "Elektrika 77",
        "Lil Spittle",
        "Rattler Venom",
        "TootiToot",
        "Tranq-O-Catch"
    };

    [Header("Slicing System")]
public SlicingInteractionManager slicingManager;

private readonly string[] slicingActionCards = {
    "Arkansas Toothpick",
    "GRV-V",
    "Horagai II",
    "igla",
    "Itamae",
    "Jinro's Whisker",
    "Monarch",
    "Naginata I",
    "Naginata S",
    "Naginata X",
    "Sasumata"
};

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

        // Check if this is a slicing action card
        if (IsSlicingActionCard(actionCard.actionName))
        {
            Debug.Log($"Detected slicing action card: {actionCard.actionName}");
            
            // Use slicing system instead of normal drop
            if (slicingManager != null)
            {
                ulong playerId = Unity.Netcode.NetworkManager.Singleton != null ? 
                                Unity.Netcode.NetworkManager.Singleton.LocalClientId : 0;
                
                bool targetPlayer = targetsPlayer;
                
                bool success = slicingManager.StartSlicingSequence(actionCard, targetPlayer, playerId);
                
                if (success)
                {
                    // Remove from player inventory
                    RemoveFromPlayerInventory(actionCard);
                    
                    // Destroy the original dragged card
                    Destroy(draggedObject);
                    
                    Debug.Log($"Started slicing sequence for {actionCard.actionName} targeting {(targetPlayer ? "player" : "fish")}");
                    return; // Exit early - slicing system handles the rest
                }
                else
                {
                    Debug.LogWarning($"Failed to start slicing sequence for {actionCard.actionName}");
                    return;
                }
            }
            else
            {
                Debug.LogError("No SlicingInteractionManager found!");
                // Fall through to normal handling
            }
        }

        // Check if this is a shooting action card
        if (IsShootingActionCard(actionCard.actionName))
        {
            Debug.Log($"Detected shooting action card: {actionCard.actionName}");
            
            // Use shooting system instead of normal drop
            if (shootingManager != null)
            {
                ulong playerId = Unity.Netcode.NetworkManager.Singleton != null ? 
                                Unity.Netcode.NetworkManager.Singleton.LocalClientId : 0;
                
                bool targetPlayer = targetsPlayer;
                
                bool success = shootingManager.StartShootingSequence(actionCard, targetPlayer, playerId);
                
                if (success)
                {
                    // Remove from player inventory
                    RemoveFromPlayerInventory(actionCard);
                    
                    // Destroy the original dragged card
                    Destroy(draggedObject);
                    
                    Debug.Log($"Started shooting sequence for {actionCard.actionName} targeting {(targetPlayer ? "player" : "fish")}");
                    return; // Exit early - shooting system handles the rest
                }
                else
                {
                    Debug.LogWarning($"Failed to start shooting sequence for {actionCard.actionName}");
                    return;
                }
            }
            else
            {
                Debug.LogError("No ShootingInteractionManager found!");
                // Fall through to normal handling
            }
        }

        // Check if this is a hitting action card
        if (IsHittingActionCard(actionCard.actionName))
        {
            Debug.Log($"Detected hitting action card: {actionCard.actionName}");

            // Use hitting system instead of normal drop
            if (hittingManager != null)
            {
                ulong playerId = Unity.Netcode.NetworkManager.Singleton != null ?
                                Unity.Netcode.NetworkManager.Singleton.LocalClientId : 0;

                // The target is determined by which drop zone this is
                bool targetPlayer = targetsPlayer; // This drop zone's setting determines the target

                bool success = hittingManager.StartHittingSequence(actionCard, targetPlayer, playerId);

                if (success)
                {
                    // Remove from player inventory
                    RemoveFromPlayerInventory(actionCard);

                    // Destroy the original dragged card
                    Destroy(draggedObject);

                    Debug.Log($"Started hitting sequence for {actionCard.actionName} targeting {(targetPlayer ? "player" : "fish")}");
                    return; // Exit early - hitting system handles the rest
                }
                else
                {
                    Debug.LogWarning($"Failed to start hitting sequence for {actionCard.actionName}");
                    return;
                }
            }
            else
            {
                Debug.LogError("No HittingInteractionManager found! Falling back to normal action card handling.");
                // Fall through to normal handling
            }
        }

        // Normal action card handling for non-interactive cards

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
        bool playSuccess = fishingManager.PlayActionCard(actionCard, targetsPlayer);

        if (playSuccess)
        {
            // Remove from player inventory
            RemoveFromPlayerInventory(actionCard);

            // Update the drag component so it can't be dragged again
            dragDrop.actionCard = null;
            dragDrop.enabled = false;

            // Destroy the original dragged card since network creates the visual
            Destroy(draggedObject);
        }
    }

    // Helper method to check if an action card is a slicing type
    private bool IsSlicingActionCard(string cardName)
    {
        foreach (string slicingCard in slicingActionCards)
        {
            if (cardName.Equals(slicingCard, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    // Helper method to check if an action card is a hitting type
    private bool IsHittingActionCard(string cardName)
    {
        foreach (string hittingCard in hittingActionCards)
        {
            if (cardName.Equals(hittingCard, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    // Helper method to check if an action card is a shooting type
    private bool IsShootingActionCard(string cardName)
    {
        foreach (string shootingCard in shootingActionCards)
        {
            if (cardName.Equals(shootingCard, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    void RemoveFromPlayerInventory(ActionCard actionCard)
    {
        // Find the player inventory and remove the card
        PlayerInventory playerInventory = FindFirstObjectByType<PlayerInventory>();
        if (playerInventory != null && playerInventory.actionCards.Contains(actionCard))
        {
            playerInventory.actionCards.Remove(actionCard);
            // Debug.Log($"Removed {actionCard.actionName} from player inventory");
        }
    }

    public void CreateNetworkedPlayedCard(string cardName, int playerEffect, int fishEffect)
    {
        // Debug.Log($"Creating networked played card: {cardName} (Player: {playerEffect}, Fish: {fishEffect})");

        // Create the circle card at a random position
        Vector2 randomPos = new Vector2(Random.Range(-100f, 100f), Random.Range(-50f, 50f));
        GameObject circleCard = Instantiate(playedCardPrefab, transform, false);
        circleCard.transform.localPosition = randomPos;

        // Set up the circle card with the effect data
        SetupNetworkedCircleCard(circleCard, cardName, playerEffect, fishEffect);

        // Add to our list
        playedCards.Add(circleCard);

        // Wake up physics
        PlayedCardPhysics.WakeAllCardsInPanel(transform);
    }

    void SetupNetworkedCircleCard(GameObject circleCard, string cardName, int playerEffect, int fishEffect)
    {
        // Debug.Log($"Setting up networked circle card for: {cardName} (Effects: P{playerEffect} F{fishEffect})");

        // Try to find the actual ActionCard to get the image
        ActionCard foundCard = FindActionCardByName(cardName);

        if (foundCard != null && foundCard.actionImage != null)
        {
            // Set the actual card image
            Transform mainArtTransform = circleCard.transform.Find("Mask/MainArt");
            if (mainArtTransform != null)
            {
                Image itemImage = mainArtTransform.GetComponent<Image>();
                if (itemImage != null)
                {
                    itemImage.sprite = foundCard.actionImage;
                    // Debug.Log($"Assigned sprite: {foundCard.actionImage.name}");
                }
            }
        }

        // Set frame color based on effect type
        Transform frameTransform = circleCard.transform.Find("Mask/FrameTop");
        if (frameTransform != null)
        {
            Image frameImage = frameTransform.GetComponent<Image>();
            if (frameImage != null)
            {
                // Different colors for different effects
                if (playerEffect > 0) frameImage.color = Color.green;      // Positive player effect
                else if (playerEffect < 0) frameImage.color = Color.red;   // Negative player effect
                else if (fishEffect != 0) frameImage.color = Color.yellow; // Fish effect
                else frameImage.color = Color.blue;                        // Default
            }
        }
    }

    // Helper method to find ActionCard by name
    ActionCard FindActionCardByName(string cardName)
    {
        #if UNITY_EDITOR
        string[] actionGuids = UnityEditor.AssetDatabase.FindAssets($"{cardName} t:ActionCard");
        
        if (actionGuids.Length > 0)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(actionGuids[0]);
            return UnityEditor.AssetDatabase.LoadAssetAtPath<ActionCard>(assetPath);
        }
        #endif

        return null;
    }

    void MoveCardToPanel(GameObject cardObject)
    {
        // Debug.Log("=== MoveCardToPanel called ==="); // ADD THIS LINE

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

        // Debug.Log($"Created circle card for {actionCard.actionName}");
    }

    void SetupCircleCard(GameObject circleCard, ActionCard actionCard)
    {
        // Debug.Log($"Setting up circle card for: {actionCard.actionName}");

        // Set the item image (MainArt is inside Mask, and we ARE the PlayedAction object)
        Transform mainArtTransform = circleCard.transform.Find("Mask/MainArt");
        // Debug.Log($"MainArt found: {mainArtTransform != null}");

        if (mainArtTransform != null)
        {
            Image itemImage = mainArtTransform.GetComponent<Image>();
            // Debug.Log($"Image component found: {itemImage != null}");

            if (itemImage != null && actionCard.actionImage != null)
            {
                itemImage.sprite = actionCard.actionImage;
                // Debug.Log($"Assigned sprite: {actionCard.actionImage.name}");
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
        // Debug.Log($"ClearPlayedCards called on {gameObject.name} - found {playedCards.Count} cards to clear");
        foreach (GameObject card in playedCards)
        {
            if (card != null)
            {
                // Debug.Log($"Destroying card: {card.name}");
                Destroy(card);
            }
            else
            {
                // Debug.Log("Found null card in playedCards list");
            }
        }
        playedCards.Clear();
        // Debug.Log($"All played cards cleared from {gameObject.name}");
    }
}
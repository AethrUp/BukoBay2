using UnityEngine;
using System.Collections.Generic;

public class PlayedCardPhysics : MonoBehaviour
{
    [Header("Physics Settings")]
    public float centerAttraction = 100f;     // Pull toward center (increased)
    public float cardRepulsion = 500f;        // Push away from other cards (much stronger)
    public float damping = 3f;                // "Water resistance" (reduced)
    public float minDistance = 120f;          // Minimum distance between cards (bigger)
    public float settleTolerance = 2f;        // When to stop moving
    public float maxSpeed = 300f;             // Maximum movement speed
    
    private Vector2 velocity = Vector2.zero;
    private bool isSettled = false;
    private Transform parentPanel; // Which panel this card belongs to
    private static Dictionary<Transform, List<PlayedCardPhysics>> cardsByPanel = new Dictionary<Transform, List<PlayedCardPhysics>>();
    
    void Start()
    {
        // Identify which panel this card belongs to
        parentPanel = transform.parent;
        
        // Add this card to the list for its specific panel
        if (!cardsByPanel.ContainsKey(parentPanel))
        {
            cardsByPanel[parentPanel] = new List<PlayedCardPhysics>();
        }
        cardsByPanel[parentPanel].Add(this);
        
        // Start with some initial movement
        isSettled = false;
        
        // Cards start where they are placed, no random velocity initially
        velocity = Vector2.zero;
        
        // Debug.Log($"Card physics started at position: {transform.localPosition}. Cards in this panel: {cardsByPanel[parentPanel].Count}");
    }
    
    void Update()
    {
        if (isSettled) return; // Don't move if we're settled
        
        // Calculate forces
        Vector2 centerForce = CalculateCenterAttraction();
        Vector2 repulsionForce = CalculateCardRepulsion();
        
        // Apply forces
        Vector2 totalForce = centerForce + repulsionForce;
        velocity += totalForce * Time.deltaTime;
        
        // Limit maximum speed
        if (velocity.magnitude > maxSpeed)
        {
            velocity = velocity.normalized * maxSpeed;
        }
        
        // Apply damping (water resistance)
        velocity *= (1f - damping * Time.deltaTime);
        
        // Move the card
        Vector2 currentPos = transform.localPosition;
        currentPos += velocity * Time.deltaTime;
        transform.localPosition = currentPos;
        
        // Check if we should settle
        if (velocity.magnitude < settleTolerance && totalForce.magnitude < 1f)
        {
            isSettled = true;
            velocity = Vector2.zero;
            // Debug.Log($"Card settled at position: {transform.localPosition}");
        }
    }
    
    Vector2 CalculateCenterAttraction()
    {
        Vector2 currentPos = transform.localPosition;
        Vector2 centerPos = Vector2.zero; // Center of the panel (0,0 in local space)
        
        Vector2 directionToCenter = (centerPos - currentPos);
        float distance = directionToCenter.magnitude;
        
        // Avoid division by zero
        if (distance < 0.1f) return Vector2.zero;
        
        // Gentle pull toward center, stronger when farther away
        Vector2 force = directionToCenter.normalized * centerAttraction * (distance * 0.01f);
        
        return force;
    }
    
    Vector2 CalculateCardRepulsion()
    {
        Vector2 totalRepulsion = Vector2.zero;
        Vector2 myPosition = transform.localPosition;
        
        // Only check cards in the same panel
        if (!cardsByPanel.ContainsKey(parentPanel)) return totalRepulsion;
        
        foreach (PlayedCardPhysics otherCard in cardsByPanel[parentPanel])
        {
            if (otherCard == this || otherCard == null) continue;
            
            Vector2 otherPosition = otherCard.transform.localPosition;
            Vector2 difference = myPosition - otherPosition;
            float distance = difference.magnitude;
            
            // Only repel if cards are close
            if (distance < minDistance)
            {
                if (distance < 0.1f)
                {
                    // If cards are on top of each other, push in random direction
                    Vector2 randomDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
                    totalRepulsion += randomDirection * cardRepulsion;
                }
                else
                {
                    // Normal repulsion - much stronger force
                    float repulsionStrength = cardRepulsion * (minDistance - distance) / distance;
                    Vector2 repulsion = difference.normalized * repulsionStrength;
                    totalRepulsion += repulsion;
                    
                    // Debug.Log($"Repelling from card at distance {distance:F1}, force: {repulsionStrength:F1}");
                }
            }
        }
        
        return totalRepulsion;
    }
    
    // Call this when a new card is added to wake up all cards in the same panel
    public static void WakeAllCardsInPanel(Transform panel)
    {
        if (!cardsByPanel.ContainsKey(panel)) return;
        
        // Debug.Log($"Waking up {cardsByPanel[panel].Count} cards in panel {panel.name}");
        foreach (PlayedCardPhysics card in cardsByPanel[panel])
        {
            if (card != null)
            {
                card.isSettled = false;
                // Give a small random impulse to break deadlocks
                card.velocity += new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f));
            }
        }
    }
    
    void OnDestroy()
    {
        // Remove from list when destroyed
        if (cardsByPanel.ContainsKey(parentPanel))
        {
            cardsByPanel[parentPanel].Remove(this);
            // Debug.Log($"Card destroyed. Remaining cards in panel: {cardsByPanel[parentPanel].Count}");
        }
    }
    
    // Debug method to visualize forces in Scene view
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        // Draw line to center
        Gizmos.color = Color.blue;
        Vector3 worldPos = transform.position;
        Vector3 centerWorld = transform.parent != null ? transform.parent.position : Vector3.zero;
        Gizmos.DrawLine(worldPos, centerWorld);
        
        // Draw repulsion lines to nearby cards in same panel
        Gizmos.color = Color.red;
        if (cardsByPanel.ContainsKey(parentPanel))
        {
            foreach (PlayedCardPhysics otherCard in cardsByPanel[parentPanel])
            {
                if (otherCard == this || otherCard == null) continue;
                
                float distance = Vector2.Distance(transform.localPosition, otherCard.transform.localPosition);
                if (distance < minDistance)
                {
                    Gizmos.DrawLine(worldPos, otherCard.transform.position);
                }
            }
        }
    }
}
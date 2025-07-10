using UnityEngine;

[CreateAssetMenu(fileName = "New Effect Card", menuName = "Cards/Effect Card")]
public class EffectCard : ScriptableObject
{
    [Header("Basic Info")]
    public string effectName;
    public Sprite effectImage;
    
    [Header("Effect Type")]
    public EffectType effectType;
    
    [Header("Repair Effects")]
    public int repairAmount = 0;        // For simple repair (like protivoyadiye: 3)
    public bool repairHalfDamage = false; // For percentage repair (like Sumūzu)
    
    [Header("Description")]
    public string description;
    
    [Header("Usage Rules")]
    public bool singleUse = true;        // Most effect cards are single use
    public bool canUseAnyTime = true;    // Can be used outside of casting phase
}

public enum EffectType
{
    Repair,           // Repairs gear durability
    Protection,       // Prevents damage
    Utility,          // Other game effects
    Persistent        // Stays in play with durability
}
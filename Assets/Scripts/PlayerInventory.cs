using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    [Header("Equipped Gear")]
    public GearCard equippedRod;
    public GearCard equippedReel;
    public GearCard equippedLine;
    public GearCard equippedLure;
    public GearCard equippedBait;
    public GearCard equippedExtra1;
    public GearCard equippedExtra2;
    
    [Header("Tackle Box")]
    public List<GearCard> extraGear = new List<GearCard>();
    public List<ActionCard> actionCards = new List<ActionCard>();
    public List<EffectCard> effectCards = new List<EffectCard>();
    
    [Header("Currency")]
    public int coins = 0;
    
    [Header("Player Protection Shield")]
    public EffectCard equippedShield;
    public int shieldStrength = 0;  // How much damage it can absorb
    
    // Static instance to persist across scenes
    public static PlayerInventory Instance;
    
    void Awake()
    {
        // If there's already an instance, destroy this one
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // Make this the persistent instance
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Create copies of all gear cards to avoid modifying the original assets
        CreateGearCopies();
    }
    
    void CreateGearCopies()
    {
        // Debug.Log("Creating gear copies to avoid modifying original assets...");
        
        // Copy equipped gear and store original durability
        if (equippedRod != null) 
        {
            equippedRod = Instantiate(equippedRod);
            equippedRod.maxDurability = equippedRod.durability; // Store original as max
        }
        if (equippedReel != null) 
        {
            equippedReel = Instantiate(equippedReel);
            equippedReel.maxDurability = equippedReel.durability;
        }
        if (equippedLine != null) 
        {
            equippedLine = Instantiate(equippedLine);
            equippedLine.maxDurability = equippedLine.durability;
        }
        if (equippedLure != null) 
        {
            equippedLure = Instantiate(equippedLure);
            equippedLure.maxDurability = equippedLure.durability;
        }
        if (equippedBait != null) 
        {
            equippedBait = Instantiate(equippedBait);
            equippedBait.maxDurability = equippedBait.durability;
        }
        if (equippedExtra1 != null) 
        {
            equippedExtra1 = Instantiate(equippedExtra1);
            equippedExtra1.maxDurability = equippedExtra1.durability;
        }
        if (equippedExtra2 != null) 
        {
            equippedExtra2 = Instantiate(equippedExtra2);
            equippedExtra2.maxDurability = equippedExtra2.durability;
        }
        
        // Copy tackle box gear and store original durability
        for (int i = 0; i < extraGear.Count; i++)
        {
            if (extraGear[i] != null)
            {
                extraGear[i] = Instantiate(extraGear[i]);
                extraGear[i].maxDurability = extraGear[i].durability;
            }
        }
        
        // Copy effect cards
        for (int i = 0; i < effectCards.Count; i++)
        {
            if (effectCards[i] != null)
                effectCards[i] = Instantiate(effectCards[i]);
        }
        
        // Copy action cards  
        for (int i = 0; i < actionCards.Count; i++)
        {
            if (actionCards[i] != null)
                actionCards[i] = Instantiate(actionCards[i]);
        }
        
        // Debug.Log("Gear copies created successfully with max durability stored");
    }
    
    // Function to check if a specific gear type is equipped
    public bool HasGearType(string gearType)
    {
        if (equippedRod != null && equippedRod.gearType == gearType) return true;
        if (equippedReel != null && equippedReel.gearType == gearType) return true;
        if (equippedLine != null && equippedLine.gearType == gearType) return true;
        if (equippedLure != null && equippedLure.gearType == gearType) return true;
        if (equippedBait != null && equippedBait.gearType == gearType) return true;
        if (equippedExtra1 != null && equippedExtra1.gearType == gearType) return true;
        if (equippedExtra2 != null && equippedExtra2.gearType == gearType) return true;
        
        return false;
    }
    
    // Function to get total power from all equipped gear
    public int GetTotalPower()
    {
        int totalPower = 0;
        // Debug.Log($"Calculating total power...");
        
        if (equippedRod != null) totalPower += equippedRod.power;
        if (equippedReel != null) totalPower += equippedReel.power;
        if (equippedLine != null) totalPower += equippedLine.power;
        if (equippedLure != null) totalPower += equippedLure.power;
        if (equippedBait != null) totalPower += equippedBait.power;
        if (equippedExtra1 != null) totalPower += equippedExtra1.power;
        if (equippedExtra2 != null) totalPower += equippedExtra2.power;
        
        // Debug.Log($"Total power calculated: {totalPower}");
        return totalPower;
    }
    
    // Function to add coins
    public void AddCoins(int amount)
    {
        coins += amount;
        // Debug.Log($"Player received {amount} coins! Total: {coins}");
    }
}
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
        
        if (equippedRod != null) totalPower += equippedRod.power;
        if (equippedReel != null) totalPower += equippedReel.power;
        if (equippedLine != null) totalPower += equippedLine.power;
        if (equippedLure != null) totalPower += equippedLure.power;
        if (equippedBait != null) totalPower += equippedBait.power;
        if (equippedExtra1 != null) totalPower += equippedExtra1.power;
        if (equippedExtra2 != null) totalPower += equippedExtra2.power;
        
        return totalPower;
    }
}
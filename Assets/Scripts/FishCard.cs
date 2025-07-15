using UnityEngine;

[CreateAssetMenu(fileName = "New Fish Card", menuName = "Cards/Fish Card")]
public class FishCard : ScriptableObject
{
    [Header("Basic Info")]
    public string fishName;
    public Sprite fishImage;
    
    [Header("Depth System")]
    public int mainDepth;        // 1=Coast, 2=Ocean, 3=Abyss
    public int subDepth;         // 1-9 (3 sub-depths per main depth)
    
    [Header("Challenge")]
    public int power;            // Fish strength
    
    [Header("Rewards")]
    public int coins;            // Coin rewards
    public int treasures;        // Treasure rewards
        
    [Header("Material Modifiers")]
    public string material1;     // First material type (e.g. "Plastic", "Bio")
    public int materialDiff1;    // Modifier for material1 (+/- to difficulty)
    
    public string material2;     // Second material type
    public int materialDiff2;    // Modifier for material2
    
    public string material3;     // Third material type  
    public int materialDiff3;    // Modifier for material3
    
    [Header("Gear Damage on Miss")]
    public int gear1Damage;      // Number of pieces to damage (0 = no damage to this gear type)
    public int gear2Damage;
    public int gear3Damage;
    public int gear4Damage;
    public int gear5Damage;
    
    [Header("Description")]
    public string description;
    
    // Helper function to get total gear pieces that will be damaged
    public int GetTotalGearDamage()
    {
        int total = 0;
        if (gear1Damage > 0) total += gear1Damage;
        if (gear2Damage > 0) total += gear2Damage;
        if (gear3Damage > 0) total += gear3Damage;
        if (gear4Damage > 0) total += gear4Damage;
        if (gear5Damage > 0) total += gear5Damage;
        return total;
    }
    
    // Helper function to get array of damage amounts (for random selection)
    public int[] GetGearDamageArray()
    {
        System.Collections.Generic.List<int> damages = new System.Collections.Generic.List<int>();
        
        if (gear1Damage > 0) damages.Add(gear1Damage);
        if (gear2Damage > 0) damages.Add(gear2Damage);
        if (gear3Damage > 0) damages.Add(gear3Damage);
        if (gear4Damage > 0) damages.Add(gear4Damage);
        if (gear5Damage > 0) damages.Add(gear5Damage);
        
        return damages.ToArray();
    }
    
    // Helper function to check if this fish has material modifiers for a specific material
    public int GetMaterialModifier(string materialType)
    {
        if (string.Equals(material1, materialType, System.StringComparison.OrdinalIgnoreCase))
            return materialDiff1;
        if (string.Equals(material2, materialType, System.StringComparison.OrdinalIgnoreCase))
            return materialDiff2;
        if (string.Equals(material3, materialType, System.StringComparison.OrdinalIgnoreCase))
            return materialDiff3;
            
        return 0; // No modifier for this material
    }
}
using UnityEngine;

[CreateAssetMenu(fileName = "New Gear Card", menuName = "Cards/Gear Card")]
public class GearCard : ScriptableObject
{
    [Header("Basic Info")]
    public string gearName;
    public string manufacturer;
    public Sprite gearImage;
    
    [Header("Stats")]
    public string gearType;
    public int power;
    public int durability;
    public string material;
    
    [Header("Depth Effects (1-9)")]
    public int depth1Effect;
    public int depth2Effect;
    public int depth3Effect;
    public int depth4Effect;
    public int depth5Effect;
    public int depth6Effect;
    public int depth7Effect;
    public int depth8Effect;
    public int depth9Effect;
    
    [Header("Pricing")]
    public float price;
    
    [Header("Description")]
    public string description;
    
    // Helper method to get depth effect by index (1-9)
    public int GetDepthEffect(int depth)
    {
        switch (depth)
        {
            case 1: return depth1Effect;
            case 2: return depth2Effect;
            case 3: return depth3Effect;
            case 4: return depth4Effect;
            case 5: return depth5Effect;
            case 6: return depth6Effect;
            case 7: return depth7Effect;
            case 8: return depth8Effect;
            case 9: return depth9Effect;
            default: return 0;
        }
    }
    
    // Helper method to set depth effect by index (useful for importing)
    public void SetDepthEffect(int depth, int effect)
    {
        switch (depth)
        {
            case 1: depth1Effect = effect; break;
            case 2: depth2Effect = effect; break;
            case 3: depth3Effect = effect; break;
            case 4: depth4Effect = effect; break;
            case 5: depth5Effect = effect; break;
            case 6: depth6Effect = effect; break;
            case 7: depth7Effect = effect; break;
            case 8: depth8Effect = effect; break;
            case 9: depth9Effect = effect; break;
        }
    }
}
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
    
    [Header("Depth Effects")]
    public int depth1Effect;
    public int depth2Effect;
    public int depth3Effect;
    public int depth4Effect;
    public int depth5Effect;
}
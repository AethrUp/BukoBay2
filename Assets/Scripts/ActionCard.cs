using UnityEngine;

[CreateAssetMenu(fileName = "New Action Card", menuName = "Cards/Action Card")]
public class ActionCard : ScriptableObject
{
    [Header("Basic Info")]
    public string actionName;
    public Sprite actionImage;
    
    [Header("Player Effect")]
    public int playerEffect;
    
    [Header("Fish Effect")]
    public int fishEffect;
    
    [Header("Target")]
    public bool canTargetPlayer = true;
    public bool canTargetFish = true;
    
    [Header("Description")]
    public string description;
}
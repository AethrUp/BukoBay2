using UnityEngine;

[CreateAssetMenu(fileName = "New Player Character", menuName = "Game/Player Character")]
public class PlayerCharacterData : ScriptableObject
{
    [Header("Character Info")]
    public string characterName;
    public Sprite characterPortrait;
    public Color primaryColor = Color.white;
    public Color secondaryColor = Color.gray;
    
    [Header("Visual Settings")]
    [Tooltip("Color used for action card tokens")]
    public Color tokenColor = Color.blue;
    
    [Header("Description")]
    [TextArea(3, 5)]
    public string characterDescription;
}
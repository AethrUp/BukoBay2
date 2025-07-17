using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerController : NetworkBehaviour
{
    [Header("Player Info")]
    public string playerName = "Player";
    
    [Header("Components")]
    public NetworkPlayerInventory playerInventory;
    
    void Awake()
    {
        playerInventory = GetComponent<NetworkPlayerInventory>();
    }
    
    public override void OnNetworkSpawn()
    {
        Debug.Log($"Network player spawned! IsOwner: {IsOwner}, IsHost: {IsHost}");
        
        if (IsOwner)
        {
            Debug.Log("This is MY player object");
        }
    }
}
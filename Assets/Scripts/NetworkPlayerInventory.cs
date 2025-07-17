using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class NetworkPlayerInventory : NetworkBehaviour
{
    [Header("Inventory Data")]
    public NetworkVariable<int> networkCoins = new NetworkVariable<int>(0);
    
    [Header("Local Player Reference")]
    public PlayerInventory localInventory;
    
    public override void OnNetworkSpawn()
    {
        Debug.Log($"NetworkPlayerInventory spawned for player {OwnerClientId}");
        
        if (IsOwner)
        {
            // This is our player - connect to the existing PlayerInventory
            ConnectToLocalInventory();
        }
    }
    
    void ConnectToLocalInventory()
    {
        // Find the existing PlayerInventory in the scene
        localInventory = FindFirstObjectByType<PlayerInventory>();
        
        if (localInventory != null)
        {
            Debug.Log("Connected to local PlayerInventory");
            
            // Sync local data to network
            SyncLocalDataToNetwork();
        }
        else
        {
            Debug.LogError("Could not find local PlayerInventory!");
        }
    }
    
    void SyncLocalDataToNetwork()
    {
        if (localInventory != null && IsOwner)
        {
            // Update network values from local inventory
            networkCoins.Value = localInventory.coins;
        }
    }
    
    // Call this when local inventory changes
    public void UpdateNetworkCoins(int newCoins)
    {
        if (IsOwner)
        {
            networkCoins.Value = newCoins;
        }
    }
    
    // Get coins for any player (local or remote)
    public int GetCoins()
    {
        return networkCoins.Value;
    }
}
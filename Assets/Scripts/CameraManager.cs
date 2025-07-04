using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("Cameras")]
    public Camera mainCamera;        // The original fishing/main camera
    public Camera shoppingCamera;    // The new shopping camera
    
    [Header("UI Panels")]
    public GameObject mainUI;        // Your main/fishing UI panels
    public GameObject shoppingUI;    // Your shopping UI panels (we'll create this next)
    
    [Header("Current State")]
    public CameraMode currentMode = CameraMode.Main;
    
    public enum CameraMode
    {
        Main,
        Shopping
    }
    
    void Start()
    {
        // Start with main camera active
        SwitchToMainCamera();
    }
    
    public void SwitchToMainCamera()
    {
        currentMode = CameraMode.Main;
        
        if (mainCamera != null) mainCamera.enabled = true;
        if (shoppingCamera != null) shoppingCamera.enabled = false;
        
        // Enable/disable UI
        if (mainUI != null) mainUI.SetActive(true);
        if (shoppingUI != null) shoppingUI.SetActive(false);
        
        Debug.Log("Switched to Main Camera");
    }
    
    public void SwitchToShoppingCamera()
    {
        currentMode = CameraMode.Shopping;
        
        if (mainCamera != null) mainCamera.enabled = false;
        if (shoppingCamera != null) shoppingCamera.enabled = true;
        
        // Enable/disable UI
        if (mainUI != null) mainUI.SetActive(false);
        if (shoppingUI != null) shoppingUI.SetActive(true);
        
        Debug.Log("Switched to Shopping Camera");
    }
    
    // Test methods you can call from inspector buttons
    [ContextMenu("Test - Switch to Main")]
    public void TestSwitchToMain()
    {
        SwitchToMainCamera();
    }
    
    [ContextMenu("Test - Switch to Shopping")]
    public void TestSwitchToShopping()
    {
        SwitchToShoppingCamera();
    }
}
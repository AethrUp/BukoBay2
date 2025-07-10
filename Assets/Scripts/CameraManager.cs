using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("Cameras")]
    public Camera mainCamera;        // The original fishing/main camera
    public Camera shoppingCamera;    // The shopping camera
    public Camera gearSetupCamera;   // The gear setup camera    // The new shopping camera
    public Camera resultsCamera;     // The fishing results camera


    [Header("UI Panels")]
    public GameObject mainUI;        // Your main/fishing UI panels
    public GameObject shoppingUI;    // Your shopping UI panels
    public GameObject gearSetupUI;   // Your gear setup UI panels
    public GameObject resultsUI;     // Your fishing results UI panels


    [Header("Current State")]
    public CameraMode currentMode = CameraMode.Main;

    public enum CameraMode
    {
        Main,
        Shopping,
        GearSetup,
        Results
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
        if (gearSetupCamera != null) gearSetupCamera.enabled = false;
        if (resultsCamera != null) resultsCamera.enabled = false;

        // Enable/disable UI
        if (mainUI != null) mainUI.SetActive(true);
        if (shoppingUI != null) shoppingUI.SetActive(false);
        if (gearSetupUI != null) gearSetupUI.SetActive(false);
        if (resultsUI != null) resultsUI.SetActive(false);

        Debug.Log("Switched to Main Camera");
    }

    public void SwitchToShoppingCamera()
    {
        currentMode = CameraMode.Shopping;

        if (mainCamera != null) mainCamera.enabled = false;
        if (shoppingCamera != null) shoppingCamera.enabled = true;
        if (gearSetupCamera != null) gearSetupCamera.enabled = false;
        if (resultsCamera != null) resultsCamera.enabled = false;

        // Enable/disable UI
        if (mainUI != null) mainUI.SetActive(false);
        if (shoppingUI != null) shoppingUI.SetActive(true);
        if (gearSetupUI != null) gearSetupUI.SetActive(false);
        if (resultsUI != null) resultsUI.SetActive(false);

        Debug.Log("Switched to Shopping Camera");
    }
    public void SwitchToGearSetupCamera()
    {
        currentMode = CameraMode.GearSetup;

        if (mainCamera != null) mainCamera.enabled = false;
        if (shoppingCamera != null) shoppingCamera.enabled = false;
        if (gearSetupCamera != null) gearSetupCamera.enabled = true;

        // Enable/disable UI
        if (mainUI != null) mainUI.SetActive(false);
        if (shoppingUI != null) shoppingUI.SetActive(false);
        if (gearSetupUI != null) gearSetupUI.SetActive(true);

        Debug.Log("Switched to Gear Setup Camera");
    }
    public void SwitchToResultsCamera()
    {
        currentMode = CameraMode.Results;

        if (mainCamera != null) mainCamera.enabled = false;
        if (shoppingCamera != null) shoppingCamera.enabled = false;
        if (gearSetupCamera != null) gearSetupCamera.enabled = false;
        if (resultsCamera != null) resultsCamera.enabled = true;

        // Enable/disable UI
        if (mainUI != null) mainUI.SetActive(false);
        if (shoppingUI != null) shoppingUI.SetActive(false);
        if (gearSetupUI != null) gearSetupUI.SetActive(false);
        if (resultsUI != null) resultsUI.SetActive(true);

        Debug.Log("Switched to Results Camera");
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
    [ContextMenu("Test - Switch to Gear Setup")]
    public void TestSwitchToGearSetup()
    {
        SwitchToGearSetupCamera();
    }
    [ContextMenu("Test - Switch to Results")]
public void TestSwitchToResults()
{
    SwitchToResultsCamera();
}
}
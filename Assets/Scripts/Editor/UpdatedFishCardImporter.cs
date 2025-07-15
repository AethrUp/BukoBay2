using UnityEngine;
using UnityEditor;
using System.IO;

public class UpdatedFishCardImporter : EditorWindow
{
    [MenuItem("Tools/Import Updated Fish Cards from CSV")]
    public static void ShowWindow()
    {
        GetWindow<UpdatedFishCardImporter>("Updated Fish Card Importer");
    }
    
    private string csvFilePath = "";
    
    void OnGUI()
    {
        GUILayout.Label("Updated Fish Card CSV Importer", EditorStyles.boldLabel);
        
        GUILayout.Space(10);
        
        GUILayout.Label("CSV File Path:");
        csvFilePath = EditorGUILayout.TextField(csvFilePath);
        
        if (GUILayout.Button("Browse for CSV File"))
        {
            csvFilePath = EditorUtility.OpenFilePanel("Select CSV File", "", "csv");
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Import Updated Fish Cards"))
        {
            ImportUpdatedFishCards();
        }
        
        GUILayout.Space(10);
        
        GUILayout.Label("Instructions:", EditorStyles.boldLabel);
        GUILayout.Label("1. Click 'Browse for CSV File' and select your new fish CSV");
        GUILayout.Label("2. Click 'Import Updated Fish Cards' to create all assets");
        GUILayout.Label("3. Fish cards will be created in Assets/Cards/UpdatedFish/");
        GUILayout.Label("Note: Now supports both Treasures and Coins!");
    }
    
    void ImportUpdatedFishCards()
    {
        if (string.IsNullOrEmpty(csvFilePath) || !File.Exists(csvFilePath))
        {
            EditorUtility.DisplayDialog("Error", "Please select a valid CSV file", "OK");
            return;
        }
        
        Debug.Log("Starting updated fish card import with treasures and coins...");
        
        // Create directories if they don't exist
        string cardsPath = "Assets/Cards";
        string fishPath = "Assets/Cards/UpdatedFish";
        
        if (!AssetDatabase.IsValidFolder(cardsPath))
            AssetDatabase.CreateFolder("Assets", "Cards");
        
        if (!AssetDatabase.IsValidFolder(fishPath))
            AssetDatabase.CreateFolder("Assets/Cards", "UpdatedFish");
        
        // Read CSV file
        string csvContent = File.ReadAllText(csvFilePath);
        Debug.Log($"CSV Content length: {csvContent.Length}");
        
        // Split into lines and clean them
        string[] allLines = csvContent.Split('\n');
        Debug.Log($"Total lines in file: {allLines.Length}");
        
        if (allLines.Length < 3) // Need number row + header row + at least one data line
        {
            EditorUtility.DisplayDialog("Error", "CSV file appears to be empty or has no data rows", "OK");
            return;
        }
        
        // Skip the first line (0,1,2,3...) and use the second line as headers
        string headerLine = allLines[1].Trim().Replace("\r", "");
        Debug.Log($"Header line: '{headerLine}'");
        
        // Simple split by comma for headers
        string[] headers = headerLine.Split(',');
        for (int i = 0; i < headers.Length; i++)
        {
            headers[i] = headers[i].Trim().Replace("\"", "");
        }
        
        Debug.Log($"Found {headers.Length} headers");
        Debug.Log("Column mapping:");
        Debug.Log("  Column 1: Fish Name");
        Debug.Log("  Column 2: Depth Type");
        Debug.Log("  Column 4: Sub Depth");
        Debug.Log("  Column 5: Treasures");
        Debug.Log("  Column 6: Coins");
        Debug.Log("  Column 7: Strength");
        Debug.Log("  Column 9-14: Materials");
        Debug.Log("  Column 15-19: Gear Damage");
        Debug.Log("  Column 20: Description");
        
        int importedCount = 0;
        int skippedCount = 0;
        
        // Process each data line (skip first two lines: numbers and headers)
        for (int lineIndex = 2; lineIndex < allLines.Length; lineIndex++)
        {
            string line = allLines[lineIndex].Trim().Replace("\r", "");
            if (string.IsNullOrEmpty(line)) continue;
            
            Debug.Log($"Processing line {lineIndex}");
            
            // Simple split by comma
            string[] values = line.Split(',');
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = values[i].Trim().Replace("\"", "");
            }
            
            if (values.Length < 20) // Need at least through Description column
            {
                Debug.LogWarning($"Line {lineIndex} has too few values ({values.Length}), skipping");
                skippedCount++;
                continue;
            }
            
            // Get fish name from column 1 (Fish)
            string fishName = GetValue(values, 1);
            if (string.IsNullOrEmpty(fishName))
            {
                Debug.LogWarning($"Line {lineIndex} has empty fish name, skipping");
                skippedCount++;
                continue;
            }
            
            Debug.Log($"Creating updated fish card for: '{fishName}'");
            
            // Create new FishCard
            FishCard fishCard = ScriptableObject.CreateInstance<FishCard>();
            
            // Set basic info
            fishCard.fishName = fishName;                    // Column 1 - Fish
            
            // Set depth info
            if (int.TryParse(GetValue(values, 2), out int mainDepth))
                fishCard.mainDepth = mainDepth;              // Column 2 - depthType
            
            if (int.TryParse(GetValue(values, 4), out int subDepth))
                fishCard.subDepth = subDepth;                // Column 4 - Depth Num
            
            // Set rewards (NEW: Both treasures and coins!)
            if (int.TryParse(GetValue(values, 5), out int treasures))
                fishCard.treasures = treasures;             // Column 5 - Treasure
            
            if (int.TryParse(GetValue(values, 6), out int coins))
                fishCard.coins = coins;                     // Column 6 - Coins
            
            // Set challenge
            if (int.TryParse(GetValue(values, 7), out int strength))
                fishCard.power = strength;                   // Column 7 - Strength
            
            // Set material modifiers (ALL SHIFTED BY +1 because of new Treasure column)
            fishCard.material1 = GetValue(values, 9);        // Column 9 - Mat 1
            if (int.TryParse(GetValue(values, 10), out int matDiff1))
                fishCard.materialDiff1 = matDiff1;           // Column 10 - MatDif 1
            
            fishCard.material2 = GetValue(values, 11);       // Column 11 - Mat 2
            if (int.TryParse(GetValue(values, 12), out int matDiff2))
                fishCard.materialDiff2 = matDiff2;           // Column 12 - MatDif 2
            
            fishCard.material3 = GetValue(values, 13);       // Column 13 - Mat 3
            if (int.TryParse(GetValue(values, 14), out int matDiff3))
                fishCard.materialDiff3 = matDiff3;           // Column 14 - MatDif 3
            
            // Set gear damage values (columns 15-19: Gear 1-5)
            if (int.TryParse(GetValue(values, 15), out int gear1))
                fishCard.gear1Damage = gear1;
            
            if (int.TryParse(GetValue(values, 16), out int gear2))
                fishCard.gear2Damage = gear2;
            
            if (int.TryParse(GetValue(values, 17), out int gear3))
                fishCard.gear3Damage = gear3;
            
            if (int.TryParse(GetValue(values, 18), out int gear4))
                fishCard.gear4Damage = gear4;
            
            if (int.TryParse(GetValue(values, 19), out int gear5))
                fishCard.gear5Damage = gear5;
            
            // Set description
            fishCard.description = GetValue(values, 20);     // Column 20 - Description
            
            // Try to find and assign the fish image
            string imageName = $"fishx_{fishName}";
            Sprite fishSprite = FindFishImage(imageName);
            if (fishSprite != null)
            {
                fishCard.fishImage = fishSprite;
                Debug.Log($"Found image for {fishName}: {imageName}");
            }
            
            // Create safe filename
            string safeFileName = fishName;
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                safeFileName = safeFileName.Replace(c, '_');
            }
            
            // Create asset
            string fileName = $"{safeFileName}.asset";
            string assetPath = $"{fishPath}/{fileName}";
            
            Debug.Log($"Creating asset at: {assetPath}");
            Debug.Log($"  Treasures: {fishCard.treasures}");
            Debug.Log($"  Coins: {fishCard.coins}");
            Debug.Log($"  Power: {fishCard.power}");
            
            AssetDatabase.CreateAsset(fishCard, assetPath);
            importedCount++;
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"Import complete! Created {importedCount} updated fish cards with treasures and coins, skipped {skippedCount} rows");
        EditorUtility.DisplayDialog("Import Complete", 
            $"Successfully imported {importedCount} updated fish cards with treasures and coins.\nSkipped {skippedCount} rows.\nSee console for details.", "OK");
    }
    
    string GetValue(string[] values, int index)
    {
        if (index >= 0 && index < values.Length)
            return values[index].Trim();
        return "";
    }
    
    Sprite FindFishImage(string imageName)
    {
        // Look for the image in Assets/Images/Fish/
        string[] guids = AssetDatabase.FindAssets($"{imageName} t:Sprite", new[] { "Assets/Images/Fish" });
        
        if (guids.Length > 0)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }
        
        return null;
    }
}
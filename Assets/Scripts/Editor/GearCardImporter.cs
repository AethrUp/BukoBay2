using UnityEngine;
using UnityEditor;
using System.IO;

public class GearCardImporter : EditorWindow
{
    [MenuItem("Tools/Import Gear Cards from CSV")]
    public static void ShowWindow()
    {
        GetWindow<GearCardImporter>("Gear Card Importer");
    }
    
    private string csvFilePath = "";
    
    void OnGUI()
    {
        GUILayout.Label("Gear Card CSV Importer", EditorStyles.boldLabel);
        
        GUILayout.Space(10);
        
        GUILayout.Label("CSV File Path:");
        csvFilePath = EditorGUILayout.TextField(csvFilePath);
        
        if (GUILayout.Button("Browse for CSV File"))
        {
            csvFilePath = EditorUtility.OpenFilePanel("Select CSV File", "", "csv");
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Import Gear Cards"))
        {
            ImportGearCards();
        }
        
        GUILayout.Space(10);
        
        GUILayout.Label("Instructions:", EditorStyles.boldLabel);
        GUILayout.Label("1. Click 'Browse for CSV File' and select your gear CSV");
        GUILayout.Label("2. Click 'Import Gear Cards' to create all assets");
        GUILayout.Label("3. Gear cards will be created in Assets/Cards/Gear/");
    }
    
    void ImportGearCards()
    {
        if (string.IsNullOrEmpty(csvFilePath) || !File.Exists(csvFilePath))
        {
            EditorUtility.DisplayDialog("Error", "Please select a valid CSV file", "OK");
            return;
        }
        
        Debug.Log("Starting gear card import...");
        
        // Create directories if they don't exist
        string cardsPath = "Assets/Cards";
        string gearPath = "Assets/Cards/Gear";
        
        if (!AssetDatabase.IsValidFolder(cardsPath))
            AssetDatabase.CreateFolder("Assets", "Cards");
        
        if (!AssetDatabase.IsValidFolder(gearPath))
            AssetDatabase.CreateFolder("Assets/Cards", "Gear");
        
        // Read CSV file
        string csvContent = File.ReadAllText(csvFilePath);
        Debug.Log($"CSV Content length: {csvContent.Length}");
        
        // Split into lines and clean them
        string[] allLines = csvContent.Split('\n');
        Debug.Log($"Total lines in file: {allLines.Length}");
        
        if (allLines.Length < 3) // Need header line + at least one data line
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
        
        Debug.Log($"Found {headers.Length} headers: {string.Join(" | ", headers)}");
        
        int importedCount = 0;
        int skippedCount = 0;
        
        // Process each data line (skip first two lines: numbers and headers)
        for (int lineIndex = 2; lineIndex < allLines.Length; lineIndex++)
        {
            string line = allLines[lineIndex].Trim().Replace("\r", "");
            if (string.IsNullOrEmpty(line)) continue;
            
            Debug.Log($"Processing line {lineIndex}: '{line.Substring(0, System.Math.Min(100, line.Length))}...'");
            
            // Simple split by comma
            string[] values = line.Split(',');
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = values[i].Trim().Replace("\"", "");
            }
            
            Debug.Log($"Split into {values.Length} values. Gear name: '{(values.Length > 3 ? values[3] : "NOT FOUND")}'");
            
            if (values.Length < 14) // Need at least through D5 column
            {
                Debug.LogWarning($"Line {lineIndex} has too few values ({values.Length}), skipping");
                skippedCount++;
                continue;
            }
            
            // Get gear name from column 3 (Item)
            string gearName = values[3];
            if (string.IsNullOrEmpty(gearName))
            {
                Debug.LogWarning($"Line {lineIndex} has empty gear name, skipping");
                skippedCount++;
                continue;
            }
            
            Debug.Log($"Creating gear card for: '{gearName}'");
            
            // Create new GearCard
            GearCard gearCard = ScriptableObject.CreateInstance<GearCard>();
            
            // Set basic info
            gearCard.gearName = gearName;                    // Column 3 - Item
            gearCard.manufacturer = values[1];               // Column 1 - Brand
            gearCard.gearType = values[4];                   // Column 4 - Type
            gearCard.material = values[5];                   // Column 5 - Material
            
            // Set numeric values with error checking
            if (int.TryParse(values[6], out int power))
                gearCard.power = power;                      // Column 6 - Power
            
            if (int.TryParse(values[7], out int durability))
                gearCard.durability = durability;            // Column 7 - Durability
            
            // Set depth effects (columns 9-13: D1, D2, D3, D4, D5)
            if (int.TryParse(values[9], out int d1))
                gearCard.depth1Effect = d1;
            
            if (int.TryParse(values[10], out int d2))
                gearCard.depth2Effect = d2;
            
            if (int.TryParse(values[11], out int d3))
                gearCard.depth3Effect = d3;
            
            if (int.TryParse(values[12], out int d4))
                gearCard.depth4Effect = d4;
            
            if (int.TryParse(values[13], out int d5))
                gearCard.depth5Effect = d5;
            
            // Create safe filename
            string safeFileName = gearName;
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                safeFileName = safeFileName.Replace(c, '_');
            }
            
            // Create asset
            string fileName = $"{safeFileName}.asset";
            string assetPath = $"{gearPath}/{fileName}";
            
            Debug.Log($"Creating asset at: {assetPath}");
            
            AssetDatabase.CreateAsset(gearCard, assetPath);
            importedCount++;
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"Import complete! Created {importedCount} gear cards, skipped {skippedCount} rows");
        EditorUtility.DisplayDialog("Import Complete", 
            $"Successfully imported {importedCount} gear cards.\nSkipped {skippedCount} rows.\nSee console for details.", "OK");
    }
}
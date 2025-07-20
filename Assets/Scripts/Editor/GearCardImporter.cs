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
        GUILayout.Label("3. Gear cards will be created in Assets/Cards/UpdatedGear/");
        GUILayout.Label("Note: Now supports 9 depth effects (D1-D9)");
    }
    
    void ImportGearCards()
    {
        if (string.IsNullOrEmpty(csvFilePath) || !File.Exists(csvFilePath))
        {
            EditorUtility.DisplayDialog("Error", "Please select a valid CSV file", "OK");
            return;
        }
        
        // Debug.Log("Starting updated gear card import...");
        
        // Create directories if they don't exist
        string cardsPath = "Assets/Cards";
        string gearPath = "Assets/Cards/UpdatedGear";
        
        if (!AssetDatabase.IsValidFolder(cardsPath))
            AssetDatabase.CreateFolder("Assets", "Cards");
        
        if (!AssetDatabase.IsValidFolder(gearPath))
            AssetDatabase.CreateFolder("Assets/Cards", "UpdatedGear");
        
        // Read CSV file
        string csvContent = File.ReadAllText(csvFilePath);
        // Debug.Log($"CSV Content length: {csvContent.Length}");
        
        // Split into lines and clean them
        string[] allLines = csvContent.Split('\n');
        // Debug.Log($"Total lines in file: {allLines.Length}");
        
        if (allLines.Length < 2) // Need header + at least one data line
        {
            EditorUtility.DisplayDialog("Error", "CSV file appears to be empty or has no data rows", "OK");
            return;
        }
        
        // Use the first line as headers (no number row in this CSV)
        string headerLine = allLines[0].Trim().Replace("\r", "");
        // Debug.Log($"Header line: '{headerLine}'");
        
        // Parse CSV headers
        string[] headers = ParseCSVLine(headerLine);
        // Debug.Log($"Found {headers.Length} headers");
        
        // Find column indices
        int titleIndex = FindColumnIndex(headers, "Title");
        int typeIndex = FindColumnIndex(headers, "Type");
        int brandIndex = FindColumnIndex(headers, "Brand");
        int itemIndex = FindColumnIndex(headers, "Item");
        int materialIndex = FindColumnIndex(headers, "Material");
        int powerIndex = FindColumnIndex(headers, "Power");
        int durabilityIndex = FindColumnIndex(headers, "Durability");
        int priceIndex = FindColumnIndex(headers, "Price");
        int descriptionIndex = FindColumnIndex(headers, "Description");
        
        // Debug column indices
        // Debug.Log($"Column indices - Type: {typeIndex}, Item: {itemIndex}, Material: {materialIndex}, Power: {powerIndex}, Durability: {durabilityIndex}");
        
        // Find depth effect indices (D1-D9)
        int[] depthIndices = new int[9];
        for (int i = 1; i <= 9; i++)
        {
            depthIndices[i-1] = FindColumnIndex(headers, $"D{i}");
        }
        
        int importedCount = 0;
        int skippedCount = 0;
        
        // Process each data line (skip header)
        for (int lineIndex = 1; lineIndex < allLines.Length; lineIndex++)
        {
            string line = allLines[lineIndex].Trim().Replace("\r", "");
            if (string.IsNullOrEmpty(line)) continue;
            
            // Debug.Log($"Processing line {lineIndex}");
            
            // Parse CSV line
            string[] values = ParseCSVLine(line);
            
            if (values.Length < 20) // Need at least through D9 column
            {
                // Debug.LogWarning($"Line {lineIndex} has too few values ({values.Length}), skipping");
                skippedCount++;
                continue;
            }
            
            // Get gear name from Item column
            string gearName = GetValue(values, itemIndex);
            if (string.IsNullOrEmpty(gearName))
            {
                // Debug.LogWarning($"Line {lineIndex} has empty gear name, skipping");
                skippedCount++;
                continue;
            }
            
            // Debug.Log($"Creating updated gear card for: '{gearName}'");
            
            // Create new GearCard
            GearCard gearCard = ScriptableObject.CreateInstance<GearCard>();
            
            // Set basic info
            gearCard.gearName = gearName;
            gearCard.manufacturer = GetValue(values, brandIndex);
            gearCard.gearType = GetValue(values, typeIndex);  // This should be Type column
            gearCard.material = GetValue(values, materialIndex);
            gearCard.description = GetValue(values, descriptionIndex);
            
            // Set numeric values with error checking
            if (int.TryParse(GetValue(values, powerIndex), out int power))
                gearCard.power = power;
            
            if (int.TryParse(GetValue(values, durabilityIndex), out int durability))
                gearCard.durability = durability;
            
            if (float.TryParse(GetValue(values, priceIndex), out float price))
                gearCard.price = price;
            
            // Set depth effects (D1-D9)
            for (int d = 1; d <= 9; d++)
            {
                if (depthIndices[d-1] >= 0) // Check if column exists
                {
                    if (int.TryParse(GetValue(values, depthIndices[d-1]), out int depthEffect))
                    {
                        gearCard.SetDepthEffect(d, depthEffect);
                    }
                }
            }
            
            // Try to find and assign gear image
            // Look for images in Assets/Images/Gear/ folder
            string imageName = gearName;
            Sprite gearSprite = FindGearImage(imageName);
            if (gearSprite != null)
            {
                gearCard.gearImage = gearSprite;
                // Debug.Log($"Found image for {gearName}");
            }
            
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
            
            // Debug.Log($"Creating asset at: {assetPath}");
            
            AssetDatabase.CreateAsset(gearCard, assetPath);
            importedCount++;
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        // Debug.Log($"Import complete! Created {importedCount} updated gear cards, skipped {skippedCount} rows");
        EditorUtility.DisplayDialog("Import Complete", 
            $"Successfully imported {importedCount} updated gear cards.\nSkipped {skippedCount} rows.\nSee console for details.", "OK");
    }
    
    string[] ParseCSVLine(string line)
    {
        System.Collections.Generic.List<string> result = new System.Collections.Generic.List<string>();
        bool inQuotes = false;
        string currentField = "";
        
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentField.Trim());
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }
        
        result.Add(currentField.Trim());
        return result.ToArray();
    }
    
    int FindColumnIndex(string[] headers, string columnName)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            if (headers[i].Trim().Equals(columnName, System.StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }
    
    string GetValue(string[] values, int index)
    {
        if (index >= 0 && index < values.Length)
            return values[index].Trim().Replace("\"", "");
        return "";
    }
    
    Sprite FindGearImage(string imageName)
    {
        // Look for the image in Assets/Images/Gear/
        string[] guids = AssetDatabase.FindAssets($"{imageName} t:Sprite", new[] { "Assets/Images/Gear" });
        
        if (guids.Length > 0)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }
        
        return null;
    }
}
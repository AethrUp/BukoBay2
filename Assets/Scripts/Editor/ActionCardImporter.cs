using UnityEngine;
using UnityEditor;
using System.IO;

public class ActionCardImporter : EditorWindow
{
    [MenuItem("Tools/Import Action Cards from CSV")]
    public static void ShowWindow()
    {
        GetWindow<ActionCardImporter>("Action Card Importer");
    }
    
    private string csvFilePath = "";
    
    void OnGUI()
    {
        GUILayout.Label("Action Card CSV Importer", EditorStyles.boldLabel);
        
        GUILayout.Space(10);
        
        GUILayout.Label("CSV File Path:");
        csvFilePath = EditorGUILayout.TextField(csvFilePath);
        
        if (GUILayout.Button("Browse for CSV File"))
        {
            csvFilePath = EditorUtility.OpenFilePanel("Select CSV File", "", "csv");
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Import Action Cards"))
        {
            ImportActionCards();
        }
        
        GUILayout.Space(10);
        
        GUILayout.Label("Instructions:", EditorStyles.boldLabel);
        GUILayout.Label("1. Click 'Browse for CSV File' and select your action CSV");
        GUILayout.Label("2. Click 'Import Action Cards' to create all assets");
        GUILayout.Label("3. Action cards will be created in Assets/Cards/Actions/");
        GUILayout.Label("Note: Imports both Action and Effect type cards");
    }
    
    void ImportActionCards()
    {
        if (string.IsNullOrEmpty(csvFilePath) || !File.Exists(csvFilePath))
        {
            EditorUtility.DisplayDialog("Error", "Please select a valid CSV file", "OK");
            return;
        }
        
        Debug.Log("Starting action card import...");
        
        // Create directories if they don't exist
        string cardsPath = "Assets/Cards";
        string actionPath = "Assets/Cards/Actions";
        
        if (!AssetDatabase.IsValidFolder(cardsPath))
            AssetDatabase.CreateFolder("Assets", "Cards");
        
        if (!AssetDatabase.IsValidFolder(actionPath))
            AssetDatabase.CreateFolder("Assets/Cards", "Actions");
        
        // Read CSV file
        string csvContent = File.ReadAllText(csvFilePath);
        Debug.Log($"CSV Content length: {csvContent.Length}");
        
        // Split into lines and clean them
        string[] allLines = csvContent.Split('\n');
        Debug.Log($"Total lines in file: {allLines.Length}");
        
        if (allLines.Length < 2)
        {
            EditorUtility.DisplayDialog("Error", "CSV file appears to be empty or has no data rows", "OK");
            return;
        }
        
        // Get header line (first line)
        string headerLine = allLines[0].Trim().Replace("\r", "");
        Debug.Log($"Header line: '{headerLine}'");
        
        // Simple split by comma for headers
        string[] headers = headerLine.Split(',');
        for (int i = 0; i < headers.Length; i++)
        {
            headers[i] = headers[i].Trim().Replace("\"", "");
        }
        
        Debug.Log($"Found {headers.Length} headers: {string.Join(" | ", headers)}");
        
        // Find column indices
        int typeIndex = FindColumnIndex(headers, "Type");
        int itemIndex = FindColumnIndex(headers, "Item");
        int playerIndex = FindColumnIndex(headers, "Player");
        int fishIndex = FindColumnIndex(headers, "Fish");
        int descriptionIndex = FindColumnIndex(headers, "Description");
        int effectIndex = FindColumnIndex(headers, "Effect");
        
        Debug.Log($"Column indices - Type: {typeIndex}, Item: {itemIndex}, Player: {playerIndex}, Fish: {fishIndex}");
        
        int importedCount = 0;
        int skippedCount = 0;
        
        // Process each data line (skip header)
        for (int lineIndex = 1; lineIndex < allLines.Length; lineIndex++)
        {
            string line = allLines[lineIndex].Trim().Replace("\r", "");
            if (string.IsNullOrEmpty(line)) continue;
            
            Debug.Log($"Processing line {lineIndex}: '{line.Substring(0, System.Math.Min(80, line.Length))}...'");
            
            // Parse CSV line (handle quotes properly)
            string[] values = ParseCSVLine(line);
            
            Debug.Log($"Split into {values.Length} values. Action name: '{GetValue(values, itemIndex)}'");
            
            if (values.Length < 6) // Need at least Type, Item, Player, Fish, Description
            {
                Debug.LogWarning($"Line {lineIndex} has too few values ({values.Length}), skipping");
                skippedCount++;
                continue;
            }
            
            // Get action name from Item column
            string actionName = GetValue(values, itemIndex);
            if (string.IsNullOrEmpty(actionName))
            {
                Debug.LogWarning($"Line {lineIndex} has empty action name, skipping");
                skippedCount++;
                continue;
            }
            
            // Get the type (Action or Effect)
            string cardType = GetValue(values, typeIndex);
            Debug.Log($"Creating {cardType} card for: '{actionName}'");
            
            // Create new ActionCard
            ActionCard actionCard = ScriptableObject.CreateInstance<ActionCard>();
            
            // Set basic info
            actionCard.actionName = actionName;
            
            // Set player and fish effects
            if (float.TryParse(GetValue(values, playerIndex), out float playerEffect))
                actionCard.playerEffect = (int)playerEffect;
            
            if (float.TryParse(GetValue(values, fishIndex), out float fishEffect))
                actionCard.fishEffect = (int)fishEffect;
            
            // Set targeting based on non-zero effects
            actionCard.canTargetPlayer = actionCard.playerEffect != 0;
            actionCard.canTargetFish = actionCard.fishEffect != 0;
            
            // Set description (prefer Description over Effect column)
            string description = GetValue(values, descriptionIndex);
            if (string.IsNullOrEmpty(description))
                description = GetValue(values, effectIndex);
            actionCard.description = description;
            
            // Create safe filename with type prefix
            string prefix = cardType.ToLower() == "effect" ? "Effect_" : "Action_";
            string safeFileName = prefix + actionName;
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                safeFileName = safeFileName.Replace(c, '_');
            }
            
            // Create asset
            string fileName = $"{safeFileName}.asset";
            string assetPath = $"{actionPath}/{fileName}";
            
            Debug.Log($"Creating asset at: {assetPath}");
            
            AssetDatabase.CreateAsset(actionCard, assetPath);
            importedCount++;
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"Import complete! Created {importedCount} action cards, skipped {skippedCount} rows");
        EditorUtility.DisplayDialog("Import Complete", 
            $"Successfully imported {importedCount} action cards.\nSkipped {skippedCount} rows.\nSee console for details.", "OK");
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
}
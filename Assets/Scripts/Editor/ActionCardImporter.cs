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
        GUILayout.Label("4. Effect cards will be created in Assets/Cards/Effects/");
        GUILayout.Label("Note: Automatically separates Action and Effect type cards");
    }
    
    void ImportActionCards()
    {
        if (string.IsNullOrEmpty(csvFilePath) || !File.Exists(csvFilePath))
        {
            EditorUtility.DisplayDialog("Error", "Please select a valid CSV file", "OK");
            return;
        }
        
        // Debug.Log("Starting action and effect card import...");
        
        // Create directories if they don't exist
        string cardsPath = "Assets/Cards";
        string actionPath = "Assets/Cards/Actions";
        string effectPath = "Assets/Cards/Effects";
        
        if (!AssetDatabase.IsValidFolder(cardsPath))
            AssetDatabase.CreateFolder("Assets", "Cards");
        
        if (!AssetDatabase.IsValidFolder(actionPath))
            AssetDatabase.CreateFolder("Assets/Cards", "Actions");
        
        if (!AssetDatabase.IsValidFolder(effectPath))
            AssetDatabase.CreateFolder("Assets/Cards", "Effects");
        
        // Read CSV file
        string csvContent = File.ReadAllText(csvFilePath);
        // Debug.Log($"CSV Content length: {csvContent.Length}");
        
        // Split into lines and clean them
        string[] allLines = csvContent.Split('\n');
        // Debug.Log($"Total lines in file: {allLines.Length}");
        
        if (allLines.Length < 2)
        {
            EditorUtility.DisplayDialog("Error", "CSV file appears to be empty or has no data rows", "OK");
            return;
        }
        
        // Get header line (first line)
        string headerLine = allLines[0].Trim().Replace("\r", "");
        // Debug.Log($"Header line: '{headerLine}'");
        
        // Simple split by comma for headers
        string[] headers = headerLine.Split(',');
        for (int i = 0; i < headers.Length; i++)
        {
            headers[i] = headers[i].Trim().Replace("\"", "");
        }
        
        // Debug.Log($"Found {headers.Length} headers: {string.Join(" | ", headers)}");
        
        // Find column indices
        int typeIndex = FindColumnIndex(headers, "Type");
        int itemIndex = FindColumnIndex(headers, "Item");
        int playerIndex = FindColumnIndex(headers, "Player");
        int fishIndex = FindColumnIndex(headers, "Fish");
        int descriptionIndex = FindColumnIndex(headers, "Description");
        int effectIndex = FindColumnIndex(headers, "Effect");
        
        // Debug.Log($"Column indices - Type: {typeIndex}, Item: {itemIndex}, Player: {playerIndex}, Fish: {fishIndex}");
        
        int actionCount = 0;
        int effectCount = 0;
        int skippedCount = 0;
        
        // Process each data line (skip header)
        for (int lineIndex = 1; lineIndex < allLines.Length; lineIndex++)
        {
            string line = allLines[lineIndex].Trim().Replace("\r", "");
            if (string.IsNullOrEmpty(line)) continue;
            
            // Debug.Log($"Processing line {lineIndex}: '{line.Substring(0, System.Math.Min(80, line.Length))}...'");
            
            // Parse CSV line (handle quotes properly)
            string[] values = ParseCSVLine(line);
            
            if (values.Length < 6) // Need at least Type, Item, Player, Fish, Description
            {
                // Debug.LogWarning($"Line {lineIndex} has too few values ({values.Length}), skipping");
                skippedCount++;
                continue;
            }
            
            // Get card name and type
            string cardName = GetValue(values, itemIndex);
            string cardType = GetValue(values, typeIndex);
            
            if (string.IsNullOrEmpty(cardName))
            {
                // Debug.LogWarning($"Line {lineIndex} has empty card name, skipping");
                skippedCount++;
                continue;
            }
            
            // Debug.Log($"Creating {cardType} card for: '{cardName}'");
            
            // Create appropriate card based on type
            if (cardType.Equals("Action", System.StringComparison.OrdinalIgnoreCase))
            {
                CreateActionCard(values, headers, actionPath);
                actionCount++;
            }
            else if (cardType.Equals("Effect", System.StringComparison.OrdinalIgnoreCase))
            {
                CreateEffectCard(values, headers, effectPath);
                effectCount++;
            }
            else
            {
                // Debug.LogWarning($"Unknown card type '{cardType}' for {cardName}, skipping");
                skippedCount++;
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        // Debug.Log($"Import complete! Created {actionCount} action cards, {effectCount} effect cards, skipped {skippedCount} rows");
        EditorUtility.DisplayDialog("Import Complete", 
            $"Successfully imported:\n{actionCount} Action Cards\n{effectCount} Effect Cards\nSkipped {skippedCount} rows.\n\nSee console for details.", "OK");
    }
    
    void CreateActionCard(string[] values, string[] headers, string outputPath)
    {
        int typeIndex = FindColumnIndex(headers, "Type");
        int itemIndex = FindColumnIndex(headers, "Item");
        int playerIndex = FindColumnIndex(headers, "Player");
        int fishIndex = FindColumnIndex(headers, "Fish");
        int descriptionIndex = FindColumnIndex(headers, "Description");
        int effectIndex = FindColumnIndex(headers, "Effect");
        
        string actionName = GetValue(values, itemIndex);
        
        // Create new ActionCard
        ActionCard actionCard = ScriptableObject.CreateInstance<ActionCard>();
        
        // Set basic info
        actionCard.actionName = actionName;
        
        // Try to find and assign the action image
        Sprite actionSprite = FindActionImage(actionName);
        if (actionSprite != null)
        {
            actionCard.actionImage = actionSprite;
            // Debug.Log($"Found image for {actionName}");
        }
        else
        {
            // Debug.LogWarning($"No image found for action: {actionName}");
        }
        
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
        
        // Create safe filename
        string safeFileName = "Action_" + actionName;
        char[] invalidChars = Path.GetInvalidFileNameChars();
        foreach (char c in invalidChars)
        {
            safeFileName = safeFileName.Replace(c, '_');
        }
        
        // Create asset
        string fileName = $"{safeFileName}.asset";
        string assetPath = $"{outputPath}/{fileName}";
        
        // Debug.Log($"Creating action card asset at: {assetPath}");
        
        AssetDatabase.CreateAsset(actionCard, assetPath);
    }
    
    void CreateEffectCard(string[] values, string[] headers, string outputPath)
    {
        int typeIndex = FindColumnIndex(headers, "Type");
        int itemIndex = FindColumnIndex(headers, "Item");
        int playerIndex = FindColumnIndex(headers, "Player");
        int fishIndex = FindColumnIndex(headers, "Fish");
        int descriptionIndex = FindColumnIndex(headers, "Description");
        int effectIndex = FindColumnIndex(headers, "Effect");
        
        string effectName = GetValue(values, itemIndex);
        string effectDescription = GetValue(values, effectIndex); // Effect cards use the "Effect" column for their main description
        
        // Create new EffectCard
        EffectCard effectCard = ScriptableObject.CreateInstance<EffectCard>();
        
        // Set basic info
        effectCard.effectName = effectName;
        effectCard.description = GetValue(values, descriptionIndex);
        
        // Try to find and assign the effect image
        Sprite effectSprite = FindEffectImage(effectName);
        if (effectSprite != null)
        {
            effectCard.effectImage = effectSprite;
            // Debug.Log($"Found image for {effectName}");
        }
        else
        {
            // Debug.LogWarning($"No image found for effect: {effectName}");
        }
        
        // Try to automatically detect effect type based on the effect description
        EffectType detectedType = DetectEffectType(effectDescription);
        effectCard.effectType = detectedType;
        
        // Try to parse repair amounts for repair effects
        if (detectedType == EffectType.Repair)
        {
            int repairAmount = ExtractRepairAmount(effectDescription);
            if (repairAmount > 0)
            {
                effectCard.repairAmount = repairAmount;
                effectCard.repairHalfDamage = false;
            }
            else if (effectDescription.ToLower().Contains("half"))
            {
                effectCard.repairHalfDamage = true;
                effectCard.repairAmount = 0;
            }
        }
        
        // Most effect cards are single use
        effectCard.singleUse = true;
        effectCard.canUseAnyTime = true;
        
        // Create safe filename
        string safeFileName = "Effect_" + effectName;
        char[] invalidChars = Path.GetInvalidFileNameChars();
        foreach (char c in invalidChars)
        {
            safeFileName = safeFileName.Replace(c, '_');
        }
        
        // Create asset
        string fileName = $"{safeFileName}.asset";
        string assetPath = $"{outputPath}/{fileName}";
        
        // Debug.Log($"Creating effect card asset at: {assetPath}");
        
        AssetDatabase.CreateAsset(effectCard, assetPath);
    }
    
    EffectType DetectEffectType(string effectDescription)
    {
        if (string.IsNullOrEmpty(effectDescription))
            return EffectType.Utility;
        
        string lowerEffect = effectDescription.ToLower();
        
        // Check for repair/healing effects
        if (lowerEffect.Contains("heal") || lowerEffect.Contains("repair") || 
            lowerEffect.Contains("damage") && (lowerEffect.Contains("remove") || lowerEffect.Contains("heal")))
        {
            return EffectType.Repair;
        }
        
        // Check for protection effects
        if (lowerEffect.Contains("block") || lowerEffect.Contains("protect") || 
            lowerEffect.Contains("prevent") || lowerEffect.Contains("absorb"))
        {
            return EffectType.Protection;
        }
        
        // Check for persistent effects (things that stay in play)
        if (lowerEffect.Contains("takes") && lowerEffect.Contains("damage") || 
            lowerEffect.Contains("destroyed") || lowerEffect.Contains("bot"))
        {
            return EffectType.Persistent;
        }
        
        // Default to utility
        return EffectType.Utility;
    }
    
    int ExtractRepairAmount(string effectDescription)
    {
        if (string.IsNullOrEmpty(effectDescription))
            return 0;
        
        // Look for patterns like "Heals 3 Damage" or "Heal 5 Damage"
        string[] words = effectDescription.Split(' ');
        for (int i = 0; i < words.Length - 1; i++)
        {
            if (words[i].ToLower().Contains("heal") && int.TryParse(words[i + 1], out int amount))
            {
                return amount;
            }
        }
        
        return 0;
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
    
    Sprite FindActionImage(string actionName)
    {
        // Look for action images in Assets/Images/Actions/
        string[] guids = AssetDatabase.FindAssets($"{actionName} t:Sprite", new[] { "Assets/Images/Actions" });
        
        if (guids.Length > 0)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }
        
        // Also try a more general search
        guids = AssetDatabase.FindAssets($"{actionName} t:Sprite");
        if (guids.Length > 0)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }
        
        return null;
    }
    
    Sprite FindEffectImage(string effectName)
    {
        // Look for effect images in Assets/Images/Effects/
        string[] guids = AssetDatabase.FindAssets($"{effectName} t:Sprite", new[] { "Assets/Images/Effects" });
        
        if (guids.Length > 0)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }
        
        // Also try searching in Actions folder (in case they're mixed)
        guids = AssetDatabase.FindAssets($"{effectName} t:Sprite", new[] { "Assets/Images/Actions" });
        if (guids.Length > 0)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }
        
        // Try a general search as fallback
        guids = AssetDatabase.FindAssets($"{effectName} t:Sprite");
        if (guids.Length > 0)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }
        
        return null;
    }
}
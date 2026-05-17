using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Tanki.Models;

namespace Tanki.Editor
{
    public class RankSettingsProcessor : EditorWindow
    {
        private const string RanksPath = "Assets/Textures/UI/images/ranks";
        private const string SettingsPath = "Assets/Data/RankSettings.asset";

        [MenuItem("Tanki/Process Rank Icons")]
        public static void ProcessRanks()
        {
            // 1. Ensure texture settings are correct (Sprite 2D and UI)
            ConfigureTexturesAsSprites(Path.Combine(RanksPath, "DefaultRanksSmallRank"));
            ConfigureTexturesAsSprites(Path.Combine(RanksPath, "PremiumRankSmallRank"));

            // AssetDatabase.Refresh();

            // 2. Load or Create RankSettingsSO
            RankSettingsSO settings = AssetDatabase.LoadAssetAtPath<RankSettingsSO>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<RankSettingsSO>();
                if (!Directory.Exists("Assets/Data")) Directory.CreateDirectory("Assets/Data");
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            // 3. Populate sprites
            settings.defaultRanksSmall = LoadSpritesFromFolder(Path.Combine(RanksPath, "DefaultRanksSmallRank"));
            settings.premiumRanksSmall = LoadSpritesFromFolder(Path.Combine(RanksPath, "PremiumRankSmallRank"));

            Debug.Log($"[RankProcessor] Finished. Default={settings.defaultRanksSmall?.Length}, Premium={settings.premiumRanksSmall?.Length}");

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            Debug.Log($"[RankProcessor] Successfully processed ranks. Settings saved to {SettingsPath}");
            EditorUtility.DisplayDialog("Success", $"Processed {settings.defaultRanksSmall?.Length} icons!", "OK");
        }

        private static void ConfigureTexturesAsSprites(string folderPath)
        {
            string[] files = Directory.GetFiles(folderPath, "*.png");
            string dataPath = Application.dataPath.Replace('\\', '/');
            foreach (string file in files)
            {
                string normalizedFile = file.Replace('\\', '/');
                string relativePath = normalizedFile.Replace(dataPath, "Assets");
                
                TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
                if (importer != null && importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.mipmapEnabled = false;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                }
            }
        }

        private static Sprite[] LoadSpritesFromFolder(string folderPath)
        {
            string unityPath = folderPath.Replace('\\', '/');
            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { unityPath });
            Debug.Log($"[RankProcessor] Found {guids.Length} sprites in {unityPath}");
            
            List<Sprite> sprites = new List<Sprite>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null) sprites.Add(sprite);
            }
            Debug.Log($"[RankProcessor] Successfully loaded {sprites.Count} sprites from {unityPath}");

            // Sort by name to ensure correct order (01, 02, etc.)
            return sprites.OrderBy(s => s.name).ToArray();
        }
    }
}

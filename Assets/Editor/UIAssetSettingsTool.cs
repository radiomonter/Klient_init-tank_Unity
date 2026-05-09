using UnityEngine;
using UnityEditor;
using System.IO;

namespace Tanki.Editor
{
    public class UIAssetSettingsTool : EditorWindow
    {
        [MenuItem("Tanki/UI/Configure UI Sprites")]
        public static void ConfigureSprites()
        {
            string path = "Assets/Textures/UI";
            if (!Directory.Exists(path))
            {
                Debug.LogError($"Directory {path} not found!");
                return;
            }

            string[] files = Directory.GetFiles(path, "*.png", SearchOption.AllDirectories);
            int count = 0;

            foreach (string file in files)
            {
                string relativePath = file.Replace(Application.dataPath, "Assets");
                TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;

                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    
                    // Specific settings for 9-slice
                    if (file.Contains("Window") || file.Contains("Input") || file.Contains("button"))
                    {
                        importer.spriteBorder = new Vector4(12, 12, 12, 12);
                    }

                    importer.SaveAndReimport();
                    count++;
                }
            }

            Debug.Log($"[Tanki] Configured {count} sprites in {path}");
        }
    }
}

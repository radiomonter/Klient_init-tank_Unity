using UnityEngine;
using UnityEditor;

namespace Tanki.Editor
{
    public class UIAssetConfigurator : EditorWindow
    {
        [MenuItem("Tanki/UI/Configure Sprites")]
        public static void ConfigureSprites()
        {
            string path = "Assets/Textures/UI/images/";
            string[] guids = AssetDatabase.FindAssets("t:Texture", new[] { path });

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.filterMode = FilterMode.Point;
                    importer.wrapMode = TextureWrapMode.Repeat;
                    
                    AssetDatabase.ImportAsset(assetPath);
                }
            }
            Debug.Log("[UI Config] UI sprites configured.");
        }
    }
}

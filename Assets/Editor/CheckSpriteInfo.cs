using UnityEditor;
using UnityEngine;

public class CheckSpriteInfo
{
    [MenuItem("Tanki/Debug/Check and Fix Progress Sprites")]
    public static void Check()
    {
        string[] names = { "ProgressBarLeftRight", "ProgressBarLeft", "ProgressBarCentr", "ProgressBarRight" };
        foreach (var n in names)
        {
            string[] guids = AssetDatabase.FindAssets(n + " t:texture");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    bool changed = false;
                    if (n.Contains("Centr"))
                    {
                        if (importer.wrapMode != TextureWrapMode.Repeat) { importer.wrapMode = TextureWrapMode.Repeat; changed = true; }
                        if (importer.spriteBorder != Vector4.zero) { importer.spriteBorder = Vector4.zero; changed = true; }
                    }
                    
                    if (changed)
                    {
                        importer.SaveAndReimport();
                    }
                    
                    Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (tex != null)
                    {
                        Debug.Log($"[Check] {n}: {tex.width}x{tex.height}, Wrap={importer.wrapMode}, Border={importer.spriteBorder}");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[Check] Asset NOT FOUND: {n}");
            }
        }
        AssetDatabase.Refresh();
    }
}

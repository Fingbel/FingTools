using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace FingTools.Internal
{
public class TiledWorldAssetProcessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string assetPath in importedAssets)
        {
            if (assetPath.EndsWith(".world"))
            {
                Debug.Log($"World asset imported: {assetPath}");
                MapManager.RefreshUniverse();
            }
             if (assetPath.EndsWith(".tiled-session"))
            {
                // Retrieve the AssetImporter for this asset
                AssetImporter importer = AssetImporter.GetAtPath(assetPath);
                
                // Set the importer to not import the asset by clearing its import settings
                importer.assetBundleName = null; 
                importer.userData = "Ignored"; 

                // Returning here prevents Unity from further processing the asset
                return;

            }
        }

        foreach (string assetPath in deletedAssets)
        {
            if (assetPath.EndsWith(".world"))
            {
                Debug.Log($"World asset deleted: {assetPath}");
                MapManager.RefreshUniverse();
            }
        } 
        
    }
}
}
#endif
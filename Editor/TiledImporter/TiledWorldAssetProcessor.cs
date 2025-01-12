using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
public class TiledWorldAssetProcessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string assetPath in importedAssets)
        {
            if (assetPath.EndsWith(".world"))
            {
                Debug.Log($"World asset imported: {assetPath}");
                MapManager.RefreshMaps();
            }
        }

        foreach (string assetPath in deletedAssets)
        {
            if (assetPath.EndsWith(".world"))
            {
                Debug.Log($"World asset deleted: {assetPath}");
                MapManager.RefreshMaps();
            }
        }       
    }
}
#endif
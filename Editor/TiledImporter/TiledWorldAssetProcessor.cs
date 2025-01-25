using UnityEditor;

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
               Refresh(assetPath);
            }
            if (assetPath.EndsWith(".tmx"))
            {
                TiledLinker.CheckForNPCAttribute(assetPath);                    
                Refresh(assetPath);                     
                AssetDatabase.SaveAssets();          
                AssetDatabase.Refresh();               
            }
                    
            }

        foreach (string assetPath in deletedAssets)
        {
            if (assetPath.EndsWith(".world"))
            {
                Refresh(assetPath);
            }

            if(assetPath.EndsWith(".tmx"))
            {                
                Refresh(assetPath);
                
            }
        } 
        
    }

        private static void Refresh(string assetPath)
        {
            MapManager.RefreshUniverse();
            if (MapLoader.IsInitialized)
                MapLoader.RefreshMapObjects();
        }
    }
}
#endif
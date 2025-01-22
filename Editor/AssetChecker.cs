using UnityEditor;
using UnityEngine;
using System.IO;
using FingTools.Internal;

#if UNITY_EDITOR
using UnityEngine.SceneManagement;

namespace FingTools.Tiled
{
    public static class AssetChecker
    {
        
        internal static bool MapLoaderInitRefresh()
        {
            if (MapLoader.IsInitialized)
            {
                MapLoader.RefreshMapObjects();
                return true;
            }
            else
            {
                if (EditorUtility.DisplayDialog("MissingMapLoader", "An action requiring a MapLoader has been detected and no MapLoader has been found in the active scene.", $"Add MapLoader to {SceneManager.GetActiveScene().name}", "Cancel"))
                {
                    var man = MapLoader.Instance;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        internal static bool CheckForTiledProject()
        {
            string projectPath = Path.Combine(Application.dataPath, "FingTools", "Tiled", $"TiledProject.tiled-project");
            bool tiledProjectFileDetected = File.Exists(projectPath);
            if (!tiledProjectFileDetected)
            {
                if (EditorUtility.DisplayDialog("Map Loader", "No tilesets have been imported yet, would you like to import some ?", "Yes", "No"))
                {
                    TiledImporterEditorWindow.ShowWindow();
                    return false;
                }
                return false;
            }
            else
            {
                return true;
            }
        }
        internal static bool CheckForMaps()
        {
            bool mapDetected = MapManager.Instance.HasMaps();
            if (mapDetected)
            {
                return true;
            }
            else
            {
                if (EditorUtility.DisplayDialog("Map Loader", "No Tiled maps have been created yet, would you like to create one ?", "Yes", "No"))
                {
                    CreateNewTiledMapWindow.ShowWindow();
                    
                }                    
            }
            return false;
        }
        internal static bool CheckForTilesets()
        {
            string projectPath = Path.Combine(Application.dataPath, "FingTools", "Tiled", $"TiledProject.tiled-project");
            bool tilesetDetected = File.Exists(projectPath);
            if (!tilesetDetected)
            {
                if (EditorUtility.DisplayDialog("Tiled Loader", "No tilesets have been imported yet, would you like to import some ?", "Yes", "No"))
                {
                    TiledImporterEditorWindow.ShowWindow();
                    
                }
                return false;
            }
            else return true;
        }
        internal static bool CheckForSpriteManager()
        {
            if (Directory.Exists("Assets/Resources/FingTools"))
            {
                var manager = Resources.Load<SpriteManager>("FingTools/SpriteManager");
                if (manager?.HasAssetsImported() == true)
                {
                    return true;
                }
            }
            if (EditorUtility.DisplayDialog("Actor Editor", "No character assets have been imported yet. Would you like to import assets now?", "Yes", "No"))
            {
                CharacterImporterEditorWindow.ShowWindow();
                return false;
            }
            else
            {
                return false;
            }
        }
    }
#endif
}
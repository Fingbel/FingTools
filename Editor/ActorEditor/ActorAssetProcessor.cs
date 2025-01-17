using System;
using System.IO;
using FingTools.Internal;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
public class ActorPostProcessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string assetPath in deletedAssets)
        {
            FileInfo fi = null;
            try
            {
            fi = new System.IO.FileInfo(assetPath);
            }
            catch (ArgumentException) { }
            catch (PathTooLongException) { }
            catch (NotSupportedException) { }
            if (ReferenceEquals(fi, null))
            {
            }
            else
            {                        
            var path = Path.GetDirectoryName(assetPath);
            var fileName = Path.GetFileNameWithoutExtension(assetPath);
            var portraitsPath = "Assets/Resources/FingTools/Portraits/";
            if(path == "Assets\\Resources\\FingTools\\Actors")
            {
                var portraitPath = Path.Combine(portraitsPath,fileName+".asset");
                bool isDeleted = AssetDatabase.DeleteAsset(portraitPath);                
                if(!isDeleted)
                {
                    Debug.LogError($"We failed to delete a portrait named :{fileName}, from {path}");
                }
            }
            }
        }

    }
}
#endif
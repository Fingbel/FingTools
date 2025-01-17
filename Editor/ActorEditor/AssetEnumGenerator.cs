using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
#if UNITY_EDITOR
namespace FingTools.Internal
{
    public class AssetEnumGenerator : EditorWindow
    {
        private const string GeneratedPath = "Assets/Resources/Fingtools/GeneratedAssetEnum.cs";
        public static void GenerateAssetEnum()
        {
            string[] guids = AssetDatabase.FindAssets("t:SpritePart_SO");
            Dictionary<ActorPartType,List<string>> assets = new();

            foreach (var guid in guids)
            {                
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if(path.StartsWith("Assets/copy")) continue;
                SpritePart_SO spritePart = AssetDatabase.LoadAssetAtPath<SpritePart_SO>(path);

                if(!assets.ContainsKey(spritePart.type))
                {                
                    assets.Add(spritePart.type,new List<string>());    
                }
                assets[spritePart.type].Add(spritePart.name);
                                            
            }

            Directory.CreateDirectory("Assets/Resources/Fingtools");
            string output = "";
            foreach(var assetType in assets.Keys)
            {
                string enumContent = "public enum " +assetType+"Assets\n{\n";
                foreach (var asset in assets[assetType])
                {
                    enumContent += $"    {asset},\n";
                }
                enumContent += "}\n";
                output += enumContent;
            }  
            File.WriteAllText(GeneratedPath, output);
            AssetDatabase.Refresh();

            Debug.Log("Asset enum generated at " + GeneratedPath);
        }
    }
}
#endif
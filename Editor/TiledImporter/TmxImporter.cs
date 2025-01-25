#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using FingTools.Internal;


#if SUPER_TILED2UNITY_INSTALLED
using SuperTiled2Unity.Editor;

[AutoCustomTmxImporter()]
public class TmxImporter : CustomTmxImporter
 {
    private TmxAssetImportedArgs m_ImportedArgs;

    public override void TmxAssetImported(TmxAssetImportedArgs args)
    {
        var map = args.ImportedSuperMap;
        
        int tileSize;
        if (EditorPrefs.HasKey("TileSize"))
        {
            tileSize = EditorPrefs.GetInt("TileSize");
        }
        else
        {
            tileSize = 16;
        }
        if(args.AssetImporter.PixelsPerUnit != tileSize)
            UpdatePPU(args.AssetImporter, tileSize);        
        string tempMapName = map.name;
        EditorApplication.delayCall += () =>
        {            
            NPCManager.RefreshNPCSpawners();
            MapManager.RefreshUniverse();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        };
    }

    public static void UpdatePPU(TmxAssetImporter importer,int pixelsPerUnit)
    {           
        // Use SerializedObject to modify properties
        SerializedObject serializedObject = new SerializedObject(importer);
        SerializedProperty pixelsPerUnitProp = serializedObject.FindProperty("m_PixelsPerUnit");

        if (pixelsPerUnitProp != null)
        {
            float newValue = pixelsPerUnit;
            pixelsPerUnitProp.floatValue = newValue;
            serializedObject.ApplyModifiedProperties();

            // Force reimport to apply changes
            AssetDatabase.ImportAsset(importer.assetPath, ImportAssetOptions.ForceUpdate);
        }
    }        
 }
#endif
#endif
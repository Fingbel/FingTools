using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
#if SUPER_TILED2UNITY_INSTALLED
using SuperTiled2Unity.Editor;

[AutoCustomTmxImporter()]
 public class TmxImporter : CustomTmxImporter
 {
    public override void TmxAssetImported(TmxAssetImportedArgs args)
    {
        int tileSize;
        var map = args.ImportedSuperMap;
        if (EditorPrefs.HasKey("TileSize"))
        {
            tileSize = EditorPrefs.GetInt("TileSize");
        }
        else
        {
            tileSize = 16;
        }
        UpdatePPU(args.AssetImporter, tileSize);
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

            Debug.Log($"Successfully updated Pixels Per Unit to {newValue} for {importer}");
        }
        else
        {
            Debug.LogWarning("Property 'm_PixelsPerUnit' not found. Ensure the importer supports this property.");
        }
    }        
 }
 #endif
#endif
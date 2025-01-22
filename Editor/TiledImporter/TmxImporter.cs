#if UNITY_EDITOR
using UnityEditor;
using SuperTiled2Unity;
using System.Linq;
using UnityEngine;

#if SUPER_TILED2UNITY_INSTALLED
using SuperTiled2Unity.Editor;

[AutoCustomTmxImporter()]
 public class TmxImporter : CustomTmxImporter
 {
    private TmxAssetImportedArgs m_ImportedArgs;

    public override void TmxAssetImported(TmxAssetImportedArgs args)
    {
        m_ImportedArgs = args;
        var NPCSpawner = m_ImportedArgs.ImportedSuperMap.GetComponentsInChildren<SuperObjectLayer>().Where(o => o.m_TiledName == "NPCSpawner");
        foreach(var spawner in NPCSpawner)
        {
            for(int i=0;i<spawner.transform.childCount;i++)
            {
                var obj = spawner.transform.GetChild(i).GetComponent<SuperObject>();
                
            }
        }
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
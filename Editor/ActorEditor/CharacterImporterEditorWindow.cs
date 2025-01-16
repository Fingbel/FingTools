using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using FingTools.Helper;

#if UNITY_EDITOR
namespace FingTools.Internal
{  
public class CharacterImporterEditorWindow : EditorWindow
{
    private const string InteriorZipFilePathKey = "InteriorZipFilePath";
    private const string ExteriorZipFilePathKey = "ExteriorZipFilePath";
    private string intZipFilePath = "";
    private string extZipFilePath = "";
    private string resourcesFolderPath = "Assets/Resources/FingTools/Sprites"; // Hardcoded output folder
    private int selectedSizeIndex = 0; 
    private readonly List<string> validBodyParts = new () { "Accessories","Accessory", "Bodies", "Eyes", "Hairstyles", "Outfits","Outfit" };
    private readonly List<string> validSizes = new () { "16", "32"};
    private int unzipedAssets = 0;
    private bool enableMaxAssetsPerType = false;   
    private readonly List<int>  spritesPerRowList = new List<int> { 4, 24, 24, 6, 12, 12, 12, 12, 24, 48, 40, 56, 56, 24, 24, 24, 16, 24, 12, 12 };
    private int maxAssetsPerType = 3;
    
    [MenuItem("FingTools/Importer/Character Importer",false,20)]
    public static void ShowWindow()
    {
        GetWindow<CharacterImporterEditorWindow>(true,"Character Importer");
    }

    public void OnEnable()
    {
        intZipFilePath = EditorPrefs.GetString(InteriorZipFilePathKey, null);
        extZipFilePath = EditorPrefs.GetString(ExteriorZipFilePathKey, null);
    }
    public void OnDisable()
    {
        if (!string.IsNullOrEmpty(intZipFilePath))
        {
            EditorPrefs.SetString(InteriorZipFilePathKey, intZipFilePath);
        }
        if (!string.IsNullOrEmpty(extZipFilePath))
        {
            EditorPrefs.SetString(ExteriorZipFilePathKey, extZipFilePath);
        }
    }
    private void OnGUI()
    {
        bool sizeLocked = SpriteManager.Instance.HasAssetsImported();

        GUILayout.Space(15);
        //Interior zip file selection
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Modern Interior Zip File:", intZipFilePath ?? "None");
        if(!string.IsNullOrEmpty(intZipFilePath))
        {
            if(GUILayout.Button("X",GUILayout.Width(20)))
            {
                intZipFilePath = "";
                EditorPrefs.SetString(InteriorZipFilePathKey, intZipFilePath);
            }
        }
        GUILayout.EndHorizontal();
        if (GUILayout.Button("Select Modern Interior zip file")) 
        {            
            intZipFilePath = EditorUtility.OpenFilePanel("Select Modern Interior zip file", "", "zip");
        }        
        DrawSeparator();

        //Exterior zip file selection
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Modern Exterior Zip File:", extZipFilePath ?? "None");
        if(!string.IsNullOrEmpty(extZipFilePath))
        {
            if(GUILayout.Button("X",GUILayout.Width(20)))
            {
                extZipFilePath = "";
                EditorPrefs.SetString(ExteriorZipFilePathKey, extZipFilePath);
            }
        }
        GUILayout.EndHorizontal();
        if(GUILayout.Button(" Optional : Select Modern Exterior zip file "))
        {
            extZipFilePath = EditorUtility.OpenFilePanel("Select Modern Exterior zip file", "", "zip");
        }        
        DrawSeparator();
        
        //Test Mode
        enableMaxAssetsPerType = EditorGUILayout.Toggle("Test Mode", enableMaxAssetsPerType);
        if (enableMaxAssetsPerType)
        {
            EditorGUILayout.LabelField("This option is there if you want to test the tool without importing all the assets, set the number of assets per body part you want to import:",EditorStyles.wordWrappedLabel);
            maxAssetsPerType = EditorGUILayout.IntField("Assets per bodyPart", maxAssetsPerType);
            maxAssetsPerType = Mathf.Max(1, maxAssetsPerType);  // Ensure it's always a positive number
        }
        
        EditorGUI.BeginDisabledGroup(sizeLocked);       

        // Size selection
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Select the sprite size to import: (48x48 is not available at the moment)", EditorStyles.wordWrappedLabel);
        selectedSizeIndex = EditorGUILayout.Popup(selectedSizeIndex, validSizes.ToArray());
            
        SpriteManager.Instance.SelectedSizeIndex = selectedSizeIndex;        
        EditorGUI.EndDisabledGroup();

        if (intZipFilePath == "" && extZipFilePath == "")
        {
            EditorGUI.BeginDisabledGroup(true);
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Import Assets"))
        {
            Directory.CreateDirectory("Assets/Resources/FingTools/Actors");
            if (FingHelper.ValidateInteriorZipFile(intZipFilePath))
            {                           
                CharacterImporter.UnzipIntSprites(intZipFilePath, validSizes[selectedSizeIndex], ref unzipedAssets, enableMaxAssetsPerType, maxAssetsPerType, validBodyParts);            
            }        

            if(!string.IsNullOrEmpty(extZipFilePath) && FingHelper.ValidateExteriorZipFile(extZipFilePath))
            {            
                CharacterImporter.UnzipExtSprites(extZipFilePath, validSizes[selectedSizeIndex], ref unzipedAssets,enableMaxAssetsPerType,maxAssetsPerType, validBodyParts);
            }        
            AssetDatabase.Refresh();
            CharacterImporter.ProcessImportedAssets(resourcesFolderPath, unzipedAssets, spritesPerRowList, validSizes[selectedSizeIndex]);
            SpriteManager.Instance.PopulateSpriteLists(resourcesFolderPath);
            SpriteLibraryBuilder.BuildAllSpriteLibraries();
            Directory.CreateDirectory("Assets/Resources/FingTools/Actors");
            AssetEnumGenerator.GenerateAssetEnum();
        }
        EditorGUI.EndDisabledGroup();
    }
    private void DrawSeparator()
    {
        GUILayout.Space(5);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(5);
    }
}
}

#endif
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

#if UNITY_EDITOR
namespace FingTools.Internal
{  
public class CharacterImporterEditorWindow : EditorWindow
{
    private const string InteriorZipFilePathKey = "InteriorZipFilePath";
    private const string ExteriorZipFilePathKey = "ExteriorZipFilePath";
    private const string InterfaceZipFilePathKey = "InterfaceZipFilePath";
    private string intZipFilePath = "";
    private string extZipFilePath = "";
    private string uiZipFilePath = "";
    private int selectedSizeIndex = 0; 
    
    private readonly List<string> validSizes = new () { "16", "32"};
    private int unzipedAssets = 0;
    private bool enableMaxAssetsPerType = false;   
    
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
        uiZipFilePath = EditorPrefs.GetString(InterfaceZipFilePathKey, null);
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
        if(!string.IsNullOrEmpty(uiZipFilePath))
        {
            EditorPrefs.SetString(InterfaceZipFilePathKey,uiZipFilePath);
        }
    }
    private void OnGUI()
    {
        bool sizeLocked = SpriteManager.Instance.HasAssetsImported();

        GUILayout.Space(15);
        //Interior zip file selection
        if (GUILayout.Button("Select Modern Interior zip file")) 
        {            
            intZipFilePath = EditorUtility.OpenFilePanel("Select Modern Interior zip file", "", "zip");            
        }      
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
            bool intValid = FingHelper.ValidateInteriorZipFile(intZipFilePath);
            if (!string.IsNullOrEmpty(intZipFilePath) && intValid)
            {
                //Zip file is valid
            }
            else if(!string.IsNullOrEmpty(intZipFilePath) && !intValid)
            {
                if (EditorUtility.DisplayDialog("Wrong Zip File", "The Zip File you provided is incorrect", "Ok"))
                {
                    intZipFilePath = "";
                    EditorPrefs.SetString(InteriorZipFilePathKey, intZipFilePath);
                }
                ;
            }
            //Exterior zip file selection
            DrawSeparator();
        if(GUILayout.Button(" Optional : Select Modern Exterior zip file "))
        {
            extZipFilePath = EditorUtility.OpenFilePanel("Select Modern Exterior zip file", "", "zip");                   
        }    
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
            bool extValid = FingHelper.ValidateExteriorZipFile(extZipFilePath);
            if (!string.IsNullOrEmpty(extZipFilePath) && extValid)
            {
                //Zip file is valid
            }
            else if(!string.IsNullOrEmpty(extZipFilePath) && !intValid)
            {
                if (EditorUtility.DisplayDialog("Wrong Zip File", "The Zip File you provided is incorrect", "Ok"))
                {
                    extZipFilePath = "";
                    EditorPrefs.SetString(ExteriorZipFilePathKey, extZipFilePath);
                }
                ;
            }

            //User Interface zip file selection
            DrawSeparator();
        if(GUILayout.Button(" Optional : Select Modern UI zip file "))
        {
            uiZipFilePath = EditorUtility.OpenFilePanel("Select Modern UI zip file", "", "zip");            
        }  
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Modern UI Zip File:", uiZipFilePath ?? "None");
        if(!string.IsNullOrEmpty(uiZipFilePath))
        {
            if(GUILayout.Button("X",GUILayout.Width(20)))
            {
                uiZipFilePath = "";
                EditorPrefs.SetString(InterfaceZipFilePathKey, uiZipFilePath);
            }
        }
        GUILayout.EndHorizontal();              
        if(!string.IsNullOrEmpty(uiZipFilePath) && !FingHelper.ValidateUIZipFile(uiZipFilePath))
        {
            if(EditorUtility.DisplayDialog("Wrong Zip File", "The Zip File you provided is incorrect", "Ok"))
            {
                uiZipFilePath = "";
                EditorPrefs.SetString(InterfaceZipFilePathKey, uiZipFilePath);
            };      
        }
                
        //Test Mode
        DrawSeparator();
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

        if (intZipFilePath == "" && extZipFilePath == "" && uiZipFilePath == "")
        {
            EditorGUI.BeginDisabledGroup(true);
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Import Assets"))
            {
                Directory.CreateDirectory("Assets/Resources/FingTools/Actors");
                if (!string.IsNullOrEmpty(intZipFilePath) && FingHelper.ValidateInteriorZipFile(intZipFilePath))
                {
                    CharacterImporter.UnzipIntSprites(intZipFilePath, validSizes[selectedSizeIndex], ref unzipedAssets, enableMaxAssetsPerType, maxAssetsPerType);
                }

                if (!string.IsNullOrEmpty(extZipFilePath) && FingHelper.ValidateExteriorZipFile(extZipFilePath))
                {
                    CharacterImporter.UnzipExtSprites(extZipFilePath, validSizes[selectedSizeIndex], ref unzipedAssets, enableMaxAssetsPerType, maxAssetsPerType);
                }

                if (!string.IsNullOrEmpty(uiZipFilePath) && FingHelper.ValidateUIZipFile(uiZipFilePath))
                {
                    PortraitImporter.UnzipUISprites(uiZipFilePath, validSizes[selectedSizeIndex], enableMaxAssetsPerType, maxAssetsPerType);
                }
                AssetDatabase.Refresh();
                CharacterImporter.ProcessImportedAssets(validSizes[selectedSizeIndex]);
                if (!string.IsNullOrEmpty(uiZipFilePath) && FingHelper.ValidateUIZipFile(uiZipFilePath))
                {
                    PortraitImporter.ProcessImportedAsset(validSizes[selectedSizeIndex]);
                }
                CharacterImporter.LinkCharAssets();
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
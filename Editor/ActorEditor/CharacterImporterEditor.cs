using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using FingTools.Helper;


#if UNITY_EDITOR
using UnityEditor.U2D.Sprites;
namespace FingTools.Internal
{  
public class CharacterImporterEditor : EditorWindow
{
    private string intZipFilePath = "";
    private string extZipFilePath = "";
    private string resourcesFolderPath = "Assets/Resources/FingTools/Sprites"; // Hardcoded output folder
    private int selectedSizeIndex = 0; 
    private readonly List<string> validBodyParts = new () { "Accessories","Accessory", "Bodies", "Eyes", "Hairstyles", "Outfits","Outfit" };
    private readonly List<string> validSizes = new () { "16", "32"};
    private SpriteManager spriteManager;
    private int unzipedAssets = 0;
    private bool enableMaxAssetsPerType = false;   
    private readonly List<int>  spritesPerRowList = new List<int> { 4, 24, 24, 6, 12, 12, 12, 12, 24, 48, 40, 56, 56, 24, 24, 24, 16, 24, 12, 12 };
    private int maxAssetsPerType = 3;
    private Dictionary<ActorPartType, int> processedAssetsPerType = new Dictionary<ActorPartType, int>()
    {
        { ActorPartType.Accessories, 0 },
        { ActorPartType.Bodies, 0 },
        { ActorPartType.Eyes, 0 },
        { ActorPartType.Hairstyles, 0 },
        { ActorPartType.Outfits, 0 }
    };

    [MenuItem("FingTools/Importer/Character Importer",false,20)]
    public static void ShowWindow()
    {
        GetWindow<CharacterImporterEditor>("Character Importer");
    }

    private SpriteManager LoadSpriteManager()
    {
        if(!Directory.Exists("Assets/Resources/FingTools/"))
        {
            Directory.CreateDirectory("Assets/Resources/FingTools/");
        }        
        var _spriteManager = Resources.Load<SpriteManager>("FingTools/SpriteManager");
        if (_spriteManager == null)
        {
            // Create the SpriteManager if it doesn't exist
            _spriteManager = CreateInstance<SpriteManager>();
            AssetDatabase.CreateAsset(_spriteManager, "Assets/Resources/FingTools/SpriteManager.asset");            
            AssetDatabase.SaveAssets();
            return _spriteManager;
        }
        else
        {
            // Load previously saved paths and size index
            selectedSizeIndex = _spriteManager.SelectedSizeIndex;
        }
        return _spriteManager;
    }

    private void OnGUI()
{
    EditorGUILayout.HelpBox(
        "This tool imports sprites from Limezu's Modern Interior pack into Unity.\n\n" +
        "To use this tool:\n" +
        "1. Select the Modern Interior zip.\n" +
        "2. Choose a sprite size to import.\n" +
        "3. Click 'Import Assets'.\n\n" +
        "WARNING: This process may take a while depending on your hardware (~20 minutes). \n" +
        "However, this is a one-time operation.",
        MessageType.Info
    );

    GUILayout.Space(15);

    if (GUILayout.Button("Select Modern Interior zip file")) 
    {            
        intZipFilePath = EditorUtility.OpenFilePanel("Select Modern Interior zip file", "", "zip");
    }
    EditorGUILayout.LabelField("Interior Zip File:", intZipFilePath ?? "None");

    if(GUILayout.Button(" Optional : Select Modern Exterior zip file "))
    {
        extZipFilePath = EditorUtility.OpenFilePanel("Select Modern Exterior zip file", "", "zip");
    }
    EditorGUILayout.LabelField("Exterior Zip File:", extZipFilePath ?? "None");

    

    // Checkbox to enable maxAssetsPerType
    enableMaxAssetsPerType = EditorGUILayout.Toggle("Test Mode", enableMaxAssetsPerType);

    if (enableMaxAssetsPerType)
    {
        EditorGUILayout.LabelField("This option is there if you want to test the tool without importing all the assets, set the number of assets per body part you want to import:",EditorStyles.wordWrappedLabel);
        maxAssetsPerType = EditorGUILayout.IntField("Assets per bodyPart", maxAssetsPerType);
        maxAssetsPerType = Mathf.Max(1, maxAssetsPerType);  // Ensure it's always a positive number
    }

    if (spriteManager != null)
    {
        bool assetsImported = spriteManager.HasAssetsImported();
        EditorGUI.BeginDisabledGroup(assetsImported);
    }

    // Size selection
    EditorGUILayout.Space(10);
    EditorGUILayout.LabelField("Select the sprite size to import: (48x48 is not available at the moment)", EditorStyles.wordWrappedLabel);
    selectedSizeIndex = EditorGUILayout.Popup(selectedSizeIndex, validSizes.ToArray());

    if (spriteManager != null)
    {
        spriteManager.SelectedSizeIndex = selectedSizeIndex;
    }

    EditorGUI.EndDisabledGroup();

    if (intZipFilePath == "")
    {
        EditorGUI.BeginDisabledGroup(true);
    }

    EditorGUILayout.Space(10);

    if (GUILayout.Button("Import Assets"))
    {
        Directory.CreateDirectory("Assets/Resources/FingTools/Actors");
        spriteManager = LoadSpriteManager();
        if (FingHelper.ValidateInteriorZipFile(intZipFilePath))
        {           
            UnzipIntSprites(intZipFilePath, validSizes[selectedSizeIndex]);            
        }        

        if(!string.IsNullOrEmpty(extZipFilePath) && FingHelper.ValidateExteriorZipFile(extZipFilePath))
        {            
            UnzipExtSprites(extZipFilePath,validSizes[selectedSizeIndex]);
        }        
        AssetDatabase.Refresh();
        ProcessImportedAssets();
        spriteManager.PopulateSpriteLists(resourcesFolderPath);
        SpriteLibraryBuilder.BuildAllSpriteLibraries();
        Directory.CreateDirectory("Assets/Resources/FingTools/Actors");
        AssetEnumGenerator.GenerateAssetEnum();
    }
    EditorGUI.EndDisabledGroup();
}

    private void UnzipIntSprites(string zipFilePath, string spriteSize)
    {
        unzipedAssets = 0;
        ZipArchive archive = ZipFile.OpenRead(zipFilePath);
        
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            ActorPartType? type = validBodyParts.FirstOrDefault(x => entry.FullName.Contains(x)) switch
            {
                "Accessories" => ActorPartType.Accessories,
                "Bodies"     => ActorPartType.Bodies,
                "Eyes"       => ActorPartType.Eyes,
                "Hairstyles" => ActorPartType.Hairstyles,
                "Outfits"    => ActorPartType.Outfits,
                _            => null
            };

            if (type == null)
                continue;

            if (enableMaxAssetsPerType && processedAssetsPerType[type.Value] >= maxAssetsPerType)
            {
                //Debug.Log($"Reached the limit for {type.Value}");
                continue;
            }

            string sizeDir = $"{spriteSize}x{spriteSize}";
            string expectedPath = $"2_Characters/Character_Generator/{type}/{sizeDir}";
            
            if (entry.FullName.StartsWith(expectedPath) && entry.FullName.EndsWith(".png"))
            {
                string outputPath = $"Assets/Resources/FingTools/Sprites/{type}/";
                if (!Directory.Exists(outputPath))
                    Directory.CreateDirectory(outputPath);

                entry.ExtractToFile($"{outputPath}/{entry.Name}", true);

                processedAssetsPerType[type.Value]++;
                unzipedAssets++;
            }
        }
    }
    private void UnzipExtSprites(string zipFilePath, string spriteSize)
    {
       
        ZipArchive archive = ZipFile.OpenRead(zipFilePath);
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            ActorPartType? type = validBodyParts.FirstOrDefault(x => entry.FullName.Contains(x)) switch
            {
                "Accessory" => ActorPartType.Accessories, 
                "Outfit"    => ActorPartType.Outfits,
                _            => null
            };

            if (type == null)
                continue;
            string expectedPath = $"Modern_Exteriors_{spriteSize}x{spriteSize}/Character_Generator_Addons_{spriteSize}x{spriteSize}";
            if (entry.FullName.StartsWith(expectedPath) && entry.FullName.EndsWith(".png"))
            {
                string outputPath = $"Assets/Resources/FingTools/Sprites/{type}/";
                if (!Directory.Exists(outputPath))
                    Directory.CreateDirectory(outputPath);

                entry.ExtractToFile($"{outputPath}/{entry.Name}", true);
                unzipedAssets++;
            }
        }
    }
    private void ProcessImportedAssets()
    {
        string destinationFolderPath = "Assets/Resources/FingTools/Sprites/";
        string[] bodyPartFolders = Directory.GetDirectories(destinationFolderPath);
        int skipped = 0;
        int processed = 0;
        List<string> importList = new List<string>();
        // Iterate through body part folders and find assets
        foreach (string bodyPartFolder in bodyPartFolders)
        {            
            string[] assetFiles = Directory.GetFiles(bodyPartFolder, "*.png", SearchOption.AllDirectories);

            foreach (string assetFile in assetFiles)
            {
                var sprite = AssetDatabase.LoadAllAssetsAtPath(assetFile);
                var bodyPartType = bodyPartFolder.Split('/').Last();
                // Check if the asset has already been processed
                
                if (sprite.Length == 467 && File.Exists(Path.Combine("Assets/Resources/FingTools/ScriptableObjects/",bodyPartType,Path.GetFileNameWithoutExtension(assetFile))+".asset"))
                {                    
                    //Debug.Log($"Skipped asset : {Path.GetFileNameWithoutExtension(assetFile)}");
                    skipped++;
                    continue;
                }

                string relativeAssetPath = assetFile.Replace(Application.dataPath, "").Replace("\\", "/");
                ApplyImportSettings(AssetImporter.GetAtPath(relativeAssetPath) as TextureImporter);
                AutoSliceTexture(relativeAssetPath);
                importList.Add(relativeAssetPath);           
                EditorUtility.DisplayProgressBar("Processing Assets", $"Processing asset {processed}  ", 0.5f);                
                processed++;           
            }
            
        }
        EditorUtility.ClearProgressBar();
        foreach (var assetPath in importList)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }
        Debug.Log($"New assets processed and added : {processed}");
    }
    
    private void AutoSliceTexture(string assetPath)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (texture == null)
        {
            Debug.LogError($"Failed to load texture at path: {assetPath}");
            return;
        }

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(texture);
        dataProvider.InitSpriteEditorDataProvider();

        // Generate the sprite rect data for slicing
        SpriteRect[] spriteRects = GenerateSpriteRectData(texture.height, spritesPerRowList);

        dataProvider.SetSpriteRects(spriteRects);
        dataProvider.Apply();
    }

    private SpriteRect[] GenerateSpriteRectData(int textureHeight, List<int> spritesPerRowList)
{
    List<SpriteRect> spriteRects = new List<SpriteRect>();
    int yOffset = 0;
    int sliceHeight = int.Parse(validSizes[selectedSizeIndex]) * 2;

    for (int row = 0; row < spritesPerRowList.Count; row++)
    {
        int spritesInRow = spritesPerRowList[row];
        int sliceWidth = int.Parse(validSizes[selectedSizeIndex]);

        for (int col = 0; col < spritesInRow; col++)
        {
            float x = col * sliceWidth;
            float y = textureHeight - (yOffset + sliceHeight);

            spriteRects.Add(new SpriteRect
            {
                rect = new Rect(x, y, sliceWidth, sliceHeight),
                pivot = new Vector2(0.5f, 0),
                name = $"Slice_{row}_{col}",
                alignment = SpriteAlignment.BottomCenter,
                border = Vector4.zero
            });
        }
        yOffset += sliceHeight;
    }

    return spriteRects.ToArray();
}
private void ApplyImportSettings(TextureImporter textureImporter)
{
    textureImporter.textureType = TextureImporterType.Sprite;
    textureImporter.spriteImportMode = SpriteImportMode.Multiple;
    textureImporter.mipmapEnabled = false;
    textureImporter.filterMode = FilterMode.Point;
    textureImporter.maxTextureSize = 4096;
    textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
    textureImporter.spritePixelsPerUnit = int.Parse(validSizes[selectedSizeIndex]);
}

}

}
#endif
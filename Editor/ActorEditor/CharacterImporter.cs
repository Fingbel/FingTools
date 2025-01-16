using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor.U2D.Sprites;
namespace FingTools.Internal
{
    public static class CharacterImporter
    {
        private const int slicePerSprite = 467;         
        public static void UnzipIntSprites(string zipFilePath, string spriteSize, ref int unzipedAssets, bool enableMaxAssetsPerType, int maxAssetsPerType, List<string> validBodyParts)
        {
            unzipedAssets = 0;     
            Dictionary<ActorPartType, int> processedAssetsPerType = new Dictionary<ActorPartType, int>()
            {
                { ActorPartType.Accessories, 0 },
                { ActorPartType.Bodies, 0 },
                { ActorPartType.Eyes, 0 },
                { ActorPartType.Hairstyles, 0 },
                { ActorPartType.Outfits, 0 }
            };       
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

        public static void UnzipExtSprites(string zipFilePath, string spriteSize, ref int unzipedAssets, List<string> validBodyParts)
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

        public static void ProcessImportedAssets(string destinationFolderPath, int unzipedAssets, List<int> spritesPerRowList, string selectedSize)
        {
            string[] bodyPartFolders = Directory.GetDirectories(destinationFolderPath);
            List<string> importList = new List<string>();
            int i = 0;        
            // Iterate through body part folders and find assets
            foreach (string bodyPartFolder in bodyPartFolders)
            {            
                string[] assetFiles = Directory.GetFiles(bodyPartFolder, "*.png", SearchOption.AllDirectories);

                foreach (string assetFile in assetFiles)
                {
                    string assetName = Path.GetFileNameWithoutExtension(assetFile);
                    string spritePartSoPath = Path.Combine("Assets/Resources/FingTools/ScriptableObjects/",bodyPartFolder.Split("\\").Last(), $"{assetName}.asset");

                    // Check if the asset has already been processed
                    if (AssetDatabase.LoadAllAssetsAtPath(assetFile).Length == slicePerSprite &&
                        File.Exists(spritePartSoPath))
                    {                  
                        continue;
                    }

                    importList.Add(assetFile);                 
                }
            }

            int totalAssetsToProcess = importList.Count;
            foreach (var assetFile in importList)
            {
                string relativeAssetPath = assetFile.Replace(Application.dataPath, "").Replace("\\", "/");
                ApplyImportSettings(AssetImporter.GetAtPath(relativeAssetPath) as TextureImporter, selectedSize);
                AutoSliceTexture(relativeAssetPath, spritesPerRowList, selectedSize);
                EditorUtility.DisplayProgressBar("Processing Assets", $"Slicing asset {i + 1} of {totalAssetsToProcess} ", (i + 1) / (float)totalAssetsToProcess);                
                i++;                 
            }
            
            EditorUtility.ClearProgressBar();
            foreach (var assetPath in importList)
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }
        }

        private static void AutoSliceTexture(string assetPath, List<int> spritesPerRowList, string selectedSize)
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
            SpriteRect[] spriteRects = GenerateSpriteRectData(texture.height, spritesPerRowList, selectedSize);

            dataProvider.SetSpriteRects(spriteRects);
            dataProvider.Apply();
        }

        private static SpriteRect[] GenerateSpriteRectData(int textureHeight, List<int> spritesPerRowList, string selectedSize)
        {
            List<SpriteRect> spriteRects = new List<SpriteRect>();
            int yOffset = 0;
            int sliceHeight = int.Parse(selectedSize) * 2;

            for (int row = 0; row < spritesPerRowList.Count; row++)
            {
                int spritesInRow = spritesPerRowList[row];
                int sliceWidth = int.Parse(selectedSize);

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

        private static void ApplyImportSettings(TextureImporter textureImporter, string selectedSize)
        {
            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.spriteImportMode = SpriteImportMode.Multiple;
            textureImporter.mipmapEnabled = false;
            textureImporter.filterMode = FilterMode.Point;
            textureImporter.maxTextureSize = 4096;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
            textureImporter.spritePixelsPerUnit = int.Parse(selectedSize);
        }
    }
}
#endif

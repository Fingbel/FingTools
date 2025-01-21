using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.U2D.Sprites;
namespace FingTools.Internal
{
public static class CommonImporter
{
    internal static void ApplyImportSettings(TextureImporter textureImporter, string selectedSize,int textureSize)
    {
        textureImporter.textureType = TextureImporterType.Sprite;
        textureImporter.spriteImportMode = SpriteImportMode.Multiple;
        textureImporter.mipmapEnabled = false;
        textureImporter.filterMode = FilterMode.Point;
        textureImporter.maxTextureSize = textureSize;
        textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
        textureImporter.spritePixelsPerUnit = int.Parse(selectedSize);
    }
    public static void AutoSliceTexture(string assetPath, List<int> spritesPerRowList, string selectedSize,bool isPortrait = false)
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
        SpriteRect[] spriteRects = GenerateSpriteRectData(texture.height, spritesPerRowList, selectedSize,isPortrait);

        dataProvider.SetSpriteRects(spriteRects);
        dataProvider.Apply();
    }
    public static SpriteRect[] GenerateSpriteRectData(int textureHeight, List<int> spritesPerRowList, string selectedSize,bool isPortrait)
    {
        List<SpriteRect> spriteRects = new List<SpriteRect>();
        int yOffset = 0;
        int sliceHeight = int.Parse(selectedSize) * 2;

        for (int row = 0; row < spritesPerRowList.Count; row++)
        {
            int spritesInRow = spritesPerRowList[row];
            int sliceWidth = int.Parse(selectedSize);
            if(isPortrait) sliceWidth = sliceWidth *2;
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
}
}
#endif
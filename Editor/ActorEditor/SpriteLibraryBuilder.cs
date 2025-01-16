using System;
using FingTools.Internal;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

#if UNITY_EDITOR
namespace FingTools.Internal
{
    public class SpriteLibraryBuilder : Editor
    {
        // Root folder where SpriteLibraries will be stored
        private const string LibraryRootFolderName = "SpriteLibraries/CharacterParts";

        public static void BuildAllSpriteLibraries()
        {
            // Get all SpritePart_SO assets
            string[] guids = AssetDatabase.FindAssets("t:SpritePart_SO");
            foreach (string guid in guids)
            {
                string spritePartPath = AssetDatabase.GUIDToAssetPath(guid);
                SpritePart_SO spritePart = AssetDatabase.LoadAssetAtPath<SpritePart_SO>(spritePartPath);

                if (spritePart != null)
                {
                    // Create a new SpriteLibraryAsset
                    SpriteLibraryAsset library = CreateInstance<SpriteLibraryAsset>();

                    // Add sprites to the library
                    AddSpritesToLibrary(spritePart, library);

                    // Create a folder for the CharSpriteType
                    string baseFolder = GetBaseFolder(spritePartPath, "FingTools");
                    string libraryFolder = $"{baseFolder}/{LibraryRootFolderName}/{spritePart.type}";

                    // Ensure the subfolder exists
                    CreateFolderHierarchy(libraryFolder);

                    // Save the library in the designated folder
                    string libraryPath = $"{libraryFolder}/{spritePart.name}_Library.asset";
                    AssetDatabase.CreateAsset(library, libraryPath);

                    // Assign the library to the SpritePart_SO
                    spritePart.spriteLibraryAsset = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(libraryPath);
                    EditorUtility.SetDirty(spritePart);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void AddSpritesToLibrary(SpritePart_SO spritePart, SpriteLibraryAsset library)
        {
            if (spritePart.sprites == null || spritePart.sprites.Length == 0)
            {
                Debug.LogWarning($"SpritePart_SO {spritePart.name} has no sprites assigned.");
                return;
            }

            int lastIndex = 0;

            // Animation configurations
            var animationConfigs = new (string category, int spritesPerDirection, bool fixedDirection)[]
            {
                ("Fixed", 1, false),
                ("Idle", 6, false),
                ("Walking", 6, false),
                ("Sleeping", 6, true),
                ("Sitting", 6, false),
                ("Phone_Out", 3, true),
                ("Phoning", 6, true),
                ("Phone_In", 3, true),
                ("Reading", 6, true),
                ("BookTurning", 6, true),
                ("Pushing", 6, false),
                ("Picking", 12, false),
                ("Gifting", 10, false),
                ("Lifting", 14, false),
                ("Throwing", 14, false),
                ("Hitting", 6, false),
                ("Punching", 6, false),
                ("Stabbing", 6, false),
                ("GunGrabbing", 4, false),
                ("GunIdling", 6, false),
                ("GunShooting", 3, false),
                ("Hurting", 3, false),
            };

            foreach (var config in animationConfigs)
            {
                library = AddSpritesToLibrary(lastIndex, config.category, config.spritesPerDirection, library, spritePart, out lastIndex, config.fixedDirection);
            }
        }

        private static SpriteLibraryAsset AddSpritesToLibrary(
            int startIndex,
            string category,
            int spritesPerDirection,
            SpriteLibraryAsset library,
            SpritePart_SO spritePart,
            out int nextIndex,
            bool fixedDirection = false)
        {
            int index = startIndex;
            var directions = Enum.GetNames(typeof(CardinalDirection));
            if (fixedDirection)
            {
                directions = new string[] { CardinalDirection.S.ToString() };
            }

            foreach (var direction in directions)
            {
                for (int i = 0; i < spritesPerDirection; i++)
                {
                    if (index >= spritePart.sprites.Length)
                    {
                        Debug.LogError($"Index out of bounds for {spritePart.name} in {category}.");
                        nextIndex = index;
                        return library;
                    }

                    string label = $"{direction}_{i}";
                    library.AddCategoryLabel(spritePart.sprites[index], category, label);
                    index++;
                }
            }

            nextIndex = index;
            return library;
        }

        private static string GetBaseFolder(string assetPath, string rootFolderName)
        {
            // Find the root folder's index
            int rootIndex = assetPath.IndexOf(rootFolderName, StringComparison.OrdinalIgnoreCase);
            if (rootIndex == -1)
            {
                Debug.LogError($"Root folder '{rootFolderName}' not found in path: {assetPath}");
                return "Assets";
            }

            // Return the base path up to and including the root folder
            return assetPath.Substring(0, rootIndex + rootFolderName.Length);
        }

        private static void CreateFolderHierarchy(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string currentPath = "";

            for (int i = 0; i < parts.Length; i++)
            {
                currentPath = string.IsNullOrEmpty(currentPath) ? parts[i] : $"{currentPath}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(currentPath))
                {
                    string parent = currentPath.Contains("/") ? currentPath.Substring(0, currentPath.LastIndexOf('/')) : "";
                    string folderName = currentPath.Substring(currentPath.LastIndexOf('/') + 1);
                    AssetDatabase.CreateFolder(parent, folderName);
                }
            }
        }
    }
}
#endif
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

        public static void BuildAllPortraitSpriteLibrairies()
        {
            string[] guids = AssetDatabase.FindAssets("t:PortraitPart_SO");
            foreach (string guid in guids)
            {
                string spritePartPath = AssetDatabase.GUIDToAssetPath(guid);
                PortraitPart_SO portraitSpritePart = AssetDatabase.LoadAssetAtPath<PortraitPart_SO>(spritePartPath);
                SpriteLibraryAsset library = CreateInstance<SpriteLibraryAsset>();
                AddPortraitSpritesToLibrary(portraitSpritePart,library);

                // Create a folder for the CharSpriteType
                string baseFolder = GetBaseFolder(spritePartPath, "FingTools");
                string libraryFolder = $"{baseFolder}/{CharacterImporter.PortraitLibraryRootFolderName}/{portraitSpritePart.type}";

                CreateFolderHierarchy(libraryFolder);

                // Save the library in the designated folder
                string libraryPath = $"{libraryFolder}/{portraitSpritePart.name}_Library.asset";
                AssetDatabase.CreateAsset(library, libraryPath);

                // Assign the library to the SpritePart_SO
                portraitSpritePart.spriteLibraryAsset = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(libraryPath);
                EditorUtility.SetDirty(portraitSpritePart);
            }
        }
        public static void BuildAllActorSpriteLibrairies()
        {
            // Get all SpritePart_SO assets
            string[] guids = AssetDatabase.FindAssets("t:ActorSpritePart_SO");
            foreach (string guid in guids)
            {
                string spritePartPath = AssetDatabase.GUIDToAssetPath(guid);
                ActorSpritePart_SO actorSpritePart = AssetDatabase.LoadAssetAtPath<ActorSpritePart_SO>(spritePartPath);

                if (actorSpritePart != null)
                {
                    // Create a new SpriteLibraryAsset
                    SpriteLibraryAsset library = CreateInstance<SpriteLibraryAsset>();

                    // Add sprites to the library
                    AddActorSpritesToLibrary(actorSpritePart, library);

                    // Create a folder for the CharSpriteType
                    string baseFolder = GetBaseFolder(spritePartPath, "FingTools");
                    string libraryFolder = $"{baseFolder}/{CharacterImporter.ActorLibraryRootFolderName}/{actorSpritePart.type}";

                    // Ensure the subfolder exists
                    CreateFolderHierarchy(libraryFolder);

                    // Save the library in the designated folder
                    string libraryPath = $"{libraryFolder}/{actorSpritePart.name}_Library.asset";
                    AssetDatabase.CreateAsset(library, libraryPath);

                    // Assign the library to the SpritePart_SO
                    actorSpritePart.spriteLibraryAsset = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(libraryPath);
                    EditorUtility.SetDirty(actorSpritePart);
                }
            }

            
        }

        private static void AddPortraitSpritesToLibrary(PortraitPart_SO portraitPart_SO, SpriteLibraryAsset library)
        {
            if (portraitPart_SO.sprites == null || portraitPart_SO.sprites.Length == 0)
            {
                Debug.LogWarning($"SpritePart_SO {portraitPart_SO.name} has no sprites assigned.");
                return;
            }
            var portraitAnimationConfigs = new (string category,int spritesPerDirection,bool fixedDirection)[]
            {
                ("Talk",10,true),
                ("Nod",10,true),
                ("Shake",10,true)
            };
            int lastIndex = 0;
            foreach (var (category, spritesPerDirection, fixedDirection) in portraitAnimationConfigs)
            {
                library = AddPortraitSpritesToLibrary(lastIndex, category, spritesPerDirection, library, portraitPart_SO, out lastIndex, fixedDirection);
            }
        }
        private static void AddActorSpritesToLibrary(ActorSpritePart_SO spritePart, SpriteLibraryAsset library)
        {
            if (spritePart.sprites == null || spritePart.sprites.Length == 0)
            {
                Debug.LogWarning($"SpritePart_SO {spritePart.name} has no sprites assigned.");
                return;
            }

            int lastIndex = 0;

            // Animation configurations
            var actorAnimationConfigs = new (string category, int spritesPerDirection, bool fixedDirection)[]
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

            foreach (var (category, spritesPerDirection, fixedDirection) in actorAnimationConfigs)
            {
                library = AddActorSpritesToLibrary(lastIndex, category, spritesPerDirection, library, spritePart, out lastIndex, fixedDirection);
            }
        }

        private static SpriteLibraryAsset AddActorSpritesToLibrary(
            int startIndex,
            string category,
            int spritesPerDirection,
            SpriteLibraryAsset library,
            ActorSpritePart_SO spritePart,
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
        private static SpriteLibraryAsset AddPortraitSpritesToLibrary(
            int startIndex,
            string category,
            int spritesPerDirection,
            SpriteLibraryAsset library,
            PortraitPart_SO spritePart,
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
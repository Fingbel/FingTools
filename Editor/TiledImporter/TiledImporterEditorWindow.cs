using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEditor.Build;

#if SUPER_TILED2UNITY_INSTALLED
using SuperTiled2Unity; // Include the SuperTiled2Unity namespaces if available
#endif

namespace FingTools.Tiled
{
    public class TiledImporterEditorWindow : EditorWindow
    {
        private static string superTiled2UnityPackageId = "com.seanba.super-tiled2unity";
        private static string superTiled2UnityGitUrl = "https://github.com/Seanba/SuperTiled2Unity.git?path=/SuperTiled2Unity/Packages/com.seanba.super-tiled2unity";
        private static bool? isSuperTiled2UnityInstalled = null;
        private string selectedInteriorZipFile = null;
        private string selectedExteriorZipFile = null;
        private int selectedSizeIndex = 0;
        private bool importInterior = true;
        private bool importExterior = true;
        private readonly List<string> validSizes = new() { "16", "32" };
        private string outputPath = "Assets/FingTools/Tiled/";
        private List<string> selectedInteriorTilesets = new List<string>();
        private List<string> selectedExteriorTilesets = new List<string>();
        private List<string> availableInteriorTilesets = new List<string>();
        private List<string> availableExteriorTilesets = new List<string>();
        private Vector2 interiorScrollPos;
        private Vector2 exteriorScrollPos;
        private bool interiorExpanded = false;
        private bool exteriorExpanded = false;

        [MenuItem("FingTools/Importer/Tilesets Importer", false, 99)]
        public static void ShowWindow()
        {
            GetWindow<TiledImporterEditorWindow>(true, "Tilesets Importer");
        }

        private void OnGUI()
        {
            if (isSuperTiled2UnityInstalled == null)
            {
                isSuperTiled2UnityInstalled = CheckSuperTiled2Unity();
            }
            EditorGUILayout.HelpBox(
                "This tool use Tilesets from Limezu's Modern Interior & Exterior packs.\n" +
                "The tool automatically create a Tiled project and add the imported assets as usable tilesets inside Tiled\n\n" +
                "To use this tool:\n" +
                "1. Select the packs you want to import.\n" +
                "2. Choose a sprite size to import.\n" +
                "3. Click 'Import Assets'.\n\n" +
                "WARNING: This process TAKE A VERY LONG TIME, optimizing is on his way. \n",
                MessageType.Info
            );
            //EditorGUILayout.LabelField("The Tiled importer requires SuperTiled2Unity in order to work properly", EditorStyles.boldLabel);
            if (isSuperTiled2UnityInstalled == true)
            {
                EditorGUILayout.LabelField("✅ SuperTiled2Unity is correctly installed", EditorStyles.boldLabel);
                // Select the sprite size to import
                EditorGUILayout.LabelField("Select the sprite size to import: (48x48 is not available at the moment)", EditorStyles.wordWrappedLabel);
                selectedSizeIndex = EditorGUILayout.Popup(selectedSizeIndex, validSizes.ToArray());

                
                DrawSeparator();
                // Import interior asset selection
                importInterior = EditorGUILayout.Toggle("Import Interior Assets", importInterior);
                if (importInterior)
                {
                    // Show button to select interior zip file if selected
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Select Modern Interior zip file", GUILayout.Width(180)))
                    {
                        selectedInteriorZipFile = EditorUtility.OpenFilePanel("Select Modern Interior zip file", "", "zip");
                        if (!string.IsNullOrEmpty(selectedInteriorZipFile))
                        {
                            availableInteriorTilesets = GetAvailableInteriorTilesets(selectedInteriorZipFile, int.Parse(validSizes[selectedSizeIndex]));
                            availableInteriorTilesets.Sort(CompareTilesetNames);
                        }
                    }
                    EditorGUILayout.LabelField("Selected Zip File:", selectedInteriorZipFile ?? "None");
                    EditorGUILayout.EndHorizontal();                    
                    if(!string.IsNullOrEmpty(selectedInteriorZipFile))
                    {
                        EditorGUI.BeginDisabledGroup(false);   
                        if(!TiledImporter.ValidateInteriorZipFile(selectedInteriorZipFile))
                        {
                            selectedInteriorZipFile = "Wrong file selected";   
                            EditorGUI.BeginDisabledGroup(true);                         
                        }
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Available Tilesets:");
                        if (GUILayout.Button(interiorExpanded ? "Collapse" : "Select", GUILayout.Width(80)))
                        {
                            interiorExpanded = !interiorExpanded;
                        }
                        if (GUILayout.Button("Select All", GUILayout.Width(80)))
                        {
                            selectedInteriorTilesets = new List<string>(availableInteriorTilesets);
                        }
                        if (GUILayout.Button("Deselect All", GUILayout.Width(80)))
                        {
                            selectedInteriorTilesets.Clear();
                        }
                        EditorGUI.EndDisabledGroup();
                        EditorGUILayout.EndHorizontal();

                        if (interiorExpanded)
                        {
                            interiorScrollPos = EditorGUILayout.BeginScrollView(interiorScrollPos, GUILayout.Height(130));
                            int halfCount = Mathf.CeilToInt(availableInteriorTilesets.Count / 2f);
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.BeginVertical();
                            for (int i = 0; i < halfCount; i++)
                            {
                                var tileset = availableInteriorTilesets[i];
                                bool isSelected = selectedInteriorTilesets.Contains(tileset);
                                bool isAlreadyImported = IsTilesetAlreadyImported(tileset, "Interior");
                                EditorGUI.BeginDisabledGroup(isAlreadyImported);
                                bool newIsSelected = EditorGUILayout.ToggleLeft(CleanTilesetName(tileset), isSelected || isAlreadyImported);
                                EditorGUI.EndDisabledGroup();
                                if (newIsSelected != isSelected)
                                {
                                    if (newIsSelected)
                                    {
                                        selectedInteriorTilesets.Add(tileset);
                                    }
                                    else
                                    {
                                        selectedInteriorTilesets.Remove(tileset);
                                    }
                                }
                            }
                            EditorGUILayout.EndVertical();
                            EditorGUILayout.BeginVertical();
                            for (int i = halfCount; i < availableInteriorTilesets.Count; i++)
                            {
                                var tileset = availableInteriorTilesets[i];
                                bool isSelected = selectedInteriorTilesets.Contains(tileset);
                                bool isAlreadyImported = IsTilesetAlreadyImported(tileset, "Interior");
                                EditorGUI.BeginDisabledGroup(isAlreadyImported);
                                bool newIsSelected = EditorGUILayout.ToggleLeft(CleanTilesetName(tileset), isSelected || isAlreadyImported);
                                EditorGUI.EndDisabledGroup();
                                if (newIsSelected != isSelected)
                                {
                                    if (newIsSelected)
                                    {
                                        selectedInteriorTilesets.Add(tileset);
                                    }
                                    else
                                    {
                                        selectedInteriorTilesets.Remove(tileset);
                                    }
                                }
                            }
                            EditorGUILayout.EndVertical();
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.EndScrollView();
                        }
                    }
                }

                DrawSeparator();

                // Import exterior asset selection
                importExterior = EditorGUILayout.Toggle("Import Exterior Assets", importExterior);
                if (importExterior)
                {
                    // Show button to select exterior zip file if selected
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Select Modern Exterior zip file", GUILayout.Width(180)))
                    {
                        selectedExteriorZipFile = EditorUtility.OpenFilePanel("Select Modern Exterior zip file", "", "zip");
                        if (!string.IsNullOrEmpty(selectedExteriorZipFile))
                        {
                            availableExteriorTilesets = GetAvailableExteriorTilesets(selectedExteriorZipFile, int.Parse(validSizes[selectedSizeIndex]));
                            availableExteriorTilesets.Sort(CompareTilesetNames);
                        }
                    }
                    EditorGUILayout.LabelField("Selected Zip File:", selectedExteriorZipFile ?? "None");
                    EditorGUILayout.EndHorizontal();
                    if (!string.IsNullOrEmpty(selectedExteriorZipFile) )
                    {
                        EditorGUI.BeginDisabledGroup(false);   
                        if(!TiledImporter.ValidateExteriorZipFile(selectedExteriorZipFile))
                        {
                            selectedExteriorZipFile = "Wrong file selected";
                            EditorGUI.BeginDisabledGroup(true);
                        }
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Available Tilesets:");
                        if (GUILayout.Button(exteriorExpanded ? "Collapse" : "Select", GUILayout.Width(80)))
                        {
                            exteriorExpanded = !exteriorExpanded;
                        }
                        if (GUILayout.Button("Select All", GUILayout.Width(80)))
                        {
                            selectedExteriorTilesets = new List<string>(availableExteriorTilesets);
                        }
                        if (GUILayout.Button("Deselect All", GUILayout.Width(80)))
                        {
                            selectedExteriorTilesets.Clear();
                        }
                        EditorGUI.EndDisabledGroup();
                        EditorGUILayout.EndHorizontal();

                        if (exteriorExpanded)
                        {
                            exteriorScrollPos = EditorGUILayout.BeginScrollView(exteriorScrollPos, GUILayout.Height(130));
                            int halfCount = Mathf.CeilToInt(availableExteriorTilesets.Count / 2f);
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.BeginVertical();
                            for (int i = 0; i < halfCount; i++)
                            {
                                var tileset = availableExteriorTilesets[i];
                                bool isSelected = selectedExteriorTilesets.Contains(tileset);
                                bool isAlreadyImported = IsTilesetAlreadyImported(tileset, "Exterior");
                                EditorGUI.BeginDisabledGroup(isAlreadyImported);
                                bool newIsSelected = EditorGUILayout.ToggleLeft(CleanTilesetName(tileset), isSelected || isAlreadyImported);
                                EditorGUI.EndDisabledGroup();
                                if (newIsSelected != isSelected)
                                {
                                    if (newIsSelected)
                                    {
                                        selectedExteriorTilesets.Add(tileset);
                                    }
                                    else
                                    {
                                        selectedExteriorTilesets.Remove(tileset);
                                    }
                                }
                            }
                            EditorGUILayout.EndVertical();
                            EditorGUILayout.BeginVertical();
                            for (int i = halfCount; i < availableExteriorTilesets.Count; i++)
                            {
                                var tileset = availableExteriorTilesets[i];
                                bool isSelected = selectedExteriorTilesets.Contains(tileset);
                                bool isAlreadyImported = IsTilesetAlreadyImported(tileset, "Exterior");
                                EditorGUI.BeginDisabledGroup(isAlreadyImported);
                                bool newIsSelected = EditorGUILayout.ToggleLeft(CleanTilesetName(tileset), isSelected || isAlreadyImported);
                                EditorGUI.EndDisabledGroup();
                                if (newIsSelected != isSelected)
                                {
                                    if (newIsSelected)
                                    {
                                        selectedExteriorTilesets.Add(tileset);
                                    }
                                    else
                                    {
                                        selectedExteriorTilesets.Remove(tileset);
                                    }
                                }
                            }
                            EditorGUILayout.EndVertical();
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.EndScrollView();
                        }
                    }
                }

                DrawSeparator();               

                // Import button
                EditorGUI.BeginDisabledGroup(
                    !importExterior && !importInterior ||
                    (string.IsNullOrEmpty(selectedInteriorZipFile) && !importExterior) ||
                    (string.IsNullOrEmpty(selectedExteriorZipFile) && !importInterior) ||
                    string.IsNullOrEmpty(selectedExteriorZipFile) && string.IsNullOrEmpty(selectedInteriorZipFile)
                    );
                if (GUILayout.Button("Import Assets"))
                {
                    EditorUtility.DisplayProgressBar("Importing Tilesets", $"Processing tilesets", 0.5f);
#if SUPER_TILED2UNITY_INSTALLED
                    TiledImporter.ImportAssets(importInterior, selectedInteriorZipFile, selectedInteriorTilesets, importExterior, selectedExteriorZipFile, selectedExteriorTilesets, outputPath, selectedSizeIndex, validSizes);
#endif
                    TiledImporter.GenerateTiledProjectFile(Application.productName, outputPath);
                    AssetDatabase.Refresh();
                    AssetDatabase.SaveAssets();
                    EditorUtility.ClearProgressBar();
                }
                EditorGUI.EndDisabledGroup();
            }
            else if (isSuperTiled2UnityInstalled == false)
            {
                EditorGUILayout.LabelField("🔴 SuperTiled2Unity is not currently installed.", EditorStyles.wordWrappedLabel);
                EditorGUILayout.LabelField("Click on the button below to add SuperTiled2Unity Package to this Unity project.");

                if (GUILayout.Button("Install SuperTiled2Unity"))
                {
                    AddPackage(superTiled2UnityGitUrl);
                }
            }
        }

        private void DrawSeparator()
        {
            GUILayout.Space(5);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Space(5);
        }

        private static bool CheckSuperTiled2Unity()
        {
            // Request the list of installed packages
            ListRequest listRequest = Client.List();

            // Wait for the request to finish
            while (!listRequest.IsCompleted)
            {
            }
            if (listRequest.Status == StatusCode.Success)
            {
                var installedPackages = listRequest.Result;

                // Check if SuperTiled2Unity is installed
                bool packageFound = installedPackages.Any(p => p.name == superTiled2UnityPackageId);

                if (packageFound)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

        public static void AddPackage(string packageUrl)
        {
            // Create a request to add the package
            AddRequest addRequest = Client.Add(packageUrl);

            // Wait for the request to complete
            while (!addRequest.IsCompleted)
            {
                // You can optionally display a loading progress bar here
                EditorUtility.DisplayProgressBar("Adding Package", "Please wait while the package is added...", 0.5f);
            }
            EditorUtility.ClearProgressBar();

            // Check the result
            if (addRequest.Status == StatusCode.Success)
            {
                UnityEngine.Debug.Log("Package added successfully: " + packageUrl);
            }
            else
            {
                UnityEngine.Debug.LogError("Failed to add package: " + packageUrl);
            }
        }

        [InitializeOnLoadMethod]
        private static void DefineSuperTiled2UnitySymbolIfNeeded()
        {
            if (CheckSuperTiled2Unity())
            {
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, "SUPER_TILED2UNITY_INSTALLED");
            }
            else
            {
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, "");
            }
        }

        private int CompareTilesetNames(string x, string y)
        {
            int xNum = int.Parse(x.Split('_')[0]);
            int yNum = int.Parse(y.Split('_')[0]);
            return xNum.CompareTo(yNum);
        }

        private string CleanTilesetName(string tilesetName)
        {
            var parts = tilesetName.Split('_');
            if (parts.Length > 1)
            {
                var nameWithoutPrefix = string.Join(" ", parts.Skip(1));
                var sizeIndex = nameWithoutPrefix.IndexOf(validSizes[selectedSizeIndex] + "x" + validSizes[selectedSizeIndex]);
                if (sizeIndex > 0)
                {
                    nameWithoutPrefix = nameWithoutPrefix.Substring(0, sizeIndex).Trim();
                }
                else
                {
                    nameWithoutPrefix = nameWithoutPrefix.Replace(".png", "").Trim();
                }
                return nameWithoutPrefix.Replace('_', ' ');
            }
            return tilesetName.Replace(".png", "").Trim();
        }

        private List<string> GetAvailableInteriorTilesets(string zipFilePath, int spriteSize)
        {
            List<string> tilesets = new List<string>();
            var archive = ZipFile.OpenRead(zipFilePath);
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (entry.FullName.StartsWith($"1_Interiors/{spriteSize}x{spriteSize}/Theme_Sorter/") && entry.FullName.EndsWith(".png"))
                {
                    tilesets.Add(entry.Name);
                }
            }
            return tilesets;
        }

        private List<string> GetAvailableExteriorTilesets(string zipFilePath, int spriteSize)
        {
            List<string> tilesets = new List<string>();
            var archive = ZipFile.OpenRead(zipFilePath);
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (entry.FullName.StartsWith($"Modern_Exteriors_{spriteSize}x{spriteSize}/ME_Theme_Sorter_{spriteSize}x{spriteSize}/") && entry.FullName.EndsWith(".png") && !entry.FullName.Contains("Singles") && !entry.FullName.Contains("Old_Sorting"))
                {
                    tilesets.Add(entry.Name);
                }
            }
            return tilesets;
        }

        private bool IsTilesetAlreadyImported(string tileset, string type)
        {
            string tilesetPath = Path.Combine(outputPath, "Tilesets", type, $"{Path.GetFileNameWithoutExtension(tileset)}.tsx");
            return File.Exists(tilesetPath);
        }
    }
}
using UnityEditor;
using UnityEngine;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using System.IO;
using FingTools.Internal;
using System.Linq;

#if UNITY_EDITOR
namespace FingTools.Tiled
{
[Overlay(typeof(EditorWindow), "FloatingToolbar", true)]
public class FloatingToolbar : ToolbarOverlay
{
    FloatingToolbar() : base(ActorEditor.Id,MapSwitch.Id,NewMap.Id,OpenTiled.Id) { }

    [EditorToolbarElement(Id, typeof(EditorWindow))]
    class MapSwitch : EditorToolbarButton
    {
        public const string Id = "SwitchMap";

        public MapSwitch()
        {
            text = "SwitchMap";
            icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.fingcorp.fingtools/Media/Icons/icon.png");
            
            clicked += () =>
            {        
                MapManager.RefreshUniverse();
                string projectPath = Path.Combine(Application.dataPath, "FingTools", "Tiled", $"TiledProject.tiled-project");
                bool tilesetDetected = File.Exists(projectPath);
                bool mapDetected = MapManager.Instance.HasMaps();
                // Check if Tiled project is detected
                if(!tilesetDetected)
                {
                    if(EditorUtility.DisplayDialog("Map Loader", "No tilesets have been imported yet, would you like to import some ?", "Yes", "No"))
                    {
                            TiledImporterEditorWindow.ShowWindow();
                            return;
                    }
                    return;
                }                
                else
                {                    
                    if(mapDetected)
                    {
                        ShowSearchWindow();
                    }
                    else
                    {
                        if(EditorUtility.DisplayDialog("Map Loader", "No Tiled maps have been created yet, would you like to create one ?", "Yes", "No"))
                        {
                            CreateNewTiledMapWindow.ShowWindow();
                        };              
                    }
                }                
            };
        }

        private void ShowSearchWindow()
        {
            var searchWindow = ScriptableObject.CreateInstance<MapSearchWindow>();
            SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(Event.current.mousePosition)), searchWindow);
        }
    }
    [EditorToolbarElement(Id, typeof(EditorWindow))]
    class OpenTiled : EditorToolbarButton
    {
        public const string Id = "Tiled";

        public OpenTiled()
        {
            text = "Tiled";
            icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.fingcorp.fingtools/Media/Icons/tiled-logo.png");
            clicked += TiledLinker.OpenTiled;
        }
    }
    [EditorToolbarElement(Id, typeof(EditorWindow))]
    class NewMap : EditorToolbarButton
    {
        public const string Id = "NewMap";

        public NewMap()
        {
            text = "New Map";
            icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.fingcorp.fingtools/Media/Icons/icon.png");
            clicked += () =>
            {
                bool tilesetDetected = File.Exists(Path.Combine(Application.dataPath, "FingTools", "Tiled", $"TiledProject.tiled-project"));
                if(!tilesetDetected)
                {
                    if(EditorUtility.DisplayDialog("Map Loader", "No tilesets have been imported yet, would you like to import some ?", "Yes", "No"))
                    {
                            TiledImporterEditorWindow.ShowWindow();
                            return;
                    }
                }
                else
                {
                    CreateNewTiledMapWindow.ShowWindow();
                }
            };
        }
    }
    
    [EditorToolbarElement(Id, typeof(EditorWindow))]
    class ActorEditor : EditorToolbarButton
    {
        public const string Id = "ActorEditor";

        public ActorEditor()
        {
            text = "ActorEditor";
            icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.fingcorp.fingtools/Media/Icons/actor-logo.png");
            clicked += () => {
                if(Directory.Exists("Assets/Resources/FingTools"))
                {
                    var manager = Resources.Load<SpriteManager>("FingTools/SpriteManager");
                    if(manager?.HasAssetsImported() == true)
                    {
                        ActorEditorWindow.ShowWindow();
                        return;
                    }
                }              
                if(EditorUtility.DisplayDialog("Actor Editor", "No character assets have been imported yet. Would you like to import assets now?", "Yes", "No"))
                {
                    CharacterImporterEditorWindow.ShowWindow();
                };
            };
        }
    }

}

public class MapSearchWindow : ScriptableObject, ISearchWindowProvider
{
    private List<SearchTreeEntry> searchTreeEntries;

    public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
    {
        searchTreeEntries = new List<SearchTreeEntry>
        {
            new SearchTreeGroupEntry(new GUIContent("Tiled Maps and Worlds"), 0)
        };

        List<string> mapsInWorlds = new List<string>();
        foreach (string worldPath in MapManager.Instance.existingWorlds)
        {
            string worldContent = File.ReadAllText(worldPath);
            var worldData = JsonUtility.FromJson<MapLoader.WorldData>(worldContent);
            mapsInWorlds.AddRange(worldData.maps.Select(m => Path.GetFileNameWithoutExtension(m.fileName)));
        }        
        Dictionary<GUIContent,string> mapContents = new ();
        mapContents.Add(new GUIContent("==== Maps ===="), "");
        foreach (string mapPath in MapManager.Instance.existingMaps)
        {
            string mapName = Path.GetFileNameWithoutExtension(mapPath);
            if (!mapsInWorlds.Contains(mapName))
            {
                mapContents.Add(new GUIContent(mapName), mapPath);
                //searchTreeEntries.Add(new SearchTreeEntry(new GUIContent(mapName)) { level = 1, userData = mapPath });
            }
        }       

        if(MapManager.Instance.existingWorlds.Count > 0)
        {
            mapContents.Add(new GUIContent("==== Worlds ===="), "");
            foreach (string worldPath in MapManager.Instance.existingWorlds)
            {
                string worldName = Path.GetFileNameWithoutExtension(worldPath);
                mapContents.Add(new GUIContent(worldName), worldPath);            
            }
            
        }
        foreach (var mapContent in mapContents)
        {
            searchTreeEntries.Add(new SearchTreeEntry(mapContent.Key) { level = 1, userData = mapContent.Value });
        }
        return searchTreeEntries;
    }

    public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
    {
        if (entry.userData is string filePath)
        {
            if (filePath.EndsWith(".tmx"))
            {
                MapLoader.LoadMap(filePath);
            }
            else if (filePath.EndsWith(".world"))
            {
                MapLoader.LoadMap(filePath, true);
            }
            return true;
        }
        return false;
    }
}
#endif
}
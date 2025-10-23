using UnityEditor;
using UnityEngine;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using System.IO;
using FingTools.Internal;
using System.Linq;
using UnityEngine.UIElements;

#if UNITY_EDITOR

namespace FingTools.Tiled
{
[Overlay(typeof(EditorWindow), "FingToolbar", true)]
public class FingToolbar : ToolbarOverlay
{ 
    
    FingToolbar() : base(ActorEditor.Id,MapToolsGroup.Id,NewMap.Id,OpenTiled.Id) 
    {
        
    }   
    [EditorToolbarElement("MapTools/MapToolsGroup", typeof(EditorWindow))]
    public class MapToolsGroup : VisualElement
    {
        public const string Id = "MapTools/MapToolsGroup";

        public MapToolsGroup()
        {
            // Create and add MapSwitch
            var mapSwitch = new MapSwitch();
            Add(mapSwitch);

            // Create and add CurrentMapLabel
            var mapLabel = new CurrentMapLabel();
            Add(mapLabel);

            // NavGrid overlay toggle
            var overlayToggle = new Button(() => {
                FingTools.Internal.Editor.NavGridSceneOverlay.Toggle();
            }) { text = "NavGrid" };
            overlayToggle.style.marginLeft = 6;
            Add(overlayToggle);

           

            // Optional: Add spacing or layout customization
            style.flexDirection = FlexDirection.Row; // Horizontal layout
            style.alignItems = Align.Center;         // Center align elements
            style.paddingLeft = 5;
            style.paddingRight = 5;
        }
    } 

    [EditorToolbarElement(Id, typeof(EditorWindow))]
    class MapSwitch : EditorToolbarButton
    {
        public const string Id = "SwitchMap";
        
        public MapSwitch()
        {
            text = "SwitchMap";
            clicked += () =>
            {
                MapManager.RefreshUniverse();
                if (!AssetChecker.MapLoaderInitRefresh()) return;
                if (!AssetChecker.CheckForTiledProject()) return;
                if(!AssetChecker.CheckForMaps()) return;
                ShowSearchWindow();
            };
            // delay icon load to attach time (avoid Unity API from loading thread)
            this.RegisterCallback<AttachToPanelEvent>(evt => {
                if (icon == null)
                    icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.fingcorp.fingtools/Media/Icons/switchMap.png");
            });
        }          
        private string ShowSearchWindow()
        {
            var searchWindow = ScriptableObject.CreateInstance<MapSearchWindow>();
            SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(Event.current.mousePosition)), searchWindow);
            return "";
        }
    }

    [EditorToolbarElement(Id, typeof(EditorWindow))]
    class OpenTiled : EditorToolbarButton
    {
        public const string Id = "Tiled";

        public OpenTiled()
        {
            text = "Open Map";
            clicked += () =>
            {
                if(!TiledLinker.CheckForTiled()) return;
                if(!AssetChecker.MapLoaderInitRefresh()) return;
                if(!AssetChecker.CheckForTilesets()) return;
                if(!AssetChecker.CheckForMaps()) return;           
                MapLoader.RefreshMapObjects();
                string loaded = MapManager.Instance.LoadedMapObject;
                if(string.IsNullOrEmpty(loaded))
                    TiledLinker.OpenTiled();
                else
                    if(MapManager.Instance.IsLoadedMapObjectAWorld)
                        TiledLinker.OpenTiledWithProjectAndMap("Assets\\FingTools\\Tiled\\Tiledworlds\\" + MapManager.Instance.LoadedMapObject + ".world");                
                    else
                        TiledLinker.OpenTiledWithProjectAndMap("Assets\\FingTools\\Tiled\\Tilemaps\\" + MapManager.Instance.LoadedMapObject + ".tmx");                
            };
            this.RegisterCallback<AttachToPanelEvent>(evt => {
                if (icon == null)
                    icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.fingcorp.fingtools/Media/Icons/tiled-logo.png");
            });
        }

            
        }
    [EditorToolbarElement(Id, typeof(EditorWindow))]
    class NewMap : EditorToolbarButton
    {
        public const string Id = "NewMap";

        public NewMap()
        {
            text = "New Map";
            clicked += () =>
            {
                if(!AssetChecker.MapLoaderInitRefresh()) return;
                if(!AssetChecker.CheckForTilesets()) return;            
                CreateNewTiledMapWindow.ShowWindow();
            };
            this.RegisterCallback<AttachToPanelEvent>(evt => {
                if (icon == null)
                    icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.fingcorp.fingtools/Media/Icons/newMap.png");
            });
        }
    }
    
    [EditorToolbarElement(Id, typeof(EditorWindow))]
    class ActorEditor : EditorToolbarButton
    {
        public const string Id = "ActorEditor";

        public ActorEditor()
        {
            text = "ActorEditor";
            clicked += () =>
            {
                if(!AssetChecker.CheckForSpriteManager()) return;
                ActorEditorWindow.ShowWindow();
            };
            this.RegisterCallback<AttachToPanelEvent>(evt => {
                if (icon == null)
                    icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.fingcorp.fingtools/Media/Icons/actor-logo.png");
            });
        }

            
        }
    [EditorToolbarElement(Id,typeof(EditorWindow))]
    public class CurrentMapLabel : VisualElement
    {
         public const string Id = "MapLabel";

        private Label mapLabel;
        private bool ShouldShowButton(string mapLabel)
        {
            return !string.IsNullOrEmpty(mapLabel);
        }
        public CurrentMapLabel()
        {
            // Create label element but delay runtime-dependent initialization
            mapLabel = new Label("No Map Selected");
            mapLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            mapLabel.style.paddingLeft = 10;
            mapLabel.style.paddingRight = 10;
            Add(mapLabel);

            // Subscribe when attached to panel to ensure main-thread safe API usage
            this.RegisterCallback<AttachToPanelEvent>(evt => {
                EditorApplication.update += UpdateMapName;
            });
            this.RegisterCallback<DetachFromPanelEvent>(evt => {
                EditorApplication.update -= UpdateMapName;
            });
        }

        private void UpdateMapName()
        {
            if (MapManager.Instance == null) return;
            string currentMapName = MapManager.Instance.LoadedMapObject; 
            style.display = ShouldShowButton(currentMapName) ? DisplayStyle.Flex : DisplayStyle.None;
            mapLabel.text = currentMapName;             
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
            var worldData = JsonUtility.FromJson<WorldData>(worldContent);
            mapsInWorlds.AddRange(worldData.maps.Select(m => Path.GetFileNameWithoutExtension(m.fileName)));
        }        
        Dictionary<GUIContent,string> mapContents = new ();
        if(MapManager.Instance.existingWorlds.Count > 0) //Only show separator if we have worlds to show
        {
            mapContents.Add(new GUIContent("==== Maps ===="), "");
        }
        foreach (string mapPath in MapManager.Instance.existingMaps)
        {
            string mapName = Path.GetFileNameWithoutExtension(mapPath);
            if (!mapsInWorlds.Contains(mapName))
            {
                mapContents.Add(new GUIContent(mapName), mapPath);               
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
using UnityEditor;
using UnityEngine;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using System.IO;
using Codice.Client.BaseCommands;
using FingTools.Internal;

#if UNITY_EDITOR
[Overlay(typeof(EditorWindow), "FloatingToolbar", true)]
public class FloatingToolbar : ToolbarOverlay
{
    FloatingToolbar() : base(ActorEditor.Id,MapLoader.Id,NewMap.Id,OpenTiled.Id) { }

    [EditorToolbarElement(Id, typeof(EditorWindow))]
    class MapLoader : EditorToolbarButton
    {
        public const string Id = "SwitchMap";

        public MapLoader()
        {
            text = "SwitchMap";
            icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.fingcorp.fingtools/Media/Icons/icon.png");
            
            clicked += () =>
            {
                MapManager.RefreshMaps();
                if(MapManager.Instance.HasMaps())
                {
                    ShowSearchWindow();
                }
                else
                {
                    CreateNewTiledMapWindow.ShowWindow();
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
        public const string Id = "OpenTiled";

        public OpenTiled()
        {
            text = "OpenTiled";
            icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.fingcorp.fingtools/Media/Icons/icon.png");
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
            clicked += CreateNewTiledMapWindow.ShowWindow;
        }
    }

    [EditorToolbarElement(Id, typeof(EditorWindow))]
    class ActorEditor : EditorToolbarButton
    {
        public const string Id = "ActorEditor";

        public ActorEditor()
        {
            text = "ActorEditor";
            icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.fingcorp.fingtools/Media/Icons/icon.png");
            clicked += ActorEditorWindow.ShowWindow;
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
            new SearchTreeGroupEntry(new GUIContent("Tiled Maps"), 0)
        };

        if (MapManager.Instance.HasMaps())
        {
            foreach (string mapPath in MapManager.Instance.existingMaps)
            {
                string mapName = Path.GetFileNameWithoutExtension(mapPath);
                searchTreeEntries.Add(new SearchTreeEntry(new GUIContent(mapName)) { level = 1, userData = mapPath });
            }
        }
        else
        {
            searchTreeEntries.Add(new SearchTreeEntry(new GUIContent("No maps available")) { level = 1 });
        }

        return searchTreeEntries;
    }

    public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
    {
        if (entry.userData is string mapFilePath)
        {
            // Load the map in the MapManager 
            return true;
        }
        return false;
    }
}
#endif
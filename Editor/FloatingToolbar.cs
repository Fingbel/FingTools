using UnityEditor;
using UnityEngine;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

#if UNITY_EDITOR
[Overlay(typeof(EditorWindow), "FloatingToolbar", true)]
public class FloatingToolbar : ToolbarOverlay
{
    FloatingToolbar() : base(MapLoader.Id,TiledLoader.Id) { }

    [EditorToolbarElement(Id, typeof(EditorWindow))]
    class MapLoader : EditorToolbarButton
    {
        public const string Id = "MapLoader";

        public MapLoader()
        {
            text = "Map";
            icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.fingcorp.fingtools/Media/Icons/icon.png");
            clicked += ShowSearchWindow;
        }

        private void ShowSearchWindow()
        {
            var searchWindow = ScriptableObject.CreateInstance<MapSearchWindow>();
            SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(Event.current.mousePosition)), searchWindow);
        }
    }
    [EditorToolbarElement(Id, typeof(EditorWindow))]
    class TiledLoader : EditorToolbarButton
    {
        public const string Id = "TiledLoader";

        public TiledLoader()
        {
            text = "Tiled";
            icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.fingcorp.fingtools/Media/Icons/icon.png");
            clicked += TiledLinker.OpenTiled;
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

        string mapDirectory = Path.Combine(Application.dataPath, "FingTools", "Tiled", "Tilemaps");
        if (Directory.Exists(mapDirectory))
        {
            string[] mapFiles = Directory.GetFiles(mapDirectory, "*.tmx", SearchOption.AllDirectories);
            foreach (string mapFile in mapFiles)
            {
                string mapName = Path.GetFileNameWithoutExtension(mapFile);
                searchTreeEntries.Add(new SearchTreeEntry(new GUIContent(mapName)) { level = 1, userData = mapFile });
            }
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
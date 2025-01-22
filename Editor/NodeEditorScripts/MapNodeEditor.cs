using FingTools.NodeEditor;
using UnityEngine;
using UnityEditor;
using FingTools.Internal;
public class MapNodeEditor : GenericNodeEditor<NPCSpawner>
{
    private Color mapGroupColor = new Color(0.3f, 0.6f, 1.0f, 0.15f);  // Semi-transparent blue
    [MenuItem("FingTools/DEBUG/Node Editor")]
    public static void ShowWindow()
    {
        MapNodeEditor window = GetWindow<MapNodeEditor>(true);
        window.titleContent = new GUIContent("Map Editor");
        window.Show();
    }
    protected override void DrawConnections()
    {
    }

    protected override void DrawGroups()
    {
        DrawRectangularGroups(mapGroupColor);
    }

    protected override void HandleToolbarActions()
    {
        //AddToolbarButton(new MapNodeToolBarButton("Test"));
    }

    protected override void Init()
    {
        panOffset.x = 0;
        panOffset.y = 0;
    }

    protected override void LoadGroups()
    {
        groups.Clear();

        // Find all DoorSpawner instances in the scene
        NPCSpawner[] spawners = FindObjectsByType<NPCSpawner>(FindObjectsSortMode.None);
        foreach (var spawner in spawners)
        {
            Debug.Log(spawner.name);
            // Find the map name by climbing up the hierarchy
            string mapName = GetMapName(spawner.transform);

            if (!groups.ContainsKey(mapName))
                groups[mapName] = new GenericNodeGroup<NPCSpawner>(mapName);

            GenericNode<NPCSpawner> node = new GenericNode<NPCSpawner>(
                spawner.name,
                new Rect(spawner.transform.position.x, spawner.transform.position.y, 150, 50),
                spawner
            );

            groups[mapName].AddNode(node);
        }
    }
    internal class MapNodeToolBarButton : GenericToolbarButton
    {
        public MapNodeToolBarButton (string name) : base(name, 50, 50) 
        {
        
            
        }
    }
}
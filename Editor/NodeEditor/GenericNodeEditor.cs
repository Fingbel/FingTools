using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;


namespace FingTools.NodeEditor
{
    public abstract class GenericNodeEditor<T> : EditorWindow
{
    protected Vector2 panOffset;
    protected float zoom = 1.0f;
    private bool isDraggingNode = false;
    private GenericNode<T> draggedNode;
    private Vector2 dragOffset;
    private bool isPanning = false;
    protected Dictionary<string, GenericNodeGroup<T>> groups = new Dictionary<string, GenericNodeGroup<T>>();
    private static List<GenericToolbarButton> toolbarButtons = new ();
    private GenericToolbarButton selectedButton = null; 
    protected abstract void Init();

    protected abstract void LoadGroups();
    protected abstract void DrawConnections();
    protected abstract void DrawGroups();

    protected void SaveZoomAndPan()
    {
        EditorPrefs.SetFloat($"{GetType().Name}_Zoom", zoom);
        EditorPrefs.SetFloat($"{GetType().Name}_PanOffsetX", panOffset.x);
        EditorPrefs.SetFloat($"{GetType().Name}_PanOffsetY", panOffset.y);
    }

    
    protected virtual void OnEnable()
    {
        toolbarButtons.Clear();        
        LoadZoomAndPan();
        Init();
        LoadGroups();
        LoadNodePositions();        
        Repaint();
    }

    protected virtual void OnDisable()
    {
        SaveNodePositions();
        SaveZoomAndPan();
    }

    private void OnGUI()
    {
        GUILayout.BeginHorizontal();
        DrawToolbar(); 
        GUILayout.EndHorizontal();

        DrawBackground();        
        DrawGroups();
        DrawNodes();
        DrawConnections();
        HandleInput();
    }
    // Load zoom and panOffset from EditorPrefs
    private void LoadZoomAndPan()
    {
        zoom = EditorPrefs.GetFloat($"{GetType().Name}_Zoom", 1.0f);
        panOffset.x = EditorPrefs.GetFloat($"{GetType().Name}_PanOffsetX", 0.0f);
        panOffset.y = EditorPrefs.GetFloat($"{GetType().Name}_PanOffsetY", 0.0f);
    }

    protected string GetMapName(Transform transform)
    {
        // We ne to go up 3 times to reach the map 
        if (transform.parent.parent.parent != null)
        {
            Debug.Log(transform.parent.parent.parent.name);
            return transform.parent.parent.parent.name;            
        }

        return "UnknownMap";
    }
    private void DrawBackground()
    {
        const float gridSpacing = 20f;
        const float gridOpacity = 0.2f;
        Color gridColor = new Color(0.5f, 0.5f, 0.5f, gridOpacity);

        Handles.BeginGUI();
        Handles.color = gridColor;

        float offsetX = panOffset.x % gridSpacing;
        float offsetY = panOffset.y % gridSpacing;

        for (float x = offsetX; x < position.width; x += gridSpacing)
        {
            Handles.DrawLine(new Vector3(x, 0, 0), new Vector3(x, position.height, 0));
        }

        for (float y = offsetY; y < position.height; y += gridSpacing)
        {
            Handles.DrawLine(new Vector3(0, y, 0), new Vector3(position.width, y, 0));
        }

        Handles.EndGUI();
    }

    protected virtual void DrawToolbar()
    {       
        GUILayout.BeginHorizontal(); 
        foreach (var button in toolbarButtons)
        {
            if(GUILayout.Button(button.Name, EditorStyles.miniButton))
            {
                selectedButton = button;
            }
        }
        GUILayout.EndHorizontal();
        HandleToolbarActions();
    }

    protected abstract void HandleToolbarActions();

    public static void AddToolbarButton(GenericToolbarButton button) 
    {
        toolbarButtons.Add(button);
    }

    private void DrawNodes()
    {
        float xOffset = 200 * zoom;  // Adjust horizontal spacing based on zoom
        float yOffset = 50 * zoom;   // Adjust vertical spacing based on zoom
        float groupOffsetX = 0;
        float groupOffsetY = 0;

        foreach (var group in groups)
        {
            string groupName = group.Key;
            List<GenericNode<T>> nodes = group.Value.Nodes;

            // Draw each node within the group
            foreach (var node in nodes)
            {
                // Scale the node's rectangle according to zoom
                Rect adjustedNodeRect = new Rect(
                    node.Rect.position * zoom + panOffset,
                    node.Rect.size * zoom
                );

                GUI.Box(adjustedNodeRect, node.Name, EditorStyles.helpBox);

                if (Event.current.type == EventType.MouseDown && adjustedNodeRect.Contains(Event.current.mousePosition))
                {
                    isDraggingNode = true;
                    draggedNode = node;
                    dragOffset = Event.current.mousePosition - adjustedNodeRect.position;
                }
            }

            groupOffsetY += yOffset;
            groupOffsetX += xOffset;
            groupOffsetY = 0;
        }
    }
    protected Rect GetBoundingBoxForNodes(List<GenericNode<T>> nodes)
    {
        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        foreach (var node in nodes)
        {
            float nodeX = node.Rect.x * zoom + panOffset.x;
            float nodeY = node.Rect.y * zoom + panOffset.y;
            float nodeWidth = node.Rect.width * zoom;
            float nodeHeight = node.Rect.height * zoom;

            minX = Mathf.Min(minX, nodeX);
            minY = Mathf.Min(minY, nodeY);
            maxX = Mathf.Max(maxX, nodeX + nodeWidth);
            maxY = Mathf.Max(maxY, nodeY + nodeHeight);
        }

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }
    
    protected void DrawRectangularGroups(Color mapGroupColor)
    {
        foreach (var group in groups.Values)
        {
            if (group.Nodes.Count == 1)
            {
                GenericNode<T> node = group.Nodes[0];

                // Calculate the node's center position
                Vector2 nodeCenterPosition = node.Rect.center * zoom + panOffset;

                // Define a dynamic gap that scales with zoom but remains within bounds
                float minGap = 10;
                float maxGap = 40;
                float dynamicGap = Mathf.Clamp(20 * zoom, minGap, maxGap);

                // Apply a tighter offset for single-node groups to prevent overlap
                float singleNodeLabelGap = dynamicGap * 2f;

                // Adjust the label position slightly higher than the node's center
                Vector2 adjustedLabelPosition = nodeCenterPosition + new Vector2(0, -singleNodeLabelGap);

                // Use a scalable GUIStyle for better readability
                GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
                labelStyle.fontSize = Mathf.Clamp((int)(12 * zoom), 8, 24);

                // Calculate the size of the label text
                Vector2 labelSize = labelStyle.CalcSize(new GUIContent(group.Name));

                // Center the text horizontally
                Rect centeredLabelRect = new Rect(
                    adjustedLabelPosition.x - labelSize.x / 2,
                    adjustedLabelPosition.y - labelSize.y / 2,
                    labelSize.x,
                    labelSize.y
                );

                GUI.Label(centeredLabelRect, group.Name, labelStyle);
            }
            else if (group.Nodes.Count > 1)
            {
                // For multiple nodes, calculate the bounding box
                Rect boundingBox = GetBoundingBoxForNodes(group.Nodes);

                // Draw the bounding box with a semi-transparent panel
                EditorGUI.DrawRect(boundingBox, mapGroupColor);

                float minGroupGap = 15;
                float maxGroupGap = 40;
                float dynamicGroupGap = Mathf.Clamp(20 * zoom, minGroupGap, maxGroupGap);

                GUIStyle groupLabelStyle = new GUIStyle(EditorStyles.boldLabel);
                groupLabelStyle.fontSize = Mathf.Clamp((int)(16 * zoom), 8, 24);

                Vector2 groupLabelPosition = new Vector2(
                    boundingBox.x + boundingBox.width / 2,
                    boundingBox.y - dynamicGroupGap
                );

                Vector2 groupLabelSize = groupLabelStyle.CalcSize(new GUIContent(group.Name));

                Rect centeredGroupLabelRect = new Rect(
                    groupLabelPosition.x - groupLabelSize.x / 2,
                    groupLabelPosition.y - groupLabelSize.y / 2,
                    groupLabelSize.x,
                    groupLabelSize.y
                );

                GUI.Label(centeredGroupLabelRect, group.Name, groupLabelStyle);
            }
        }
    }
    
    private void HandleInput()
    {
        Event e = Event.current;

        // Right-click panning
        if (e.type == EventType.MouseDown && e.button == 1)
        {
            isPanning = true;
            e.Use();
        }

        if (e.type == EventType.MouseDrag && isPanning)
        {
            panOffset += e.delta;
            Repaint();
            e.Use();
            SaveZoomAndPan();
        }

        if (e.type == EventType.MouseUp && e.button == 1)
        {
            isPanning = false;
            e.Use();
        }

        // Dragging a node with left mouse button
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            foreach (var group in groups.Values)
            {
                foreach (var node in group.Nodes)
                {
                    float nodeX = node.Rect.x * zoom + panOffset.x;
                    float nodeY = node.Rect.y * zoom + panOffset.y;
                    Rect adjustedNodeRect = new Rect(nodeX, nodeY, node.Rect.width * zoom, node.Rect.height * zoom );

                    if (adjustedNodeRect.Contains(e.mousePosition))
                    {
                        isDraggingNode = true;
                        draggedNode = node;
                        dragOffset = e.mousePosition - adjustedNodeRect.position;
                        e.Use();
                    }
                }
            }
        }

        if (e.type == EventType.MouseDrag && isDraggingNode && draggedNode != null)
    {
        Vector2 mousePos = e.mousePosition;

        // Adjust mouse position by zoom and pan offset
        Vector2 adjustedMousePos = (mousePos - panOffset) / zoom - dragOffset / zoom;

        draggedNode.UpdatePosition(adjustedMousePos);
        Repaint();

        e.Use();
    }

        if (e.type == EventType.MouseUp && e.button == 0 && isDraggingNode)
        {
            isDraggingNode = false;
            draggedNode = null;
            e.Use();
        }

        if (e.type == EventType.ScrollWheel)
            {
                HandleZoom(e);
            }
    }
    private void HandleZoom(Event e)
    {
        if (e.type == EventType.ScrollWheel)
        {
            zoom -= e.delta.y * 0.01f;
            zoom = Mathf.Clamp(zoom, 0.5f, 1f);
            Repaint();
            e.Use();
            SaveZoomAndPan();
        }
    }

    private void SaveNodePositions()
    {
        var nodePositions = new List<NodePositionData>();
        foreach (var group in groups.Values)
        {
            foreach (var node in group.Nodes)
            {
                nodePositions.Add(new NodePositionData
                {
                    NodeName = node.Name,
                    Position = node.Rect.position
                });
            }
        }

        string json = JsonUtility.ToJson(new NodePositionDataCollection { NodePositions = nodePositions });
        File.WriteAllText(GetSaveFilePath(), json);
    }

    private void LoadNodePositions()
    {
        if (!File.Exists(GetSaveFilePath())) return;

        string json = File.ReadAllText(GetSaveFilePath());
        NodePositionDataCollection dataCollection = JsonUtility.FromJson<NodePositionDataCollection>(json);

        foreach (var nodeData in dataCollection.NodePositions)
        {
            foreach (var group in groups.Values)
            {
                var node = group.Nodes.Find(n => n.Name == nodeData.NodeName);
                if (node != null)
                {
                    node.UpdatePosition(nodeData.Position);
                }
            }
        }
    }

    private string GetSaveFilePath()
    {
        if(!Directory.Exists($"{Application.dataPath}/Editor/"))
        {
            Directory.CreateDirectory($"{Application.dataPath}/Editor/");
        }
        return $"{Application.dataPath}/Editor/NodePositions.json";
    }

}

}

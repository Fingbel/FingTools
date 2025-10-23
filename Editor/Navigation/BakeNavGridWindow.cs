using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using System.IO;

namespace FingTools.Internal.Editor
{
    public class BakeNavGridWindow : EditorWindow
    {
        private GameObject selectedMap;
        private List<GameObject> availableMaps = new();
        private int selectedMapIndex = 0;

        private float agentRadius = 0.4f;
        private int sampleResolution = 4;
        private float coverageThreshold = 0.5f;
        private string tilePropertyName = "isWalkable";
        private LayerMask staticLayerMask = ~0;
        private float padding = 0f;

        [MenuItem("FingTools/Navigation/Bake NavGrid")]
        public static void ShowWindow()
        {
            var w = GetWindow<BakeNavGridWindow>();
            w.titleContent = new GUIContent("Bake NavGrid");
            w.RefreshMaps();
        }

        void OnEnable()
        {
            RefreshMaps();
        }

        void RefreshMaps()
        {
            availableMaps.Clear();
            if (MapLoader.IsInitialized)
            {
                availableMaps.AddRange(MapLoader.Instance.SpawnedMaps);
            }
            else
            {
                // try find map objects in scene root by checking root GameObjects for Tilemap children
                var roots = SceneManager.GetActiveScene().GetRootGameObjects();
                foreach (var root in roots)
                {
                    if (root.GetComponentInChildren<Tilemap>(true) != null || root.name.ToLower().Contains("map"))
                        availableMaps.Add(root);
                }
            }
            if (availableMaps.Count == 0)
            {
                selectedMapIndex = -1;
            }
            else
            {
                selectedMapIndex = Mathf.Clamp(selectedMapIndex, 0, availableMaps.Count - 1);
                selectedMap = availableMaps[selectedMapIndex];
            }
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Bake NavGrid from Tilemap colliders", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh maps")) RefreshMaps();

            if (availableMaps.Count == 0)
            {
                EditorGUILayout.HelpBox("No maps found. Ensure MapLoader has loaded maps or select a map GameObject in the scene.", MessageType.Warning);
            }
            else
            {
                string[] names = new string[availableMaps.Count];
                for (int i = 0; i < availableMaps.Count; i++) names[i] = availableMaps[i]?.name ?? "<null>";
                selectedMapIndex = EditorGUILayout.Popup("Map", selectedMapIndex, names);
                selectedMap = availableMaps[selectedMapIndex];
            }

            agentRadius = EditorGUILayout.FloatField("Agent Radius (world)", agentRadius);
            sampleResolution = EditorGUILayout.IntField("Sample resolution (per axis)", sampleResolution);
            coverageThreshold = EditorGUILayout.Slider("Coverage threshold", coverageThreshold, 0f, 1f);
            padding = EditorGUILayout.Slider("Padding (0-0.9)", padding, 0f, 0.9f);
            tilePropertyName = EditorGUILayout.TextField("Tile property name", tilePropertyName);
            staticLayerMask = LayerMaskField("Static Layer Mask", staticLayerMask);

            EditorGUILayout.Space();
            if (GUILayout.Button("Bake NavGrid"))
            {
                if (selectedMap == null) EditorUtility.DisplayDialog("No map", "Please select a map to bake.", "OK");
                else
                {
                    BakeSelectedMap();
                }
            }
        }

        static LayerMask LayerMaskField(string label, LayerMask mask)
        {
            var layers = new List<string>();
            var layerNumbers = new List<int>();
            for (int i = 0; i < 32; i++)
            {
                string name = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(name)) { layers.Add(name); layerNumbers.Add(i); }
            }
            int newMaskVal = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if (((1 << layerNumbers[i]) & mask.value) != 0) newMaskVal |= (1 << i);
            }
            newMaskVal = EditorGUILayout.MaskField(label, newMaskVal, layers.ToArray());
            int outMask = 0;
            for (int i = 0; i < layerNumbers.Count; i++) { if ((newMaskVal & (1 << i)) != 0) outMask |= (1 << layerNumbers[i]); }
            return outMask;
        }

        void BakeSelectedMap()
        {
            var tilemap = selectedMap.GetComponentInChildren<Tilemap>();
            if (tilemap == null) { EditorUtility.DisplayDialog("No Tilemap", "Selected map has no Tilemap child.", "OK"); return; }

            var bounds = tilemap.cellBounds;
            int w = bounds.size.x;
            int h = bounds.size.y;
            Vector3Int min = bounds.min;

            FingTools.Internal.NavGrid asset = ScriptableObject.CreateInstance<FingTools.Internal.NavGrid>();
            asset.width = w;
            asset.height = h;
            asset.cellSize = tilemap.cellSize;
            // Calculate originWorld as the bottom-left corner of the min cell
            // CellToWorld returns the corner position directly (pivot is at corner for tilemaps)
            asset.originWorld = tilemap.CellToWorld(min);
            asset.bakedAgentRadius = agentRadius;
            asset.sourceMapName = selectedMap.name;
            asset.walkable = new bool[w * h];

            int totalSamples = sampleResolution * sampleResolution;
            int progress = 0;
            int totalCells = w * h;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (EditorUtility.DisplayCancelableProgressBar("Baking NavGrid", $"Processing {x},{y}", (float)progress / totalCells)) { EditorUtility.ClearProgressBar(); return; }
                    progress++;
                    Vector3Int cell = new Vector3Int(min.x + x, min.y + y, 0);

                    // check tile property override if provider available
                    bool? overrideWalkable = null;
                        var provider = selectedMap.GetComponentInChildren<MonoBehaviour>() as FingTools.Internal.ITilePropertyProvider;
                    if (provider != null)
                    {
                        var prop = provider.GetProperty(cell, tilePropertyName);
                        if (!string.IsNullOrEmpty(prop))
                        {
                            if (prop.Equals("true", StringComparison.OrdinalIgnoreCase)) overrideWalkable = true;
                            else if (prop.Equals("false", StringComparison.OrdinalIgnoreCase)) overrideWalkable = false;
                        }
                    }

                    bool isWalkable = true;
                    if (overrideWalkable.HasValue) isWalkable = overrideWalkable.Value;
                    else
                    {
                        // Sample the cell to determine collision coverage
                        // Note: SuperTile2Unity uses a single PolygonCollider2D per layer
                        // covering multiple tiles (potentially non-contiguous)
                        float cellWidth = tilemap.cellSize.x;
                        float cellHeight = tilemap.cellSize.y;
                        
                        // IMPORTANT: Use our own coordinate system based on originWorld
                        // to ensure consistency with NavGrid.CellCenterWorld()
                        Vector2 center = new Vector2(
                            asset.originWorld.x + (x + 0.5f) * cellWidth,
                            asset.originWorld.y + (y + 0.5f) * cellHeight
                        );
                        
                        float pad = Mathf.Clamp01(padding);
                        
                        int res = Mathf.Max(1, sampleResolution);
                        
                        // Get all polygon colliders that might intersect this cell
                        Vector2 boxSize = new Vector2(cellWidth * (1f - pad), cellHeight * (1f - pad));
                        Collider2D[] hits = Physics2D.OverlapBoxAll(center, boxSize, 0f, staticLayerMask.value);
                        var polyColliders = hits.Where(h => h is PolygonCollider2D).Cast<PolygonCollider2D>().ToList();
                        
                        // Debug: Log polygon info for the first cell only
                        if (x == 0 && y == 0 && polyColliders.Count > 0)
                        {
                            foreach (var poly in polyColliders)
                            {
                                Debug.Log($"[NavGridBake] Polygon collider has {poly.pathCount} paths");
                                for (int pi = 0; pi < poly.pathCount; pi++)
                                {
                                    var debugPath = poly.GetPath(pi);
                                    bool isClockwise = IsClockwise(debugPath);
                                    Debug.Log($"  Path {pi}: {debugPath.Length} points, clockwise={isClockwise}");
                                }
                            }
                        }
                        
                        if (polyColliders.Count == 0)
                        {
                            // No colliders near this cell - it's walkable
                            isWalkable = true;
                        }
                        else
                        {
                            // Sample the cell using Physics2D.OverlapPoint
                            // This correctly handles hollow polygons and complex shapes
                            int hitCount = 0;
                            
                            for (int sy = 0; sy < res; sy++)
                            {
                                for (int sx = 0; sx < res; sx++)
                                {
                                    // Map sample to [0,1] range, then to cell space [-0.5, +0.5] * boxSize
                                    float u = (sx + 0.5f) / res;
                                    float v = (sy + 0.5f) / res;
                                    
                                    float fx = u - 0.5f;
                                    float fy = v - 0.5f;
                                    
                                    Vector2 sampleWorldPos = new Vector2(
                                        center.x + fx * boxSize.x,
                                        center.y + fy * boxSize.y
                                    );
                                    
                                    // Use Physics2D which correctly handles polygon collider geometry
                                    Collider2D hit = Physics2D.OverlapPoint(sampleWorldPos, staticLayerMask.value);
                                    if (hit != null && polyColliders.Contains(hit as PolygonCollider2D))
                                    {
                                        hitCount++;
                                    }
                                }
                            }
                            
                            float sampleCoverage = (float)hitCount / (res * res);
                            isWalkable = sampleCoverage < coverageThreshold;
                        }
                    }

                    asset.walkable[y * w + x] = isWalkable;
                }
            }

            EditorUtility.ClearProgressBar();

            string rootFolder = "Assets/MapData";
            string folder = "Assets/MapData/NavGrids";
            // ensure parent folders exist (create recursively if necessary)
            if (!AssetDatabase.IsValidFolder("Assets/MapData"))
            {
                AssetDatabase.CreateFolder("Assets", "MapData");
            }
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder(rootFolder, "NavGrids");
            }
            // use forward slashes for AssetDatabase paths
            string path = Path.Combine(folder, selectedMap.name + "_NavGrid.asset").Replace("\\", "/");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Bake complete", $"NavGrid saved to {path}", "OK");
        }

        // New overload that supports collider-first mode
        public static bool BakeForMap(GameObject mapObject, float agentRadius, int sampleResolution, float coverageThreshold, float padding, bool useFastCoverage, bool useColliderFirst, string tilePropertyName, LayerMask staticLayerMask, out string outPath)
        {
            outPath = null;
            if (mapObject == null) return false;

            var tilemap = mapObject.GetComponentInChildren<Tilemap>();
            if (tilemap == null) return false;

            var bounds = tilemap.cellBounds;
            int w = bounds.size.x;
            int h = bounds.size.y;
            Vector3Int min = bounds.min;

            FingTools.Internal.NavGrid asset = ScriptableObject.CreateInstance<FingTools.Internal.NavGrid>();
            asset.width = w;
            asset.height = h;
            asset.cellSize = tilemap.cellSize;
            // Calculate originWorld as the bottom-left corner of the min cell
            // CellToWorld returns the corner position directly (pivot is at corner for tilemaps)
            asset.originWorld = tilemap.CellToWorld(min);
            asset.bakedAgentRadius = agentRadius;
            asset.sourceMapName = mapObject.name;
            asset.walkable = new bool[w * h];

            int totalSamples = sampleResolution * sampleResolution;
            int progress = 0;
            int totalCells = w * h;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    progress++;
                    Vector3Int cell = new Vector3Int(min.x + x, min.y + y, 0);

                    // check tile property override if provider available
                    bool? overrideWalkable = null;
                    var provider = mapObject.GetComponentInChildren<MonoBehaviour>() as FingTools.Internal.ITilePropertyProvider;
                    if (provider != null)
                    {
                        var prop = provider.GetProperty(cell, tilePropertyName);
                        if (!string.IsNullOrEmpty(prop))
                        {
                            if (prop.Equals("true", StringComparison.OrdinalIgnoreCase)) overrideWalkable = true;
                            else if (prop.Equals("false", StringComparison.OrdinalIgnoreCase)) overrideWalkable = false;
                        }
                    }

                    bool isWalkable = true;
                    if (overrideWalkable.HasValue) isWalkable = overrideWalkable.Value;
                    else
                    {
                        // Sample the cell to determine collision coverage
                        // Note: SuperTile2Unity uses a single PolygonCollider2D per layer
                        // covering multiple tiles (potentially non-contiguous)
                        float cellWidth = tilemap.cellSize.x;
                        float cellHeight = tilemap.cellSize.y;
                        
                        // IMPORTANT: Use our own coordinate system based on originWorld
                        // to ensure consistency with NavGrid.CellCenterWorld()
                        Vector2 center = new Vector2(
                            asset.originWorld.x + (x + 0.5f) * cellWidth,
                            asset.originWorld.y + (y + 0.5f) * cellHeight
                        );
                        
                        float pad = Mathf.Clamp01(padding);
                        Vector2 boxSize = new Vector2(cellWidth * (1f - pad), cellHeight * (1f - pad));

                        // Calculate cell bounds for filtering hits
                        float cellMinX = center.x - cellWidth * 0.5f;
                        float cellMaxX = center.x + cellWidth * 0.5f;
                        float cellMinY = center.y - cellHeight * 0.5f;
                        float cellMaxY = center.y + cellHeight * 0.5f;

                        if (useFastCoverage)
                        {
                            Collider2D[] hits = Physics2D.OverlapBoxAll(center, boxSize, 0f, staticLayerMask.value);
                            if (hits == null || hits.Length == 0) isWalkable = true;
                            else
                            {
                                var polyHits = hits.Where(h => h is PolygonCollider2D).ToArray();
                                if (polyHits == null || polyHits.Length == 0) isWalkable = true;
                                else
                                {
                                    float cellMinXPad = center.x - cellWidth * 0.5f * (1f - pad);
                                    float cellMinYPad = center.y - cellHeight * 0.5f * (1f - pad);
                                    float cellMaxXPad = center.x + cellWidth * 0.5f * (1f - pad);
                                    float cellMaxYPad = center.y + cellHeight * 0.5f * (1f - pad);
                                    float cellArea = boxSize.x * boxSize.y;
                                    float coveredArea = 0f;
                                    foreach (var c in polyHits)
                                    {
                                        if (c == null) continue;
                                        Bounds b = c.bounds;
                                        float interMinX = Math.Max(cellMinXPad, b.min.x);
                                        float interMaxX = Math.Min(cellMaxXPad, b.max.x);
                                        float interMinY = Math.Max(cellMinYPad, b.min.y);
                                        float interMaxY = Math.Min(cellMaxYPad, b.max.y);
                                        if (interMaxX > interMinX && interMaxY > interMinY)
                                        {
                                            coveredArea += (interMaxX - interMinX) * (interMaxY - interMinY);
                                        }
                                    }
                                    float coverage = (cellArea <= 0f) ? 0f : Mathf.Clamp01(coveredArea / cellArea);
                                    isWalkable = coverage < coverageThreshold;
                                }
                            }
                        }
                        else
                        {
                            int res = Mathf.Max(1, sampleResolution);
                            int hitCount = 0;
                            
                            // Get all polygon colliders that might intersect this cell
                            Collider2D[] hits = Physics2D.OverlapBoxAll(center, boxSize, 0f, staticLayerMask.value);
                            var polyColliders = hits.Where(h => h is PolygonCollider2D).Cast<PolygonCollider2D>().ToList();
                            
                            if (polyColliders.Count > 0)
                            {
                                // Sample the cell and test each sample against all polygon colliders
                                for (int sy = 0; sy < res; sy++)
                                {
                                    for (int sx = 0; sx < res; sx++)
                                    {
                                        // Map sample to [0,1] range, then to cell space [-0.5, +0.5] * boxSize
                                        float u = (sx + 0.5f) / res;
                                        float v = (sy + 0.5f) / res;
                                        
                                        float fx = u - 0.5f;
                                        float fy = v - 0.5f;
                                        
                                        Vector2 sampleWorldPos = new Vector2(
                                            center.x + fx * boxSize.x,
                                            center.y + fy * boxSize.y
                                        );
                                        
                                        // Test if this sample point is inside any of the polygon colliders
                                        bool isInsideAnyPolygon = false;
                                        foreach (var poly in polyColliders)
                                        {
                                            Vector2 localPoint = poly.transform.InverseTransformPoint(sampleWorldPos);
                                            
                                            for (int pathIndex = 0; pathIndex < poly.pathCount; pathIndex++)
                                            {
                                                Vector2[] polygonPath = poly.GetPath(pathIndex);
                                                if (IsPointInPolygon(localPoint, polygonPath))
                                                {
                                                    isInsideAnyPolygon = true;
                                                    break;
                                                }
                                            }
                                            
                                            if (isInsideAnyPolygon) break;
                                        }
                                        
                                        if (isInsideAnyPolygon) hitCount++;
                                    }
                                }
                            }
                            
                            float sampleCoverage = (float)hitCount / (res * res);
                            isWalkable = sampleCoverage < coverageThreshold;
                        }
                    }

                    asset.walkable[y * w + x] = isWalkable;
                }
            }

            string rootFolder = "Assets/MapData";
            string folder = "Assets/MapData/NavGrids";
            if (!AssetDatabase.IsValidFolder("Assets/MapData")) AssetDatabase.CreateFolder("Assets", "MapData");
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(rootFolder, "NavGrids");
            string path = Path.Combine(folder, mapObject.name + "_NavGrid.asset").Replace("\\", "/");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            outPath = path;
            return true;
        }

        // Point-in-polygon test using ray casting algorithm
        // https://en.wikipedia.org/wiki/Point_in_polygon
        private static bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
        {
            if (polygon == null || polygon.Length < 3) return false;
            
            bool inside = false;
            int j = polygon.Length - 1;
            
            for (int i = 0; i < polygon.Length; i++)
            {
                if ((polygon[i].y > point.y) != (polygon[j].y > point.y) &&
                    point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
                {
                    inside = !inside;
                }
                j = i;
            }
            
            return inside;
        }

        // Check if polygon vertices are in clockwise order using signed area
        private static bool IsClockwise(Vector2[] polygon)
        {
            if (polygon == null || polygon.Length < 3) return false;
            
            float sum = 0f;
            for (int i = 0; i < polygon.Length; i++)
            {
                Vector2 v1 = polygon[i];
                Vector2 v2 = polygon[(i + 1) % polygon.Length];
                sum += (v2.x - v1.x) * (v2.y + v1.y);
            }
            
            return sum > 0f; // Positive = clockwise in Unity's coordinate system
        }
    }
}
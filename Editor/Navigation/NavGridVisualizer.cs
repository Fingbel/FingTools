using UnityEngine;
using FingTools.Internal;

#if UNITY_EDITOR
using UnityEditor;

namespace FingTools.Internal
{
    [ExecuteAlways]
    public class NavGridVisualizer : MonoBehaviour
    {
        public NavGrid grid;
        public Color walkableColor = new Color(0f, 1f, 0f, 0.25f);
        public Color blockedColor = new Color(1f, 0f, 0f, 0.5f);
        public bool showOnlyWhenSelected = true;
        public int maxCellsToDraw = 20000; // avoid drawing huge grids
        public bool autoSwitchToLoadedMap = true;

        void Update()
        {
            // In edit mode, sync grid to currently loaded map (MapManager) if requested
            if (!autoSwitchToLoadedMap) return;
            if (MapManager.Instance == null) return;
            string mapName = MapManager.Instance.LoadedMapObject;
            if (string.IsNullOrEmpty(mapName)) return;
            string path = $"Assets/MapData/NavGrids/{mapName}_NavGrid.asset";
            var loaded = AssetDatabase.LoadAssetAtPath<NavGrid>(path);
            if (loaded != null)
            {
                if (grid != loaded) grid = loaded;
            }
            else
            {
                // offer to bake if grid missing and we are editing
                if (grid == null || grid.sourceMapName != mapName)
                {
                    // prompt once per missing map
                    if (Application.isEditor && !Application.isPlaying)
                    {
                        bool doBake = EditorUtility.DisplayDialog("NavGrid missing", $"NavGrid for map '{mapName}' not found. Bake now?", "Bake", "Skip");
                        if (doBake)
                        {
                            // find the loaded map GameObject
                            GameObject mapGO = null;
                            if (MapLoader.IsInitialized)
                            {
                                mapGO = MapLoader.Instance.SpawnedMaps.Find(m => m.name == mapName);
                            }
                            if (mapGO == null)
                            {
                                // try find in scene roots
                                var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
                                foreach (var r in roots) if (r.name == mapName) { mapGO = r; break; }
                            }
                            if (mapGO != null)
                            {
                                // open the Bake NavGrid window so user can bake for this map
                                EditorApplication.ExecuteMenuItem("FingTools/Navigation/Bake NavGrid");
                            }
                        }
                    }
                }
            }
        }

        void OnDrawGizmosSelected()
        {
            if (showOnlyWhenSelected == false) return; // handled in OnDrawGizmos
            DrawGrid();
        }

        void OnDrawGizmos()
        {
            if (showOnlyWhenSelected) return;
            DrawGrid();
        }

        void DrawGrid()
        {
            if (grid == null) return;
            if (!grid.IsValid) return;

            int w = grid.width;
            int h = grid.height;
            if (w * h > maxCellsToDraw) return;

            Matrix4x4 old = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.identity;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool walk = grid.IsWalkableCell(x, y);
                    Vector2 center = grid.CellCenterWorld(x, y);
                    Vector3 c = new Vector3(center.x, center.y, transform.position.z);
                    Vector3 size = new Vector3(grid.cellSize.x, grid.cellSize.y, 0f);

                    Gizmos.color = walk ? walkableColor : blockedColor;
                    Gizmos.DrawCube(c, new Vector3(size.x, size.y, 0.01f));
                }
            }

            Gizmos.matrix = old;
        }
    }
}
#endif

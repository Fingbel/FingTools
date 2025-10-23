using System;
using System.Collections.Generic;
using UnityEngine;

namespace FingTools.Internal
{
    /// <summary>
    /// Runtime singleton that holds the currently active NavGrid and exposes pathfinding helpers.
    /// Keep this component on a persistent GameObject (or create at runtime) to use Navigation services.
    /// </summary>
    public class NavigationManager : MonoBehaviour
    {
        private static NavigationManager instance;
        public static NavigationManager Instance
        {
            get
            {
                if (instance == null)
                {
                    // Try to find an existing instance in the scene
                    instance = FindObjectOfType<NavigationManager>();
                    if (instance == null)
                    {
                        // Create a new GameObject to host the manager
                        var go = new GameObject("NavigationManager");
                        instance = go.AddComponent<NavigationManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        public NavGrid CurrentGrid { get; private set; }

        /// <summary>
        /// Fired when CurrentGrid changes. Passes the new NavGrid (may be null).
        /// </summary>
        public event Action<NavGrid> OnGridChanged;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Set the active grid instance (runtime). This does not load assets.
        /// </summary>
        public void SetGrid(NavGrid grid)
        {
            CurrentGrid = grid;
            OnGridChanged?.Invoke(CurrentGrid);
        }

        /// <summary>
        /// Try to load a NavGrid from Resources at the given path (example: "NavGrids/MyMapGrid").
        /// Returns true if loaded and set.
        /// </summary>
        public bool LoadGridFromResources(string resourcesPath)
        {
            if (string.IsNullOrEmpty(resourcesPath)) return false;
            var loaded = Resources.Load<NavGrid>(resourcesPath);
            if (loaded != null)
            {
                SetGrid(loaded);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Find path in world coordinates using the current grid. Returns null if no grid or no path.
        /// </summary>
        public List<Vector2> FindPathWorld(Vector2 fromWorld, Vector2 toWorld, bool allowDiagonal = true, bool allowCornerCutting = false)
        {
            if (CurrentGrid == null || !CurrentGrid.IsValid) return null;
            return PathfindingService.FindPathWorld(CurrentGrid, fromWorld, toWorld, allowDiagonal, allowCornerCutting);
        }

        /// <summary>
        /// Find path in cell coordinates using the current grid. Returns null if no grid or no path.
        /// </summary>
        public List<PathfindingService.Cell> FindPathCells(int sx, int sy, int gx, int gy, bool allowDiagonal = true, bool allowCornerCutting = false)
        {
            if (CurrentGrid == null || !CurrentGrid.IsValid) return null;
            return PathfindingService.FindPathCells(CurrentGrid, sx, sy, gx, gy, allowDiagonal, allowCornerCutting);
        }
    }
}

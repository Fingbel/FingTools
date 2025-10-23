using System;
using UnityEngine;

namespace FingTools.Internal
{
    [CreateAssetMenu(fileName = "NavGrid", menuName = "FingTools/Navigation/NavGrid")]
    public class NavGrid : ScriptableObject
    {
        public int width;
        public int height;
        public Vector2 originWorld;
        public Vector2 cellSize = Vector2.one;
        public bool[] walkable;
        public float bakedAgentRadius;
        public string sourceMapName;

        public bool IsValid => walkable != null && walkable.Length == width * height;

        public bool IsWalkableCell(int x, int y)
        {
            if (!IsValid) return true;
            if (x < 0 || x >= width || y < 0 || y >= height) return false;
            return walkable[y * width + x];
        }

        public bool IsWalkableWorld(Vector2 worldPos)
        {
            int ix = Mathf.FloorToInt((worldPos.x - originWorld.x) / cellSize.x);
            int iy = Mathf.FloorToInt((worldPos.y - originWorld.y) / cellSize.y);
            return IsWalkableCell(ix, iy);
        }

        public Vector2 CellCenterWorld(int x, int y)
        {
            return new Vector2(originWorld.x + (x + 0.5f) * cellSize.x, originWorld.y + (y + 0.5f) * cellSize.y);
        }
    }

    // Optional interface - implement on MapLoader or Tilemap container to provide per-tile properties
    public interface ITilePropertyProvider
    {
        // Return the property value or null if not present
        string GetProperty(UnityEngine.Vector3Int cell, string propertyName);
    }
}

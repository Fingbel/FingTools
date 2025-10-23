using System;
using System.Collections.Generic;
using UnityEngine;

namespace FingTools.Internal
{
    /// <summary>
    /// Simple grid A* pathfinder that operates on a NavGrid.
    /// Returns a list of world-space waypoints (cell centers) or null if no path.
    /// </summary>
    public static class PathfindingService
    {
        private const float SQRT2 = 1.41421356237f;

        public static List<Vector2> FindPathWorld(NavGrid grid, Vector2 startWorld, Vector2 goalWorld, bool allowDiagonal = true, bool allowCornerCutting = false)
        {
            if (grid == null || !grid.IsValid) return null;

            // Convert world to cell indices (same math as NavGrid.IsWalkableWorld)
            int sx = Mathf.FloorToInt((startWorld.x - grid.originWorld.x) / grid.cellSize.x);
            int sy = Mathf.FloorToInt((startWorld.y - grid.originWorld.y) / grid.cellSize.y);
            int gx = Mathf.FloorToInt((goalWorld.x - grid.originWorld.x) / grid.cellSize.x);
            int gy = Mathf.FloorToInt((goalWorld.y - grid.originWorld.y) / grid.cellSize.y);

            var cells = FindPathCells(grid, sx, sy, gx, gy, allowDiagonal, allowCornerCutting);
            if (cells == null) return null;

            var worldPath = new List<Vector2>(cells.Count);
            foreach (var c in cells)
            {
                worldPath.Add(grid.CellCenterWorld(c.x, c.y));
            }
            return worldPath;
        }

        public struct Cell
        {
            public int x;
            public int y;
            public Cell(int x, int y) { this.x = x; this.y = y; }
        }

        public static List<Cell> FindPathCells(NavGrid grid, int startX, int startY, int goalX, int goalY, bool allowDiagonal = true, bool allowCornerCutting = false)
        {
            if (grid == null || !grid.IsValid) return null;

            int width = grid.width;
            int height = grid.height;

            if (startX < 0 || startX >= width || startY < 0 || startY >= height) return null;
            if (goalX < 0 || goalX >= width || goalY < 0 || goalY >= height) return null;

            int startIdx = startY * width + startX;
            int goalIdx = goalY * width + goalX;

            if (!grid.IsWalkableCell(startX, startY) || !grid.IsWalkableCell(goalX, goalY)) return null;

            // Per-node data
            var gScore = new float[width * height];
            var cameFrom = new int[width * height];
            var closed = new byte[width * height];

            for (int i = 0; i < gScore.Length; i++)
            {
                gScore[i] = float.PositiveInfinity;
                cameFrom[i] = -1;
                closed[i] = 0;
            }

            var openHeap = new BinaryHeap();

            gScore[startIdx] = 0f;
            float startH = Heuristic(startX, startY, goalX, goalY, allowDiagonal);
            openHeap.Push(new Node(startX, startY, 0f, startH, -1));

            while (openHeap.Count > 0)
            {
                Node current = openHeap.Pop();
                int curIdx = current.y * width + current.x;

                if (closed[curIdx] != 0) continue; // already processed
                closed[curIdx] = 1;

                if (curIdx == goalIdx)
                {
                    // Reconstruct path
                    var path = new List<Cell>();
                    int idx = curIdx;
                    while (idx != -1)
                    {
                        int cx = idx % width;
                        int cy = idx / width;
                        path.Add(new Cell(cx, cy));
                        idx = cameFrom[idx];
                    }
                    path.Reverse();
                    return path;
                }

                // Explore neighbors
                for (int oy = -1; oy <= 1; oy++)
                {
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        if (ox == 0 && oy == 0) continue;

                        bool diagonal = (ox != 0 && oy != 0);
                        if (diagonal && !allowDiagonal) continue;

                        int nx = current.x + ox;
                        int ny = current.y + oy;

                        if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                        if (!grid.IsWalkableCell(nx, ny)) continue;

                        if (diagonal && !allowCornerCutting)
                        {
                            // prevent cutting corners: both adjacent cardinal neighbours must be walkable
                            if (!grid.IsWalkableCell(current.x + ox, current.y) || !grid.IsWalkableCell(current.x, current.y + oy))
                                continue;
                        }

                        int neighborIdx = ny * width + nx;
                        if (closed[neighborIdx] != 0) continue;

                        float moveCost = diagonal ? SQRT2 : 1f;
                        float tentativeG = gScore[curIdx] + moveCost;
                        if (tentativeG < gScore[neighborIdx])
                        {
                            gScore[neighborIdx] = tentativeG;
                            cameFrom[neighborIdx] = curIdx;
                            float h = Heuristic(nx, ny, goalX, goalY, allowDiagonal);
                            openHeap.Push(new Node(nx, ny, tentativeG, tentativeG + h, curIdx));
                        }
                    }
                }
            }

            // No path found
            return null;
        }

        private static float Heuristic(int x, int y, int gx, int gy, bool allowDiagonal)
        {
            int dx = Math.Abs(gx - x);
            int dy = Math.Abs(gy - y);
            if (!allowDiagonal)
            {
                return dx + dy; // Manhattan
            }
            else
            {
                // Octile distance
                int min = Math.Min(dx, dy);
                int max = Math.Max(dx, dy);
                return (max - min) + SQRT2 * min;
            }
        }

        private struct Node
        {
            public int x, y;
            public float g;
            public float f;
            public int parentIdx;

            public Node(int x, int y, float g, float f, int parentIdx)
            {
                this.x = x; this.y = y; this.g = g; this.f = f; this.parentIdx = parentIdx;
            }
        }

        /// <summary>
        /// Small binary min-heap keyed on Node.f
        /// </summary>
        private class BinaryHeap
        {
            private List<Node> heap = new List<Node>();

            public int Count => heap.Count;

            public void Push(Node n)
            {
                heap.Add(n);
                SiftUp(heap.Count - 1);
            }

            public Node Pop()
            {
                var root = heap[0];
                int last = heap.Count - 1;
                heap[0] = heap[last];
                heap.RemoveAt(last);
                if (heap.Count > 0) SiftDown(0);
                return root;
            }

            private void SiftUp(int idx)
            {
                while (idx > 0)
                {
                    int parent = (idx - 1) / 2;
                    if (heap[idx].f >= heap[parent].f) break;
                    Swap(idx, parent);
                    idx = parent;
                }
            }

            private void SiftDown(int idx)
            {
                int count = heap.Count;
                while (true)
                {
                    int left = idx * 2 + 1;
                    int right = idx * 2 + 2;
                    int smallest = idx;
                    if (left < count && heap[left].f < heap[smallest].f) smallest = left;
                    if (right < count && heap[right].f < heap[smallest].f) smallest = right;
                    if (smallest == idx) break;
                    Swap(idx, smallest);
                    idx = smallest;
                }
            }

            private void Swap(int a, int b)
            {
                var tmp = heap[a];
                heap[a] = heap[b];
                heap[b] = tmp;
            }
        }
    }
}

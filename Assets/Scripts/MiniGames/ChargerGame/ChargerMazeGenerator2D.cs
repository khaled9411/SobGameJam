using System.Collections.Generic;
using UnityEngine;

namespace SobGameJam.MiniGames.ChargerGame
{
    public class ChargerMazeGenerator2D : MonoBehaviour
    {
        public Vector2 startPosition { get; private set; }
        public Vector2 endPosition { get; private set; }

        private List<GameObject> spawnedObjects = new List<GameObject>();

        /// <summary>
        /// Generates the maze, spawns the walls, and determines start/end positions.
        /// </summary>
        public void GenerateMaze(ChargerGameDataSO data)
        {
            ClearMaze();

            int width = data.baseGridWidth;
            int height = data.baseGridHeight;
            float cellSize = data.cellSize;

            if (data.isFixedMaze)
            {
                Random.InitState(data.fixedMazeSeed);
            }
            else
            {
                Random.InitState(System.Environment.TickCount);
            }

            int gridWidth = width * 2 + 1;
            int gridHeight = height * 2 + 1;
            int[,] maze = new int[gridWidth, gridHeight];

            // Initialize all as walls
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    maze[x, y] = 1; // 1 = Wall
                }
            }

            // Recursive Backtracker to carve paths
            int startX = 1;
            int startY = 1;
            maze[startX, startY] = 0; // 0 = Path

            Stack<Vector2Int> stack = new Stack<Vector2Int>();
            stack.Push(new Vector2Int(startX, startY));

            int effectiveGridWidth = gridWidth > 3 ? gridWidth - 2 : gridWidth;

            int maxX = 0;
            int maxDistanceAtMaxX = 0;
            Vector2Int endCell = new Vector2Int(startX, startY);

            while (stack.Count > 0)
            {
                Vector2Int current = stack.Peek();
                List<Vector2Int> unvisitedNeighbors = GetUnvisitedNeighbors(current.x, current.y, maze, effectiveGridWidth, gridHeight);

                if (unvisitedNeighbors.Count > 0)
                {
                    Vector2Int next = unvisitedNeighbors[Random.Range(0, unvisitedNeighbors.Count)];
                    
                    // Carve path between current and next
                    maze[next.x, next.y] = 0;
                    maze[(current.x + next.x) / 2, (current.y + next.y) / 2] = 0;

                    stack.Push(next);

                    if (next.x > maxX)
                    {
                        maxX = next.x;
                        maxDistanceAtMaxX = stack.Count;
                        endCell = next;
                    }
                    else if (next.x == maxX && stack.Count > maxDistanceAtMaxX)
                    {
                        maxDistanceAtMaxX = stack.Count;
                        endCell = next;
                    }
                }
                else
                {
                    stack.Pop();
                }
            }

            // Manually extend the end cell to the right edge to make it the ONLY path there
            if (gridWidth > 3)
            {
                maze[endCell.x + 1, endCell.y] = 0;
                maze[endCell.x + 2, endCell.y] = 0;
                endCell = new Vector2Int(endCell.x + 2, endCell.y);
            }

            // Instantiate maze geometry
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector2 pos = new Vector2(x * cellSize, y * cellSize);

                    if (maze[x, y] == 1) // Wall
                    {
                        if (data.wallPrefab != null)
                        {
                            
                            Vector3 wallScale = new Vector3(cellSize, cellSize, 1f);
                            
                            GameObject wall = Instantiate(data.wallPrefab, pos, Quaternion.identity, transform);
                            wall.transform.localScale = wallScale;
                            spawnedObjects.Add(wall);
                        }
                    }
                    else // Path
                    {
                        if (data.floorPrefab != null)
                        {
                            GameObject floor = Instantiate(data.floorPrefab, pos, Quaternion.identity, transform);
                            // Floor covers the whole cell area
                            floor.transform.localScale = new Vector3(cellSize, cellSize, 1f);
                            spawnedObjects.Add(floor);
                        }
                    }
                }
            }

            startPosition = new Vector2(startX * cellSize, startY * cellSize);
            endPosition = new Vector2(endCell.x * cellSize, endCell.y * cellSize);
        }

        private List<Vector2Int> GetUnvisitedNeighbors(int x, int y, int[,] maze, int gridWidth, int gridHeight)
        {
            List<Vector2Int> neighbors = new List<Vector2Int>();

            // Check Top (y + 2)
            if (y + 2 < gridHeight - 1 && maze[x, y + 2] == 1) neighbors.Add(new Vector2Int(x, y + 2));
            // Check Bottom (y - 2)
            if (y - 2 > 0 && maze[x, y - 2] == 1) neighbors.Add(new Vector2Int(x, y - 2));
            // Check Right (x + 2)
            if (x + 2 < gridWidth - 1 && maze[x + 2, y] == 1) neighbors.Add(new Vector2Int(x + 2, y));
            // Check Left (x - 2)
            if (x - 2 > 0 && maze[x - 2, y] == 1) neighbors.Add(new Vector2Int(x - 2, y));

            return neighbors;
        }

        public void ClearMaze()
        {
            foreach (GameObject obj in spawnedObjects)
            {
                if (obj != null) Destroy(obj);
            }
            spawnedObjects.Clear();
        }
    }
}

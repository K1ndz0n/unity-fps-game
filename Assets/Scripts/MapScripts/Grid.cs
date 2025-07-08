using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace MapScripts
{
    public class Grid : MonoBehaviour
    {
        public Vector2 gridWorldSize = new Vector2(20, 20);
        public float nodeRadius = 0.5f; // czyli pole ma 1x1 metr
        
        Node[,] grid;
        public float nodeDiameter;
        int gridSizeX, gridSizeY;

        public void Awake()
        {
            nodeDiameter = nodeRadius*2;
            gridSizeX = Mathf.RoundToInt(gridWorldSize.x/nodeDiameter);
            gridSizeY = Mathf.RoundToInt(gridWorldSize.y/nodeDiameter);
        }
        
        public int MaxSize {
            get {
                return gridSizeX * gridSizeY;
            }
        }

        public void CreateGrid()
        {
            int gridSizeX = Mathf.RoundToInt(gridWorldSize.x / (nodeRadius * 2));
            int gridSizeY = Mathf.RoundToInt(gridWorldSize.y / (nodeRadius * 2));
            grid = new Node[gridSizeX, gridSizeY];

            Vector3 bottomLeft = Vector3.zero - Vector3.right * gridWorldSize.x / 2
                                              - Vector3.forward * gridWorldSize.y / 2;

            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    Vector3 worldPoint = bottomLeft + Vector3.right * (x * nodeRadius * 2 + nodeRadius)
                                                    + Vector3.forward * (y * nodeRadius * 2 + nodeRadius);

                    bool hasWall = Physics.CheckSphere(worldPoint, nodeRadius * 0.9f, LayerMask.GetMask("Wall"));
                    bool hasFloor = Physics.CheckSphere(worldPoint, nodeRadius * 0.9f, LayerMask.GetMask("Ground"));

                    bool walkable = hasFloor && !hasWall;

                    grid[x, y] = new Node(walkable, worldPoint, x, y);
                }
            }
        }
        
        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < gridSizeX && y >= 0 && y < gridSizeY;
        }

        public Node GetNodeAt(int x, int y)
        {
            return grid[x, y];
        }
        
        public Node TryGetNode(int x, int y)
        {
            if (x >= 0 && x < gridSizeX && y >= 0 && y < gridSizeY)
            {
                return grid[x, y];
            }
            return null;
        }
        
        public Node NodeFromWorldPoint(Vector3 worldPosition)
        {
            float percentX = (worldPosition.x + gridWorldSize.x / 2) / gridWorldSize.x;
            float percentY = (worldPosition.z + gridWorldSize.y / 2) / gridWorldSize.y;
            percentX = Mathf.Clamp01(percentX);
            percentY = Mathf.Clamp01(percentY);

            int x = Mathf.RoundToInt((gridWorldSize.x - 1) * percentX);
            int y = Mathf.RoundToInt((gridWorldSize.y - 1) * percentY);
            return grid[x, y];
        }

        public List<Node> GetNeighbours(Node node)
        {
            List<Node> neighbours = new List<Node>();

            for (int x = -1; x < 2; x++)
            {
                for (int y = -1; y < 2; y++)
                {
                    if (x == 0 && y == 0) continue;
                    
                    int checkX = node.gridX + x;
                    int checkY = node.gridY + y;
                    
                    if (checkX >= 0 && checkX < grid.GetLength(0) &&
                        checkY >= 0 && checkY < grid.GetLength(1))
                    {
                        neighbours.Add(grid[checkX, checkY]);
                    }
                } 
            }

            return neighbours;
        }
        
        public List<Node> GetCardinalNeighbours(Node node)
        {
            List<Node> neighbours = new List<Node>();

            // Tylko 4 kierunki: góra, dół, lewo, prawo
            int[,] directions = new int[,]
            {
                { 0,  1 },  // Góra
                { 0, -1 },  // Dół
                { -1, 0 },  // Lewo
                { 1,  0 }   // Prawo
            };

            for (int i = 0; i < directions.GetLength(0); i++)
            {
                int checkX = node.gridX + directions[i, 0];
                int checkY = node.gridY + directions[i, 1];

                if (checkX >= 0 && checkX < grid.GetLength(0) &&
                    checkY >= 0 && checkY < grid.GetLength(1))
                {
                    neighbours.Add(grid[checkX, checkY]);
                }
            }

            return neighbours;
        }
        
        public List<Vector3> ConvertNodesToWorldPositions(List<Node> path)
        {
            List<Vector3> positions = new List<Vector3>();
            foreach (Node node in path)
            {
                positions.Add(node.worldPosition);
            }
            return positions;
        }

        public List<Node> path;
        public List<Node> fullPath;
        private void OnDrawGizmos()
        {
            // Rysuj obrys całej siatki (opcjonalnie)
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));

            // Rysuj tylko ścieżkę, jeśli istnieje
            if (fullPath != null)
            {
                Gizmos.color = Color.green;
                foreach (Node node in fullPath)
                {
                    Gizmos.DrawCube(node.worldPosition, Vector3.one * (nodeRadius * 2 - 0.1f));
                }
            }
        }
    }
}
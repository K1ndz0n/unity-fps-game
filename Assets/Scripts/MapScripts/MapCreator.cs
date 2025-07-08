using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MapScripts
{
    public class MapCreator : MonoBehaviour
    {
        public int mapSizeX = 25;
        public int mapSizeY = 25;
        
        public List<MapSegment> mapSegments;
        public List<MapSegment> pathSegments;
        public MapSegment startPathSegment;
        public MapSegment wall;
        
        private Node[,] mapArray;
        private bool isStartRoomSpawned = false;
        
        private struct RoomData
        {
            public int mapX;
            public int mapY;
            public int roomIndex;
            public MapSegment parentSegment;

            public RoomData(int x, int y, int index, MapSegment segment)
            {
                mapX = x;
                mapY = y;
                roomIndex = index;
                parentSegment = segment;
            }
        }

        public void CreateMap()
        {
            startPathSegment.isPathSegment = true;
            foreach (MapSegment segment in pathSegments)
            {
                segment.isPathSegment = true;
            }
            wall.isWall = true;
            mapArray  = new Node[mapSizeX, mapSizeY];
            InitializeArray();

            int startPosX = Random.Range(8, 13);
            int startPosY = Random.Range(8, 13);
            int startRoomIndex = Random.Range(0, 3);
            
            GenerateRoomsIterative(startPosX, startPosY, startRoomIndex);
            GenerateWalls();
        }

        private void InitializeArray()
        {
            for (int x = 0; x < mapSizeX; x++)
            {
                for (int y = 0; y < mapSizeY; y++)
                {
                    mapArray[x, y] = new Node(true, false, x, y);
                }
            }
        }

        private void GenerateRoomsIterative(int startX, int startY, int startRoomIndex)
        {
            Queue<RoomData> roomQueue = new Queue<RoomData>();
            roomQueue.Enqueue(new RoomData(startX, startY, startRoomIndex, null));

            while (roomQueue.Count > 0)
            {
                RoomData currentRoom = roomQueue.Dequeue();
                
                if (CanSpawnSegment(mapSegments[currentRoom.roomIndex], currentRoom.mapX, currentRoom.mapY))
                {
                    MapSegment segment = SpawnSegment(mapSegments[currentRoom.roomIndex], currentRoom.mapX, currentRoom.mapY);

                    if (currentRoom.parentSegment != null)
                    {
                        List<Node> path = FindShortestPathBetweenSegments(segment.connectionNodes,
                            currentRoom.parentSegment.connectionNodes);
                        
                        if (path != null)
                        {
                            CreatePath(path);
                        }
                        else
                        {
                          //  Debug.Log("nie znaleziono ścieżki");
                        }
                    }

                    foreach (MapSegment.ConnectionPoint cp in mapSegments[currentRoom.roomIndex].connectionPoints)
                    {
                        int nextRoomIndex = Random.Range(3, mapSegments.Count);
                        int nextRoomSizeX = mapSegments[nextRoomIndex].sizeX;
                        int nextRoomSizeY = mapSegments[nextRoomIndex].sizeY;

                        int newX = -1;
                        int newY = -1;

                        if (cp.direction == 1)
                        {
                            if (IsInBounds(currentRoom.mapX + cp.x - 1, currentRoom.mapY + cp.y))
                            {
                                if (mapArray[currentRoom.mapX + cp.x - 1, currentRoom.mapY + cp.y].isConnected)
                                {
                                    continue;
                                }
                            }
                            newX = currentRoom.mapX + cp.x - nextRoomSizeX - 3;
                            newY = currentRoom.mapY + cp.y + Random.Range(-2, 3);
                        }
                        else if (cp.direction == 2)
                        {
                            if (IsInBounds(currentRoom.mapX + cp.x, currentRoom.mapY + cp.y - 1))
                            {
                                if (mapArray[currentRoom.mapX + cp.x, currentRoom.mapY + cp.y - 1].isConnected)
                                {
                                    continue;
                                }
                            }
                            newX = currentRoom.mapX + cp.x + Random.Range(-2, 3);
                            newY = currentRoom.mapY + cp.y - nextRoomSizeY - 3;
                        }
                        else if (cp.direction == 3)
                        {
                            if (IsInBounds(currentRoom.mapX + cp.x + 1, currentRoom.mapY + cp.y))
                            {
                                if (mapArray[currentRoom.mapX + cp.x + 1, currentRoom.mapY + cp.y].isConnected)
                                {
                                    continue;
                                }
                            }
                            newX = currentRoom.mapX + cp.x + 4;
                            newY = currentRoom.mapY + cp.y + Random.Range(-2, 3);
                        }
                        else if (cp.direction == 4)
                        {
                            if (IsInBounds(currentRoom.mapX + cp.x, currentRoom.mapY + cp.y + 1))
                            {
                                if (mapArray[currentRoom.mapX + cp.x, currentRoom.mapY + cp.y + 1].isConnected)
                                {
                                    continue;
                                }
                            }
                            newX = currentRoom.mapX + cp.x + Random.Range(-2, 3);
                            newY = currentRoom.mapY + cp.y + 4;
                        }

                        if (newX != -1 && newY != -1)
                        {
                            roomQueue.Enqueue(new RoomData(newX, newY, nextRoomIndex, segment));
                        }
                    }
                }
            }
        }

        public void GenerateWalls()
        {
            foreach (Node n in mapArray)
            {
                if (n.isConnectionNode && !n.isConnected || n.isSpacer && !n.isPathNode)
                {
                    SpawnSegment(wall, n.gridX, n.gridY);
                }
            }
            
            Vector3 bottomLeft = Vector3.zero - Vector3.right * mapSizeX * 4.75f - Vector3.forward * mapSizeY * 4.75f;
            
            for (int x = -1; x < mapSizeX + 1; x++)
            {
                for (int y = -1; y < mapSizeY + 1; y++)
                {
                    if (x == -1 || x == mapSizeX || y == -1 || y == mapSizeY)
                    {
                        Vector3 worldPoint = bottomLeft + Vector3.right * x * 10 + Vector3.forward * y * 10;
                        Instantiate(wall, worldPoint, quaternion.identity);
                    }
                }
            }
        }
        
        private List<Node> FindShortestPathBetweenSegments(List<Node> nodesA, List<Node> nodesB)
        {
            List<Node> shortestPath = null;
            float shortestLength = float.MaxValue;

            foreach (Node nodeA in nodesA)
            {
                foreach (Node nodeB in nodesB)
                {
                    if (nodeA.ownerSegment == nodeB.ownerSegment)
                        continue;

                    List<Node> path = FindPath(nodeA, nodeB);

                    if (path != null && path.Count < shortestLength)
                    {
                        shortestLength = path.Count;
                        shortestPath = path;
                    }
                }
            }

            return shortestPath;
        }

        private MapSegment SpawnSegment(MapSegment segment, int mapX, int mapY)
        {
            Vector3 bottomLeft = Vector3.zero - Vector3.right * mapSizeX * 4.75f - Vector3.forward * mapSizeY * 4.75f;
            Vector3 worldPoint = bottomLeft + Vector3.right * mapX * 10 + Vector3.forward * mapY * 10;
            
            for (int x = mapX; x < mapX + segment.sizeX; x++)
            {
                for (int y = mapY; y < mapY + segment.sizeY; y++)
                {
                    mapArray[x, y].isEmpty = false;
                }
            }
            
            MapSegment newSegment = Instantiate(segment, worldPoint, quaternion.identity);
            if (mapArray[mapX, mapY].isConnectionNode || newSegment.isWall)
            {
                return segment;
            }
            newSegment.connectionNodes = new List<Node>();
            SpawnConnectionNodes(newSegment, mapX, mapY);
            
            for (int x = mapX - 1; x <= mapX + newSegment.sizeX; x++)
            {
                for (int y = mapY - 1; y <= mapY + newSegment.sizeY; y++)
                {
                    if (newSegment.isPathSegment)
                    {
                        if (x >= 0 && x < mapSizeX && y >= 0 && y < mapSizeY)
                        {
                            if (mapArray[x, y].isEmpty)
                            {
                                mapArray[x, y].isEmpty = false;
                                mapArray[x, y].isSpacer = true;
                            }
                        }
                    }
                    else
                    {
                        if (mapArray[x, y].isEmpty)
                        {
                            mapArray[x, y].isEmpty = false;
                            mapArray[x, y].isSpacer = true;
                        }
                    }
                }
            }
            
            return newSegment;
        }
        
        private void SpawnConnectionNodes(MapSegment segment, int mapX, int mapY)
        {
            foreach (MapSegment.ConnectionPoint cp in segment.connectionPoints)
            {
                if (cp.direction == 1)
                {
                    if (CanSpawnSegment(startPathSegment, mapX + cp.x - 1, mapY + cp.y))
                    {
                        mapArray[mapX + cp.x - 1, mapY + cp.y].isConnectionNode = true;
                        SpawnSegment(startPathSegment, mapX + cp.x - 1, mapY + cp.y);
                        mapArray[mapX + cp.x - 1, mapY + cp.y].ownerSegment = segment;
                        segment.connectionNodes.Add(mapArray[mapX + cp.x - 1, mapY + cp.y]);
                    }
                }
                else if (cp.direction == 2)
                {
                    if (CanSpawnSegment(startPathSegment, mapX + cp.x, mapY + cp.y - 1))
                    {
                        mapArray[mapX + cp.x, mapY + cp.y - 1].isConnectionNode = true;
                        SpawnSegment(startPathSegment, mapX + cp.x, mapY + cp.y - 1);
                        mapArray[mapX + cp.x, mapY + cp.y - 1].ownerSegment = segment;
                        segment.connectionNodes.Add(mapArray[mapX + cp.x, mapY + cp.y - 1]);
                    }
                }
                else if (cp.direction == 3)
                {
                    if (CanSpawnSegment(startPathSegment, mapX + cp.x + 1, mapY + cp.y))
                    {
                        mapArray[mapX + cp.x + 1, mapY + cp.y].isConnectionNode = true;
                        SpawnSegment(startPathSegment, mapX + cp.x + 1, mapY + cp.y);
                        mapArray[mapX + cp.x + 1, mapY + cp.y].ownerSegment = segment;
                        segment.connectionNodes.Add(mapArray[mapX + cp.x + 1, mapY + cp.y]);
                    }
                }
                else if (cp.direction == 4)
                {
                    if (CanSpawnSegment(startPathSegment, mapX + cp.x, mapY + cp.y + 1))
                    {
                        mapArray[mapX + cp.x, mapY + cp.y + 1].isConnectionNode = true;
                        SpawnSegment(startPathSegment, mapX + cp.x, mapY + cp.y + 1);
                        mapArray[mapX + cp.x, mapY + cp.y + 1].ownerSegment = segment;
                        segment.connectionNodes.Add(mapArray[mapX + cp.x, mapY + cp.y + 1]);
                    }
                }
            }
        }

        private bool CanSpawnSegment(MapSegment segment, int mapX, int mapY)
        {
            for (int x = mapX; x < mapX + segment.sizeX; x++)
            {
                for (int y = mapY; y < mapY + segment.sizeY; y++)
                {
                    if (!IsInBounds(x, y) || !mapArray[x, y].isEmpty)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        
        private bool IsInBounds(int x, int y)
        {
            return x >= 2 && x < mapSizeX - 2 && y >= 2 && y < mapSizeY - 2;
        }
        
        private List<Node> FindPath(Node startPos, Node endPos)
        {
            int safetyCounter = 0;
            int maxIterations = 100000;

            Heap<Node> openSet = new Heap<Node>(mapSizeX * mapSizeY);
            HashSet<Node> closedSet = new HashSet<Node>();
            openSet.Add(startPos);

            while (openSet.Count > 0)
            {
                safetyCounter++;
                if (safetyCounter > maxIterations)
                {
                    return null;
                }

                Node currentNode = openSet.RemoveFirst();
                closedSet.Add(currentNode);
                
                if (currentNode == endPos)
                {
                    return GetPath(startPos, endPos);
                }

                foreach (Node neighbour in GetNeighbours(currentNode))
                {
                    if (!neighbour.isEmpty && !neighbour.isConnectionNode || closedSet.Contains(neighbour)) continue;
                    
                    int newCost = currentNode.gCost + GetDistance(currentNode, neighbour);
                    if (newCost < neighbour.gCost || !openSet.Contains(neighbour))
                    {
                        neighbour.gCost = newCost;
                        neighbour.hCost = GetDistance(neighbour, endPos);
                        neighbour.parent = currentNode;
                        if (!openSet.Contains(neighbour))
                        {
                            openSet.Add(neighbour);
                        }
                    }
                }
            }

            return null;
        }

        private List<Node> GetPath(Node startNode, Node endNode)
        {
            List<Node> path = new List<Node>();
            Node currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode);
                currentNode = currentNode.parent;
            }
            path.Add(startNode);
            path.Reverse();

            return path;
        }

        private void CreatePath(List<Node> path)
        {
            for (int i = 1; i < path.Count - 1; i++)
            {
                Node n = path[i];
                int index = Random.Range(0, pathSegments.Count);
                SpawnSegment(pathSegments[index], n.gridX, n.gridY);
                n.isPathNode = true;
            }

            path[0].isConnected = true;
            path[^1].isConnected = true;
        }
        
        private int GetDistance(Node startNode, Node endNode)
        {
            int distanceX = Mathf.Abs(startNode.gridX - endNode.gridX);
            int distanceY = Mathf.Abs(startNode.gridY - endNode.gridY);

            return 10 * (distanceX + distanceY);
        }
        
        private List<Node> GetNeighbours(Node node)
        {
            List<Node> neighbours = new List<Node>();
            
            int[,] directions = new int[,]
            {
                { -1, 0 }, // Lewo
                { 1, 0 },  // Prawo
                { 0, -1 }, // Dół
                { 0, 1 }   // Góra
            };

            for (int i = 0; i < 4; i++)
            {
                int checkX = node.gridX + directions[i, 0];
                int checkY = node.gridY + directions[i, 1];

                if (checkX >= 0 && checkX < mapArray.GetLength(0) &&
                    checkY >= 0 && checkY < mapArray.GetLength(1))
                {
                    neighbours.Add(mapArray[checkX, checkY]);
                }
            }

            return neighbours;
        }
        
        private void OnDrawGizmos()
        {
            if (mapArray == null)
                return;

            for (int x = 0; x < mapSizeX; x++)
            {
                for (int y = 0; y < mapSizeY; y++)
                {
                    Vector3 bottomLeft = Vector3.zero - Vector3.right * mapSizeX * 4.75f - Vector3.forward * mapSizeY * 4.75f;
                    Vector3 worldPoint = bottomLeft + Vector3.right * x * 10 + Vector3.forward * y * 10;

                    if (!mapArray[x, y].isEmpty)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawCube(worldPoint, Vector3.one * 8f);
                    }
                    else
                    {
                        Gizmos.color = Color.white;
                        Gizmos.DrawWireCube(worldPoint, Vector3.one * 8f);
                    }

                    if (mapArray[x, y].isConnectionNode)
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawSphere(worldPoint, 4f);
                    }
                    
                    if (mapArray[x, y].isConnected)
                    {
                        Gizmos.color = Color.blue;
                        Gizmos.DrawSphere(worldPoint, 4f);
                    }
                }
            }
        }
    }
}

using UnityEngine;

namespace MapScripts
{
    public class Node : IHeapItem<Node>
    {
        public Node parent;
        public bool walkable;
        public Vector3 worldPosition;
        public int gridX;
        public int gridY;
        public bool isEmpty;
        public bool isConnectionNode;
        public bool isPathNode = false;
        public MapSegment ownerSegment;
        public bool isConnected = false;
        public bool isSpacer = false;

        public int gCost;
        public int hCost;
        public int heapIndex;

        public Node(bool walkable, Vector3 worldPosition, int x, int y)
        {
            this.walkable = walkable;
            this.worldPosition = worldPosition;
            this.gridX = x;
            this.gridY = y;
        }

        public Node(bool isEmpty, bool isConnectionNode, int x, int y)
        {
            this.isEmpty = isEmpty;
            this.isConnectionNode = this.isConnectionNode;
            this.gridX = x;
            this.gridY = y;
        }
        
        public Node(Node original)
        {
            this.walkable = original.walkable;
            this.worldPosition = original.worldPosition;
            this.gridX = original.gridX;
            this.gridY = original.gridY;
            this.isEmpty = original.isEmpty;
            this.isConnectionNode = original.isConnectionNode;

            // Głębokie kopiowanie - relacje nie powinny przechodzić
            this.parent = null;
            this.ownerSegment = null;

            this.isConnected = original.isConnected;

            this.gCost = original.gCost;
            this.hCost = original.hCost;
            this.heapIndex = original.heapIndex;
        }
        
        public int HeapIndex {
            get {
                return heapIndex;
            }
            set {
                heapIndex = value;
            }
        }

        public int GetFCost()
        {
            return gCost + hCost;
        }

        public int CompareTo(Node toCompare)
        {
            int compare = GetFCost().CompareTo(toCompare.GetFCost());
            if (compare == 0)
            {
                compare = hCost.CompareTo(toCompare.hCost);
            }

            return -compare;
        }
    }
}
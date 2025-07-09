using System.Collections.Generic;
using UnityEngine;

namespace MapScripts
{
    public class MapSegment : MonoBehaviour
    {
        public int sizeX;
        public int sizeY;
        
        [System.Serializable]
        public struct ConnectionPoint
        {
            public int x;
            public int y;
            public int direction; // 1 - lewo, 2- dol, 3- prawo, 4 - gora

            public ConnectionPoint(int x, int y, int direction)
            {
                this.x = x;
                this.y = y;
                this.direction = direction;
            }
        }
        public List<ConnectionPoint> connectionPoints;
        public List<Node> connectionNodes;
        
        public bool isPathSegment = false;
        public bool isWall = false;
    }
}
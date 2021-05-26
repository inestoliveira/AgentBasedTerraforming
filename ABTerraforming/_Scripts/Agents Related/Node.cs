using UnityEngine;

public class Node : IHeapItem<Node>
{
    public bool walkable;
    public int gridX;
    public int gridY;

    public int gCost;
    public int hCost;
    public Node parent;

    public Node(int _gridX, int _gridY)
    {
        walkable = true;
        gridX = _gridX;
        gridY = _gridY;
    }

    public Vector2 ToVector2()
    {
        return new Vector2(gridX, gridY);
    }

    public int FCost
    {
        get { return gCost + hCost; }
    }

    public int HeapIndex { get; set; }

    public int CompareTo(Node nodeToCompare)
    {
        int compare = FCost.CompareTo(nodeToCompare.FCost);
        if (compare == 0)
        {
            compare = hCost.CompareTo(nodeToCompare.hCost);
        }
        return -compare;
    }

    public class Point
    {
        public int x;
        public int y;

        public float height;

        public Point(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public override bool Equals(object obj)
        {
            Point point = (Point)obj;
            return point.x == x && point.y == y ? true : false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
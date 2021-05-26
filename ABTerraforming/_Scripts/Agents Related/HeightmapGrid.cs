using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HeightmapGrid
{
    public float[,] heightmap;
    public Node[,] grid;
    public int heightmapSize;

    public List<Node.Point> coastlinePoints;
    public List<Node.Point> landmassPoints;
    public List<Node.Point> hillsPoints;
    public List<Node.Point> mountainsPoints;
    public List<Node.Point> beachPoints;
    public List<Node.Point> lakePoints;
    public List<Node.Point> riverPoints;

    // Threads' lists
    public Dictionary<int, List<Node.Point>> threadCoastlinePoints;
    public Dictionary<int, List<Node.Point>> threadFloodPoints;
    public Dictionary<int, List<Node.Point>> threadHillPoints;
    public Dictionary<int, List<Node.Point>> threadMountainPoints;
    public Dictionary<int, List<Node.Point>> threadBeachPoints;
    public Dictionary<int, List<Node.Point>> threadRiverPoints;
    public Dictionary<int, List<Node.Point>> threadLakePoints;
    public Dictionary<int, List<Node.Point>> threadSmoothPoints;

    static System.Random rng;

    public HeightmapGrid(float[,] heightmap, Node[,] grid)
    {
        this.heightmap = heightmap;
        this.grid = grid;
        heightmapSize = heightmap.GetLength(0);

        coastlinePoints = new List<Node.Point>();
        landmassPoints = new List<Node.Point>();
        hillsPoints = new List<Node.Point>();
        mountainsPoints = new List<Node.Point>();
        beachPoints = new List<Node.Point>();
        lakePoints = new List<Node.Point>();
        riverPoints = new List<Node.Point>();

        rng = new System.Random(13);

        // threads
        threadCoastlinePoints = new Dictionary<int, List<Node.Point>>();
        threadFloodPoints = new Dictionary<int, List<Node.Point>>();
        threadHillPoints = new Dictionary<int, List<Node.Point>>();
        threadMountainPoints = new Dictionary<int, List<Node.Point>>();
        threadBeachPoints = new Dictionary<int, List<Node.Point>>();
        threadRiverPoints = new Dictionary<int, List<Node.Point>>();
        threadLakePoints = new Dictionary<int, List<Node.Point>>();
        threadSmoothPoints = new Dictionary<int, List<Node.Point>>();
    }
    
    public enum ThreadList { Hills, Mountain, Beach }
    public bool ContainsOnList(Node.Point point, ThreadList threadList)
    {
        Dictionary<int, List<Node.Point>> temp = null;
        switch (threadList)
        {
            case ThreadList.Hills:
                temp = threadHillPoints;
                break;
            case ThreadList.Mountain:
                temp = threadMountainPoints;
                break;
            case ThreadList.Beach:
                temp = threadBeachPoints;
                break;
        }

        foreach (int key in temp.Keys)
        {
            if (temp[key].Contains(point)) return true;
        }
        return false;
    }

    public List<Node> FindPath(Node startNode, Node targetNode)
    {
        List<Node> waypoints = null;
        bool pathSuccess = false;
        startNode.parent = startNode;

        if (startNode.walkable && targetNode.walkable)
        {
            Heap<Node> openSet = new Heap<Node>(heightmapSize * heightmapSize);
            HashSet<Node> closedSet = new HashSet<Node>();
            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                Node currentNode = openSet.RemoveFirst();
                closedSet.Add(currentNode);

                if (currentNode.Equals(targetNode))
                {
                    pathSuccess = true;
                    break;
                }
                foreach (Node neighbour in GetNeighbours(currentNode))
                {
                    if (!neighbour.walkable || closedSet.Contains(neighbour))
                    {
                        continue;
                    }

                    int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);
                    if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                    {
                        neighbour.gCost = newMovementCostToNeighbour;
                        neighbour.hCost = GetDistance(neighbour, targetNode);
                        neighbour.parent = currentNode;

                        if (!openSet.Contains(neighbour))
                            openSet.Add(neighbour);
                        else
                            openSet.UpdateItem(neighbour);
                    }
                }
            }
        }
        if (pathSuccess)
        {
            waypoints = RetracePath(startNode, targetNode);
        }
        return waypoints;
    }

    List<Node> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;
        while (!currentNode.Equals(startNode))
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Reverse();
        return path;
    }

    int GetDistance(Node nodeA, Node nodeB)
    {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

        if (dstX > dstY)
            return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
    }

    public List<Node> GetNeighbours(Node targetNode)
    {
        List<Node> neighbours = new List<Node>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                    continue;

                int checkX = targetNode.gridX + x;
                int checkY = targetNode.gridY + y;

                if (checkX >= 0 && checkX < heightmapSize && checkY >= 0 && checkY < heightmapSize)
                {
                    neighbours.Add(grid[checkX, checkY]);
                }
            }
        }
        return neighbours;
    }

    public List<Node> GetNeighboursByLayers(Node targetNode, int layerCount)
    {
        List<Node> neighbours = new List<Node>();

        for (int x = -layerCount; x <= layerCount; x++)
        {
            for (int y = -layerCount; y <= layerCount; y++)
            {
                if (x == 0 && y == 0)
                    continue;

                int checkX = targetNode.gridX + x;
                int checkY = targetNode.gridY + y;

                if (checkX >= 0 && checkX < heightmapSize && checkY >= 0 && checkY < heightmapSize)
                {
                    neighbours.Add(grid[checkX, checkY]);
                }
            }
        }
        return neighbours;
    }

    public void SetHeight(Node node, float height)
    {
        heightmap[node.gridX, node.gridY] = height;
    }

    public float GetHeight(Node node)
    {
        return heightmap[node.gridX, node.gridY];
    }

    public static float RandomFloat(float minimum, float maximum)
    {
        return (float)(rng.NextDouble() * (maximum - minimum) + minimum);
    }

    #region Threads
    public List<Node.Point> GetNeighbours(Node.Point targetNode, int heightmapSize)
    {
        List<Node.Point> neighbours = new List<Node.Point>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                    continue;

                int checkX = targetNode.x + x;
                int checkY = targetNode.y + y;

                if (checkX >= 0 && checkX < heightmapSize && checkY >= 0 && checkY < heightmapSize)
                {
                    neighbours.Add(new Node.Point(checkX, checkY));
                }
            }
        }
        return neighbours;
    }

    public List<Node> GetNeighbours(Node targetNode, Node[,] ownGrid)
    {
        List<Node> neighbours = new List<Node>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                    continue;

                int checkX = targetNode.gridX + x;
                int checkY = targetNode.gridY + y;

                if (checkX >= 0 && checkX < heightmapSize && checkY >= 0 && checkY < heightmapSize)
                {
                    neighbours.Add(ownGrid[checkX, checkY]);
                }
            }
        }
        return neighbours;
    }

    public List<Node> ConcurrentFindPath(Node startNode, Node targetNode, Node[,] ownGrid)
    {
        List<Node> waypoints = null;
        bool pathSuccess = false;
        startNode.parent = startNode;

        if (startNode.walkable && targetNode.walkable)
        {
            Heap<Node> openSet = new Heap<Node>(heightmapSize * heightmapSize);
            HashSet<Node> closedSet = new HashSet<Node>();
            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                Node currentNode = openSet.RemoveFirst();
                closedSet.Add(currentNode);

                if (currentNode.Equals(targetNode))
                {
                    pathSuccess = true;
                    break;
                }
                foreach (Node neighbour in GetNeighbours(currentNode, ownGrid))
                {
                    if (!neighbour.walkable || closedSet.Contains(neighbour))
                    {
                        continue;
                    }

                    int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);
                    if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                    {
                        neighbour.gCost = newMovementCostToNeighbour;
                        neighbour.hCost = GetDistance(neighbour, targetNode);
                        neighbour.parent = currentNode;

                        if (!openSet.Contains(neighbour))
                            openSet.Add(neighbour);
                        else
                            openSet.UpdateItem(neighbour);
                    }
                }
            }
        }
        if (pathSuccess)
        {
            waypoints = RetracePath(startNode, targetNode);
        }
        return waypoints;
    }
    #endregion
}
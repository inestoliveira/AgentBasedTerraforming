using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System;
using System.Linq;

public class SmoothAgents
{
    #region Sequential
    public static void Sequential(HeightmapGrid heightmapGrid, List<Node.Point> nodesList, int iterations)
    {
        for (int t = 0; t < iterations; t++)
        {
            Queue<Node.Point> queue = new Queue<Node.Point>(nodesList);
            while (queue.Count > 0)
            {
                Node.Point point = queue.Dequeue();
                float totalHeight = heightmapGrid.GetHeight(heightmapGrid.grid[point.x, point.y]) * 10;
                foreach (Node neighbour in heightmapGrid.GetNeighbours(heightmapGrid.grid[point.x, point.y]))
                {
                    totalHeight += heightmapGrid.GetHeight(neighbour);
                }
                heightmapGrid.SetHeight(heightmapGrid.grid[point.x, point.y], totalHeight / (heightmapGrid.GetNeighbours(heightmapGrid.grid[point.x, point.y]).Count + 10));
            }
        }
    }
    #endregion

    public enum AgentType { Coastline, Flood, Hill, Mountain, Beach, River, Lake }

    #region Concurrent
    public static void Concurrent(HeightmapGrid heightmapGrid, TerrainData data)
    {
        List<Thread> threadList = new List<Thread>();
        object blocker = new object();
        // First Stage Smooth
        lock (blocker)
        {
            foreach (int key in heightmapGrid.threadCoastlinePoints.Keys)
            {
                Agent agent = new Agent(key, heightmapGrid.threadCoastlinePoints[key], data.coastline.smooth);
                Thread thread = new Thread(() => AgentCall(heightmapGrid, agent, AgentType.Coastline, blocker));
                thread.Start();
                threadList.Add(thread);
            }
        }
        lock (blocker)
        {
            foreach (int key in heightmapGrid.threadFloodPoints.Keys)
            {
                Agent agent = new Agent(key, heightmapGrid.threadFloodPoints[key], data.landmassFilling.smooth);
                Thread thread = new Thread(() => AgentCall(heightmapGrid, agent, AgentType.Flood, blocker));
                thread.Start();
                threadList.Add(thread);
            }
        }
        lock (blocker)
        {
            foreach (int key in heightmapGrid.threadHillPoints.Keys)
            {
                Agent agent = new Agent(key, heightmapGrid.threadHillPoints[key], data.hill[key].smooth);
                Thread thread = new Thread(() => AgentCall(heightmapGrid, agent, AgentType.Hill, blocker));
                thread.Start();
                threadList.Add(thread);
            }
        }
        lock (blocker)
        {
            foreach (int key in heightmapGrid.threadMountainPoints.Keys)
            {
                Agent agent = new Agent(key, heightmapGrid.threadMountainPoints[key], data.mountain[key].smooth);
                Thread thread = new Thread(() => AgentCall(heightmapGrid, agent, AgentType.Mountain, blocker));
                thread.Start();
                threadList.Add(thread);
            }
        }

        int threadsFinished = 0;
        while (threadsFinished < threadList.Count)
        {
            threadsFinished = 0;
            foreach (Thread thread in threadList)
            {
                if (!thread.IsAlive)
                {
                    threadsFinished++;
                }
            }
        }
        foreach (int key in heightmapGrid.threadCoastlinePoints.Keys)
        {
            foreach (Node.Point point in heightmapGrid.threadCoastlinePoints[key])
            {
                heightmapGrid.heightmap[point.x, point.y] = point.height;
            }
        }
        foreach (int key in heightmapGrid.threadFloodPoints.Keys)
        {
            foreach (Node.Point point in heightmapGrid.threadFloodPoints[key])
            {
                heightmapGrid.heightmap[point.x, point.y] = point.height;
            }
        }
        foreach (int key in heightmapGrid.threadHillPoints.Keys)
        {
            foreach (Node.Point point in heightmapGrid.threadHillPoints[key])
            {
                heightmapGrid.heightmap[point.x, point.y] = point.height;
            }
        }
        foreach (int key in heightmapGrid.threadMountainPoints.Keys)
        {
            foreach (Node.Point point in heightmapGrid.threadMountainPoints[key])
            {
                heightmapGrid.heightmap[point.x, point.y] = point.height;
            }
        }
        // Second Stage Smooth
        threadList.Clear();
        lock (blocker)
        {
            foreach (int key in heightmapGrid.threadBeachPoints.Keys)
            {
                Agent agent = new Agent(key, heightmapGrid.threadBeachPoints[key], data.beach[key].smooth);
                Thread thread = new Thread(() => AgentCall(heightmapGrid, agent, AgentType.Beach, blocker));
                thread.Start();
                threadList.Add(thread);
            }
        }
        lock (blocker)
        {
            foreach (int key in heightmapGrid.threadRiverPoints.Keys)
            {
                Agent agent = new Agent(key, heightmapGrid.threadRiverPoints[key], data.river[key].smooth);
                Thread thread = new Thread(() => AgentCall(heightmapGrid, agent, AgentType.River, blocker));
                thread.Start();
                threadList.Add(thread);
            }
        }
        lock (blocker)
        {
            foreach (int key in heightmapGrid.threadLakePoints.Keys)
            {
                Agent agent = new Agent(key, heightmapGrid.threadLakePoints[key], data.lake[key].smooth);
                Thread thread = new Thread(() => AgentCall(heightmapGrid, agent, AgentType.Lake, blocker));
                thread.Start();
                threadList.Add(thread);
            }
        }
        

        threadsFinished = 0;
        while (threadsFinished < threadList.Count)
        {
            threadsFinished = 0;
            foreach (Thread thread in threadList)
            {
                if (!thread.IsAlive)
                {
                    threadsFinished++;
                }
            }
        }
        
        foreach (int key in heightmapGrid.threadBeachPoints.Keys)
        {
            foreach (Node.Point point in heightmapGrid.threadBeachPoints[key])
            {
                heightmapGrid.heightmap[point.x, point.y] = point.height;
            }
        }
        foreach (int key in heightmapGrid.threadRiverPoints.Keys)
        {
            foreach (Node.Point point in heightmapGrid.threadRiverPoints[key])
            {
                heightmapGrid.heightmap[point.x, point.y] = point.height;
            }
        }
        foreach (int key in heightmapGrid.threadLakePoints.Keys)
        {
            foreach (Node.Point point in heightmapGrid.threadLakePoints[key])
            {
                heightmapGrid.heightmap[point.x, point.y] = point.height;
            }
        }
    }

    public static void AgentCall(HeightmapGrid heightmapGrid, Agent agent, AgentType type, object blocker)
    {
        List<Node.Point> result = new List<Node.Point>();
        for (int i = 0; i < agent.pointsToSmooth.Count; i++)
        {
            Node.Point temp = new Node.Point(agent.pointsToSmooth[i].x, agent.pointsToSmooth[i].y);
            temp.height = heightmapGrid.heightmap[temp.x, temp.y];
            result.Add(temp);
        }

        float totalHeight = 0;
        Node.Point point = null;
        Queue<Node.Point> queue = null;

        for (int t = 0; t < agent.iterations; t++)
        {
            queue = new Queue<Node.Point>(result);
            while (queue.Count > 0)
            {
                point = queue.Dequeue();
                totalHeight = point.height * 10;
                foreach (Node.Point neighbour in heightmapGrid.GetNeighbours(point, heightmapGrid.heightmapSize))
                {
                    totalHeight += result.Contains(neighbour) ? result.First(item => item.Equals(neighbour)).height : heightmapGrid.heightmap[neighbour.x, neighbour.y];
                }
                point.height = (point.height + (totalHeight / (heightmapGrid.GetNeighbours(point, heightmapGrid.heightmapSize).Count + 10))) / 2;
            }
        }
        lock (blocker)
        {
            switch (type)
            {
                case AgentType.Coastline:
                    heightmapGrid.threadCoastlinePoints[agent.index] = result;
                    break;
                case AgentType.Flood:
                    heightmapGrid.threadFloodPoints[agent.index] = result;
                    break;
                case AgentType.Hill:
                    heightmapGrid.threadHillPoints[agent.index] = result;
                    break;
                case AgentType.Mountain:
                    heightmapGrid.threadMountainPoints[agent.index] = result;
                    break;
                case AgentType.Beach:
                    heightmapGrid.threadBeachPoints[agent.index] = result;
                    break;
                case AgentType.River:
                    heightmapGrid.threadRiverPoints[agent.index] = result;
                    break;
                case AgentType.Lake:
                    heightmapGrid.threadLakePoints[agent.index] = result;
                    break;
            }
        }
    }
    #endregion

    public class Agent
    {
        public int index;
        public List<Node.Point> pointsToSmooth;
        public int iterations;
        
        public Agent(int index, List<Node.Point> pointsToSmooth, int iterations)
        {
            this.index = index;
            this.pointsToSmooth = pointsToSmooth;
            this.iterations = iterations;
        }
    }
}
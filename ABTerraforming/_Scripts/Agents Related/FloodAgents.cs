using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class FloodAgents
{
    public static void Sequential(HeightmapGrid heightmapGrid, LandmassFilling agentInfo)
    {
        System.Random rng = new System.Random(agentInfo.seed);
        int heightmapSize = heightmapGrid.heightmapSize;
        Node worldCenter = heightmapGrid.grid[heightmapSize / 2, heightmapSize / 2];

        Queue<Node.Point> queue = new Queue<Node.Point>();
        queue.Enqueue(new Node.Point(worldCenter.gridX, worldCenter.gridY));

        while (queue.Count > 0)
        {
            Node.Point point = queue.Dequeue();
            if (queue.Count > heightmapSize * heightmapSize)
            {
                throw new Exception("The algorithm is probably looping. Queue size: " + queue.Count);
            }
            if (heightmapGrid.coastlinePoints.Contains(point) || heightmapGrid.landmassPoints.Contains(point))
            {
                continue;
            }

            heightmapGrid.SetHeight(heightmapGrid.grid[point.x, point.y], AgentsHeightmap.RandomFloat(rng, agentInfo.minHeight, agentInfo.maxHeight));
            heightmapGrid.landmassPoints.Add(point);

            Node.Point newPoint = new Node.Point(point.x + 1, point.y);
            if (CheckValidity(heightmapGrid.heightmapSize, heightmapGrid.heightmapSize, newPoint))
                queue.Enqueue(newPoint);

            newPoint = new Node.Point(point.x - 1, point.y);
            if (CheckValidity(heightmapGrid.heightmapSize, heightmapGrid.heightmapSize, newPoint))
                queue.Enqueue(newPoint);

            newPoint = new Node.Point(point.x, point.y + 1);
            if (CheckValidity(heightmapGrid.heightmapSize, heightmapGrid.heightmapSize, newPoint))
                queue.Enqueue(newPoint);

            newPoint = new Node.Point(point.x, point.y - 1);
            if (CheckValidity(heightmapGrid.heightmapSize, heightmapGrid.heightmapSize, newPoint))
                queue.Enqueue(newPoint);
        }
        SmoothAgents.Sequential(heightmapGrid, heightmapGrid.landmassPoints, agentInfo.smooth);
    }

    static bool CheckValidity(int width, int height, Node.Point p)
    {
        if (p.x < 0 || p.x >= width)
        {
            return false;
        }
        if (p.y < 0 || p.y >= height)
        {
            return false;
        }
        return true;
    }

    public static void Concurrent(HeightmapGrid heightmapGrid, LandmassFilling agentData)
    {
        int center = heightmapGrid.heightmapSize / 2;
        List<Agent> agents = new List<Agent>();
        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                Node.Point point = new Node.Point(center + x, center + y);
                int minHeight = 0;
                int maxHeight = 0;
                int minWidth = 0;
                int maxWidth = 0;
                if (y + x * 2 == 0)
                {
                    maxHeight = center + 1;
                    maxWidth = center + 1;
                }
                else if (y + x * 2 == 1)
                {
                    minHeight = center;
                    maxHeight = heightmapGrid.heightmapSize;
                    maxWidth = center + 1;
                }
                else if (y + x * 2 == 2)
                {
                    maxHeight = center + 1;
                    minWidth = center;
                    maxWidth = heightmapGrid.heightmapSize;
                }
                else
                {
                    minHeight = center;
                    maxHeight = heightmapGrid.heightmapSize;
                    minWidth = center + 1;
                    maxWidth = heightmapGrid.heightmapSize;
                }
                Agent agent = new Agent(y + x * 2, point, minHeight, maxHeight, minWidth, maxWidth);
                agents.Add(agent);
            }
        }

        List<Thread> threadList = new List<Thread>();
        object blocker = new object();
        foreach (Agent agent in agents)
        {
            Thread thread = new Thread(() => AgentCall(heightmapGrid, agentData, agent, blocker));
            thread.Start();
            threadList.Add(thread);
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

        // Threads Finished
        foreach (int key in heightmapGrid.threadFloodPoints.Keys)
        {
            foreach (Node.Point point in heightmapGrid.threadFloodPoints[key])
            {
                heightmapGrid.heightmap[point.x, point.y] = point.height;
            }
        }
    }

    static void AgentCall(HeightmapGrid heightmapGrid, LandmassFilling agentData, Agent agent, object blocker)
    {
        System.Random rng = new System.Random(agentData.seed);

        List<Node.Point> result = new List<Node.Point>();

        Queue<Node.Point> queue = new Queue<Node.Point>();
        queue.Enqueue(agent.centerPoint);

        if (!Check(agent.minHeight, agent.maxHeight, agent.minWidth, agent.maxWidth, agent.centerPoint))
        {
            Debug.Log("OUT!: " + agent.centerPoint.x + " | " + agent.centerPoint.y);
        }

        while (queue.Count > 0)
        {
            Node.Point point = queue.Dequeue();
            if (queue.Count > heightmapGrid.heightmapSize * heightmapGrid.heightmapSize)
            {
                throw new Exception("The algorithm is probably looping. Queue size: " + queue.Count);
            }
            if (heightmapGrid.coastlinePoints.Contains(point) || result.Contains(point))
            {
                continue;
            }

            point.height = AgentsHeightmap.RandomFloat(rng, agentData.minHeight, agentData.maxHeight);
            result.Add(point);

            Node.Point newPoint = new Node.Point(point.x + 1, point.y);
            if (Check(agent.minHeight, agent.maxHeight, agent.minWidth, agent.maxWidth, newPoint))
                queue.Enqueue(newPoint);

            newPoint = new Node.Point(point.x - 1, point.y);
            if (Check(agent.minHeight, agent.maxHeight, agent.minWidth, agent.maxWidth, newPoint))
                queue.Enqueue(newPoint);

            newPoint = new Node.Point(point.x, point.y + 1);
            if (Check(agent.minHeight, agent.maxHeight, agent.minWidth, agent.maxWidth, newPoint))
                queue.Enqueue(newPoint);

            newPoint = new Node.Point(point.x, point.y - 1);
            if (Check(agent.minHeight, agent.maxHeight, agent.minWidth, agent.maxWidth, newPoint))
                queue.Enqueue(newPoint);
        }
        lock (blocker)
        {
            heightmapGrid.threadFloodPoints[agent.index] = result;
            foreach (Node.Point point in result)
            {
                heightmapGrid.landmassPoints.Add(point);
            }
        }
    }

    class Agent
    {
        public int index;
        public Node.Point centerPoint;
        public int minHeight;
        public int maxHeight;
        public int minWidth;
        public int maxWidth;

        public Agent(int index, Node.Point centerPoint, int minHeight, int maxHeight, int minWidth, int maxWidth)
        {
            this.index = index;
            this.centerPoint = centerPoint;
            this.minHeight = minHeight;
            this.maxHeight = maxHeight;
            this.minWidth = minWidth;
            this.maxWidth = maxWidth;
        }
    }

    static bool Check(int minHeight, int maxHeight, int minWidth, int maxWidth, Node.Point p)
    {
        if (p.x < minWidth || p.x >= maxWidth)
        {
            return false;
        }
        if (p.y < minHeight || p.y >= maxHeight)
        {
            return false;
        }
        return true;
    }
}
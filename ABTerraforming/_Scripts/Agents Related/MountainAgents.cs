using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class MountainAgents
{
    public static void Sequential(HeightmapGrid heightmapGrid, Mountain[] agentsInfo, int resolutionMultiplier)
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(heightmapGrid.heightmap.GetLength(0), heightmapGrid.heightmap.GetLength(1), 13, 20, 4, 0.5f, 1.87f, Vector2.zero);

        List<Node.Point> possibleStartPositions = new List<Node.Point>();
        foreach (Node.Point point in heightmapGrid.landmassPoints)
        {
            if (!heightmapGrid.hillsPoints.Contains(point))
            {
                possibleStartPositions.Add(point);
            }
        }

        List<Agent> agents = new List<Agent>();
        for (int i = 0; i < agentsInfo.Length; i++)
        {
            if (possibleStartPositions.Count > 0)
            {
                Agent agent = new Agent(agentsInfo[i].length * resolutionMultiplier, agentsInfo[i].smooth, agentsInfo[i].maxHeight, agentsInfo[i].minHeight, agentsInfo[i].seed, i);
                int index = agent.rng.Next(0, possibleStartPositions.Count);
                agent.SetInitialPosition(possibleStartPositions[index]);
                possibleStartPositions.Remove(possibleStartPositions[index]);
                agents.Add(agent);
            }
        }

        List<Node> availableNeighbours = new List<Node>();
        while (agents.Count > 0)
        {
            for (int i = agents.Count - 1; i >= 0; i--)
            {
                if (agents[i].buildingTokens > 0)
                {
                    availableNeighbours.Clear();
                    foreach (Node node in heightmapGrid.GetNeighbours(heightmapGrid.grid[agents[i].current.x, agents[i].current.y]))
                    {
                        Node.Point point = new Node.Point(node.gridX, node.gridY);
                        if (heightmapGrid.landmassPoints.Contains(point) && !heightmapGrid.hillsPoints.Contains(point))
                        {
                            availableNeighbours.Add(node);
                        }
                    }
                    int value = agents[i].rng.Next(0, availableNeighbours.Count);
                    Node.Point next = new Node.Point(availableNeighbours[value].gridX, availableNeighbours[value].gridY);
                    agents[i].current = next;
                    List<Node> neighbours = heightmapGrid.GetNeighbours(availableNeighbours[value]);
                    for (int k = 0; k < neighbours.Count; k++)
                    {
                        Node.Point point = new Node.Point(neighbours[k].gridX, neighbours[k].gridY);
                        if (!heightmapGrid.mountainsPoints.Contains(point) && heightmapGrid.landmassPoints.Contains(point))
                        {
                            heightmapGrid.mountainsPoints.Add(point);
                            agents[i].mountainPoints.Add(point);
                            if (k % 2 == 0)
                            {
                                heightmapGrid.SetHeight(neighbours[k], AgentsHeightmap.RandomFloat(agents[i].rng, agents[i].minHeight, 0.7f));
                            }
                            else
                            {
                                heightmapGrid.SetHeight(neighbours[k], AgentsHeightmap.RandomFloat(agents[i].rng, 0.65f, agents[i].maxHeight));
                            }
                        }
                    }
                    if (!heightmapGrid.mountainsPoints.Contains(agents[i].current))
                    {
                        heightmapGrid.mountainsPoints.Add(agents[i].current);
                        agents[i].mountainPoints.Add(agents[i].current);
                        heightmapGrid.SetHeight(heightmapGrid.grid[agents[i].current.x, agents[i].current.y], 2);
                    }
                    agents[i].buildingTokens--;
                }
                else
                {
                    foreach (Node.Point position in agents[i].mountainPoints)
                    {
                        float height = heightmapGrid.GetHeight(heightmapGrid.grid[position.x, position.y]);
                        if (height > 0.7f)
                        {
                            heightmapGrid.SetHeight(heightmapGrid.grid[position.x, position.y], height * 0.6f + noiseMap[position.x, position.y] * 0.4f);
                        }
                    }
                    SmoothAgents.Sequential(heightmapGrid, agents[i].mountainPoints, agents[i].smoothIterations);
                    agents.Remove(agents[i]);
                }
            }
        }
    }

    public static void Concurrent(HeightmapGrid heightmapGrid, Mountain[] agentsInfo, int resolutionMultiplier)
    {
        // Create the agents
        List<Agent> agents = new List<Agent>();
        for (int i = 0; i < agentsInfo.Length; i++)
        {
            Agent agent = new Agent(agentsInfo[i].length * resolutionMultiplier, agentsInfo[i].smooth, agentsInfo[i].maxHeight, agentsInfo[i].minHeight, agentsInfo[i].seed, i);
            agent.SetInitialPosition(heightmapGrid.landmassPoints[agent.rng.Next(0, heightmapGrid.landmassPoints.Count)]);
            agents.Add(agent);
            heightmapGrid.threadMountainPoints.Add(i, new List<Node.Point>());
        }
        // Prepare and Initiate the threads for each agent
        List<Thread> threadList = new List<Thread>();
        object blocker = new object();
        foreach (Agent agent in agents)
        {
            Thread thread = new Thread(() => AgentCall(heightmapGrid, agent, blocker));
            thread.Start();
            threadList.Add(thread);
        }
        // Wait here till all the threads finish
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
        // Threads Finished -> Change heightmap
        foreach (int key in heightmapGrid.threadMountainPoints.Keys)
        {
            foreach (Node.Point point in heightmapGrid.threadMountainPoints[key])
            {
                heightmapGrid.heightmap[point.x, point.y] = point.height;
            }
        }
    }

    public static void AgentCall(HeightmapGrid heightmapGrid, Agent agent, object blocker)
    {
        List<Node.Point> finalPoints = new List<Node.Point>();
        bool running = true;
        List<Node.Point> availableNeighbours = new List<Node.Point>();
        while (running)
        {
            if (agent.buildingTokens > 0)
            {
                availableNeighbours.Clear();
                foreach (Node.Point point in heightmapGrid.GetNeighbours(agent.current, heightmapGrid.heightmapSize))
                {
                    if (heightmapGrid.landmassPoints.Contains(point))
                    {
                        availableNeighbours.Add(point);
                    }
                }
                int value = agent.rng.Next(0, availableNeighbours.Count);
                Node.Point next = availableNeighbours[value];
                agent.current = next;
                List<Node.Point> neighbours = heightmapGrid.GetNeighbours(availableNeighbours[value], heightmapGrid.heightmapSize);
                for (int k = 0; k < neighbours.Count; k++)
                {
                    Node.Point point = neighbours[k];
                    if (!agent.mountainPoints.Contains(point))
                    {
                        agent.mountainPoints.Add(point);
                        point.height = k % 2 == 0 ? AgentsHeightmap.RandomFloat(agent.rng, agent.minHeight, 0.7f) : AgentsHeightmap.RandomFloat(agent.rng, 0.65f, agent.maxHeight);
                        finalPoints.Add(point);
                    }
                }
                if (!agent.mountainPoints.Contains(next))
                {
                    agent.mountainPoints.Add(next);
                    next.height = 2;
                    finalPoints.Add(next);
                }
                agent.buildingTokens--;
            }
            else
            {
                foreach (Node.Point point in agent.mountainPoints)
                {
                    List<Node> nodes = heightmapGrid.GetNeighbours(heightmapGrid.grid[point.x, point.y]);
                    foreach (Node neighbour in nodes)
                    {
                        Node.Point current = new Node.Point(neighbour.gridX, neighbour.gridY);
                        if (!agent.mountainPoints.Contains(current))
                        {
                            current.height = heightmapGrid.heightmap[current.x, current.y];
                            finalPoints.Add(point);
                        }
                    }
                }
                running = false;
                lock (blocker)
                {
                    heightmapGrid.threadMountainPoints[agent.index] = finalPoints;
                    heightmapGrid.mountainsPoints.AddRange(finalPoints);
                }
            }
        }
    }

    public class Agent
    {
        public Node.Point current;

        public int buildingTokens;
        public int smoothIterations;

        public float maxHeight;
        public float minHeight;

        public int index;

        public System.Random rng;

        public List<Node.Point> mountainPoints;

        public Agent(int buildingTokens, int smoothIterations, float maxHeight, float minHeight, int seed, int index)
        {
            this.buildingTokens = buildingTokens;
            this.smoothIterations = smoothIterations;

            this.maxHeight = maxHeight;
            this.minHeight = minHeight;

            rng = new System.Random(seed);

            this.index = index;

            mountainPoints = new List<Node.Point>();
        }

        public void SetInitialPosition(Node.Point position)
        {
            current = position;
        }
    }
}
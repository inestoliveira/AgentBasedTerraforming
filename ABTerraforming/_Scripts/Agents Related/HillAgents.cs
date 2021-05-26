using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class HillAgents
{
    public static void Sequential(HeightmapGrid heightmapGrid, Hill[] agentsInfo, int resolutionMultiplier)
    {
        List<Agent> agents = new List<Agent>();
        for (int i = 0; i < agentsInfo.Length; i++)
        {
            Agent agent = new Agent(agentsInfo[i].length * resolutionMultiplier, agentsInfo[i].smooth, agentsInfo[i].seed, i);
            agent.SetInitialPosition(heightmapGrid.landmassPoints[agent.rng.Next(0, heightmapGrid.landmassPoints.Count)]);
            agents.Add(agent);
        }

        List<Node> availableNeighbours = new List<Node>();
        while (agents.Count > 0)
        {
            for (int i = agents.Count - 1; i >= 0; i--)
            {
                if (agents[i].buildingTokens > 0)
                {
                    availableNeighbours.Clear();
                    foreach (Node point in heightmapGrid.GetNeighbours(heightmapGrid.grid[agents[i].current.x, agents[i].current.y]))
                    {
                        if (heightmapGrid.landmassPoints.Contains(new Node.Point(point.gridX, point.gridY)))
                        {
                            availableNeighbours.Add(point);
                        }
                    }
                    int value = agents[i].rng.Next(0, availableNeighbours.Count);
                    Node.Point next = new Node.Point(availableNeighbours[value].gridX, availableNeighbours[value].gridY);
                    agents[i].current = next;
                    List<Node> neighbours = heightmapGrid.GetNeighbours(availableNeighbours[value]);
                    for (int k = 0; k < neighbours.Count; k++)
                    {
                        Node.Point point = new Node.Point(neighbours[k].gridX, neighbours[k].gridY);
                        if (!heightmapGrid.hillsPoints.Contains(point))
                        {
                            heightmapGrid.hillsPoints.Add(point);
                            agents[i].hillPoints.Add(point);
                            heightmapGrid.SetHeight(neighbours[k], AgentsHeightmap.RandomFloat(agents[i].rng, 0.2f, 0.6f));
                        }
                    }
                    if (!heightmapGrid.hillsPoints.Contains(next))
                    {
                        heightmapGrid.hillsPoints.Add(next);
                        agents[i].hillPoints.Add(next);
                        heightmapGrid.SetHeight(heightmapGrid.grid[next.x, next.y], AgentsHeightmap.RandomFloat(agents[i].rng, 0.3f, 0.7f));
                    }
                    agents[i].buildingTokens--;
                }
                else
                {
                    SmoothAgents.Sequential(heightmapGrid, agents[i].hillPoints, agents[i].smoothIterations);
                    agents.Remove(agents[i]);
                }
            }
        }
    }

    public static void Concurrent(HeightmapGrid heightmapGrid, Hill[] agentsInfo, int resolutionMultiplier)
    {
        // Create the agents
        List<Agent> agents = new List<Agent>();
        for (int i = 0; i < agentsInfo.Length; i++)
        {
            Agent agent = new Agent(agentsInfo[i].length * resolutionMultiplier, agentsInfo[i].smooth, agentsInfo[i].seed, i);
            agent.SetInitialPosition(heightmapGrid.landmassPoints[agent.rng.Next(0, heightmapGrid.landmassPoints.Count)]);
            agents.Add(agent);
            heightmapGrid.threadHillPoints.Add(i, new List<Node.Point>());
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
        foreach (int key in heightmapGrid.threadHillPoints.Keys)
        {
            foreach(Node.Point point in heightmapGrid.threadHillPoints[key])
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
                    if (!agent.hillPoints.Contains(point))
                    {
                        agent.hillPoints.Add(point);
                        point.height = AgentsHeightmap.RandomFloat(agent.rng, 0.2f, 0.6f);
                        finalPoints.Add(point);
                    }
                }
                if (!agent.hillPoints.Contains(next))
                {
                    agent.hillPoints.Add(next);
                    next.height = AgentsHeightmap.RandomFloat(agent.rng, 0.3f, 0.7f);
                    finalPoints.Add(next);
                }
                agent.buildingTokens--;
            }
            else
            {
                running = false;
                lock (blocker)
                {
                    heightmapGrid.threadHillPoints[agent.index] = finalPoints;
                }
            }
        }
    }

    public class Agent
    {
        public Node.Point current;

        public int buildingTokens;
        public int smoothIterations;

        public System.Random rng;

        public List<Node.Point> hillPoints;

        public int index;

        public Agent(int buildingTokens, int smoothIterations, int seed, int index)
        {
            this.buildingTokens = buildingTokens;
            this.smoothIterations = smoothIterations;

            rng = new System.Random(seed);

            hillPoints = new List<Node.Point>();

            this.index = index;
        }

        public void SetInitialPosition(Node.Point position)
        {
            current = position;
        }
    }
}

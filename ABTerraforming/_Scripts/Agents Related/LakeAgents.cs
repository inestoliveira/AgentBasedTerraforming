using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class LakeAgents
{
    public static void Sequential(HeightmapGrid heightmapGrid, Lake[] agentsInfo, int resolutionMultiplier)
    {
        List<Agent> agents = new List<Agent>();
        for (int i = 0; i < agentsInfo.Length; i++)
        {
            Agent agent = new Agent(agentsInfo[i].length * resolutionMultiplier, agentsInfo[i].width * resolutionMultiplier, agentsInfo[i].smooth, agentsInfo[i].seed, i);
            agent.SetInitialPosition(heightmapGrid.landmassPoints[agent.rng.Next(0, heightmapGrid.landmassPoints.Count)]);
            agents.Add(agent);
        }

        List<Node.Point> availableNeighbours = new List<Node.Point>();
        while (agents.Count > 0)
        {
            for (int i = agents.Count - 1; i >= 0; i--)
            {
                if (agents[i].widthTokens > 0)
                {
                    foreach (Node node in heightmapGrid.GetNeighbours(heightmapGrid.grid[agents[i].position.x, agents[i].position.y]))
                    {
                        Node.Point point = new Node.Point(node.gridX, node.gridY);
                        if (heightmapGrid.landmassPoints.Contains(point) && !heightmapGrid.mountainsPoints.Contains(point))
                        {
                            availableNeighbours.Add(point);
                        }
                    }

                    if (availableNeighbours.Count > 0)
                    {
                        Node.Point nextPosition = availableNeighbours[agents[i].rng.Next(0, availableNeighbours.Count - 1)];
                        agents[i].position = nextPosition;
                        if (!agents[i].lakePoints.Contains(nextPosition))
                        {
                            agents[i].lakePoints.Add(nextPosition);
                        }

                        heightmapGrid.SetHeight(heightmapGrid.grid[nextPosition.x, nextPosition.y], -0.4f);

                        agents[i].widthTokens--;
                    }
                    else
                    {
                        agents[i].widthTokens = 0;
                    }

                    agents[i].width--;
                }
                else if (agents[i].width > 0)
                {
                    agents[i].MoveToInitialPosition();
                    if (!agents[i].lakePoints.Contains(agents[i].initialPosition))
                    {
                        agents[i].lakePoints.Add(agents[i].initialPosition);
                        heightmapGrid.SetHeight(heightmapGrid.grid[agents[i].position.x, agents[i].position.y], -0.4f);
                    }
                    agents[i].widthTokens = agents[i].length;
                }
                else
                {
                    List<Node.Point> neighbours = new List<Node.Point>();
                    foreach (Node.Point point in agents[i].lakePoints)
                    {
                        List<Node> nodes = heightmapGrid.GetNeighbours(heightmapGrid.grid[point.x, point.y]);
                        foreach (Node neighbour in nodes)
                        {
                            Node.Point current = new Node.Point(neighbour.gridX, neighbour.gridY);
                            if (!agents[i].lakePoints.Contains(current) && !heightmapGrid.lakePoints.Contains(current))
                            {
                                neighbours.Add(current);
                            }
                        }
                    }
                    foreach (Node.Point node in neighbours)
                    {
                        agents[i].lakePoints.Add(node);
                        heightmapGrid.lakePoints.Add(node);
                    }
                    SmoothAgents.Sequential(heightmapGrid, agents[i].lakePoints, agents[i].smoothIterations);
                    agents.Remove(agents[i]);
                }
            }
        }
    }

    public static void Concurrent(HeightmapGrid heightmapGrid, Lake[] agentsInfo, int resolutionMultiplier)
    {
        // Create the agents
        List<Agent> agents = new List<Agent>();
        for (int i = 0; i < agentsInfo.Length; i++)
        {
            Agent agent = new Agent(agentsInfo[i].length * resolutionMultiplier, agentsInfo[i].width * resolutionMultiplier, agentsInfo[i].smooth, agentsInfo[i].seed, i);
            agent.SetInitialPosition(heightmapGrid.landmassPoints[agent.rng.Next(0, heightmapGrid.landmassPoints.Count)]);
            agents.Add(agent);
            heightmapGrid.threadLakePoints.Add(i, new List<Node.Point>());
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
        foreach (int key in heightmapGrid.threadLakePoints.Keys)
        {
            foreach (Node.Point point in heightmapGrid.threadLakePoints[key])
            {
                heightmapGrid.heightmap[point.x, point.y] = point.height;
            }
        }
    }

    static void AgentCall(HeightmapGrid heightmapGrid, Agent agent, object blocker)
    {
        List<Node.Point> finalPoints = new List<Node.Point>();
        bool running = true;
        List<Node.Point> availableNeighbours = new List<Node.Point>();
        while (running)
        {
            if (agent.widthTokens > 0)
            {
                foreach (Node node in heightmapGrid.GetNeighbours(heightmapGrid.grid[agent.position.x, agent.position.y]))
                {
                    Node.Point point = new Node.Point(node.gridX, node.gridY);
                    if (heightmapGrid.landmassPoints.Contains(point) && !heightmapGrid.ContainsOnList(point, HeightmapGrid.ThreadList.Mountain))
                    {
                        availableNeighbours.Add(point);
                    }
                }

                if (availableNeighbours.Count > 0)
                {
                    Node.Point nextPosition = availableNeighbours[agent.rng.Next(0, availableNeighbours.Count - 1)];
                    agent.position = nextPosition;
                    if (!agent.lakePoints.Contains(nextPosition))
                    {
                        agent.lakePoints.Add(nextPosition);
                    }
                    nextPosition.height = -0.4f;
                    finalPoints.Add(nextPosition);

                    agent.widthTokens--;
                }
                else
                {
                    agent.widthTokens = 0;
                }

                agent.width--;
            }
            else if (agent.width > 0)
            {
                agent.MoveToInitialPosition();
                if (!agent.lakePoints.Contains(agent.initialPosition))
                {
                    agent.lakePoints.Add(agent.initialPosition);
                    agent.initialPosition.height = -0.4f;
                    finalPoints.Add(agent.initialPosition);
                }
                agent.widthTokens = agent.length;
            }
            else
            {
                foreach (Node.Point point in agent.lakePoints)
                {
                    List<Node> nodes = heightmapGrid.GetNeighbours(heightmapGrid.grid[point.x, point.y]);
                    foreach (Node neighbour in nodes)
                    {
                        Node.Point current = new Node.Point(neighbour.gridX, neighbour.gridY);
                        if (!agent.lakePoints.Contains(current))
                        {
                            current.height = heightmapGrid.heightmap[current.x, current.y];
                            finalPoints.Add(point);
                        }
                    }
                }
                running = false;
                lock (blocker)
                {
                    heightmapGrid.threadLakePoints[agent.index] = finalPoints;
                }
            }
        }
    }

    class Agent
    {
        public Node.Point position;
        public Node.Point initialPosition;

        public int length;
        public int width;
        public int widthTokens;
        public int smoothIterations;

        public System.Random rng;

        public List<Node.Point> lakePoints;

        public int index;

        public Agent(int length, int width, int smoothIterations, int seed, int index)
        {
            this.length = length;
            this.width = width;

            rng = new System.Random(seed);
            this.smoothIterations = smoothIterations;

            lakePoints = new List<Node.Point>();

            this.index = index;
        }

        public void SetInitialPosition(Node.Point position)
        {
            this.position = position;
            initialPosition = position;
        }

        public void MoveToInitialPosition()
        {
            position = initialPosition;
        }
    }
}

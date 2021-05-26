using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class BeachAgents
{
    public static void Sequential(HeightmapGrid heightmapGrid, Beach[] agentsInfo, int resolutionMultiplier)
    {
        List<Agent> agents = new List<Agent>();
        for (int i = 0; i < agentsInfo.Length; i++)
        {
            Agent agent = new Agent(agentsInfo[i].length, agentsInfo[i].width, agentsInfo[i].smooth, agentsInfo[i].seed, i);
            agent.SetInitialPosition(heightmapGrid.coastlinePoints[agent.rng.Next(0, heightmapGrid.coastlinePoints.Count)]);
            agents.Add(agent);
        }
        List<Node.Point> availableNeighbours = new List<Node.Point>();
        while (agents.Count > 0)
        {
            for (int i = agents.Count - 1; i >= 0; i--)
            {
                if (agents[i].length > 0)
                {
                    availableNeighbours.Clear();
                    foreach (Node node in heightmapGrid.GetNeighbours(heightmapGrid.grid[agents[i].current.x, agents[i].current.y]))
                    {
                        Node.Point point = new Node.Point(node.gridX, node.gridY);
                        if (heightmapGrid.landmassPoints.Contains(point) && !heightmapGrid.beachPoints.Contains(point) && !heightmapGrid.hillsPoints.Contains(point) && !heightmapGrid.mountainsPoints.Contains(point))
                        {
                            float pointDist = Vector2.Distance(node.ToVector2(), new Vector2(agents[i].positionOnCoastline.x, agents[i].positionOnCoastline.y));
                            if (pointDist <= agents[i].distanceToCoast)
                            {
                                availableNeighbours.Add(point);

                            }
                        }
                    }
                    if (availableNeighbours.Count > 0)
                    {
                        int value = agents[i].rng.Next(0, availableNeighbours.Count);
                        Node.Point next = availableNeighbours[value];
                        foreach (Node node in heightmapGrid.GetNeighbours(heightmapGrid.grid[agents[i].current.x, agents[i].current.y]))
                        {
                            Node.Point point = new Node.Point(node.gridX, node.gridY);
                            if ((heightmapGrid.landmassPoints.Contains(point) || heightmapGrid.coastlinePoints.Contains(point)) && !heightmapGrid.beachPoints.Contains(point) && !node.Equals(next))
                            {
                                heightmapGrid.SetHeight(node, AgentsHeightmap.RandomFloat(agents[i].rng, -0.05f, 0f));
                                heightmapGrid.beachPoints.Add(point);
                                agents[i].beachPoints.Add(point);
                            }
                        }
                        agents[i].current = next;
                        heightmapGrid.SetHeight(heightmapGrid.grid[agents[i].current.x, agents[i].current.y], AgentsHeightmap.RandomFloat(agents[i].rng, -0.13f, 0f));
                        heightmapGrid.beachPoints.Add(agents[i].current);
                        agents[i].beachPoints.Add(agents[i].current);
                    }
                    else
                    {
                        int positionOnCoastIndex = heightmapGrid.coastlinePoints.FindIndex(a => a.Equals(agents[i].positionOnCoastline));
                        agents[i].positionOnCoastline = positionOnCoastIndex + 1 >= heightmapGrid.coastlinePoints.Count - 1 ? heightmapGrid.coastlinePoints[0] : heightmapGrid.coastlinePoints[positionOnCoastIndex + 1];
                        agents[i].current = agents[i].positionOnCoastline;

                        heightmapGrid.SetHeight(heightmapGrid.grid[agents[i].positionOnCoastline.x, agents[i].positionOnCoastline.y], -0.8f);
                        agents[i].beachPoints.Add(agents[i].positionOnCoastline);
                    }
                    agents[i].length--;
                }
                else
                {
                    List<Node.Point> neighbours = new List<Node.Point>();
                    foreach (Node.Point point in agents[i].beachPoints)
                    {
                        List<Node> nodes = heightmapGrid.GetNeighbours(heightmapGrid.grid[point.x, point.y]);
                        foreach (Node neighbour in nodes)
                        {
                            Node.Point current = new Node.Point(neighbour.gridX, neighbour.gridY);
                            if (!agents[i].beachPoints.Contains(current) && !heightmapGrid.beachPoints.Contains(current))
                            {
                                neighbours.Add(current);
                            }
                        }
                    }
                    foreach (Node.Point point in neighbours)
                    {
                        agents[i].beachPoints.Add(point);
                        heightmapGrid.beachPoints.Add(point);
                    }
                    SmoothAgents.Sequential(heightmapGrid, agents[i].beachPoints, agents[i].smoothIterations);
                    agents.Remove(agents[i]);
                }
            }
        }
        SmoothAgents.Sequential(heightmapGrid, heightmapGrid.coastlinePoints, 3);
    }

    public static void Concurrent(HeightmapGrid heightmapGrid, Beach[] agentsInfo, int resolutionMultiplier)
    {
        // Create the agents
        List<Agent> agents = new List<Agent>();
        for (int i = 0; i < agentsInfo.Length; i++)
        {
            Agent agent = new Agent(agentsInfo[i].length, agentsInfo[i].width, agentsInfo[i].smooth, agentsInfo[i].seed, i);
            agent.SetInitialPosition(heightmapGrid.coastlinePoints[agent.rng.Next(0, heightmapGrid.coastlinePoints.Count)]);
            agents.Add(agent);
            heightmapGrid.threadBeachPoints.Add(i, new List<Node.Point>());
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
        foreach (int key in heightmapGrid.threadBeachPoints.Keys)
        {
            foreach (Node.Point point in heightmapGrid.threadBeachPoints[key])
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
            if (agent.length > 0)
            {
                availableNeighbours.Clear();
                foreach (Node node in heightmapGrid.GetNeighbours(heightmapGrid.grid[agent.current.x, agent.current.y]))
                {
                    Node.Point point = new Node.Point(node.gridX, node.gridY);
                    if (heightmapGrid.landmassPoints.Contains(point) && !agent.beachPoints.Contains(point) && !heightmapGrid.ContainsOnList(point, HeightmapGrid.ThreadList.Hills) && !heightmapGrid.ContainsOnList(point, HeightmapGrid.ThreadList.Mountain))
                    {
                        float pointDist = Vector2.Distance(node.ToVector2(), new Vector2(agent.positionOnCoastline.x, agent.positionOnCoastline.y));
                        if (pointDist <= agent.distanceToCoast)
                        {
                            availableNeighbours.Add(point);
                        }
                    }
                }
                if (availableNeighbours.Count > 0)
                {
                    int value = agent.rng.Next(0, availableNeighbours.Count);
                    Node.Point next = availableNeighbours[value];
                    foreach (Node node in heightmapGrid.GetNeighbours(heightmapGrid.grid[agent.current.x, agent.current.y]))
                    {
                        Node.Point point = new Node.Point(node.gridX, node.gridY);
                        if ((heightmapGrid.landmassPoints.Contains(point) || heightmapGrid.coastlinePoints.Contains(point)) && !agent.beachPoints.Contains(point) && !node.Equals(next))
                        {
                            point.height = AgentsHeightmap.RandomFloat(agent.rng, -0.05f, 0f);
                            finalPoints.Add(point);
                            agent.beachPoints.Add(point);
                        }
                    }
                    agent.current = next;
                    agent.current.height = AgentsHeightmap.RandomFloat(agent.rng, -0.13f, 0f);
                    finalPoints.Add(agent.current);
                    agent.beachPoints.Add(agent.current);
                }
                else
                {
                    int positionOnCoastIndex = heightmapGrid.coastlinePoints.FindIndex(a => a.Equals(agent.positionOnCoastline));
                    agent.positionOnCoastline = positionOnCoastIndex + 1 >= heightmapGrid.coastlinePoints.Count - 1 ? heightmapGrid.coastlinePoints[0] : heightmapGrid.coastlinePoints[positionOnCoastIndex + 1];
                    agent.current = agent.positionOnCoastline;

                    agent.positionOnCoastline.height = -0.8f;
                    finalPoints.Add(agent.positionOnCoastline);
                    agent.beachPoints.Add(agent.positionOnCoastline);
                }
                agent.length--;
            }
            else
            {
                foreach (Node.Point point in agent.beachPoints)
                {
                    List<Node> nodes = heightmapGrid.GetNeighbours(heightmapGrid.grid[point.x, point.y]);
                    foreach (Node neighbour in nodes)
                    {
                        Node.Point current = new Node.Point(neighbour.gridX, neighbour.gridY);
                        if (!agent.beachPoints.Contains(current))
                        {
                            current.height = heightmapGrid.heightmap[current.x, current.y];
                            finalPoints.Add(point);
                        }
                    }
                }
                running = false;
                lock (blocker)
                {
                    heightmapGrid.threadBeachPoints[agent.index] = finalPoints;
                }
            }
        }
    }

    public class Agent
    {
        public Node.Point current;
        public Node.Point positionOnCoastline;

        public int length;
        public int distanceToCoast;

        public int smoothIterations;

        public System.Random rng;

        public List<Node.Point> beachPoints;

        public int index;

        public Agent(int length, int width, int smoothIterations, int seed, int index)
        {
            this.length = length;
            this.distanceToCoast = width;
            this.smoothIterations = smoothIterations;

            rng = new System.Random(seed);

            beachPoints = new List<Node.Point>();

            this.index = index;
        }

        public void SetInitialPosition(Node.Point position)
        {
            current = position;
            positionOnCoastline = current;
        }
    }
}

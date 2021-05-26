using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

public class CoastlineAgents
{
    #region Sequential
    public static void Sequential(HeightmapGrid heightmapGrid, Coastline agentsData)
    {
        Random rng = new Random(agentsData.seed);
        int heightmapSize = heightmapGrid.heightmapSize;
        Node worldCenter = heightmapGrid.grid[heightmapSize / 2, heightmapSize / 2];

        List<Agent> agents = new List<Agent>();
        for (int i = 0; i < agentsData.detail; i++)
        {
            Node spawnPosition = heightmapGrid.grid[worldCenter.gridX + rng.Next(agentsData.minLength, agentsData.maxLength), worldCenter.gridY];
            spawnPosition = RotatePoint(spawnPosition, worldCenter, 360f - i * (360f / agentsData.detail));
            Agent newAgent = new Agent(spawnPosition, i, agentsData.seed);
            agents.Add(newAgent);

            if (i > 0) agents[i - 1].SetTarget(newAgent.current);
            if (i == agentsData.detail - 1) newAgent.SetTarget(agents[0].current);
        }

        Dictionary<int, List<Node.Point>> clusterOfNodesOdered = new Dictionary<int, List<Node.Point>>();
        for (int i = 0; i < agents.Count; i++) clusterOfNodesOdered.Add(i, null);

        List<Node> neighbours = new List<Node>();
        while (agents.Count > 0)
        {
            for (int i = agents.Count - 1; i >= 0; i--)
            {
                heightmapGrid.SetHeight(heightmapGrid.grid[agents[i].current.x, agents[i].current.y], AgentsHeightmap.RandomFloat(agents[i].rng, 0.18f, 0.22f));
                if (!heightmapGrid.coastlinePoints.Contains(agents[i].current))
                {
                    heightmapGrid.coastlinePoints.Add(agents[i].current);
                    agents[i].coastlinePoints.Add(agents[i].current);
                }

                bool targetFound = false;
                Node.Point next = null;

                heightmapGrid.grid[agents[i].current.x, agents[i].current.y].walkable = true;
                heightmapGrid.grid[agents[i].target.x, agents[i].target.y].walkable = true;
                neighbours = heightmapGrid.GetNeighbours(heightmapGrid.grid[agents[i].current.x, agents[i].current.y]);
                int randomChance = rng.Next(0, 100);
                if (randomChance < 50f)
                {
                    for (int k = neighbours.Count - 1; k >= 0; k--)
                    {
                        if (neighbours[k] == heightmapGrid.grid[agents[i].target.x, agents[i].target.y])
                        {
                            targetFound = true;
                            break;
                        }
                        if (!heightmapGrid.coastlinePoints.Contains(new Node.Point(neighbours[k].gridX, neighbours[k].gridY)) && neighbours[k].walkable)
                        {
                            List<Node> path = heightmapGrid.FindPath(heightmapGrid.grid[neighbours[k].gridX, neighbours[k].gridY], heightmapGrid.grid[agents[i].target.x, agents[i].target.y]);
                            if (path == null) neighbours.Remove(neighbours[k]);
                        }
                        else neighbours.Remove(neighbours[k]);
                    }
                    if (!targetFound)
                    {
                        if (neighbours.Count == 0)
                        {
                            List<Node> path = heightmapGrid.FindPath(heightmapGrid.grid[agents[i].current.x, agents[i].current.y], heightmapGrid.grid[agents[i].target.x, agents[i].target.y]);
                            if (path == null)
                            {
                                Node node = heightmapGrid.grid[agents[i].current.x, agents[i].current.y];
                                Node node2 = heightmapGrid.grid[agents[i].target.x, agents[i].target.y];
                            }
                            next = new Node.Point(path[0].gridX, path[0].gridY);
                        }
                        else
                        {
                            int value = rng.Next(0, neighbours.Count);
                            next = new Node.Point(neighbours[value].gridX, neighbours[value].gridY);
                        }
                    }
                }
                else
                {
                    foreach (Node point in neighbours)
                    {
                        if (point == heightmapGrid.grid[agents[i].target.x, agents[i].target.y])
                        {
                            targetFound = true;
                            break;
                        }
                    }
                    if (!targetFound)
                    {
                        List<Node> path = heightmapGrid.FindPath(heightmapGrid.grid[agents[i].current.x, agents[i].current.y], heightmapGrid.grid[agents[i].target.x, agents[i].target.y]);
                        if (path == null)
                        {
                            Node node = heightmapGrid.grid[agents[i].current.x, agents[i].current.y];
                            Node node2 = heightmapGrid.grid[agents[i].target.x, agents[i].target.y];
                        }
                        next = new Node.Point(path[0].gridX, path[0].gridY);
                    }
                }
                if (targetFound)
                {
                    clusterOfNodesOdered[agents[i].index] = agents[i].coastlinePoints;
                    agents.Remove(agents[i]);
                }
                else
                {
                    heightmapGrid.grid[agents[i].current.x, agents[i].current.x].walkable = false;
                    agents[i].current = next;
                }
            }
        }
        List<Node.Point> orderedCoastlineNodes = new List<Node.Point>();
        foreach (int key in clusterOfNodesOdered.Keys)
        {
            orderedCoastlineNodes.AddRange(clusterOfNodesOdered[key]);
        }
        heightmapGrid.coastlinePoints = orderedCoastlineNodes;
        foreach (Node.Point point in heightmapGrid.coastlinePoints)
        {
            heightmapGrid.grid[point.x, point.y].walkable = true;
        }
    }
    #endregion

    #region Concurrent
    public static void Concurrent(HeightmapGrid heightmapGrid, Coastline agentsData)
    {
        Random rng = new Random(agentsData.seed);
        int heightmapSize = heightmapGrid.heightmapSize;
        Node worldCenter = heightmapGrid.grid[heightmapSize / 2, heightmapSize / 2];

        // Create the agents
        List<Agent> agents = new List<Agent>();
        for (int i = 0; i < agentsData.detail; i++)
        {
            Node spawnPosition = heightmapGrid.grid[worldCenter.gridX + rng.Next(agentsData.minLength, agentsData.maxLength), worldCenter.gridY];
            spawnPosition = RotatePoint(spawnPosition, worldCenter, 360f - i * (360f / agentsData.detail));
            Agent newAgent = new Agent(spawnPosition, i, agentsData.seed);
            agents.Add(newAgent);

            if (i > 0) agents[i - 1].SetTarget(newAgent.current);
            if (i == agentsData.detail - 1) newAgent.SetTarget(agents[0].current);

            heightmapGrid.threadCoastlinePoints.Add(i, new List<Node.Point>());
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
        foreach (int key in heightmapGrid.threadCoastlinePoints.Keys)
        {
            foreach (Node.Point point in heightmapGrid.threadCoastlinePoints[key])
            {
                heightmapGrid.heightmap[point.x, point.y] = point.height;
            }
        }
    }

    public static void AgentCall(HeightmapGrid heightmapGrid, Agent agent, object blocker)
    {
        List<Node.Point> finalPoints = new List<Node.Point>();
        bool running = true;
        List<Node> neighbours = new List<Node>();
        while (running)
        {
            // Modifies point height and adds to the final list of points
            agent.current.height = AgentsHeightmap.RandomFloat(agent.rng, 0.18f, 0.22f);
            finalPoints.Add(agent.current);
            agent.coastlinePoints.Add(agent.current);

            bool targetFound = false;
            Node.Point next = null;
            neighbours = heightmapGrid.GetNeighbours(heightmapGrid.grid[agent.current.x, agent.current.y]);
            int randomChance = agent.rng.Next(0, 100);
            // Randomly decides if randomly picks an available neighbour or picks the shortest distance
            if (randomChance < 50f) // Moves random
            {
                for (int k = neighbours.Count - 1; k >= 0; k--)
                {
                    if (neighbours[k] == heightmapGrid.grid[agent.target.x, agent.target.y])
                    {
                        targetFound = true;
                        break;
                    }
                    if (agent.coastlinePoints.Contains(new Node.Point(neighbours[k].gridX, neighbours[k].gridY)))
                    {
                        neighbours.Remove(neighbours[k]);
                    }
                }
                if (!targetFound)
                {
                    if (neighbours.Count == 0)
                    {
                        float distance = float.MaxValue;
                        Node tempNext = null;
                        foreach (Node node in heightmapGrid.GetNeighbours(heightmapGrid.grid[agent.current.x, agent.current.y]))
                        {
                            float tempDist = UnityEngine.Vector2.Distance(node.ToVector2(), new UnityEngine.Vector2(agent.target.x, agent.target.y));
                            if (tempDist < distance)
                            {
                                distance = tempDist;
                                tempNext = node;
                            }
                        }
                        next = new Node.Point(tempNext.gridX, tempNext.gridY);
                    }
                    else
                    {
                        int value = agent.rng.Next(0, neighbours.Count);
                        next = new Node.Point(neighbours[value].gridX, neighbours[value].gridY);
                    }
                }
            }
            else // Shortest Distance
            {
                foreach (Node point in neighbours)
                {
                    if (point == heightmapGrid.grid[agent.target.x, agent.target.y])
                    {
                        targetFound = true;
                        break;
                    }
                }
                if (!targetFound)
                {
                    float distance = float.MaxValue;
                    Node tempNext = null;
                    foreach (Node node in heightmapGrid.GetNeighbours(heightmapGrid.grid[agent.current.x, agent.current.y]))
                    {
                        float tempDist = UnityEngine.Vector2.Distance(node.ToVector2(), new UnityEngine.Vector2(agent.target.x, agent.target.y));
                        if (tempDist < distance)
                        {
                            distance = tempDist;
                            tempNext = node;
                        }
                    }
                    next = new Node.Point(tempNext.gridX, tempNext.gridY);
                }
            }
            if (targetFound)
            {
                running = false;
                lock (blocker)
                {
                    heightmapGrid.threadCoastlinePoints[agent.index] = finalPoints;
                }
            }
            else
            {
                agent.current = next;
            }
        }
    }
    #endregion

    #region Agents Info
    public class Agent
    {
        public Node.Point current;
        public Node.Point target;

        public List<Node.Point> coastlinePoints;

        public Random rng;

        public int index;

        public Agent(Node initialNode, int index, int seed)
        {
            current = new Node.Point(initialNode.gridX, initialNode.gridY);
            coastlinePoints = new List<Node.Point>();
            this.index = index;
            rng = new Random(seed);
        }

        public void SetTarget(Node.Point target)
        {
            this.target = target;
        }
    }

    static Node RotatePoint(Node pointToRotate, Node centerPoint, double angleInDegrees)
    {
        double angleInRadians = angleInDegrees * (Math.PI / 180);
        double cosTheta = Math.Cos(angleInRadians);
        double sinTheta = Math.Sin(angleInRadians);

        Node resultPoint = new Node(pointToRotate.gridX - centerPoint.gridX, pointToRotate.gridY - centerPoint.gridY);

        int xDif = pointToRotate.gridX - resultPoint.gridX;
        int yDif = pointToRotate.gridY - resultPoint.gridY;

        int x = (int)(resultPoint.gridX * cosTheta - resultPoint.gridY * sinTheta);
        int y = (int)(resultPoint.gridY * cosTheta + resultPoint.gridX * sinTheta);
        resultPoint = new Node(x, y);

        resultPoint.gridX += xDif;
        resultPoint.gridY += yDif;

        return resultPoint;
    }
    #endregion
}
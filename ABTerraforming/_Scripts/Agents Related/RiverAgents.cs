using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class RiverAgents
{
    public static void Sequential(HeightmapGrid heightmapGrid, River[] agentsInfo, int resolutionMultiplier)
    {
        List<Agent> agents = new List<Agent>();
        for (int i = 0; i < agentsInfo.Length; i++)
        {
            Agent agent = new Agent(agentsInfo[i].source, agentsInfo[i].riverMouth, agentsInfo[i].smooth, agentsInfo[i].seed, i);
            agents.Add(agent);
        }

        foreach (Node.Point point in heightmapGrid.mountainsPoints)
        {
            heightmapGrid.grid[point.x, point.y].walkable = false;
        }

        List<Node.Point> riverStartNodes = new List<Node.Point>();
        foreach (Node.Point point in heightmapGrid.mountainsPoints)
        {
            int mountainPointCount = 0;
            Node node = heightmapGrid.grid[point.x, point.y];
            foreach (Node neighbour in heightmapGrid.GetNeighbours(node))
            {
                Node.Point current = new Node.Point(neighbour.gridX, neighbour.gridY);
                if (heightmapGrid.landmassPoints.Contains(current))
                {
                    if (heightmapGrid.mountainsPoints.Contains(current))
                    {
                        mountainPointCount++;
                    }
                }
                else
                {
                    mountainPointCount += 4;
                }
            }
            if (mountainPointCount <= 3)
            {

                node.walkable = true;
                List<Node> path = heightmapGrid.FindPath(node, heightmapGrid.grid[heightmapGrid.coastlinePoints[0].x, heightmapGrid.coastlinePoints[0].y]);
                if (path != null)
                {
                    riverStartNodes.Add(point);
                }
                node.walkable = false;
            }
        }

        if (riverStartNodes.Count == 0)
        {
            Debug.LogWarning("Can't build River!");
        }
        else
        {
            List<Node> neighbours = new List<Node>();
            while (agents.Count > 0)
            {
                for (int i = agents.Count - 1; i >= 0; i--)
                {
                    if (!agents[i].stop)
                    {
                        if (agents[i].current == null)
                        {
                            int index = agents[i].start_rng.Next(0, riverStartNodes.Count);
                            agents[i].current = riverStartNodes[index];
                            heightmapGrid.grid[riverStartNodes[index].x, riverStartNodes[index].y].walkable = true;

                            index = agents[i].end_rng.Next(0, heightmapGrid.coastlinePoints.Count);
                            agents[i].target = new Node.Point(heightmapGrid.coastlinePoints[index].x, heightmapGrid.coastlinePoints[index].y);
                            List<Node> path = heightmapGrid.FindPath(heightmapGrid.grid[agents[i].current.x, agents[i].current.y], heightmapGrid.grid[agents[i].target.x, agents[i].target.y]);
                            agents[i].numOfPoints = path.Count;
                        }

                        neighbours = heightmapGrid.GetNeighbours(heightmapGrid.grid[agents[i].current.x, agents[i].current.y]);
                        Node.Point next = null;
                        int random = agents[i].rng.Next(0, 100);
                        if (random < 33f)
                        {
                            for (int k = neighbours.Count - 1; k >= 0; k--)
                            {
                                Node.Point point = new Node.Point(neighbours[k].gridX, neighbours[k].gridY);
                                if (neighbours[k] == heightmapGrid.grid[agents[i].target.x, agents[i].target.y] || (!agents[i].riverPoints.Contains(point) && heightmapGrid.riverPoints.Contains(point)))
                                {
                                    next = point;
                                    agents[i].current = next;
                                    agents[i].stop = true;
                                    break;
                                }
                                if (neighbours[k].walkable)
                                {
                                    List<Node> path = heightmapGrid.FindPath(heightmapGrid.grid[neighbours[k].gridX, neighbours[k].gridY], heightmapGrid.grid[agents[i].target.x, agents[i].target.y]);
                                    if (path == null) neighbours.Remove(neighbours[k]);
                                }
                                else neighbours.Remove(neighbours[k]);
                            }
                            if (!agents[i].stop)
                            {
                                int value = agents[i].rng.Next(0, neighbours.Count);
                                next = new Node.Point(neighbours[value].gridX, neighbours[value].gridY);
                            }
                        }
                        else
                        {
                            foreach (Node node in neighbours)
                            {
                                Node.Point point = new Node.Point(node.gridX, node.gridY);
                                if (node == heightmapGrid.grid[agents[i].target.x, agents[i].target.y] || (!agents[i].riverPoints.Contains(point) && heightmapGrid.riverPoints.Contains(point)))
                                {
                                    next = point;
                                    agents[i].current = next;
                                    agents[i].stop = true;
                                    break;
                                }
                            }
                            if (!agents[i].stop)
                            {
                                List<Node> path = heightmapGrid.FindPath(heightmapGrid.grid[agents[i].current.x, agents[i].current.y], heightmapGrid.grid[agents[i].target.x, agents[i].target.y]);
                                next = new Node.Point(path[0].gridX, path[0].gridY);
                            }
                        }
                        if (!agents[i].stop)
                        {
                            agents[i].current = next;

                            if (!agents[i].riverPoints.Contains(agents[i].current))
                            {
                                agents[i].riverPoints.Add(agents[i].current);
                                heightmapGrid.riverPoints.Add(agents[i].current);
                                heightmapGrid.SetHeight(heightmapGrid.grid[agents[i].current.x, agents[i].current.y], -1.3f);
                            }

                            float percent = agents[i].riverPoints.Count / (float)agents[i].numOfPoints;
                            int layer = 1 + (int)(percent * 0.5f);
                            List<Node> nodes = heightmapGrid.GetNeighboursByLayers(heightmapGrid.grid[agents[i].current.x, agents[i].current.y], layer);
                            foreach (Node node in nodes)
                            {
                                Node.Point point = new Node.Point(node.gridX, node.gridY);
                                if ((heightmapGrid.landmassPoints.Contains(point) || heightmapGrid.coastlinePoints.Contains(point)) && !agents[i].riverPoints.Contains(point) && !heightmapGrid.mountainsPoints.Contains(point))
                                {
                                    agents[i].riverPoints.Add(point);
                                    heightmapGrid.riverPoints.Add(point);
                                    heightmapGrid.SetHeight(node, -0.8f);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!agents[i].riverPoints.Contains(agents[i].current))
                        {
                            agents[i].riverPoints.Add(agents[i].current);
                            heightmapGrid.riverPoints.Add(agents[i].current);
                            heightmapGrid.SetHeight(heightmapGrid.grid[agents[i].current.x, agents[i].current.y], -0.35f);
                        }
                        foreach (Node node in heightmapGrid.GetNeighboursByLayers(heightmapGrid.grid[agents[i].current.x, agents[i].current.y], 8))
                        {
                            Node.Point point = new Node.Point(node.gridX, node.gridY);
                            if ((heightmapGrid.landmassPoints.Contains(point) || heightmapGrid.coastlinePoints.Contains(point)) && !agents[i].riverPoints.Contains(point) && !heightmapGrid.mountainsPoints.Contains(point))
                            {
                                agents[i].riverPoints.Add(point);
                                heightmapGrid.riverPoints.Add(point);
                                heightmapGrid.SetHeight(node, -0.25f);
                            }
                        }
                        SmoothAgents.Sequential(heightmapGrid, agents[i].riverPoints, agents[i].smoothIterations);
                        agents.Remove(agents[i]);
                    }
                }
            }
        }
    }

    static void CloneGrid(Agent agent, Node[,] original)
    {
        int mapWidth = original.GetLength(0);
        int mapHeight = original.GetLength(1);
        Node[,] clone = new Node[mapWidth, mapHeight];
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                clone[x, y] = new Node(x, y)
                {
                    walkable = original[x, y].walkable
                };
            }
        }
        agent.grid = clone;
    }

    public static void Concurrent(HeightmapGrid heightmapGrid, River[] agentsInfo)
    {
        List<Agent> agents = new List<Agent>();
        for (int i = 0; i < agentsInfo.Length; i++)
        {
            Agent agent = new Agent(agentsInfo[i].source, agentsInfo[i].riverMouth, agentsInfo[i].smooth, agentsInfo[i].seed, i);
            agents.Add(agent);
        }

        foreach (Node.Point point in heightmapGrid.mountainsPoints)
        {
            heightmapGrid.grid[point.x, point.y].walkable = false;
        }

        List<Node.Point> riverStartNodes = new List<Node.Point>();
        foreach (Node.Point point in heightmapGrid.mountainsPoints)
        {
            int mountainPointCount = 0;
            Node node = heightmapGrid.grid[point.x, point.y];
            foreach (Node neighbour in heightmapGrid.GetNeighbours(node))
            {
                Node.Point current = new Node.Point(neighbour.gridX, neighbour.gridY);
                if (heightmapGrid.landmassPoints.Contains(current))
                {
                    if (heightmapGrid.ContainsOnList(current, HeightmapGrid.ThreadList.Mountain))
                    {
                        mountainPointCount++;
                    }
                }
                else
                {
                    mountainPointCount += 4;
                }
            }
            if (mountainPointCount <= 3)
            {
                node.walkable = true;
                List<Node> path = heightmapGrid.FindPath(node, heightmapGrid.grid[heightmapGrid.coastlinePoints[0].x, heightmapGrid.coastlinePoints[0].y]);
                if (path != null)
                {
                    riverStartNodes.Add(point);
                }
                node.walkable = false;
            }
        }

        List<Thread> threadList = new List<Thread>();
        foreach (Agent agent in agents)
        {
            Thread thread = new Thread(() => CloneGrid(agent, heightmapGrid.grid));
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
        // Grid cloned
        threadList = new List<Thread>();
        object blocker = new object();
        foreach (Agent agent in agents)
        {
            Thread thread = new Thread(() => AgentCall(heightmapGrid, agent, riverStartNodes, blocker));
            thread.Start();
            threadList.Add(thread);
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
        // Threads Finished
        foreach (int key in heightmapGrid.threadRiverPoints.Keys)
        {
            foreach (Node.Point point in heightmapGrid.threadRiverPoints[key])
            {
                heightmapGrid.heightmap[point.x, point.y] = point.height;
            }
        }
    }

    public static void AgentCall(HeightmapGrid heightmapGrid, Agent agent, List<Node.Point> riverStartNodes, object blocker)
    {
        List<Node.Point> finalPoints = new List<Node.Point>();
        bool running = true;
        List<Node> neighbours = new List<Node>();
        while (running)
        {
            if (!agent.stop)
            {
                if (agent.current == null)
                {
                    int index = agent.start_rng.Next(0, riverStartNodes.Count);
                    agent.current = riverStartNodes[index];
                    agent.grid[riverStartNodes[index].x, riverStartNodes[index].y].walkable = true;

                    index = agent.end_rng.Next(0, heightmapGrid.coastlinePoints.Count);
                    agent.target = new Node.Point(heightmapGrid.coastlinePoints[index].x, heightmapGrid.coastlinePoints[index].y);
                    List<Node> path = heightmapGrid.ConcurrentFindPath(agent.grid[agent.current.x, agent.current.y], agent.grid[agent.target.x, agent.target.y], agent.grid);
                    agent.numOfPoints = path.Count;
                }

                neighbours = heightmapGrid.GetNeighbours(heightmapGrid.grid[agent.current.x, agent.current.y]);
                Node.Point next = null;
                int random = agent.rng.Next(0, 100);
                if (random < 33f)
                {
                    for (int k = neighbours.Count - 1; k >= 0; k--)
                    {
                        Node.Point point = new Node.Point(neighbours[k].gridX, neighbours[k].gridY);
                        if (point.x == agent.target.x && point.y == agent.target.y)
                        {
                            next = point;
                            agent.current = next;
                            agent.stop = true;
                            break;
                        }
                        if (neighbours[k].walkable)
                        {
                            List<Node> path = heightmapGrid.ConcurrentFindPath(agent.grid[neighbours[k].gridX, neighbours[k].gridY], agent.grid[agent.target.x, agent.target.y], agent.grid);
                            if (path == null) neighbours.Remove(neighbours[k]);
                        }
                        else neighbours.Remove(neighbours[k]);
                    }
                    if (!agent.stop)
                    {
                        int value = agent.rng.Next(0, neighbours.Count);
                        next = new Node.Point(neighbours[value].gridX, neighbours[value].gridY);
                    }
                }
                else
                {
                    foreach (Node node in neighbours)
                    {
                        Node.Point point = new Node.Point(node.gridX, node.gridY);
                        if (node.gridX == agent.target.x && node.gridY == agent.target.y)
                        {
                            next = point;
                            agent.current = next;
                            agent.stop = true;
                            break;
                        }
                    }
                    if (!agent.stop)
                    {
                        List<Node> path = heightmapGrid.ConcurrentFindPath(agent.grid[agent.current.x, agent.current.y], agent.grid[agent.target.x, agent.target.y], agent.grid);
                        next = new Node.Point(path[0].gridX, path[0].gridY);
                    }
                }
                if (!agent.stop)
                {
                    agent.current = next;

                    if (!agent.riverPoints.Contains(agent.current))
                    {
                        agent.riverPoints.Add(agent.current);
                        agent.current.height = -1.3f;
                        finalPoints.Add(agent.current);
                    }

                    float percent = agent.riverPoints.Count / (float)agent.numOfPoints;
                    int layer = 1 + (int)(percent * 0.5f);
                    List<Node> nodes = heightmapGrid.GetNeighboursByLayers(heightmapGrid.grid[agent.current.x, agent.current.y], layer);
                    foreach (Node node in nodes)
                    {
                        Node.Point point = new Node.Point(node.gridX, node.gridY);
                        if ((heightmapGrid.landmassPoints.Contains(point) || heightmapGrid.coastlinePoints.Contains(point)) && !agent.riverPoints.Contains(point) && !heightmapGrid.ContainsOnList(point, HeightmapGrid.ThreadList.Mountain))
                        {
                            agent.riverPoints.Add(point);
                            point.height = -0.8f;
                            finalPoints.Add(point);
                        }
                    }
                }
            }
            else
            {
                if (!agent.riverPoints.Contains(agent.current))
                {
                    agent.riverPoints.Add(agent.current);
                    agent.current.height = -0.35f;
                    finalPoints.Add(agent.current);
                }
                foreach (Node node in heightmapGrid.GetNeighboursByLayers(heightmapGrid.grid[agent.current.x, agent.current.y], 8))
                {
                    Node.Point point = new Node.Point(node.gridX, node.gridY);
                    if ((heightmapGrid.landmassPoints.Contains(point) || heightmapGrid.coastlinePoints.Contains(point)) && !agent.riverPoints.Contains(point) && !heightmapGrid.ContainsOnList(point, HeightmapGrid.ThreadList.Mountain))
                    {
                        agent.riverPoints.Add(point);
                        point.height = -0.25f;
                        finalPoints.Add(point);
                    }
                }
                running = false;
                lock (blocker)
                {
                    heightmapGrid.threadRiverPoints[agent.index] = finalPoints;
                }
            }
        }
    }

    public class Agent
    {
        public Node.Point current;
        public Node.Point target;

        public System.Random start_rng;
        public System.Random end_rng;
        public int smoothIterations;
        public int afterMathPoints;
        public int numOfPoints;

        public bool stop;

        public System.Random rng;

        public List<Node.Point> riverPoints;

        public Node[,] grid;
        public int index;

        public Agent(int start, int end, int smoothIterations, int seed, int index)
        {
            start_rng = new System.Random(start);
            end_rng = new System.Random(end);
            this.smoothIterations = smoothIterations;
            afterMathPoints = 10;

            rng = new System.Random(seed);

            riverPoints = new List<Node.Point>();
            this.index = index;
        }

        public void SetupGrid(Node[,] grid)
        {
            this.grid = grid;
        }
    }
}

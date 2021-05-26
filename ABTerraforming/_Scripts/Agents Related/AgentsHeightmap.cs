using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentsHeightmap
{
    public static float[,] GenerateMap(int mapWidth, int mapHeight, int resolutionMultiplier, TerrainData parameters, bool concurrent)
    {
        float[,] heightmap = new float[mapWidth, mapHeight];
        Node[,] grid = new Node[mapWidth, mapHeight];

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float startHeight = -1;
                heightmap[x, y] = startHeight;

                grid[x, y] = new Node(x, y);
            }
        }

        HeightmapGrid heightmapGrid = new HeightmapGrid(heightmap, grid);

        if (!concurrent)
        {
            CoastlineAgents.Sequential(heightmapGrid, parameters.coastline);
            FloodAgents.Sequential(heightmapGrid, parameters.landmassFilling);
            if (parameters.hill.Length > 0)
            {
                HillAgents.Sequential(heightmapGrid, parameters.hill, resolutionMultiplier);
            }
            if (parameters.mountain.Length > 0)
            {
                MountainAgents.Sequential(heightmapGrid, parameters.mountain, resolutionMultiplier);
            }
            if (parameters.beach.Length > 0)
            {
                BeachAgents.Sequential(heightmapGrid, parameters.beach, resolutionMultiplier);
            }
            if (parameters.river.Length > 0)
            {
                RiverAgents.Sequential(heightmapGrid, parameters.river, resolutionMultiplier);
            }
            if (parameters.lake.Length > 0)
            {
                LakeAgents.Sequential(heightmapGrid, parameters.lake, resolutionMultiplier);
            }
            Debug.Log("Sequential");
        }
        else
        {
            Debug.Log("Concurrent");
            CoastlineAgents.Concurrent(heightmapGrid, parameters.coastline);
            foreach (int key in heightmapGrid.threadCoastlinePoints.Keys)
            {
                foreach(Node.Point point in heightmapGrid.threadCoastlinePoints[key])
                {
                    heightmapGrid.coastlinePoints.Add(point);
                }
            }
            FloodAgents.Concurrent(heightmapGrid, parameters.landmassFilling);

            if (parameters.hill.Length > 0)
            {
                HillAgents.Concurrent(heightmapGrid, parameters.hill, resolutionMultiplier);
            }
            if (parameters.mountain.Length > 0)
            {
                MountainAgents.Concurrent(heightmapGrid, parameters.mountain, resolutionMultiplier);
            }
            if (parameters.beach.Length > 0)
            {
                BeachAgents.Concurrent(heightmapGrid, parameters.beach, resolutionMultiplier);
            }
            if (parameters.river.Length > 0)
            {
                RiverAgents.Concurrent(heightmapGrid, parameters.river);
            }
            if (parameters.lake.Length > 0)
            {
                LakeAgents.Concurrent(heightmapGrid, parameters.lake, resolutionMultiplier);
            }
            SmoothAgents.Concurrent(heightmapGrid, parameters);
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float noiseHeight = heightmap[x, y];

                if (noiseHeight > maxNoiseHeight)
                {
                    maxNoiseHeight = noiseHeight;
                }
                else if (noiseHeight < minNoiseHeight)
                {
                    minNoiseHeight = noiseHeight;
                }

                heightmap[x, y] = noiseHeight;
            }
        }
        
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                heightmap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, heightmap[x, y]);
            }
        }

        return heightmapGrid.heightmap;
    }

    public static float RandomFloat(System.Random rng, float minimum, float maximum)
    {
        return (float)(rng.NextDouble() * (maximum - minimum) + minimum);
    }
}
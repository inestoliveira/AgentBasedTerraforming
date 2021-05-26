using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AgentsData", menuName = "Terrain/AgentsData")]
public class AgentsData : ScriptableObject
{
    [HideInInspector]
    public float[] heightmapArray;

    public TerrainData terrainData;

    public bool CheckRebuild(TerrainData data)
    {
        // Coastline
        if (!data.coastline.Equals(terrainData.coastline))
        {
            return true;
        }
        // Hills
        if (terrainData.hill.Length != data.hill.Length)
        {
            return true;
        }
        else
        {
            for (int i = 0; i < data.hill.Length; i++)
            {
                if (!data.hill[i].Equals(terrainData.hill[i]))
                {
                    return true;
                }
            }
        }
        // Mountains
        if (data.mountain.Length != terrainData.mountain.Length)
        {
            return true;
        }
        else
        {
            for (int i = 0; i < data.mountain.Length; i++)
            {
                if (!data.mountain[i].Equals(terrainData.mountain[i]))
                {
                    return true;
                }
            }
        }
        // Beach
        if (data.beach.Length != terrainData.beach.Length)
        {
            return true;
        }
        else
        {
            for (int i = 0; i < data.beach.Length; i++)
            {
                if (!data.beach[i].Equals(terrainData.beach[i]))
                {
                    return true;
                }
            }
        }
        // River
        if (data.river.Length != terrainData.river.Length)
        {
            return true;
        }
        else
        {
            for (int i = 0; i < data.river.Length; i++)
            {
                if (!data.river[i].Equals(terrainData.river[i]))
                {
                    return true;
                }
            }
        }
        // Lake
        if (data.lake.Length != terrainData.lake.Length)
        {
            return true;
        }
        else
        {
            for (int i = 0; i < data.lake.Length; i++)
            {
                if (!data.lake[i].Equals(terrainData.lake[i]))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public float[,] Heightmap()
    {
        float[,] heightmap = new float[heightmapArray.Length / 2, heightmapArray.Length / 2];
        for (int y = 0; y < heightmap.GetLength(1); y++)
        {
            for (int x = 0; x < heightmap.GetLength(0); x++)
            {
                heightmap[x, y] = heightmapArray[y + heightmap.GetLength(0) * x];
            }
        }
        return heightmap;
    }

    public void SaveAgentsParameters(float[,] heightmap, TerrainData data)
    {
        heightmapArray = new float[heightmap.GetLength(0) * heightmap.GetLength(1)];
        for (int y = 0; y < heightmap.GetLength(1); y++)
        {
            for (int x = 0; x < heightmap.GetLength(0); x++)
            {
                heightmapArray[y + heightmap.GetLength(0) * x] = heightmap[x, y];
            }
        }
        terrainData = data;
    }
}

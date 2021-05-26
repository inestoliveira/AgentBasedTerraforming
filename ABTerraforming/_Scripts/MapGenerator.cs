using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum DrawMode { NoiseMap, ColorMap, Mesh }

public class MapGenerator : MonoBehaviour
{
    [Header("Controllers")]
    [HideInInspector]
    public bool autoUpdate;
    public DrawMode drawMode;
    public bool concurrent;
    
    const int levelOfDetail = 0;
    [Range(1, 13)]
    public int resolutionMultiplier;
    const int mapChunkSize = 241;

    [Header("Map Details")]
    public Material terrainMaterial;
    public TerrainData terrainData;

    public void GenerateMap()
    {
        Camera.main.depthTextureMode = DepthTextureMode.Depth;
        float[,] heightmap = AgentsHeightmap.GenerateMap(mapChunkSize * resolutionMultiplier, mapChunkSize * resolutionMultiplier, resolutionMultiplier, terrainData, concurrent);

        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];

        MapDisplay display = FindObjectOfType<MapDisplay>();
        switch (drawMode)
        {
            case DrawMode.NoiseMap:
                display.DrawTexture(TextureGenerator.TextureFromHeightMap(heightmap));
                break;
            case DrawMode.ColorMap:
                display.DrawTexture(TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize, mapChunkSize));
                break;
            case DrawMode.Mesh:
                display.DrawMesh(MeshGenerator.GenerateTerrainMesh(heightmap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, levelOfDetail), TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize, mapChunkSize));
                terrainData.UpdateMeshHeights(terrainMaterial);
                break;
        }
    }

    void OnValidate()
    {
        UnityEditor.EditorApplication.update += UpdateTerrain;

        // Coastline
        if (terrainData.coastline.seed < 0)
        {
            terrainData.coastline.seed = 0;
        }
        if (terrainData.coastline.detail < 4)
        {
            terrainData.coastline.detail = 4;
        }
        if (terrainData.coastline.maxLength < 1)
        {
            terrainData.coastline.maxLength = 1;
        }
        if (terrainData.coastline.minLength < 0)
        {
            terrainData.coastline.minLength = 0;
        }
        int limit = (resolutionMultiplier * mapChunkSize) / 2;
        if (terrainData.coastline.maxLength > limit - 1)
        {
            terrainData.coastline.maxLength = limit - 1;
        }
        if (terrainData.coastline.minLength >= terrainData.coastline.maxLength)
        {
            terrainData.coastline.minLength = terrainData.coastline.maxLength - 1;
        }
        if (terrainData.coastline.smooth< 0)
        {
            terrainData.coastline.smooth= 0;
        }
        // Flood
        if (terrainData.landmassFilling.seed < 0)
        {
            terrainData.landmassFilling.seed = 0;
        }
        if (terrainData.landmassFilling.smooth < 0)
        {
            terrainData.landmassFilling.smooth = 0;
        }
        // Hill
        for (int i = terrainData.hill.Length - 1; i >= 0; i--)
        {
            if (terrainData.hill[i].seed < 0)
            {
                terrainData.hill[i].seed = 0;
            }
            if (terrainData.hill[i].length < 0)
            {
                terrainData.hill[i].length = 0;
            }
            if (terrainData.hill[i].smooth < 0)
            {
                terrainData.hill[i].smooth = 0;
            }
        }
        // Mountain
        for (int i = terrainData.mountain.Length - 1; i >= 0; i--)
        {
            if (terrainData.mountain[i].seed < 0)
            {
                terrainData.mountain[i].seed = 0;
            }
            if (terrainData.mountain[i].length < 0)
            {
                terrainData.mountain[i].length = 0;
            }
            if (terrainData.mountain[i].width < 0)
            {
                terrainData.mountain[i].width = 0;
            }
            if (terrainData.mountain[i].smooth < 0)
            {
                terrainData.mountain[i].smooth = 0;
            }
        }
        // Beach
        for (int i = terrainData.beach.Length - 1; i >= 0; i--)
        {
            if (terrainData.beach[i].seed < 0)
            {
                terrainData.beach[i].seed = 0;
            }
            if (terrainData.beach[i].length < 0)
            {
                terrainData.beach[i].length = 0;
            }
            if (terrainData.beach[i].width < 0)
            {
                terrainData.beach[i].width = 0;
            }
            if (terrainData.beach[i].smooth < 0)
            {
                terrainData.beach[i].smooth = 0;
            }
        }
        // River
        for (int i = terrainData.river.Length - 1; i >= 0; i--)
        {
            if (terrainData.river[i].seed < 0)
            {
                terrainData.river[i].seed = 0;
            }
            if (terrainData.river[i].source < 0)
            {
                terrainData.river[i].source = 0;
            }
            if (terrainData.river[i].riverMouth < 0)
            {
                terrainData.river[i].riverMouth = 0;
            }
            if (terrainData.river[i].smooth < 0)
            {
                terrainData.river[i].smooth = 0;
            }
        }
        // Lake
        for (int i = terrainData.lake.Length - 1; i >= 0; i--)
        {
            if (terrainData.lake[i].seed < 0)
            {
                terrainData.lake[i].seed = 0;
            }
            if (terrainData.lake[i].length < 0)
            {
                terrainData.lake[i].length = 0;
            }
            if (terrainData.lake[i].width < 0)
            {
                terrainData.lake[i].width = 0;
            }
            if (terrainData.lake[i].smooth < 0)
            {
                terrainData.lake[i].smooth = 0;
            }
        }
    }

    void UpdateTerrain()
    {
        UnityEditor.EditorApplication.update -= UpdateTerrain;
        terrainData.ApplyToMaterial(terrainMaterial);
    }
}
[System.Serializable]
public class TerrainData
{
    [Header("Shader Data")]
    public Layer[] layers;

    [Header("Heights")]
    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    [Header("Elements")]
    [Tooltip("Creates the silhouette of the island.")]
    public Coastline coastline;
    [Tooltip("Fill the interior of the island.")]
    public LandmassFilling landmassFilling;
    [Tooltip("Creates hills.")]
    public Hill[] hill;
    [Tooltip("Creates mountains, avoiding hills.")]
    public Mountain[] mountain;
    [Tooltip("Creates beaches along the coastline.")]
    public Beach[] beach;
    [Tooltip("Creates rivers, avoiding mountains.")]
    public River[] river;
    [Tooltip("Creates lakes.")]
    public Lake[] lake;

    const int textureSize = 512;
    const TextureFormat textureFormat = TextureFormat.RGB565;

    public float MinHeight
    {
        get { return meshHeightMultiplier * meshHeightCurve.Evaluate(0); }
    }

    public float MaxHeight
    {
        get { return meshHeightMultiplier * meshHeightCurve.Evaluate(1); }
    }

    public void ApplyToMaterial(Material material)
    {
        material.SetInt("layerCount", layers.Length);
        material.SetColorArray("baseColors", layers.Select(x => x.tint).ToArray());
        material.SetFloatArray("baseStartHeights", layers.Select(x => x.startHeight).ToArray());
        material.SetFloatArray("baseBlends", layers.Select(x => x.blendStrength).ToArray());
        material.SetFloatArray("baseColorStrength", layers.Select(x => x.tintStrength).ToArray());
        material.SetFloatArray("baseTextureScales", layers.Select(x => x.textureScale).ToArray());
        Texture2DArray texturesArray = GenerateTextureArray(layers.Select(x => x.texture).ToArray());
        material.SetTexture("baseTextures", texturesArray);

        UpdateMeshHeights(material);
    }

    Texture2DArray GenerateTextureArray(Texture2D[] textures)
    {
        Texture2DArray textureArray = new Texture2DArray(textureSize, textureSize, textures.Length, textureFormat, true);
        for (int i = 0; i < textures.Length; i++)
        {
            textureArray.SetPixels(textures[i].GetPixels(), i);
        }
        textureArray.Apply();
        return textureArray;
    }

    public void UpdateMeshHeights(Material material)
    {
        material.SetFloat("minHeight", MinHeight);
        material.SetFloat("maxHeight", MaxHeight);
    }
}

[System.Serializable]
public class Layer
{
    public Texture2D texture;
    public Color tint;
    [Range(0, 1)]
    public float tintStrength;
    [Range(0, 1)]
    public float startHeight;
    [Range(0, 1)]
    public float blendStrength;
    public float textureScale;
}

[System.Serializable]
public struct Coastline
{
    [Header("Random Controller")]
    public int seed;

    [Header("Values")]
    [Tooltip("Lower the detail, more square like will be.")]
    [Range(4, 20)]
    public int detail;
    [Tooltip("Max distance to center.")]
    public int maxLength;
    [Tooltip("Min distance to center.")]
    public int minLength;
    [Tooltip("Increment the value for more smoothness.")]
    public int smooth;
}

[System.Serializable]
public struct LandmassFilling
{
    [Header("Random Controller")]
    public int seed;

    [Header("Values")]
    [Range(0.25f, 0.5f)]
    public float maxHeight;
    [Range(0, 0.24f)]
    public float minHeight;
    [Tooltip("Increment the value for more smoothness.")]
    public int smooth;
}

[System.Serializable]
public struct Hill
{
    [Header("Random Controller")]
    public int seed;

    [Header("Values")]
    [Tooltip("Size of the hill.")]
    public int length;
    [Tooltip("Increment the value for more smoothness.")]
    public int smooth;
}

[System.Serializable]
public struct Mountain
{
    [Header("Random Controller")]
    public int seed;

    [Header("Values")]
    public int length;
    public int width;
    [Tooltip("Min height of the mountain.")]
    [Range(0, 0.6f)]
    public float minHeight;
    [Tooltip("Max height of the mountain.")]
    [Range(0.65f, 2f)]
    public float maxHeight;
    [Tooltip("Increment the value for more smoothness.")]
    public int smooth;
}

[System.Serializable]
public struct Beach
{
    [Header("Random Controller")]
    public int seed;

    [Header("Values")]
    [Tooltip("Size on the coastline.")]
    public int length;
    [Tooltip("Distance from the coastline to the center of the island.")]
    public int width;
    [Tooltip("Increment the value for more smoothness.")]
    public int smooth;
}

[System.Serializable]
public struct River
{
    [Header("Random Controller")]
    public int seed;

    [Header("Values")]
    [Tooltip("Start point of the river.")]
    public int source;
    [Tooltip("End point of the river.")]
    public int riverMouth;
    [Tooltip("Increment the value for more smoothness.")]
    public int smooth;
}

[System.Serializable]
public struct Lake
{
    [Header("Random Controller")]
    public int seed;

    [Header("Values")]
    public int length;
    public int width;
    [Tooltip("Increment the value for more smoothness.")]
    public int smooth;
}
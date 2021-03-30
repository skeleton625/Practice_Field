using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GPUInstancer;

public class TerrainGenerator : MonoBehaviour
{
    [SerializeField] private float OffsetX = 0f;
    [SerializeField] private float OffsetZ = 0f;
    [SerializeField] private float RandomScale = 1f;
    [SerializeField] private float HeightScale = 1f;
    [SerializeField] GPUInstancerDetailManager detailManager = null;

    private Terrain mainTerrain = null;

    private int width;
    private int height;
    private int[,] terrainDetail = null;
    private float[,] terrainHeights = null;
    private float[,,] terrainLayers = null;

    private void Awake()
    {
        mainTerrain = GetComponent<Terrain>();
        width = height = mainTerrain.terrainData.alphamapResolution;

        terrainDetail = new int[width, height];
        for (int z = 512; z < 1536; z++)
        {
            for(int x = 512; x < 1536; x++)
            {
                terrainDetail[z, x] = 2;
            }
        }
        GenerateTerrain();

        ApplyTerrainLayers();
        //ApplyTerrainDetail();
    }

    private void GenerateTerrain()
    {
        terrainHeights = new float[width, height];
        terrainLayers = new float[width, height, mainTerrain.terrainData.alphamapLayers];
        for (int z = 0; z < width; z++)
        {
            for (int x = 0; x < height; x++)
            {
                float zCoord = OffsetZ + z / 2048f * RandomScale;
                float xCoord = OffsetX + x / 2048f * RandomScale;
                terrainLayers[z, x, 0] = 1;
                terrainHeights[z, x] = Mathf.PerlinNoise(xCoord, zCoord) * HeightScale;
            }
        }

        mainTerrain.terrainData.SetHeights(0, 0, terrainHeights);
        mainTerrain.terrainData.SetAlphamaps(0, 0, terrainLayers);
    }

    public void ApplyTerrainLayers(int sx, int sz, int i, ref float[,] layers)
    {
        int[,] intLayers = new int[layers.GetLength(0), layers.GetLength(1)];
        float[,,] tmpLayers = new float[layers.GetLength(0), layers.GetLength(1), terrainLayers.GetLength(2)];

        for(int z = 0; z < layers.GetLength(0); z++)
        {
            for (int x = 0; x < layers.GetLength(1); x++)
            {
                if (layers[z, x] > 0)
                {
                    terrainLayers[z + sz, x + sx, i] = tmpLayers[z, x, i] = layers[z, x];
                    terrainLayers[z + sz, x + sx, 0] = tmpLayers[z, x, 0] = 0;
                }
                else
                {
                    tmpLayers[z, x, i] = terrainLayers[z + sz, x + sx, i];
                    tmpLayers[z, x, 0] = terrainLayers[z + sz, x + sx, 0];
                }

            }
        }    

        mainTerrain.terrainData.SetAlphamaps(sx, sz, tmpLayers);
        mainTerrain.terrainData.SetDetailLayer(sx, sz, 0, intLayers);
        GPUInstancerAPI.UpdateDetailInstances(detailManager, true);
    }

    public void ApplyTerrainLayers()
    {
        mainTerrain.terrainData.SetAlphamaps(0, 0, terrainLayers);
    }

    public void RemoveTerrainDetail(MeshCollider meshCollider, Vector3 center, Vector3 size, float offset)
    {
        detailManager.RemoveInstancesInsideMeshCollider(meshCollider, center, size / 2, offset);
    }

    public void RemoveTerrainDetail(int sx, int sz, ref bool[,] details)
    {
        for(int z = 0; z < details.GetLength(0); z++)
        {
            for (int x = 0; x < details.GetLength(1); x++)
            {
                if (details[z, x])
                    terrainDetail[z + sz, x + sx] = 0;
            }
        }

        List<int[,]> detailMap = new List<int[,]>();
        detailMap.Add(terrainDetail);
        detailManager.SetDetailMapData(detailMap);
        GPUInstancerAPI.UpdateDetailInstances(detailManager, true);
    }

    public void ApplyTerrainDetail()
    {
        List<int[,]> detailMap = new List<int[,]>();
        detailMap.Add(terrainDetail);
        detailManager.SetDetailMapData(detailMap);
        GPUInstancerAPI.InitializeGPUInstancer(detailManager);
    }
}

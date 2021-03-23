using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    private Terrain mainTerrain = null;
    private float[,,] terrainLayers = null;

    private void Awake()
    {
        mainTerrain = GetComponent<Terrain>();
        int size = mainTerrain.terrainData.alphamapResolution;
        terrainLayers = new float[size, size, mainTerrain.terrainData.alphamapLayers];
        for (int z = 0; z < size; z++)
            for (int x = 0; x < size; x++)
                terrainLayers[z, x, 0] = 1;

        mainTerrain.terrainData.SetAlphamaps(0, 0, terrainLayers);
    }

    public void SetTerrainLayer(int x, int z, int type)
    {
        terrainLayers[z, x, 0] = 0;
        terrainLayers[z, x, type] = 1;
    }

    public void ApplyTerrainLayers(int sx, int sz, int i, ref float[,] layers)
    {
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
    }

    public void ApplyTerrainLayers()
    {
        mainTerrain.terrainData.SetAlphamaps(0, 0, terrainLayers);
    }
}

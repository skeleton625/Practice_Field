using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    private Terrain mainTerrain = null;
    private float[,,] layers = null;

    private void Awake()
    {
        mainTerrain = GetComponent<Terrain>();
        int size = mainTerrain.terrainData.heightmapResolution - 1;
        layers = new float[size, size, mainTerrain.terrainData.alphamapLayers];
        for (int z = 0; z < size; z++)
            for (int x = 0; x < size; x++)
                layers[z, x, 0] = 1;
    }

    public void SetTerrainLayer(int x, int z, int type)
    {
        layers[z, x, 0] = 0;
        layers[z, x, type] = 1;
    }

    public void ApplyTerrainLayers(int x, int z)
    {
        mainTerrain.terrainData.SetAlphamaps(x, z, layers);
    }
}

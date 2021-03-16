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
        layers = new float[mainTerrain.terrainData.heightmapResolution - 1,
                           mainTerrain.terrainData.heightmapResolution - 1,
                           mainTerrain.terrainData.alphamapLayers];
    }

    public void SetTerrainLayer(int x, int z, int type)
    {
        layers[z, x, type] = 1;
    }

    public void ApplyTerrainLayers(int x, int z)
    {
        mainTerrain.terrainData.SetAlphamaps(x, z, layers);
    }
}

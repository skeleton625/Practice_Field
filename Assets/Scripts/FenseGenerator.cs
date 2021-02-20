using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FenseGenerator : MonoBehaviour
{
    [SerializeField] private int PoleCount = 4;
    [SerializeField] private float RateScale = .1f;
    [SerializeField] private float GrassAlpha = 1f;
    [SerializeField] private float GrassScale = .1f;
    [SerializeField] private Transform Grass = null;
    [SerializeField] private Transform Fense = null;
    [SerializeField] private Transform FensePole = null;
    [SerializeField] private Transform FenseParent = null;
    [SerializeField] private LayerMask RayMask = default;

    private int prePoleCount = 0;
    private Camera mainCamera = null;
    private List<Vector3> fensePoleList = null;
    

    private void Start()
    {
        mainCamera = Camera.main;
        fensePoleList = new List<Vector3>();
    }

    // Update is called once per frame
    private void Update()
    {
        var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if(Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, 1000f, RayMask))
        {
            FensePole.position = hit.point;
            if(Input.GetMouseButtonDown(0))
            {
                if (prePoleCount.Equals(PoleCount))
                    ClearFense();

                var pole = Instantiate(FensePole, hit.point, Quaternion.identity);
                pole.SetParent(FenseParent);
                SetLayerRecursive(pole, 2);
                fensePoleList.Add(pole.position);
                if (prePoleCount > 0)
                    GenerateFense(fensePoleList[prePoleCount - 1], fensePoleList[prePoleCount]);
                if (prePoleCount.Equals(PoleCount - 1))
                {
                    GenerateFense(fensePoleList[0], fensePoleList[PoleCount - 1]);
                    GenerateGrass();
                }

                ++prePoleCount;
            }
        }
    }

    private void GenerateFense(Vector3 start, Vector3 end)
    {
        for(float rate = RateScale; rate < 1; rate += RateScale)
        {
            var position = Vector3.Lerp(start, end, rate);
            var fense = Instantiate(Fense, position, Quaternion.identity);
            fense.SetParent(FenseParent);
        }
    }

    private void GenerateGrass()
    {
        var xList = new List<float>();
        var zList = new List<float>();

        foreach(var position in fensePoleList)
        {
            xList.Add(position.x);
            zList.Add(position.z);
        }

        xList.Sort();
        zList.Sort();

        xList[0] += GrassScale;
        zList[0] += GrassScale;
        xList[3] -= GrassScale;
        zList[3] -= GrassScale;

        for(float x = xList[0]; x < xList[3]; x += GrassScale)
        {
            for(float z = zList[0]; z < zList[3]; z += GrassScale)
            {
                int crossX = 0, crossZ = 0;
                float dotX, dotZ;
                for(int i = 0; i < PoleCount; i++)
                {
                    int j = (i + 1) % PoleCount;
                    var p1 = fensePoleList[i];
                    var p2 = fensePoleList[j];

                    if((p1.z > z) ^ (p2.z > z))
                    {
                        dotX = CalculateEquation(p1.z, p1.x, p2.z, p2.x, z);

                        if (x < dotX - GrassAlpha)
                            crossX++;
                        if (x < dotX + GrassAlpha)
                            crossX++;
                    }

                    if((p1.x > x) ^ (p2.x > x))
                    {
                        dotZ = CalculateEquation(p1.x, p1.z, p2.x, p2.z, x);
                        if (z < dotZ - GrassAlpha)
                            crossZ++;
                        if (z < dotZ + GrassAlpha)
                            crossZ++;
                    }
                }

                if (crossX.Equals(2) && crossZ.Equals(2))
                {
                    var grass = Instantiate(Grass, new Vector3(x, 0, z), Quaternion.identity);
                    grass.SetParent(FenseParent);
                }
            }
        }
    }

    private void ClearFense()
    {
        foreach (Transform child in FenseParent)
            Destroy(child.gameObject);
        fensePoleList.Clear();
        prePoleCount = 0;
    }

    private float CalculateEquation(float p1X, float p1Z, float p2X, float p2Z, float dot)
    {
        return (p1Z - p2Z) * (dot - p1X) / (p1X - p2X) + p1Z;
    }

    private void SetLayerRecursive(Transform parent, int layer)
    {
        parent.gameObject.layer = layer;
        foreach (Transform child in parent)
            SetLayerRecursive(child, layer);
    }
}

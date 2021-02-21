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
    private List<Transform> fensePoleList = null;
    

    private void Start()
    {
        mainCamera = Camera.main;
        fensePoleList = new List<Transform>();
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
                else if(prePoleCount.Equals(PoleCount - 1))
                {
                    var vec1 = (fensePoleList[PoleCount - 2].position - fensePoleList[PoleCount - 3].position).normalized;
                    var vec2 = (hit.point - fensePoleList[PoleCount - 2].position).normalized;
                    var angle = Vector3.Angle(vec1, vec2);
                    if (angle < 40 || angle > 90)
                        return;
                }

                var pole = Instantiate(FensePole, hit.point, Quaternion.identity);
                pole.SetParent(FenseParent);
                SetLayerRecursive(pole, 2);
                fensePoleList.Add(pole);
                if (prePoleCount > 0)
                    GenerateFense(fensePoleList[prePoleCount - 1], fensePoleList[prePoleCount]);
                if (prePoleCount.Equals(PoleCount - 1))
                {
                    GenerateFense(fensePoleList[0], fensePoleList[PoleCount - 1]);
                    GenerateGrass();
                }

                ++prePoleCount;
            }

            if(Input.GetKeyDown(KeyCode.R))
            {
                if (!fensePoleList.Count.Equals(0))
                {
                    Destroy(fensePoleList[prePoleCount - 1].gameObject);
                    fensePoleList.RemoveAt(prePoleCount - 1);
                    prePoleCount--;
                }
                else
                    ClearFense();

            }
        }
    }

    private void GenerateFense(Transform start, Transform end)
    {
        for(float rate = RateScale; rate < 1; rate += RateScale)
        {
            var position = Vector3.Lerp(start.position, end.position, rate);
            var fense = Instantiate(Fense, position, Quaternion.identity);
            fense.SetParent(end);
        }
    }

    private void GenerateGrass()
    {
        var xList = new List<float>();
        var zList = new List<float>();

        foreach(var trans in fensePoleList)
        {
            xList.Add(trans.position.x);
            zList.Add(trans.position.z);
        }

        xList.Sort();
        zList.Sort();

        xList[0] += GrassScale;
        zList[0] += GrassScale;
        xList[PoleCount - 1] -= GrassScale;
        zList[PoleCount - 1] -= GrassScale;

        for(float x = xList[0]; x < xList[PoleCount - 1]; x += GrassScale)
        {
            for(float z = zList[0]; z < zList[PoleCount - 1]; z += GrassScale)
            {
                int crossX = 0, crossZ = 0;
                float dotX, dotZ;
                for(int i = 0; i < PoleCount; i++)
                {
                    int j = (i + 1) % PoleCount;
                    var p1 = fensePoleList[i].position;
                    var p2 = fensePoleList[j].position;

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

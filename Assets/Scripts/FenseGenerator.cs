using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FenseGenerator : MonoBehaviour
{
    [SerializeField] private float GrassAlpha = 1f;
    [SerializeField] private float GrassScale = .1f;
    [SerializeField] private Transform Grass = null;
    [SerializeField] private Transform Fense = null;
    [SerializeField] private Transform FensePole = null;
    [SerializeField] private Transform FenseParent = null;
    [SerializeField] private FieldEntity GrassParent = null;
    [SerializeField] private Transform FensePoleVisual = null;
    [SerializeField] private FieldData[] FieldDatas = null;
    [SerializeField] private TestAI TestPlayerAI = null;
    [SerializeField] private LayerMask RayMask = default;

    private int poleCount = 0;
    private bool isCollid = false;
    private bool isComplete = false;
    private Camera mainCamera = null;
    private FieldEntity preFieldEntity = null;
    private List<Transform> poleTrans = null;

    private void Start()
    {
        mainCamera = Camera.main;
        poleTrans = new List<Transform>();
    }

    // Update is called once per frame
    private void Update()
    {
        var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if(Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, 1000f, RayMask))
        {
            transform.position = hit.point;
            if(Input.GetMouseButtonDown(0))
            {
                if (isComplete)
                    ClearFense();
                else if (poleCount > 3 && isCollid)
                {
                    GenerateFense(poleTrans[poleCount - 1], poleTrans[0]);
                    GenerateGrass();
                    isComplete = true;
                }
                else
                {
                    var pole = Instantiate(FensePole, hit.point, Quaternion.identity);
                    pole.SetParent(FenseParent);
                    poleTrans.Add(pole);
                    if(poleCount > 0)
                        GenerateFense(poleTrans[poleCount - 1], poleTrans[poleCount]);
                    ++poleCount;
                }
            }

            if(Input.GetKeyDown(KeyCode.R) && !isComplete)
            {
                if (!poleTrans.Count.Equals(0))
                {
                    Destroy(poleTrans[poleCount - 1].gameObject);
                    poleTrans.RemoveAt(poleCount - 1);
                    poleCount--;
                }
                else
                    ClearFense();

            }
        }
    }

    private void GenerateFense(Transform start, Transform end)
    {
        var rateScale = 1f / (Vector3.Distance(end.position, start.position));
        for(float rate = rateScale; rate < (1 - rateScale); rate += rateScale)
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

        foreach(var trans in poleTrans)
        {
            xList.Add(trans.position.x);
            zList.Add(trans.position.z);
        }

        xList.Sort();
        zList.Sort();

        xList[0] += GrassScale;
        zList[0] += GrassScale;
        xList[poleCount - 1] -= GrassScale;
        zList[poleCount - 1] -= GrassScale;

        int odd = 0;
        var grassTrans = new List<Transform>();
        preFieldEntity = Instantiate(GrassParent, Vector3.zero, Quaternion.identity);
        for (float x = xList[0]; x < xList[poleCount - 1]; x += GrassScale)
        {
            var tmpList = new List<Transform>();
            for (float z = zList[0]; z < zList[poleCount - 1]; z += GrassScale)
            {
                int crossX = 0, crossZ = 0;
                bool isInside = true;
                for (int i = 0; i < poleCount; i++)
                {
                    int j = (i + 1) % poleCount;
                    var p1 = poleTrans[i].position;
                    var p2 = poleTrans[j].position;

                    if (CalculateEquation(p1.z, p1.x, p2.z, p2.x, z, x, ref crossX))
                    {
                        isInside = false;
                        break;
                    }
                    if (CalculateEquation(p1.x, p1.z, p2.x, p2.z, x, z, ref crossZ))
                    {
                        isInside = false;
                        break;
                    }
                }

                if (isInside && (crossX % 2).Equals(1) && (crossZ % 2).Equals(1))
                {
                    var grass = Instantiate(Grass, new Vector3(x, 0, z), Quaternion.identity);
                    grass.gameObject.SetActive(false);
                    grass.SetParent(preFieldEntity.transform);
                    tmpList.Add(grass);
                }
            }

            if ((odd % 2).Equals(1))
                tmpList.Reverse();
            grassTrans.AddRange(tmpList);
            ++odd;
        }

        preFieldEntity.Initialize(FieldDatas[0], grassTrans);
        preFieldEntity.SetWorking(TestPlayerAI, true);
    }

    private void ClearFense()
    {
        foreach (Transform child in FenseParent)
            Destroy(child.gameObject);
        Destroy(preFieldEntity.gameObject);
        poleTrans.Clear();
        poleCount = 0;
        isCollid = false;
        isComplete = false;
    }

    private bool CalculateEquation(float p1X, float p1Z, float p2X, float p2Z, float x, float z, ref int cross)
    {
        if((p1X > x) ^ (p2X > x))
        {
            var collid = (p1Z - p2Z) * (x - p1X) / (p1X - p2X) + p1Z;
            var minus = collid - GrassAlpha;
            var plus = collid + GrassAlpha;
            if (minus <= z && z <= plus)
                return true;
            if(z < collid)
                cross++;
        }
        return false;
    }

    private void SetLayerRecursive(Transform parent, int layer)
    {
        parent.gameObject.layer = layer;
        foreach (Transform child in parent)
            SetLayerRecursive(child, layer);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("FensePole"))
        {
            isCollid = true;
            FensePoleVisual.SetParent(null);
            FensePoleVisual.position = other.transform.position + Vector3.up;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("FensePole"))
        {
            isCollid = false;
            FensePoleVisual.SetParent(transform);
            FensePoleVisual.localPosition = Vector3.up;
        }
    }
}

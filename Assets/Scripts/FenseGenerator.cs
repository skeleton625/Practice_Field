using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FenseGenerator : MonoBehaviour
{
    [SerializeField] private float LengthLimit = 0f;
    [SerializeField] private float CropsAlpha = 1f;
    [SerializeField] private float CropsScale = .1f;
    [SerializeField] private Transform Fense = null;
    [SerializeField] private Transform FensePole = null;
    [SerializeField] private Transform FensePoleVisual = null;
    [SerializeField] private CropsEntity CropsParent = null;
    [SerializeField] private CropsData[] CropsDatas = null;
    [SerializeField] private TestAI TestPlayerAI = null;
    [SerializeField] private Material VisualMaterial = null;
    [SerializeField] private LayerMask RayMask = default;
    [SerializeField] private Color EnableColor = Color.black;
    [SerializeField] private Color DisableColor = Color.black;

    private int poleCount = 0;
    private float lenLimit = 0f;
    private bool isEnable = true;
    private bool isCollid = false;

    private Camera mainCamera = null;
    private CropsEntity preCropsEntity = null;
    private CropsEntity collidCropsEntity = null;
    private List<Transform> poleTrans = null;

    private void Start()
    {
        mainCamera = Camera.main;
        poleTrans = new List<Transform>();
        LengthLimit = LengthLimit * LengthLimit;
        VisualMaterial.SetColor("_BaseColor", EnableColor);
    }

    // Update is called once per frame
    private void Update()
    {
        var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if(Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, 1000f, RayMask))
        {
            transform.position = hit.point;
            if (poleCount > 0)
            {
                var flag = IsBelowLimit(poleTrans[poleCount - 1].position, hit.point);
                if (poleCount.Equals(3))
                    flag &= IsBelowLimit(hit.point, poleTrans[0].position);
                if (isEnable != flag)
                {
                    isEnable = flag;
                    if (flag)
                        VisualMaterial.SetColor("_BaseColor", EnableColor);
                    else
                        VisualMaterial.SetColor("_BaseColor", DisableColor);
                }
            }

            if(Input.GetMouseButtonDown(0) && isEnable)
            {
                var position = FensePoleVisual.position;
                position.y = hit.point.y;
                var pole = Instantiate(FensePole, position, Quaternion.identity);
                poleTrans.Add(pole);
                switch (poleCount)
                {
                    case 0:
                        if(!isCollid)
                            lenLimit = LengthLimit;
                        preCropsEntity = Instantiate(CropsParent, position, Quaternion.identity);
                        ++poleCount;
                        break;
                    case 1:
                    case 2:
                        GenerateFense(poleTrans[poleCount - 1], poleTrans[poleCount]);
                        ++poleCount;
                        break;
                    case 3:
                        GenerateFense(poleTrans[poleCount - 1], poleTrans[poleCount]);
                        GenerateFense(poleTrans[poleCount], poleTrans[0]);
                        GenerateCrops();
                        ClearFense();
                        break;
                }
                pole.SetParent(preCropsEntity.transform);
            }

            if(Input.GetKeyDown(KeyCode.R))
                ClearFense();
        }
    }

    private bool IsBelowLimit(Vector3 prevPos, Vector3 nextPos)
    {
        return (prevPos - nextPos).sqrMagnitude < lenLimit;
    }

    private void GenerateFense(Transform start, Transform end)
    {
        var rateScale = 1f / (Vector3.Distance(start.position, end.position));
        for (float rate = rateScale; rate < .99f; rate += rateScale)
        {
            var position = Vector3.Lerp(start.position, end.position, rate);
            var fense = Instantiate(Fense, position, Quaternion.identity);
            fense.SetParent(preCropsEntity.transform);
        }
    }

    private void GenerateCrops()
    {
        var centerX = 0f;
        var centerZ = 0f;
        var xList = new List<float>();
        var zList = new List<float>();
        foreach (var trans in poleTrans)
        {
            xList.Add(trans.position.x);
            zList.Add(trans.position.z);
            centerX += trans.position.x;
            centerZ += trans.position.z;
        }
        xList.Sort();
        zList.Sort();

        centerX /= 4f;
        centerZ /= 4f;
        xList[0] -= CropsScale;
        zList[0] -= CropsScale;
        xList[poleCount] += CropsScale;
        zList[poleCount] += CropsScale;
        var direction = Vector3.Normalize(poleTrans[3].position - poleTrans[0].position);
        var rotationY = Mathf.Acos(Vector3.Dot(Vector3.right, direction)) * 180 / Mathf.PI;

        int odd = 0;
        var CropsTrans = new List<Transform>();
        for (float x = xList[0]; x <= xList[poleCount]; x += CropsScale)
        {
            var tmpList = new List<Transform>();
            for (float z = zList[0]; z <= zList[poleCount]; z += CropsScale)
            {
                var pos = Quaternion.Euler(0, rotationY, 0) * new Vector3(x - centerX, 0, z - centerZ);
                int crossX = 0, crossZ = 0;
                bool isInside = true;
                pos.x += centerX; 
                pos.z += centerZ;

                for (int i = 0; i < poleCount + 1; i++)
                {
                    int j = (i + 1) % (poleCount + 1);
                    var p1 = poleTrans[i].position;
                    var p2 = poleTrans[j].position;

                    if (CalculateEquation(p1.z, p1.x, p2.z, p2.x, pos.z, pos.x, ref crossX))
                    {
                        isInside = false;
                        break;
                    }
                    if (CalculateEquation(p1.x, p1.z, p2.x, p2.z, pos.x, pos.z, ref crossZ))
                    {
                        isInside = false;
                        break;
                    }
                }

                if (isInside && (crossX % 2).Equals(1) && (crossZ % 2).Equals(1))
                {
                    var crops = Instantiate(CropsDatas[0].Visual, pos, Quaternion.identity);
                    crops.SetParent(preCropsEntity.transform);
                    crops.gameObject.SetActive(false);
                    tmpList.Add(crops);
                }
            }

            if ((odd % 2).Equals(1)) tmpList.Reverse();
            CropsTrans.AddRange(tmpList);
            ++odd;
        }

        /* Test Code */
        var playerAI = Instantiate(TestPlayerAI, new Vector3(2, 0, 2), Quaternion.identity);
        preCropsEntity.Initialize(CropsDatas[0], CropsTrans);
        preCropsEntity.SetWorking(playerAI, true);
        preCropsEntity.enabled = true;
        Debug.Log(CropsTrans.Count);
    }

    private void ClearFense()
    {
        poleTrans.Clear();
        poleCount = 0;
        isEnable = true;
        isCollid = false;
    }

    private bool CalculateEquation(float p1X, float p1Z, float p2X, float p2Z, float x, float z, ref int cross)
    {
        if((p1X > x) ^ (p2X > x))
        {
            var collid = (p1Z - p2Z) * (x - p1X) / (p1X - p2X) + p1Z;
            var minus = collid - CropsAlpha;
            var plus = collid + CropsAlpha;
            if (minus <= z && z <= plus)
                return true;
            if(z < collid)
                cross++;
        }
        return false;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("FensePole") && !isCollid)
        {
            isCollid = true;
            FensePoleVisual.SetParent(null);
            FensePoleVisual.position = other.transform.position + Vector3.up;

            var entity = other.transform.parent.GetComponent<CropsEntity>();
            if (entity && entity.enabled && poleCount.Equals(0))
            {
                lenLimit = LengthLimit - (entity.CropsCount * 4.84f);
                collidCropsEntity = entity;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("FensePole"))
        {
            isCollid = false;
            FensePoleVisual.SetParent(transform);
            FensePoleVisual.localPosition = Vector3.up;

            if(poleCount.Equals(0))
            {
                lenLimit = LengthLimit;
                collidCropsEntity = null;
            }
        }
    }
}

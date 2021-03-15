using System.Collections.Generic;
using UnityEngine;

public class FenseGenerator : MonoBehaviour
{
    [SerializeField] private float DefaultLengthLimit = 0f;
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
    [SerializeField] private ObjectCollider FenseCollider = null;

    private int poleCount = 0;
    private int connectCount = 0;
    private float lengthLimit = 0f;
    private bool isEnable = true;
    private bool isConnect = false;
    private Vector3 colliderScale = Vector3.zero;

    private Camera mainCamera = null;
    private CropsEntity preCropsEntity = null;
    private List<Transform> poleTrans = null;

    private void Start()
    {
        mainCamera = Camera.main;
        poleTrans = new List<Transform>();
        VisualMaterial.SetColor("_BaseColor", EnableColor);
        colliderScale = FenseCollider.transform.localScale;
        DefaultLengthLimit = DefaultLengthLimit * DefaultLengthLimit;
        lengthLimit = DefaultLengthLimit;
    }

    // Update is called once per frame
    private void Update()
    {
        var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if(Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, 1000f, RayMask))
        {
            transform.position = hit.point;
            var flag = isConnect || FenseCollider.IsUnCollid;
            if (poleCount > 0)
            {
                flag &= IsBelowLimit(poleTrans[poleCount - 1].position, hit.point);
                if (poleCount.Equals(3))
                {
                    float angle = 0;
                    angle += CalculateAngle(poleTrans[0].position, poleTrans[1].position, poleTrans[2].position);
                    angle += CalculateAngle(poleTrans[1].position, poleTrans[2].position, hit.point);
                    angle += CalculateAngle(poleTrans[2].position, hit.point, poleTrans[0].position);
                    angle += CalculateAngle(hit.point, poleTrans[0].position, poleTrans[1].position);
                    flag &= IsBelowLimit(hit.point, poleTrans[0].position) && angle > 350;
                    if (isConnect && (connectCount % 2).Equals(0))
                        flag = false;
                }
                FenseCollider.transform.position = Vector3.Lerp(poleTrans[poleCount - 1].position, hit.point, .05f);
                colliderScale.z = Vector3.Distance(FenseCollider.transform.position, hit.point) * 9.5f;
                FenseCollider.transform.localScale = colliderScale;
                FenseCollider.transform.LookAt(hit.point);
            }
            if (isEnable != flag)
            {
                isEnable = flag;
                if (flag)
                    VisualMaterial.SetColor("_BaseColor", EnableColor);
                else
                    VisualMaterial.SetColor("_BaseColor", DisableColor);
            }

            if (Input.GetMouseButtonDown(0) && isEnable)
            {
                var position = new Vector3(FensePoleVisual.position.x, hit.point.y, FensePoleVisual.position.z);
                var pole = Instantiate(FensePole, position, Quaternion.identity);
                poleTrans.Add(pole);
                switch (poleCount)
                {
                    case 0:
                        ++poleCount;
                        if (isConnect)
                            ++connectCount;
                        else
                            preCropsEntity = Instantiate(CropsParent, position, Quaternion.identity);
                        FenseCollider.transform.parent = null;
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
        return (prevPos - nextPos).sqrMagnitude < lengthLimit;
    }

    private void GenerateFense(Transform start, Transform end)
    {
        var distance = Vector3.Distance(start.position, end.position);
        var rateScale = 1f / distance;
        for (float rate = rateScale; rate < 1; rate += rateScale)
        {
            var position = Vector3.Lerp(start.position, end.position, rate);
            var fense = Instantiate(Fense, position, Quaternion.identity);
            var scale = fense.localScale;
            fense.localScale = scale;
            fense.LookAt(end);

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

        var verts = new Vector3[8];
        var tris = new int[] { 0, 1, 5, 0, 4, 5, 1, 2, 6, 1, 5, 6, 2, 3, 7, 2, 6, 7,
                               3, 0, 4, 3, 4, 7, 4, 5, 6, 4, 7, 6, 0, 1, 2, 0, 3, 2};
        for (int i = 0; i < 4; i++)
        {
            verts[i] = poleTrans[i].position - poleTrans[0].position;
            verts[i + 4] = poleTrans[i].position - poleTrans[0].position + Vector3.up;
        }
        var mesh = new Mesh { vertices = verts, triangles = tris };
        var meshCollider = preCropsEntity.GetComponent<MeshCollider>();

        /* 최적화 필요 */
        if (isConnect)
        {
            var position = preCropsEntity.transform.position;
            preCropsEntity.transform.position = Vector3.zero;
            preCropsEntity.AddCrops(CropsTrans);
            var combineMesh = new Mesh();
            var combine = new CombineInstance[2];
            combine[0].mesh = mesh;
            combine[0].transform = poleTrans[0].transform.localToWorldMatrix;
            combine[1].mesh = meshCollider.sharedMesh;
            combine[1].transform = meshCollider.transform.localToWorldMatrix;
            combineMesh.CombineMeshes(combine);
            meshCollider.sharedMesh = combineMesh;
            preCropsEntity.transform.position = position;
        }
        else
        {
            var playerAI = Instantiate(TestPlayerAI, new Vector3(2, 0, 2), Quaternion.identity);
            preCropsEntity.Initialize(CropsDatas[0], CropsTrans);
            preCropsEntity.SetWorking(playerAI, true);
            preCropsEntity.enabled = true;
            meshCollider.sharedMesh = mesh;
        }
    }

    private void ClearFense()
    {
        lengthLimit = DefaultLengthLimit;
        colliderScale.z = colliderScale.x;
        FenseCollider.transform.parent = transform;
        FenseCollider.transform.localScale = colliderScale;
        FenseCollider.transform.localPosition = Vector3.zero;
        poleTrans.Clear();
        poleCount = 0;
        isEnable = true;
        isConnect = false;
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

    private float CalculateAngle(Vector3 from, Vector3 center, Vector3 to)
    {
        return Vector3.Angle(from - center, to - center);
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("FensePole"))
        {
            isConnect = true;
            FensePoleVisual.SetParent(null);
            FensePoleVisual.position = other.transform.position + Vector3.up;

            var entity = other.transform.parent.GetComponent<CropsEntity>();
            if (entity && entity.enabled && poleCount.Equals(0))
            {
                preCropsEntity = entity;
                lengthLimit = DefaultLengthLimit - (entity.CropsCount * 4.84f);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("FensePole"))
        {
            isConnect = false;
            FensePoleVisual.SetParent(transform);
            FensePoleVisual.localPosition = Vector3.up;

            if (poleCount.Equals(0))
            {
                preCropsEntity = null;
                lengthLimit = DefaultLengthLimit;
            }
        }
    }
}

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
    [SerializeField] private TerrainGenerator Generator = null;

    private int poleCount = 0;
    private int connectCount = 0;
    private float lengthLimit = 0f;
    private bool isEnable = true;
    private bool isConnect = false;
    private Vector3 colliderScale = Vector3.zero;

    private Camera mainCamera = null;
    private CropsEntity preCropsEntity = null;
    private List<Transform> poleTrans = null;

    #region Unity Functions
    private void Start()
    {
        mainCamera = Camera.main;
        poleTrans = new List<Transform>();
        VisualMaterial.SetColor("_BaseColor", EnableColor);
        colliderScale = FenseCollider.transform.localScale;
        DefaultLengthLimit = DefaultLengthLimit * DefaultLengthLimit;
        lengthLimit = DefaultLengthLimit;
    }

    private void OnDestroy()
    {
        VisualMaterial.SetColor("_BaseColor", EnableColor);
    }

    // Update is called once per frame
    private void Update()
    {
        var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if(Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, 1000f, RayMask))
        {
            Vector3 fixedPosition = new Vector3((int)hit.point.x, hit.point.y, (int)hit.point.z);
            transform.position = fixedPosition;
            var flag = FenseCollider.IsUnCollid;
            if (poleCount > 0)
            {
                flag &= IsBelowLimit(poleTrans[poleCount - 1].position, fixedPosition);
                if (poleCount.Equals(3))
                {
                    float angle = 0;
                    angle += CalculateAngle(poleTrans[0].position, poleTrans[1].position, poleTrans[2].position);
                    angle += CalculateAngle(poleTrans[1].position, poleTrans[2].position, fixedPosition);
                    angle += CalculateAngle(poleTrans[2].position, fixedPosition, poleTrans[0].position);
                    angle += CalculateAngle(fixedPosition, poleTrans[0].position, poleTrans[1].position);
                    flag &= IsBelowLimit(fixedPosition, poleTrans[0].position) && angle > 350;
                }
                FenseCollider.transform.position = Vector3.Lerp(poleTrans[poleCount - 1].position, fixedPosition, .05f);
                colliderScale.z = Vector3.Distance(FenseCollider.transform.position, fixedPosition) * 9.5f;
                FenseCollider.transform.localScale = colliderScale;
                FenseCollider.transform.LookAt(fixedPosition);
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
                        if (!isConnect)
                            preCropsEntity = Instantiate(CropsParent, position, Quaternion.identity);
                        pole.SetParent(preCropsEntity.transform);
                        FenseCollider.transform.parent = null;
                        break;
                    case 1:
                    case 2:
                        pole.SetParent(preCropsEntity.transform);
                        GenerateFense(poleTrans[poleCount - 1], poleTrans[poleCount]);
                        ++poleCount;
                        break;
                    case 3:
                        pole.SetParent(preCropsEntity.transform);
                        GenerateFense(poleTrans[poleCount - 1], poleTrans[poleCount]);
                        GenerateFense(poleTrans[poleCount], poleTrans[0]);
                        GenerateCropsEntity();
                        ClearFense();
                        break;
                }
            }

            if(Input.GetKeyDown(KeyCode.R) && preCropsEntity != null)
            {
                Destroy(preCropsEntity.gameObject);
                ClearFense();
            }
        }
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
#endregion

    private void GenerateFense(Transform start, Transform end)
    {
        var length = Vector3.Distance(start.position, end.position);
        var count = (int)(length / 2) / 2 * 2;

        var direction = (end.position - start.position).normalized;
        for (int i = 1; i <= count; i++)
        {
            var position = start.position + direction * i * 2;
            var fense = Instantiate(Fense, position, Quaternion.identity);
            var scale = fense.localScale;
            fense.localScale = scale;
            fense.LookAt(end);
            fense.SetParent(preCropsEntity.transform);
        }
    }

    private void GenerateCropsEntity()
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
        var cropsTrans = new List<Transform>();
        for (float x = xList[0]; x <= xList[poleCount]; x += CropsScale)
        {
            var tmpList = new List<Transform>();
            for (float z = zList[0]; z <= zList[poleCount]; z += CropsScale)
            {
                var position = Quaternion.Euler(0, rotationY, 0) * new Vector3(x - centerX, 0, z - centerZ);
                var fixedPosition = new Vector3Int((int)x, 0, (int)z);
                position.x += centerX;
                position.z += centerZ;
                int cropsX = 0, cropsZ = 0;
                int dirtX = 0;
                bool inCrops = true;
                bool inDirt = true;

                for (int i = 0; i < poleCount + 1; i++)
                {
                    int j = (i + 1) % (poleCount + 1);
                    var p1 = poleTrans[i].position;
                    var p2 = poleTrans[j].position;

                    if (CalculateEquation(p1.z, p1.x, p2.z, p2.x, position.z, position.x, ref cropsX, CropsAlpha))
                        inCrops = false;
                    if (CalculateEquation(p1.x, p1.z, p2.x, p2.z, position.x, position.z, ref cropsZ, CropsAlpha))
                        inCrops = false;
                    if (CalculateEquation(p1.z, p1.x, p2.z, p2.x, fixedPosition.z, fixedPosition.x, ref dirtX, 0))
                        inDirt = false;
                }

                if (inDirt && (dirtX % 2).Equals(1))
                    Generator.SetTerrainLayer(fixedPosition.x, fixedPosition.z, 1);

                if (inCrops && (cropsX % 2).Equals(1) && (cropsZ % 2).Equals(1) &&
                    (x % 2).Equals(0) && (z % 2).Equals(0))
                {
                    var crops = Instantiate(CropsDatas[0].Visual, position, Quaternion.identity);
                    crops.SetParent(preCropsEntity.transform);
                    crops.gameObject.SetActive(false);
                    tmpList.Add(crops);
                }
            }

            if ((x % 2).Equals(0))
            {
                if ((odd % 2).Equals(1)) tmpList.Reverse();
                cropsTrans.AddRange(tmpList);
                ++odd;
            }
        }

        Generator.ApplyTerrainLayers(0, 0);

        var verts = new Vector3[8];
        var tris = new int[] { 0, 1, 5, 0, 4, 5, 1, 2, 6, 1, 5, 6, 2, 3, 7, 2, 6, 7,
                               3, 0, 4, 3, 4, 7, 4, 5, 6, 4, 7, 6, 0, 1, 2, 0, 3, 2};
        verts[0] = (poleTrans[0].position - poleTrans[0].position) + (poleTrans[2].position - poleTrans[0].position).normalized * .5f;
        verts[1] = (poleTrans[1].position - poleTrans[0].position) + (poleTrans[3].position - poleTrans[1].position).normalized * .5f;
        verts[2] = (poleTrans[2].position - poleTrans[0].position) + (poleTrans[0].position - poleTrans[2].position).normalized * .5f;
        verts[3] = (poleTrans[3].position - poleTrans[0].position) + (poleTrans[1].position - poleTrans[3].position).normalized * .5f;
        for (int i = 0; i < 4; i++)
            verts[i + 4] = verts[i] + Vector3.up;
        var mesh = new Mesh { vertices = verts, triangles = tris };
        var meshCollider = preCropsEntity.GetComponent<MeshCollider>();

        /* 최적화 필요 */
        if (preCropsEntity.CropsCount > 0)
        {
            var position = preCropsEntity.transform.position;
            preCropsEntity.transform.position = Vector3.zero;
            preCropsEntity.AddCrops(cropsTrans);
            var combineMesh = new Mesh();
            var combine = new CombineInstance[2];
            combine[0].mesh = mesh;
            combine[1].mesh = meshCollider.sharedMesh;
            combine[0].transform = poleTrans[0].transform.localToWorldMatrix;
            combine[1].transform = meshCollider.transform.localToWorldMatrix;
            combineMesh.CombineMeshes(combine);
            meshCollider.sharedMesh = combineMesh;
            preCropsEntity.transform.position = position;
        }
        else
        {
            var playerAI = Instantiate(TestPlayerAI, new Vector3(2, 0, 2), Quaternion.identity);
            preCropsEntity.Initialize(CropsDatas[0], cropsTrans);
            preCropsEntity.SetWorking(playerAI, true);
            preCropsEntity.enabled = true;
            meshCollider.sharedMesh = mesh;
        }
    }

    private void ClearFense()
    {
        lengthLimit = DefaultLengthLimit;
        colliderScale.z = colliderScale.x;
        FenseCollider.transform.parent = FensePoleVisual;
        FenseCollider.transform.localScale = colliderScale;
        FenseCollider.transform.localPosition = new Vector3(0, -.5f, 0);
        FenseCollider.transform.rotation = Quaternion.identity;
        poleTrans.Clear();
        poleCount = 0;
        connectCount = 0;
        isEnable = true;
        isConnect = false;
        preCropsEntity = null;
    }

    private bool CalculateEquation(float p1X, float p1Z, float p2X, float p2Z, float x, float z, ref int cross, float alpha)
    {
        if((p1X > x) ^ (p2X > x))
        {
            var collid = (p1Z - p2Z) * (x - p1X) / (p1X - p2X) + p1Z;
            var minus = collid - alpha;
            var plus = collid + alpha;
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

    private bool IsBelowLimit(Vector3 prevPos, Vector3 nextPos)
    {
        return (prevPos - nextPos).sqrMagnitude < lengthLimit;
    }
}

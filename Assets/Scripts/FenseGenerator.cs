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
    [SerializeField] private ObjectCollider[] FenseCollider = null;
    [SerializeField] private TerrainGenerator Generator = null;

    private int poleCount = 0;
    private float lengthLimit = 0f;
    private bool isEnable = true;
    private bool isCropConnect = false;
    private bool isPoleConnect = false;
    private Vector3 colliderScale = Vector3.zero;

    private Camera mainCamera = null;
    private Transform firstFensePole = null;
    private CropsEntity preCropsEntity = null;
    private List<Vector3> polePosition = null;

    #region Unity Functions
    private void Start()
    {
        mainCamera = Camera.main;
        polePosition = new List<Vector3> ();
        VisualMaterial.SetColor("_BaseColor", EnableColor);
        colliderScale = FenseCollider[0].transform.localScale;
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

            var flag = FenseCollider[0].IsUnCollid;
            switch(poleCount)
            {
                case 0:
                    flag |= isPoleConnect;
                    break;
                case 1:
                case 2:
                    MoveFenseCollider(0, polePosition[poleCount - 1], fixedPosition);
                    flag &= !isPoleConnect && IsBelowLimit(polePosition[poleCount - 1], fixedPosition);
                    break;
                case 3:
                    MoveFenseCollider(0, polePosition[poleCount - 1], fixedPosition);
                    MoveFenseCollider(1, polePosition[0], fixedPosition);

                    if(isCropConnect)
                        flag &= isPoleConnect && IsBelowLimit(fixedPosition, polePosition[0]) && IsSquareForm(fixedPosition) && FenseCollider[1].IsUnCollid;
                    else
                        flag &= !isPoleConnect && IsBelowLimit(fixedPosition, polePosition[0]) && IsSquareForm(fixedPosition) && FenseCollider[1].IsUnCollid;
                    break;
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
                polePosition.Add(position);
                switch (poleCount)
                {
                    case 0:
                        ++poleCount;
                        if (!isPoleConnect)
                            preCropsEntity = Instantiate(CropsParent, position, Quaternion.identity);
                        else
                            isCropConnect = true;
                        firstFensePole = pole;
                        pole.SetParent(preCropsEntity.transform);
                        FenseCollider[0].transform.parent = null;
                        break;
                    case 1:
                        pole.SetParent(preCropsEntity.transform);
                        GenerateFense(polePosition[poleCount - 1], polePosition[poleCount]);
                        ++poleCount;
                        break;
                    case 2:
                        pole.SetParent(preCropsEntity.transform);
                        GenerateFense(polePosition[poleCount - 1], polePosition[poleCount]);
                        FenseCollider[1].transform.parent = null;
                        ++poleCount;
                        break;
                    case 3:
                        pole.SetParent(preCropsEntity.transform);
                        GenerateFense(polePosition[poleCount - 1], polePosition[poleCount]);
                        GenerateFense(polePosition[poleCount], polePosition[0]);
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
            isPoleConnect = true;
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
            isPoleConnect = false;
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

    private void GenerateFense(Vector3 start, Vector3 end)
    {
        var length = Vector3.Distance(start, end);
        var rate = 1f / length;
        var scale = Vector3.Distance(start, Vector3.Lerp(start, end, rate));

        for (float i = rate; i < .95f; i += rate)
        {
            var position = Vector3.Lerp(start, end,i);
            var fense = Instantiate(Fense, position, Quaternion.identity);
            fense.SetParent(preCropsEntity.transform);
            fense.localScale *= scale;
            fense.LookAt(end);
        }
    }

    private void GenerateCropsEntity()
    {
        float centerX = 0f, centerZ = 0f;
        List<float> xList = new List<float>();
        List<float> zList = new List<float>();
        foreach (var position in polePosition)
        {
            xList.Add(position.x);
            zList.Add(position.z);
            centerX += position.x;
            centerZ += position.z;
        }
        xList.Sort();
        zList.Sort();

        centerX /= 4f;
        centerZ /= 4f;
        xList[0] -= CropsScale;
        zList[0] -= CropsScale;
        xList[poleCount] += CropsScale;
        zList[poleCount] += CropsScale;
        var direction = Vector3.Normalize(polePosition[3] - polePosition[0]);
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
                    Vector3 rP = Quaternion.Euler(0, 180, 0) * -position;
                    Vector3 rfP = Quaternion.Euler(0, 180, 0) * -fixedPosition;
                    Vector3 rP1 = Quaternion.Euler(0, 180, 0) * -polePosition[i];
                    Vector3 rP2 = Quaternion.Euler(0, 180, 0) * -polePosition[j];

                    if (CalculateEquation(rP1, rP2, rfP, ref dirtX, 0))
                        inDirt = false;
                    if (CalculateEquation(rP1, rP2, rP, ref cropsX, CropsAlpha))
                        inCrops = false;
                    if (CalculateEquation(polePosition[i], polePosition[j], position, ref cropsZ, CropsAlpha))
                        inCrops = false;
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
        var tris = new int[] { 0, 1, 5,  0, 4, 5,  1, 2, 6,  1, 5, 6,  2, 3, 7,  2, 6, 7,
                               3, 0, 4,  3, 4, 7,  4, 5, 6,  4, 7, 6,  0, 1, 2,  0, 3, 2};
        for (int i = 0; i < 4; i++)
        {
            verts[i] = Vector3.Lerp(polePosition[i], polePosition[(i + 2)%4], .05f) - polePosition[0];
            verts[i + 4] = verts[i] + Vector3.up;
        }    
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
            combine[0].transform = firstFensePole.localToWorldMatrix;
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
        polePosition.Clear();
        poleCount = 0;

        isEnable = true;
        isCropConnect = false;
        isPoleConnect = false;
        preCropsEntity = null;

        FenseCollider[0].ClearCollider(FensePoleVisual);
        FenseCollider[1].ClearCollider(FensePoleVisual);
    }

    private void MoveFenseCollider(int index, Vector3 prevPosition, Vector3 nextPosition)
    {
        FenseCollider[index].transform.position = Vector3.Lerp(prevPosition, nextPosition, .05f);
        colliderScale.z = Vector3.Distance(FenseCollider[index].transform.position, nextPosition) * 9.5f;
        FenseCollider[index].transform.localScale = colliderScale;
        FenseCollider[index].transform.LookAt(nextPosition);
    }

    private bool CalculateEquation(Vector3 p1, Vector3 p2, Vector3 p, ref int cross, float alpha)
    {
        if((p1.x > p.x) ^ (p2.x > p.x))
        {
            var collid = (p1.z - p2.z) * (p.x - p1.x) / (p1.x - p2.x) + p1.z;
            var minus = collid - alpha;
            var plus = collid + alpha;
            if (minus <= p.z && p.z <= plus)
                return true;
            if(p.z < collid)
                cross++;
        }
        return false;
    }

    private bool IsSquareForm(Vector3 lastPosition)
    {
        float angle = Vector3.Angle(polePosition[0] - polePosition[1], polePosition[2] - polePosition[1]);
        angle += Vector3.Angle(polePosition[1] - polePosition[2], lastPosition - polePosition[2]);
        angle += Vector3.Angle(polePosition[2] - lastPosition, polePosition[0] - lastPosition);
        angle += Vector3.Angle(lastPosition - polePosition[0], polePosition[1] - polePosition[0]);
        return 350 < angle;
    }

    private bool IsBelowLimit(Vector3 prevPos, Vector3 nextPos)
    {
        return (prevPos - nextPos).sqrMagnitude < lengthLimit;
    }
}

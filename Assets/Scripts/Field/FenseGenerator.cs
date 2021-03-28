using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GPUInstancer;

public class FenseGenerator : MonoBehaviour
{
    [Header("GPUInstancecr")]
    [SerializeField] private GPUInstancerPrefabManager prefabManager = null;
    [Header("Crops Settings")]
    [SerializeField] private float CropsAlpha = 1f;
    [SerializeField] private CropsData[] CropsDatas = null;
    [SerializeField] private CropsEntity CropsParent = null;
    [Header("Fense Settings")]
    /* width = 22, height = 22 -> FenseLimit = 22 * 22 = 484 */
    [SerializeField] private float FenseLimit = 0f;
    [SerializeField] private LayerMask RayMask = default;
    [SerializeField] private Transform FensePole = null;
    [SerializeField] private GPUInstancerPrefab FenseSide = null;
    [SerializeField] private GPUInstancerPrefab FenseMain = null;
    [SerializeField] private ObjectCollider[] FenseCollider = null;
    [Header("Visual Settings")]
    [SerializeField] private Transform GeneratorVisual = null;
    [SerializeField] private Material VisualMaterial = null;
    [SerializeField] private Color EnableColor = Color.black;
    [SerializeField] private Color DisableColor = Color.black;
    [SerializeField] private TerrainGenerator Generator = null;
    [Header("CropsEntity Settings")]
    [SerializeField] private float[] BuildPeriod = null;
    [SerializeField] private TestAI TestPlayerAI = null;

    private bool[,] cropsDetail = null;
    private float[,] cropsLayer = null;

    private int poleCount = 0;
    private int connectHash = 0;
    private bool isEnable = true;
    private bool isBuilding = false;
    private bool isPoleConnect = false;
    private Vector3 colliderScale = Vector3.zero;
    private Vector2Int startPosition = Vector2Int.zero;

    private Camera mainCamera = null;
    private CropsEntity createCrops = null;
    private CropsEntity expendCrops = null;
    private List<Transform> poleTransform = null;

    public int PoleCount { get => poleCount; }

    #region Unity Functions
    private void Awake()
    {
        poleTransform = new List<Transform>();
        gameObject.SetActive(false);
        mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        colliderScale = FenseCollider[0].transform.localScale;
        VisualMaterial.SetColor("_BaseColor", EnableColor);
        poleTransform.Clear();
    }

    private void OnDestroy()
    {
        VisualMaterial.SetColor("_BaseColor", EnableColor);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("FensePole"))
        {
            if (connectHash != other.transform.parent.GetHashCode())
                return;

            isPoleConnect = true;
            GeneratorVisual.SetParent(null);
            GeneratorVisual.position = other.transform.position;

            var entity = other.transform.parent.GetComponent<CropsEntity>();
            if (entity && entity.enabled && poleCount.Equals(0))
                createCrops = entity;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("FensePole"))
        {
            isPoleConnect = false;
            GeneratorVisual.SetParent(transform);
            GeneratorVisual.localPosition = Vector3.zero;

            if (poleCount.Equals(0))
                createCrops = null;
        }
    }
    #endregion

    #region Field Generate Functions
    public IEnumerator ConnectFieldCoroutine(int connectHash)
    {
        this.connectHash = connectHash;
        while (true)
        {
            var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, 1000f, RayMask) &&
                hit.transform.CompareTag("Terrain"))
            {
                transform.position = new Vector3((int)hit.point.x, hit.point.y, (int)hit.point.z);

                var flag = FenseCollider[0].IsUnCollid;
                switch (poleCount)
                {
                    case 0:
                        flag &= isPoleConnect;
                        break;
                    case 1:
                    case 2:
                        MoveFenseCollider(0, poleTransform[poleCount - 1].position, GeneratorVisual.position);
                        flag &= !isPoleConnect && IsBelowLimit(poleTransform[poleCount - 1].position, GeneratorVisual.position);
                        break;
                    case 3:
                        MoveFenseCollider(0, poleTransform[poleCount - 1].position, GeneratorVisual.position);
                        MoveFenseCollider(1, poleTransform[0].position, GeneratorVisual.position);
                        flag &= isPoleConnect && FenseCollider[1].IsUnCollid && IsSquareForm(GeneratorVisual.position) &&
                                IsBelowLimit(poleTransform[poleCount - 1].position, GeneratorVisual.position);
                        break;
                    default:
                        flag = false;
                        break;
                }
                ChangeVisualColor(flag);

                if (Input.GetMouseButtonDown(0) && isEnable)
                {
                    var position = new Vector3(GeneratorVisual.position.x, hit.point.y, GeneratorVisual.position.z);
                    var pole = Instantiate(FensePole, position, Quaternion.identity);
                    poleTransform.Add(pole);
                    switch (poleCount)
                    {
                        case 0:
                            expendCrops = Instantiate(CropsParent, position, Quaternion.identity);
                            FenseCollider[0].transform.parent = null;
                            FenseCollider[0].transform.position = poleTransform[poleCount].position;
                            break;
                        case 1:
                            FenseCollider[0].transform.position = poleTransform[poleCount].position;
                            break;
                        case 2:
                            FenseCollider[1].transform.parent = null;
                            FenseCollider[1].transform.position = poleTransform[0].position;
                            FenseCollider[0].transform.position = poleTransform[poleCount].position;
                            break;
                        case 3:
                            GenerateCollider(expendCrops);
                            GenerateCrops(true);
                            UIManager.Instance.ChangeCropsCount(true, createCrops.CropsCount);
                            break;
                    }
                    ++poleCount;
                    pole.SetParent(expendCrops.transform);
                }

                if (Input.GetKeyDown(KeyCode.R))
                {
                    DestroyPrevFense(true);
                }
            }
            yield return null;
        }
    }

    public IEnumerator UnConnectFieldCoroutine()
    {
        while (true)
        {
            var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, 1000f, RayMask) &&
                hit.transform.CompareTag("Terrain"))
            {
                transform.position = new Vector3((int)hit.point.x, hit.point.y, (int)hit.point.z);

                bool flag = !isPoleConnect && FenseCollider[0].IsUnCollid;
                if (poleCount > 3)
                    flag = false;
                else if (poleCount > 0)
                {
                    MoveFenseCollider(0, poleTransform[poleCount - 1].position, GeneratorVisual.position);
                    flag &= IsBelowLimit(poleTransform[poleCount - 1].position, GeneratorVisual.position);
                    if (poleCount.Equals(3))
                    {
                        MoveFenseCollider(1, poleTransform[0].position, GeneratorVisual.position);
                        flag &= FenseCollider[1].IsUnCollid && IsSquareForm(GeneratorVisual.position) && IsBelowLimit(poleTransform[0].position, GeneratorVisual.position);
                    }
                }
                ChangeVisualColor(flag);

                if (Input.GetMouseButtonDown(0) && isEnable)
                {
                    var position = new Vector3(GeneratorVisual.position.x, hit.point.y, GeneratorVisual.position.z);
                    var pole = Instantiate(FensePole, position, Quaternion.identity);
                    poleTransform.Add(pole);
                    switch (poleCount)
                    {
                        case 0:
                            createCrops = Instantiate(CropsParent, position, Quaternion.identity);
                            FenseCollider[0].transform.parent = null;
                            FenseCollider[0].transform.position = poleTransform[poleCount].position;
                            break;
                        case 1:
                            FenseCollider[0].transform.position = poleTransform[poleCount].position;
                            break;
                        case 2:
                            FenseCollider[1].transform.parent = null;
                            FenseCollider[1].transform.position = poleTransform[0].position;
                            FenseCollider[0].transform.position = poleTransform[poleCount].position;
                            break;
                        case 3:
                            GenerateCollider(createCrops);
                            GenerateCrops(false);
                            UIManager.Instance.ChangeCropsCount(false, createCrops.CropsCount);
                            break;
                    }
                    ++poleCount;
                    pole.SetParent(createCrops.transform);
                }

                if(Input.GetKeyDown(KeyCode.R))
                {
                    DestroyPrevFense(false);
                }
            }
            yield return null;
        }
    }

    public IEnumerator GenerateField(bool isExpension)
    {
        isBuilding = true;
        int timeI = 0;
        float timer = 0;
        float limitTimer = BuildPeriod[0];

        while (true)
        {
            timer += Time.deltaTime;
            if(timer > limitTimer)
            {
                switch(timeI)
                {
                    case 0:
                        GenerateFense(poleTransform[0].position, poleTransform[1].position);
                        GenerateFense(poleTransform[1].position, poleTransform[2].position);
                        GenerateFense(poleTransform[2].position, poleTransform[3].position);
                        if (!isExpension)
                            GenerateFense(poleTransform[3].position, poleTransform[0].position);
                        else
                            MergeCrops();
                        for (int i = 0; i < 4; i++)
                            Destroy(poleTransform[i].GetChild(0).gameObject);
                        break;
                    case 1:
                        Generator.ApplyTerrainLayers(startPosition.x, startPosition.y, 1, ref cropsLayer);
                        //Generator.RemoveTerrainDetail(startPosition.x, startPosition.y, ref cropsDetail);

                        if (isExpension)
                        {
                            var children = new List<Transform>();
                            foreach (Transform child in expendCrops.transform)
                                children.Add(child);

                            foreach (Transform child in children)
                                child.parent = createCrops.transform;
                            createCrops.AddCrops(expendCrops);
                            Destroy(expendCrops.gameObject);
                        }
                        else
                            createCrops.Initialize(CropsDatas[0]);
                        break;
                }

                timeI++;
                if (BuildPeriod.Length > timeI)
                {
                    timer = 0;
                    limitTimer = BuildPeriod[timeI];
                }
                else
                {
                    isBuilding = false;
                    ClearFense();
                    break;
                }
            }
            yield return null;
        }
    }

    public IEnumerator DestroyField()
    {
        yield return null;
    }

    private void GenerateCrops(bool isExpension)
    {
        float centerX = 0f, centerZ = 0f;
        List<Vector3> dirtPosition = new List<Vector3>();
        for (int i = 0; i < 4; i++)
        {
            centerX += poleTransform[i].position.x;
            centerZ += poleTransform[i].position.z;
            dirtPosition.Add((poleTransform[i].position - poleTransform[(i + 2) % 4].position).normalized + poleTransform[i].position + new Vector3(.5f, 0, .5f));
        }
        centerX /= 4f;
        centerZ /= 4f;

        int odd = 0;
        int sx = (int)(centerX - 12);
        int sz = (int)(centerZ - 12);
        int ex = (int)(centerX + 12);
        int ez = (int)(centerZ + 12);
        var direction = (poleTransform[3].position - poleTransform[0].position).normalized;
        var rotationY = Mathf.Acos(Vector3.Dot(direction, Vector3.right)) * 180 / Mathf.PI;
        var cropsFieldList = new List<Transform>();

        cropsDetail = new bool[ez - sz + 1, ex - sx + 1];
        cropsLayer = new float[ez - sz + 1, ex - sx + 1];
        for (int x = sx; x <= ex; x++)
        {
            var cropsLineList = new List<Transform>();
            for (int z = sz; z <= ez; z++)
            {
                var position = Quaternion.Euler(0, rotationY, 0) * new Vector3(x - centerX, 0, z - centerZ);
                var fixedPosition = new Vector3Int(x, 0, z);
                int cropsX = 0, cropsZ = 0, dirtX = 0;
                bool inCrops = true, inDirt = true;
                position.x += centerX;
                position.z += centerZ;

                for (int i = 0; i < 4; i++)
                {
                    int j = (i + 1) % 4;
                    Vector3 p1 = poleTransform[i].position;
                    Vector3 p2 = poleTransform[j].position;
                    Vector3 rP1 = new Vector3(p1.z, 0, p1.x);
                    Vector3 rP2 = new Vector3(p2.z, 0, p2.x);
                    Vector3 rP = new Vector3(position.z, 0, position.x);

                    if (InBelowLine(dirtPosition[i], dirtPosition[j], fixedPosition, ref dirtX, 0))
                        inDirt = false;
                    if (InBelowLine(rP1, rP2, rP, ref cropsX, CropsAlpha))
                        inCrops = false;
                    if (InBelowLine(p1, p2, position, ref cropsZ, CropsAlpha))
                        inCrops = false;
                }

                if (inDirt && (dirtX % 2).Equals(1))
                {
                    cropsLayer[fixedPosition.z - sz, fixedPosition.x - sx] = 1f;
                    cropsDetail[fixedPosition.z - sz, fixedPosition.x - sx] = true;
                }

                if (inCrops && (cropsX % 2).Equals(1) && (cropsZ % 2).Equals(1) &&
                    (x % 2).Equals(0) && (z % 2).Equals(0))
                {
                    var crops = Instantiate(CropsDatas[0].Visual, position, Quaternion.identity);
                    crops.SetParent(createCrops.transform);
                    crops.gameObject.SetActive(false);
                    cropsLineList.Add(crops);
                }
            }

            if ((x % 2).Equals(0))
            {
                ++odd;
                if ((odd % 2).Equals(1))
                    cropsLineList.Reverse();
                cropsFieldList.AddRange(cropsLineList);
            }
        }

        startPosition.x = sx - 1;
        startPosition.y = sz - 1;
        if (isExpension)
            expendCrops.AddCrops(cropsFieldList);
        else
            createCrops.AddCrops(cropsFieldList);
    }
    #endregion

    #region Fense Functions
    private void GenerateFense(Vector3 start, Vector3 end)
    {
        var direction = (end - start).normalized / 4f;
        var startPosition = start + direction;
        var endPosition = end - direction;
        var fenseEnd = Instantiate(FenseSide, endPosition, Quaternion.identity);
        fenseEnd.transform.SetParent(createCrops.transform);
        fenseEnd.transform.LookAt(start);
        var fenseStart = Instantiate(FenseSide, startPosition, Quaternion.identity);
        fenseStart.transform.SetParent(createCrops.transform);
        fenseStart.transform.LookAt(end);

        var fenseScale = FenseMain.transform.localScale;
        fenseScale.z = Vector3.Distance(endPosition, startPosition);
        var fenseMid = Instantiate(FenseMain, Vector3.Lerp(startPosition, endPosition, .5f), Quaternion.identity);
        fenseMid.transform.SetParent(createCrops.transform);
        fenseMid.transform.localScale = fenseScale;
        fenseMid.transform.LookAt(end);

        GPUInstancerAPI.AddPrefabInstance(prefabManager, fenseStart);
        GPUInstancerAPI.AddPrefabInstance(prefabManager, fenseEnd);
        GPUInstancerAPI.AddPrefabInstance(prefabManager, fenseMid);
    }

    private void GenerateCollider(CropsEntity entity)
    {
        var verts = new Vector3[8];
        var tris = new int[] { 0, 1, 2,  2, 3, 0,  7, 4, 0,  0, 3, 7,  6, 5, 4,  4, 7, 6,
                               1, 5, 6,  6, 2, 1,  4, 5, 1,  1, 0, 4,  6, 7, 3,  3, 2, 6};
        for (int i = 0; i < 4; i++)
        {
            poleTransform[i].GetComponent<Collider>().enabled = true;
            var p1 = poleTransform[i].position;
            var p2 = poleTransform[(i + 2) % 4].position;
            verts[i] = Vector3.Lerp(p1, p2, .05f) - poleTransform[0].position;
            verts[i + 4] = verts[i] + Vector3.up;
        }
        var mesh = new Mesh { vertices = verts, triangles = tris };
        var meshCollider = entity.GetComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
    }

    private void MergeCrops()
    {
        var createMesh = createCrops.GetComponent<MeshCollider>();
        var expendMesh = expendCrops.GetComponent<MeshCollider>();
        var combineMesh = new Mesh();
        var combine = new CombineInstance[2];

        var defaultPos1 = createCrops.transform.position;
        var defaultPos2 = expendCrops.transform.position;

        createCrops.transform.position = Vector3.zero;
        expendCrops.transform.position = defaultPos2 - defaultPos1;

        combine[0].mesh = createMesh.sharedMesh;
        combine[1].mesh = expendMesh.sharedMesh;
        combine[0].transform = createCrops.transform.localToWorldMatrix;
        combine[1].transform = expendCrops.transform.localToWorldMatrix;
        combineMesh.CombineMeshes(combine);
        createMesh.sharedMesh = combineMesh;

        createCrops.transform.position = defaultPos1;
        expendCrops.transform.position = defaultPos2;
    }

    public void ClearFense()
    {
        if(!isBuilding)
        {
            poleTransform.Clear();
            poleCount = 0;

            isEnable = true;
            isPoleConnect = false;
            createCrops = null;
            expendCrops = null;
            VisualMaterial.SetColor("_BaseColor", EnableColor);

            GeneratorVisual.SetParent(transform);
            GeneratorVisual.localPosition = Vector3.zero;
            FenseCollider[0].ClearCollider(GeneratorVisual);
            FenseCollider[1].ClearCollider(GeneratorVisual);
        }
    }

    public void DestroyFense(bool isExpension)
    {
        if(!isBuilding)
        {
            if (isExpension)
            {
                if(expendCrops != null)
                {
                    Destroy(expendCrops.gameObject);
                    expendCrops = null;
                }
            }
            else
            {
                if (createCrops != null)
                {
                    Destroy(createCrops.gameObject);
                    createCrops = null;
                }
            }
        }
    }

    private void DestroyPrevFense(bool isExpension)
    {
        switch(poleCount)
        {
            case 1:
                DestroyFense(isExpension);
                ClearFense();
                break;
            case 2:
            case 3:
                poleCount--;
                Destroy(poleTransform[poleCount].gameObject);
                break;
            case 4:
                if (isExpension)
                    expendCrops.GetComponent<MeshCollider>().sharedMesh = null;
                else
                    createCrops.GetComponent<MeshCollider>().sharedMesh = null;
                poleCount--;
                Destroy(poleTransform[poleCount].gameObject);
                break;
        }
    }
    #endregion

    private void MoveFenseCollider(int index, Vector3 prevPosition, Vector3 nextPosition)
    {
        colliderScale.z = Vector3.Distance(prevPosition, nextPosition);
        FenseCollider[index].transform.localScale = colliderScale;
        FenseCollider[index].transform.LookAt(nextPosition);
    }

    private bool InBelowLine(Vector3 p1, Vector3 p2, Vector3 p, ref int cross, float alpha)
    {
        if((p1.x > p.x) ^ (p2.x > p.x))
        {
            var collid = (p1.z - p2.z) * (p.x - p1.x) / (p1.x - p2.x) + p1.z;
            var minus = collid - alpha;
            var plus = collid + alpha;
            if (minus <= p.z && p.z <= plus)
                return true;
            if(p.z <= collid)
                cross++;
        }
        return false;
    }

    private bool IsBelowLimit(Vector3 prevPos, Vector3 nextPos)
    {
        return (prevPos - nextPos).sqrMagnitude < FenseLimit;
    }

    private bool IsSquareForm(Vector3 lastPosition)
    {
        float angle = Vector3.Angle(poleTransform[0].position - poleTransform[1].position, 
                                    poleTransform[2].position - poleTransform[1].position);
        angle += Vector3.Angle(poleTransform[1].position - poleTransform[2].position, 
                               lastPosition - poleTransform[2].position);
        angle += Vector3.Angle(poleTransform[2].position - lastPosition, 
                               poleTransform[0].position - lastPosition);
        angle += Vector3.Angle(lastPosition - poleTransform[0].position, 
                               poleTransform[1].position - poleTransform[0].position);
        return 350 < angle;
    }

    private void ChangeVisualColor(bool flag)
    {
        if (isEnable != flag)
        {
            isEnable = flag;
            if (flag)
                VisualMaterial.SetColor("_BaseColor", EnableColor);
            else
                VisualMaterial.SetColor("_BaseColor", DisableColor);
        }
    }
}

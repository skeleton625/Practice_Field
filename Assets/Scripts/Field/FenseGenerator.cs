using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FenseGenerator : MonoBehaviour
{
    [Header("Generator Float Settings")]
    /* width = 22, height = 22 -> DefaultLengthLimit = 22 * 22 = 484 */
    [SerializeField] private float DefaultLengthLimit = 0f;
    [SerializeField] private float CropsAlpha = 1f;
    [SerializeField] private float[] BuildPeriod = null;
    [Header("Generator Fense Settings")]
    [SerializeField] private Transform Fense = null;
    [SerializeField] private Transform FensePole = null;
    [SerializeField] private Transform FensePoleVisual = null;
    [SerializeField] private Material VisualMaterial = null;
    [SerializeField] private LayerMask RayMask = default;
    [SerializeField] private Color EnableColor = Color.black;
    [SerializeField] private Color DisableColor = Color.black;
    [Header("Generator Crops Settings")]
    [SerializeField] private CropsEntity CropsParent = null;
    [SerializeField] private CropsData[] CropsDatas = null;
    [SerializeField] private TestAI TestPlayerAI = null;

    [SerializeField] private ObjectCollider[] FenseCollider = null;
    [SerializeField] private TerrainGenerator Generator = null;

    private int poleCount = 0;
    private float lengthLimit = 0f;
    private float[,] cropsLayer = null;
    private bool isEnable = true;
    private bool isBuilding = false;
    private bool isPoleConnect = false;
    private Vector3 colliderScale = Vector3.zero;

    private Camera mainCamera = null;
    private CropsEntity createCrops = null;
    private CropsEntity expendCrops = null;
    private List<Transform> poleTransform = null;

    #region Unity Functions
    private void Awake()
    {
        poleTransform = new List<Transform>();
    }

    private void OnEnable()
    {
        if(poleTransform != null)
            poleTransform.Clear();
        mainCamera = Camera.main;
        VisualMaterial.SetColor("_BaseColor", EnableColor);
        colliderScale = FenseCollider[0].transform.localScale;
        lengthLimit = DefaultLengthLimit;
    }

    private void OnDestroy()
    {
        VisualMaterial.SetColor("_BaseColor", EnableColor);
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
                createCrops = entity;
                lengthLimit = DefaultLengthLimit - (entity.CropsCount * 4.84f);
            }
            else if (poleCount.Equals(3) && !entity.Equals(createCrops))
                isPoleConnect = false;
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
                createCrops = null;
                lengthLimit = DefaultLengthLimit;
            }
        }
    }
    #endregion

    #region Field Generate Functions
    public IEnumerator ConnectFieldCoroutine()
    {
        while (true)
        {
            var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, 1000f, RayMask) &&
                hit.transform.CompareTag("Terrain"))
            {
                Vector3 fixedPosition = new Vector3((int)hit.point.x, hit.point.y, (int)hit.point.z);
                transform.position = fixedPosition;

                var flag = FenseCollider[0].IsUnCollid;
                switch (poleCount)
                {
                    case 0:
                        flag &= isPoleConnect;
                        break;
                    case 1:
                    case 2:
                        MoveFenseCollider(0, poleTransform[poleCount - 1].position, FensePoleVisual.position);
                        flag &= !isPoleConnect && IsBelowLimit(poleTransform[poleCount - 1].position, FensePoleVisual.position);
                        break;
                    case 3:
                        MoveFenseCollider(0, poleTransform[poleCount - 1].position, FensePoleVisual.position);
                        MoveFenseCollider(1, poleTransform[0].position, FensePoleVisual.position);
                        flag &= isPoleConnect && FenseCollider[1].IsUnCollid && IsSquareForm(FensePoleVisual.position);
                        break;
                    default:
                        flag = false;
                        break;
                }
                ChangeVisualColor(flag);

                if (Input.GetMouseButtonDown(0) && isEnable)
                {
                    var position = new Vector3(FensePoleVisual.position.x, hit.point.y, FensePoleVisual.position.z);
                    var pole = Instantiate(FensePole, position, Quaternion.identity);
                    poleTransform.Add(pole);
                    switch (poleCount)
                    {
                        case 0:
                            expendCrops = Instantiate(CropsParent, position, Quaternion.identity);
                            FenseCollider[0].transform.parent = null;
                            break;
                        case 2:
                            FenseCollider[1].transform.parent = null;
                            break;
                        case 3:
                            GenerateCollider(expendCrops);
                            break;
                    }
                    ++poleCount;
                    pole.SetParent(expendCrops.transform);
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
                Vector3 fixedPosition = new Vector3((int)hit.point.x, hit.point.y, (int)hit.point.z);
                transform.position = fixedPosition;

                bool flag = !isPoleConnect && FenseCollider[0].IsUnCollid;
                if (poleCount > 3)
                    flag = false;
                else if (poleCount > 0)
                {
                    MoveFenseCollider(0, poleTransform[poleCount - 1].position, fixedPosition);
                    flag &= IsBelowLimit(poleTransform[poleCount - 1].position, fixedPosition);
                    if (poleCount.Equals(3))
                        flag &= IsSquareForm(fixedPosition) && IsBelowLimit(poleTransform[0].position, fixedPosition);
                }
                ChangeVisualColor(flag);

                if (Input.GetMouseButtonDown(0) && isEnable)
                {
                    var position = new Vector3(FensePoleVisual.position.x, hit.point.y, FensePoleVisual.position.z);
                    var pole = Instantiate(FensePole, position, Quaternion.identity);
                    poleTransform.Add(pole);
                    switch (poleCount)
                    {
                        case 0:
                            createCrops = Instantiate(CropsParent, position, Quaternion.identity);
                            FenseCollider[0].transform.parent = null;
                            break;
                        case 3:
                            GenerateCollider(createCrops);
                            break;
                    }
                    ++poleCount;
                    pole.SetParent(createCrops.transform);
                }
            }
            yield return null;
        }
    }

    public IEnumerator GenerateField(bool isExpension)
    {
        if (createCrops == null || poleCount <= 3)
            yield break;

        isBuilding = true;
        int timeI = 0;
        float timer = 0;
        float limitTimer = BuildPeriod[0];
        Vector2Int startPosition = Vector2Int.zero;
        List<Transform> cropsTransform = GenerateCrops(ref startPosition);

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
                        if (isExpension)
                            createCrops.AddCrops(cropsTransform);
                        else
                            createCrops.Initialize(CropsDatas[0], cropsTransform);
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

    private List<Transform> GenerateCrops(ref Vector2Int startPosition)
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

        var direction = (poleTransform[3].position - poleTransform[0].position).normalized;
        var rotationY = Mathf.Acos(Vector3.Dot(direction, -Vector3.right)) * 180 / Mathf.PI;

        int odd = 0;
        int sx = (int)(centerX - 12);
        int sz = (int)(centerZ - 12);
        int ex = (int)(centerX + 12);
        int ez = (int)(centerZ + 12);
        var cropsTrans = new List<Transform>();
        cropsLayer = new float[ez - sz + 1, ex - sx + 1];
        for (int x = sx; x <= ex; x++)
        {
            var tmpList = new List<Transform>();
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
                    cropsLayer[fixedPosition.z - sz, fixedPosition.x - sx] = 1f;

                if (inCrops && (cropsX % 2).Equals(1) && (cropsZ % 2).Equals(1) &&
                    (x % 2).Equals(0) && (z % 2).Equals(0))
                {
                    var crops = Instantiate(CropsDatas[0].Visual, position, Quaternion.identity);
                    crops.SetParent(createCrops.transform);
                    crops.gameObject.SetActive(false);
                    tmpList.Add(crops);
                }
            }

            if ((x % 2).Equals(0))
            {
                ++odd;
                if ((odd % 2).Equals(1)) tmpList.Reverse();
                cropsTrans.AddRange(tmpList);
            }
        }

        startPosition.x = sx - 1;
        startPosition.y = sz - 1;
        return cropsTrans;
    }
    #endregion

    #region Fense Functions
    private void GenerateFense(Vector3 start, Vector3 end)
    {
        var length = Vector3.Distance(start, end);
        var rate = 1f / length;
        var scale = Vector3.Distance(start, Vector3.Lerp(start, end, rate));

        for (float i = rate; i < .95f; i += rate)
        {
            var position = Vector3.Lerp(start, end, i);
            var fense = Instantiate(Fense, position, Quaternion.identity);
            fense.SetParent(createCrops.transform);
            fense.localScale *= scale;
            fense.LookAt(end);
        }
    }

    private void GenerateCollider(CropsEntity entity)
    {
        var verts = new Vector3[8];
        var tris = new int[] { 0, 1, 5,  0, 4, 5,  1, 2, 6,  1, 5, 6,  2, 3, 7,  2, 6, 7,
                               3, 0, 4,  3, 4, 7,  4, 5, 6,  4, 7, 6,  0, 1, 2,  0, 3, 2};
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

        var children = new List<Transform>();
        foreach (Transform child in expendCrops.transform)
            children.Add(child);

        foreach (Transform child in children)
            child.parent = createCrops.transform;
        Destroy(expendCrops.gameObject);
    }

    public void ClearFense()
    {
        if(!isBuilding)
        {
            lengthLimit = DefaultLengthLimit;
            poleTransform.Clear();
            poleCount = 0;

            isEnable = true;
            isPoleConnect = false;
            createCrops = null;
            expendCrops = null;
            VisualMaterial.SetColor("_BaseColor", EnableColor);

            FensePoleVisual.SetParent(transform);
            FensePoleVisual.localPosition = Vector3.up;
            FenseCollider[0].ClearCollider(FensePoleVisual);
            FenseCollider[1].ClearCollider(FensePoleVisual);
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
    #endregion

    private void MoveFenseCollider(int index, Vector3 prevPosition, Vector3 nextPosition)
    {
        FenseCollider[index].transform.position = Vector3.Lerp(prevPosition, nextPosition, .05f);
        colliderScale.z = Vector3.Distance(FenseCollider[index].transform.position, nextPosition) * 9.5f;
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
        return (prevPos - nextPos).sqrMagnitude < lengthLimit;
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

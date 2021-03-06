using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class FieldGenerator : MonoBehaviour
{
    [SerializeField] private TerrainGenerator Generator = null;
    [Header("Field PartScale Setting")]
    [SerializeField] private int PartScale = 2;
    [SerializeField] private int LimitScale = 10;
    [Header("Field Transform Setting")]
    [SerializeField] private Vector3 InitScale = default;
    [SerializeField] private LayerMask RayMask = default;
    [SerializeField] private LayerMask DeleteMask = default;
    [SerializeField] private Transform FieldBody = null;
    [SerializeField] private CropsEntity FieldEntity = null;
    [SerializeField] private DecalProjector FieldVisual = null;
    [Header("BatDuk Transform Setting")]
    [SerializeField] private Transform BatDukMid = null;
    [SerializeField] private Transform BatDukSide = null;
    [SerializeField] private Transform RotateSpace = null;
    [Header("Field UI Setting")]
    [SerializeField] private Material FieldGrid = null;
    [SerializeField] private Color PossibleColor = Color.white;
    [SerializeField] private Color ImpossibleColor = Color.white;
    [Header("Field Crops Setting")]
    [SerializeField] private CropsData FieldData = null;
    [SerializeField] private Material RotateMaterial = null;

    private bool isFixed = false;
    private bool isExpand = false;
    private bool isRotate = false;
    private bool isConnected = false;
    private CropsEntity connectEntity = null;
    private Quaternion fieldSpace = Quaternion.identity;
    private Quaternion reverseFieldSpace = Quaternion.identity;

    #region Unity MonoBehaviour Functions
    private void Awake()
    {
        RaycastTool.Initialize();
    }

    private void OnEnable()
    {
        Initialize();
    }

    private void OnDisable()
    {
        FieldVisual.transform.parent = FieldBody;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Initialize();
            if (isExpand)
                UIManager.Instance.InitializeCropsCount(connectEntity);
            else
                UIManager.Instance.InitializeCropsCount();
            UIManager.Instance.InitializeCoroutine();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if(other.CompareTag("FensePole") && !isFixed)
        {
            isConnected = true;
            FieldVisual.transform.parent = null;
            FieldVisual.transform.position = other.transform.position + Vector3.up * 2;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("FensePole") && !isFixed)
        {
            isConnected = false;
            FieldVisual.transform.parent = FieldBody;
            FieldVisual.transform.localScale = Vector3.one;
            FieldVisual.transform.localPosition = Vector3.up * 2;
        }
    }
    #endregion

    #region Field Generate Functions
    public IEnumerator MakeFieldCoroutine()
    {
        var isRotate = false;
        var isEnable = true;
        var preEnable = false;
        var rayPosition = Vector3.zero;
        var startPosition = Vector3.zero;
        var fieldCollider = FieldVisual.GetComponent<ObjectCollider>();
        while(true)
        {
            preEnable = fieldCollider.IsUnCollid;
            CheckPossiblePosition(ref isEnable, preEnable);
            if (Input.GetKeyDown(KeyCode.R) && !isFixed)
            {
                isRotate = !isRotate;
                RotateFieldGenerator(isRotate);
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (isEnable)
                {
                    isFixed = true;
                    break;
                }
                else
                {
                    Initialize();
                    UIManager.Instance.SetDeactiveWindows();
                }
            }

            if(RaycastTool.RaycastFromMouse(ref rayPosition, RayMask))
            {
                startPosition = fieldSpace * rayPosition;
                startPosition.x = Mathf.FloorToInt((startPosition.x / PartScale)) * PartScale + PartScale / 2;
                startPosition.z = Mathf.FloorToInt((startPosition.z / PartScale)) * PartScale + PartScale / 2;
                transform.localPosition = startPosition;
            }
            yield return null;
        }

        this.isRotate = isRotate;
        transform.position = FieldVisual.transform.position;
        transform.localRotation = Quaternion.identity;
        FieldBody.localPosition = new Vector3(-PartScale / 2, 0, -PartScale / 2);
        FieldVisual.transform.parent = FieldBody;
        FieldVisual.transform.localPosition = new Vector3(.5f, 0, .5f);

        int partScaleX = 0, partScaleZ = 0;
        var nextPosition = Vector3.zero;
        var localRotation = Vector3.zero;
        var fieldPartScale = Vector3Int.one;
        while (true)
        {
            if (RaycastTool.RaycastFromMouse(ref rayPosition, RayMask))
            {
                nextPosition = fieldSpace * rayPosition;
                fieldPartScale.x = Mathf.FloorToInt(nextPosition.x - startPosition.x);
                fieldPartScale.z = Mathf.FloorToInt(nextPosition.z - startPosition.z);
 
                localRotation.x = fieldPartScale.z < 0 ? 180 : 0;
                localRotation.z = fieldPartScale.x < 0 ? 180 : 0;
                localRotation.y = localRotation.x + localRotation.z;
                partScaleX = Mathf.Clamp(Mathf.Abs(fieldPartScale.x / PartScale * PartScale) + PartScale, 2, LimitScale);
                partScaleZ = Mathf.Clamp(Mathf.Abs(fieldPartScale.z / PartScale * PartScale) + PartScale, 2, LimitScale);
                fieldPartScale.x = partScaleX;
                fieldPartScale.z = partScaleZ;
            }

            preEnable = fieldCollider.IsUnCollid && partScaleX > 2 && partScaleZ > 2;
            CheckPossiblePosition(ref isEnable, preEnable);
            if (Input.GetMouseButtonUp(0))
            {
                if (isEnable)
                {
                    var count = (fieldPartScale.x - 2) * (fieldPartScale.z - 2) / 4;
                    UIManager.Instance.ChangeCropsCount(0, count);
                    break;
                }    
                else
                {
                    Initialize();
                    UIManager.Instance.SetDeactiveWindows();
                }
            }

            FieldBody.localScale = fieldPartScale;
            FieldVisual.size = new Vector3(fieldPartScale.x, fieldPartScale.z, 5);
            FieldVisual.transform.localRotation = Quaternion.Euler(localRotation.y + 90, 0, 0);
            transform.localRotation = Quaternion.Euler(localRotation.x, 0, localRotation.z);
            yield return null;
        }
    }

    public IEnumerator ExpandFieldCoroutine(CropsEntity expandEntity, bool isRotate)
    {
        isExpand = true;
        this.isRotate = isRotate;
        var isEnable = true;
        var preEnable = false;
        var rayPosition = Vector3.zero;
        var startPosition = Vector3.zero;
        var fieldCollider = FieldVisual.GetComponent<ObjectCollider>();

        connectEntity = expandEntity;
        RotateFieldGenerator(isRotate);
        while (true)
        {
            preEnable = fieldCollider.IsUnCollid && isConnected;
            CheckPossiblePosition(ref isEnable, preEnable);

            if (Input.GetMouseButtonDown(0))
            {
                if (isEnable)
                {
                    isFixed = true;
                    break;
                }
                else
                {
                    Initialize();
                    UIManager.Instance.SetDeactiveWindows();
                }
            }

            if (RaycastTool.RaycastFromMouse(ref rayPosition, RayMask))
            {
                startPosition = fieldSpace * rayPosition;
                startPosition.x = Mathf.FloorToInt(startPosition.x / PartScale) * PartScale + PartScale / 2;
                startPosition.z = Mathf.FloorToInt(startPosition.z / PartScale) * PartScale + PartScale / 2;
                transform.localPosition = startPosition;
            }
            yield return null;
        }

        transform.position = FieldVisual.transform.position;
        transform.localRotation = Quaternion.identity;
        FieldBody.localPosition = new Vector3(-PartScale / 2, 0, -PartScale / 2);
        FieldVisual.transform.parent = FieldBody;
        FieldVisual.transform.localPosition = new Vector3(.5f, 0, .5f);

        int partScaleX = 0, partScaleZ = 0;
        var nextPosition = Vector3.zero;
        var localRotation = Vector3.zero;
        var fieldPartScale = Vector3Int.one;
        while (true)
        {
            if (RaycastTool.RaycastFromMouse(ref rayPosition, RayMask))
            {
                nextPosition = fieldSpace * rayPosition;
                fieldPartScale.x = Mathf.FloorToInt(nextPosition.x - startPosition.x);
                fieldPartScale.z = Mathf.FloorToInt(nextPosition.z - startPosition.z);

                localRotation.x = fieldPartScale.z < 0 ? 180 : 0;
                localRotation.z = fieldPartScale.x < 0 ? 180 : 0;
                localRotation.y = localRotation.x + localRotation.z;
                partScaleX = Mathf.Clamp(Mathf.Abs(fieldPartScale.x / PartScale * PartScale) + PartScale, 2, LimitScale);
                partScaleZ = Mathf.Clamp(Mathf.Abs(fieldPartScale.z / PartScale * PartScale) + PartScale, 2, LimitScale);
                fieldPartScale.x = partScaleX;
                fieldPartScale.z = partScaleZ;
            }

            preEnable = fieldCollider.IsUnCollid && partScaleX > 2 && partScaleZ > 2 && isConnected;
            CheckPossiblePosition(ref isEnable, preEnable);
            if (Input.GetMouseButtonUp(0))
            {
                if (isEnable)
                {
                    var count = (fieldPartScale.x - 2) * (fieldPartScale.z - 2) / 4;
                    UIManager.Instance.ChangeCropsCount(1, count);
                    UIManager.Instance.ChangeCropsCount(2, connectEntity.CropsCount + count);
                    break;
                }
                else
                {
                    Initialize();
                    UIManager.Instance.SetDeactiveWindows();
                }
            }

            FieldBody.localScale = fieldPartScale;
            FieldVisual.size = new Vector3(fieldPartScale.x, fieldPartScale.z, 5);
            FieldVisual.transform.localRotation = Quaternion.Euler(localRotation.y + 90, 0, 0);
            transform.localRotation = Quaternion.Euler(localRotation.x, 0, localRotation.z);
            yield return null;
        }
    }

    public IEnumerator DestroyFieldCoroutine()
    {
        var isActive = false;
        var fieldHash = 0;
        Transform fieldTransform = null;
        while (true)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (fieldTransform != null)
                {
                    var fieldEntity = fieldTransform.GetComponent<CropsEntity>();
                    isActive = false;
                    UIManager.Instance.SetDeactiveOutlineUI();
                    if (fieldEntity.ParentEntity == null && fieldEntity.ExpandEntities.Length.Equals(1))
                        UIManager.Instance.SetDeactiveWindows();
                    else
                    {
                        var count = fieldEntity.CropsCount - fieldEntity.EachCropsCount;
                        UIManager.Instance.ChangeCropsCount(2, count);
                    }
                    Destroy(fieldTransform.gameObject);
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                UIManager.Instance.SetDeactiveWindows();
            }

            if (RaycastTool.RaycastFromMouse(out RaycastHit hit, DeleteMask) &&
                hit.transform.CompareTag("Crops"))
            {
                var preHash = hit.transform.GetHashCode();
                if (!preHash.Equals(fieldHash))
                {
                    if (isActive)
                        UIManager.Instance.SetDeactiveOutlineUI();
                    isActive = true;
                    fieldHash = preHash;
                    fieldTransform = hit.transform;
                    var size = fieldTransform.GetComponent<DecalProjector>().size;
                    UIManager.Instance.SetActiveOutlineUI(hit.transform, size);
                }
            }
            else if (isActive)
            {
                fieldHash = 0;
                isActive = false;
                fieldTransform = null;
                UIManager.Instance.SetDeactiveOutlineUI();
            }
            yield return null;
        }
    }

    public IEnumerator StartBuildCoroutine() 
    {
        var fieldEntity = Instantiate(FieldEntity, FieldVisual.transform.position, Quaternion.Euler(90, reverseFieldSpace.eulerAngles.y, 0));
        var fieldDecal = fieldEntity.GetComponent<DecalProjector>();
        var fieldCollider = fieldEntity.GetComponent<BoxCollider>();
        fieldDecal.size = new Vector3(FieldBody.localScale.x - 1, FieldBody.localScale.z - 1, 10);
        fieldCollider.size = new Vector3(FieldBody.localScale.x, FieldBody.localScale.z, 3);
        Generator.RemoveTerrainDetail(fieldCollider);

        var cropsTransform = GenerateFieldCrops();
        var polePositions = GenerateBatDuk(fieldEntity.transform);
        fieldEntity.AddCrops(cropsTransform, polePositions);
        fieldEntity.Initialize(0, FieldData);

        if (isExpand)
            connectEntity.ExpandCropsEntity(fieldEntity);
        if (isRotate)
            fieldDecal.material = RotateMaterial;

        yield return null;
    }

    private List<Vector3> GenerateBatDuk(Transform parent)
    {
        var zScale = Mathf.FloorToInt(FieldBody.localScale.z) / 2;
        var xScale = Mathf.FloorToInt(FieldBody.localScale.x) / 2;
        var vertexPosition = new List<Vector3>
        {
            reverseFieldSpace * new Vector3(-xScale, 100, -zScale) + FieldVisual.transform.position,
            reverseFieldSpace * new Vector3(-xScale, 100, zScale) + FieldVisual.transform.position,
            reverseFieldSpace * new Vector3(xScale, 100, zScale) + FieldVisual.transform.position,
            reverseFieldSpace * new Vector3(xScale, 100, -zScale) + FieldVisual.transform.position
        };

        for (int i = 0; i < 4; i++)
        {
            var j = (i + 1) % 4;

            var prevSidePosition = RaycastTool.RaycastFromUp(vertexPosition[i], RayMask);
            var nextSidePosition = RaycastTool.RaycastFromUp(vertexPosition[j], RayMask);
            var direction = (nextSidePosition - prevSidePosition).normalized;
            var batDukStart = Instantiate(BatDukSide, prevSidePosition + direction, Quaternion.identity);
            var batDukEnd = Instantiate(BatDukSide, nextSidePosition - direction, Quaternion.identity);
            batDukStart.transform.LookAt(nextSidePosition);
            batDukStart.transform.Rotate(-90, 0, 0);
            batDukStart.transform.parent = parent;
            batDukEnd.transform.LookAt(prevSidePosition);
            batDukEnd.transform.Rotate(-90, 0, 0);
            batDukEnd.transform.parent = parent;

            var batDukMid = Instantiate(BatDukMid, Vector3.Lerp(prevSidePosition, nextSidePosition, .5f), Quaternion.identity);
            var batDukScale = BatDukMid.transform.localScale;
            batDukScale.y = Vector3.Distance(prevSidePosition + direction, nextSidePosition - direction) / 2;
            batDukMid.transform.localScale =batDukScale;
            batDukMid.transform.LookAt(nextSidePosition);
            batDukMid.transform.Rotate(-90, 0, 0);
            batDukMid.transform.parent = parent;
        }

        int sx = 2, ex = xScale * 2;
        int sz = 2, ez = zScale * 2;

        var polePosition = new List<Vector3>();
        for (int k = sx; k <= ex; k += 2)
        {
            polePosition.Add(vertexPosition[0] + reverseFieldSpace * new Vector3(k - 1, 0, -1));
            polePosition.Add(vertexPosition[1] + reverseFieldSpace * new Vector3(k - 1, 0, 1));
        }
        for(int k = sz; k <= ez; k += 2)
        {
            polePosition.Add(vertexPosition[0] + reverseFieldSpace * new Vector3(-1, 0, k - 1));
            polePosition.Add(vertexPosition[3] + reverseFieldSpace * new Vector3(1, 0, k - 1));
        }
        return polePosition;
    }

    private List<Vector3> GenerateFieldCrops()
    {
        var zScale = Mathf.FloorToInt(FieldBody.localScale.z) / 2;
        var xScale = Mathf.FloorToInt(FieldBody.localScale.x) / 2;
        var startPosition = FieldVisual.transform.position + reverseFieldSpace * new Vector3(1, 0, 1);

        var oddCheck = 0;
        var tmpCropsList = new List<Vector3>();
        var fieldCropsList = new List<Vector3>();
        for (int z = -zScale + 1; z < zScale - 1; z += 2)
        {
            for(int x = -xScale + 1; x < xScale - 1; x += 2)
                tmpCropsList.Add(RaycastTool.RaycastFromUp(reverseFieldSpace * new Vector3(x, 0, z) + startPosition, RayMask));

            if ((oddCheck % 2).Equals(1))
                tmpCropsList.Reverse();
            fieldCropsList.AddRange(tmpCropsList);
            tmpCropsList.Clear();
            ++oddCheck;
        }
        return fieldCropsList;
    }

    public void DestroyField(CropsEntity entity)
    {
        Generator.CreateTerrainDetail(entity.GetComponent<BoxCollider>());
        Destroy(entity.gameObject);
    }

    public void Initialize()
    {
        isFixed = false;
        isRotate = false;
        isExpand = false;
        isConnected = false;
        transform.parent = null;
        fieldSpace = Quaternion.identity;
        reverseFieldSpace = Quaternion.identity;
        transform.localRotation = Quaternion.identity;

        FieldBody.localScale = InitScale;
        FieldBody.localPosition = Vector3.zero;
        FieldVisual.transform.parent = FieldBody;
        FieldVisual.transform.localPosition = Vector3.up * 2;
        FieldVisual.transform.localRotation = Quaternion.Euler(90, 0, 0);
        FieldVisual.size = new Vector3(PartScale, PartScale, 5);
        transform.position = Vector3.zero;

        FieldGrid.SetFloat("_Rotate", 90);
        FieldGrid.SetColor("_OutlineColor", PossibleColor);
        FieldGrid.SetVector("_Offset", new Vector2(0, .5f));
    }
    #endregion

    #region Sub Functions
    private void CheckPossiblePosition(ref bool isEnable, bool preEnable)
    {
        if (isEnable != preEnable)
        {
            isEnable = preEnable;
            if (isEnable)
                FieldGrid.SetColor("_OutlineColor", PossibleColor);
            else
                FieldGrid.SetColor("_OutlineColor", ImpossibleColor);
        }
    }

    private void RotateFieldGenerator(bool isRotate)
    {
        if (isRotate)
        {
            transform.parent = RotateSpace;
            reverseFieldSpace = Quaternion.Euler(0, 45, 0);
            transform.localRotation = Quaternion.identity;
            fieldSpace.y = -reverseFieldSpace.y;
            fieldSpace.w = reverseFieldSpace.w;

            FieldGrid.SetFloat("_Rotate", 45);
            FieldGrid.SetVector("_Offset", new Vector2(.105f, .75f));
        }
        else
        {
            transform.parent = null;
            reverseFieldSpace = Quaternion.identity;
            transform.localRotation = Quaternion.identity;
            fieldSpace = reverseFieldSpace;
            
            FieldGrid.SetFloat("_Rotate", 90);
            FieldGrid.SetVector("_Offset", new Vector2(0, .5f));
        }
    }
    #endregion
}
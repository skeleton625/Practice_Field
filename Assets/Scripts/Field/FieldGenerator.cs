using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using GPUInstancer;

public class FieldGenerator : MonoBehaviour
{
    [Header("GPUInstancer")]
    [SerializeField] private GPUInstancerPrefabManager CropsManager = null;
    [SerializeField] private GPUInstancerPrefabManager BuildingManager = null;
    [Header("Field PartScale Setting")]
    [SerializeField] private int PartScale = 2;
    [SerializeField] private int LimitScale = 10;
    [Header("Field Transform Setting")]
    [SerializeField] private Vector3 InitScale = default;
    [SerializeField] private LayerMask RayMask = default;
    [SerializeField] private Transform FieldBody = null;
    [SerializeField] private Transform FieldVisual = null;
    [SerializeField] private CropsEntity FieldEntity = null;
    [Header("BatDuk Transform Setting")]
    [SerializeField] private GPUInstancerPrefab BatDukMid = null;
    [SerializeField] private GPUInstancerPrefab BatDukSide = null;
    [SerializeField] private Transform RotateSpace = null;
    [Header("Field UI Setting")]
    [SerializeField] private Material VisualMaterial = null;
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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Initialize();
            if (isExpand)
            {
                UIManager.Instance.ChangeCropsCount(1, 0);
                UIManager.Instance.ChangeCropsCount(2, connectEntity.CropsCount);
            }
            else
                UIManager.Instance.ChangeCropsCount(0, 0);
            UIManager.Instance.InitializeCoroutine();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if(other.CompareTag("FensePole") && !isFixed)
        {
            isConnected = true;
            FieldVisual.parent = null;
            FieldVisual.position = other.transform.position + Vector3.up;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("FensePole"))
        {
            isConnected = false;
            FieldVisual.parent = FieldBody;
            FieldVisual.localPosition = Vector3.up;
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
                startPosition.x = (int)(startPosition.x / PartScale) * PartScale + PartScale / 2;
                startPosition.z = (int)(startPosition.z / PartScale) * PartScale + PartScale / 2;
                transform.localPosition = startPosition;
            }
            yield return null;
        }

        this.isRotate = isRotate;
        transform.position = FieldVisual.position;
        transform.localRotation = Quaternion.identity;
        FieldBody.localPosition = new Vector3(-PartScale / 2, 0, -PartScale / 2);
        FieldVisual.parent = FieldBody;
        FieldVisual.localPosition = new Vector3(.5f, 0, .5f);

        int partScaleX = 0, partScaleZ = 0;
        var nextPosition = Vector3.zero;
        var localRotation = Vector3.zero;
        var fieldPartScale = Vector3.one;
        while (true)
        {
            if (RaycastTool.RaycastFromMouse(ref rayPosition, RayMask))
            {
                nextPosition = fieldSpace * rayPosition;
                fieldPartScale.x = nextPosition.x - startPosition.x;
                fieldPartScale.z = nextPosition.z - startPosition.z;

                localRotation.x = fieldPartScale.z < 0 ? 180 : 0;
                localRotation.z = fieldPartScale.x < 0 ? 180 : 0;
                partScaleX = Mathf.Clamp(Mathf.Abs((int)fieldPartScale.x / PartScale * PartScale) + PartScale, 2, LimitScale);
                partScaleZ = Mathf.Clamp(Mathf.Abs((int)fieldPartScale.z / PartScale * PartScale) + PartScale, 2, LimitScale);
                fieldPartScale.x = partScaleX;
                fieldPartScale.z = partScaleZ;
            }

            preEnable = fieldCollider.IsUnCollid && partScaleX > 2 && partScaleZ > 2;
            CheckPossiblePosition(ref isEnable, preEnable);
            if (Input.GetMouseButtonUp(0))
            {
                if (isEnable)
                {
                    var count = (int)((fieldPartScale.x - 2) * (fieldPartScale.z - 2) / 4);
                    UIManager.Instance.ChangeCropsCount(0, count);
                    break;
                }    
                else
                {
                    Initialize();
                    UIManager.Instance.SetDeactiveWindows();
                }
            }

            transform.localRotation = Quaternion.Euler(localRotation);
            FieldBody.localScale = fieldPartScale;
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
                startPosition.x = (int)(startPosition.x / PartScale) * PartScale + PartScale / 2;
                startPosition.z = (int)(startPosition.z / PartScale) * PartScale + PartScale / 2;
                transform.localPosition = startPosition;
            }
            yield return null;
        }

        transform.position = FieldVisual.position;
        transform.localRotation = Quaternion.identity;
        FieldBody.localPosition = new Vector3(-PartScale / 2, 0, -PartScale / 2);
        FieldVisual.parent = FieldBody;
        FieldVisual.localPosition = new Vector3(.5f, 0, .5f);

        int partScaleX = 0, partScaleZ = 0;
        var nextPosition = Vector3.zero;
        var localRotation = Vector3.zero;
        var fieldPartScale = Vector3.one;
        while (true)
        {
            if (RaycastTool.RaycastFromMouse(ref rayPosition, RayMask))
            {
                nextPosition = fieldSpace * rayPosition;
                fieldPartScale.x = nextPosition.x - startPosition.x;
                fieldPartScale.z = nextPosition.z - startPosition.z;

                localRotation.x = fieldPartScale.z < 0 ? 180 : 0;
                localRotation.z = fieldPartScale.x < 0 ? 180 : 0;
                partScaleX = Mathf.Clamp(Mathf.Abs((int)fieldPartScale.x / PartScale * PartScale) + PartScale, 2, LimitScale);
                partScaleZ = Mathf.Clamp(Mathf.Abs((int)fieldPartScale.z / PartScale * PartScale) + PartScale, 2, LimitScale);
                fieldPartScale.x = partScaleX;
                fieldPartScale.z = partScaleZ;
            }

            preEnable = fieldCollider.IsUnCollid && partScaleX > 2 && partScaleZ > 2 && isConnected;
            CheckPossiblePosition(ref isEnable, preEnable);
            if (Input.GetMouseButtonUp(0))
            {
                if (isEnable)
                {
                    var count = (int)((fieldPartScale.x - 2) * (fieldPartScale.z - 2) / 4);
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

            transform.localRotation = Quaternion.Euler(localRotation);
            FieldBody.localScale = fieldPartScale;
            yield return null;
        }
    }

    public IEnumerator StartBuildCoroutine()
    {
        var fieldEntity = Instantiate(FieldEntity, FieldVisual.position, Quaternion.Euler(90, reverseFieldSpace.eulerAngles.y, 0));
        fieldEntity.GetComponent<DecalProjector>().size = new Vector3(FieldBody.localScale.x - 1, FieldBody.localScale.z - 1, 5);
        fieldEntity.GetComponent<BoxCollider>().size = new Vector3(FieldBody.localScale.x - 1, FieldBody.localScale.z - 1, 5);
        fieldEntity.transform.parent = fieldEntity.transform;
        if (isRotate)
            fieldEntity.GetComponent<DecalProjector>().material = RotateMaterial;

        var polePositions = GenerateBatDuk(fieldEntity.transform);
        var cropsTransform = GenerateFieldCrops(fieldEntity.transform);
        fieldEntity.AddCrops(cropsTransform, polePositions);
        fieldEntity.Initialize(FieldData);
        if (isExpand)
            connectEntity.ExpandCropsEntity(fieldEntity);

        yield return null;
    }

    private List<Vector3> GenerateBatDuk(Transform parent)
    {
        int zScale = (int)FieldBody.localScale.z / 2;
        int xScale = (int)FieldBody.localScale.x / 2;
        var vertexPosition = new List<Vector3>();
        vertexPosition.Add(reverseFieldSpace * new Vector3(-xScale, 100, -zScale) + FieldVisual.position);
        vertexPosition.Add(reverseFieldSpace * new Vector3(-xScale, 100, zScale) + FieldVisual.position);
        vertexPosition.Add(reverseFieldSpace * new Vector3(xScale, 100, zScale) + FieldVisual.position);
        vertexPosition.Add(reverseFieldSpace * new Vector3(xScale, 100, -zScale) + FieldVisual.position);

        for(int i = 0; i < 4; i++)
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

            GPUInstancerAPI.AddPrefabInstance(BuildingManager, batDukStart);
            GPUInstancerAPI.AddPrefabInstance(BuildingManager, batDukEnd);
            GPUInstancerAPI.AddPrefabInstance(BuildingManager, batDukMid);
        }

        int sx = 2, ex = xScale * 2;
        int sz = 2, ez = zScale * 2;
        if((xScale % 2).Equals(0)) { sx = 3; ex = xScale * 2 - 1; }
        if ((zScale % 2).Equals(0)) { sz = 3; ez = zScale * 2 - 1; }

        var polePosition = new List<Vector3>();
        for (int k = sx; k <= ex; k += 4)
        {
            polePosition.Add(vertexPosition[0] + reverseFieldSpace * new Vector3(k - 1, 0, -1));
            polePosition.Add(vertexPosition[1] + reverseFieldSpace * new Vector3(k - 1, 0, 1));
        }
        for(int k = sz; k <= ez; k += 4)
        {
            polePosition.Add(vertexPosition[0] + reverseFieldSpace * new Vector3(-1, 0, k - 1));
            polePosition.Add(vertexPosition[3] + reverseFieldSpace * new Vector3(1, 0, k - 1));
        }
        return polePosition;
    }

    private List<Transform> GenerateFieldCrops(Transform parent)
    {
        var oddCheck = 0;
        var zScale = (int)FieldBody.localScale.z / 2;
        var xScale = (int)FieldBody.localScale.x / 2;
        var startPosition = FieldVisual.position + reverseFieldSpace * new Vector3(1, 0, 1);

        var tmpCropsList = new List<Transform>();
        var fieldCropsList = new List<Transform>();
        var rotateY = new float[4] { 0, 90, 180, 270 };
        for (int z = -zScale + 1; z < zScale - 1; z += 2)
        {
            for(int x = -xScale + 1; x < xScale - 1; x += 2)
            {
                var position = RaycastTool.RaycastFromUp(reverseFieldSpace * new Vector3(x, 0, z) + startPosition, RayMask);
                var rotation = Quaternion.Euler(0, rotateY[Random.Range(0, 4)] + (isRotate ? 45 : 0), 0);
                var crops = Instantiate(FieldData.Visual, position, rotation);
                tmpCropsList.Add(crops.transform);
                crops.transform.parent = parent;

                GPUInstancerAPI.AddPrefabInstance(CropsManager, crops);
            }

            if ((oddCheck % 2).Equals(1))
                tmpCropsList.Reverse();
            fieldCropsList.AddRange(tmpCropsList);
            tmpCropsList.Clear();
            ++oddCheck;
        }
        return fieldCropsList;
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

        transform.position = Vector3.zero;
        FieldVisual.parent = FieldBody;
        FieldBody.localScale = InitScale;
        FieldBody.localPosition = Vector3.zero;
        FieldVisual.localPosition = Vector3.up;
        VisualMaterial.SetColor("_BaseColor", PossibleColor);
    }
    #endregion

    #region Sub Functions
    private void CheckPossiblePosition(ref bool isEnable, bool preEnable)
    {

        if (isEnable != preEnable)
        {
            isEnable = preEnable;
            if (isEnable)
                VisualMaterial.SetColor("_BaseColor", PossibleColor);
            else
                VisualMaterial.SetColor("_BaseColor", ImpossibleColor);
        }
    }

    private void RotateFieldGenerator(bool isRotate)
    {
        if (isRotate)
        {
            transform.SetParent(RotateSpace);
            reverseFieldSpace = Quaternion.Euler(0, 45, 0);
            transform.localRotation = Quaternion.identity;
            fieldSpace.y = -reverseFieldSpace.y;
            fieldSpace.w = reverseFieldSpace.w;
        }
        else
        {
            transform.SetParent(null);
            reverseFieldSpace = Quaternion.identity;
            transform.localRotation = Quaternion.identity;
            fieldSpace = reverseFieldSpace;
        }
    }
    #endregion
}
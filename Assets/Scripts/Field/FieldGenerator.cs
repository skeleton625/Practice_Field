using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class FieldGenerator : MonoBehaviour
{
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
    [SerializeField] private Transform BatDukMid = null;
    [SerializeField] private Transform BatDukSide = null;
    [SerializeField] private Transform BatDukPole = null;
    [SerializeField] private Transform RotateSpace = null;
    [Header("Field UI Setting")]
    [SerializeField] private Material VisualMaterial = null;
    [SerializeField] private Color PossibleColor = Color.white;
    [SerializeField] private Color ImpossibleColor = Color.white;
    [Header("Field Crops Setting")]
    [SerializeField] private CropsData FieldData = null;

    private bool isFixed = false;
    private bool isExpand = false;
    private bool isRotate = false;
    private bool isConnected = false;
    private Camera mainCamera = null;
    private CropsEntity connectEntity = null;
    private Quaternion fieldSpace = Quaternion.identity;
    private Quaternion reverseFieldSpace = Quaternion.identity;

    #region Unity MonoBehaviour Functions
    private void Awake()
    {
        mainCamera = Camera.main;
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
            UIManager.Instance.SetActiveButtonWindows(2, 0);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if(other.CompareTag("FensePole") && !isFixed)
        {
            isConnected = true;
            FieldVisual.parent = null;
            FieldVisual.position = other.transform.position + Vector3.up;
            connectEntity = other.transform.parent.GetComponent<CropsEntity>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("FensePole"))
        {
            isConnected = false;
            FieldVisual.parent = FieldBody;
            FieldVisual.localPosition = Vector3.up;
            connectEntity = null;
        }
    }
    #endregion

    #region Field Generate Functions
    public IEnumerator MakeBuildingCoroutine(bool isExpand)
    {
        this.isExpand = isExpand;
        var isEnable = false;
        var preEnable = false;
        var startPosition = Vector3.zero;
        var fieldSpace = Quaternion.identity;
        var fieldCollider = FieldVisual.GetComponent<ObjectCollider>();
        while(true)
        {
            preEnable = fieldCollider.IsUnCollid;
            CheckPossiblePosition(ref isEnable, preEnable);
            if (Input.GetKeyDown(KeyCode.R) && !isFixed)
            {
                isRotate = !isRotate;
                if (isRotate)
                {
                    transform.SetParent(RotateSpace);
                    fieldSpace = Quaternion.Euler(0, -45, 0);
                    reverseFieldSpace = Quaternion.Euler(0, 45, 0);
                }
                else
                {
                    transform.SetParent(null);
                    fieldSpace = Quaternion.identity;
                    reverseFieldSpace = Quaternion.identity;
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (isEnable)
                {
                    if (isExpand)
                    {
                        if (isConnected)
                        {
                            isFixed = true;
                            break;
                        }
                    }
                    else
                    {
                        isFixed = true;
                        break;
                    }
                }
                else
                {
                    Initialize();
                    UIManager.Instance.SetActiveButtonWindows(2, 0);
                }
            }

            startPosition = fieldSpace * RaycastMouseRay();
            startPosition.x = (int)startPosition.x + PartScale / 2;
            startPosition.z = (int)startPosition.z + PartScale / 2;
            transform.localPosition = startPosition;
            transform.localRotation = Quaternion.Euler(0, 0, 0);
            yield return null;
        }

        transform.position = FieldVisual.position;
        FieldBody.localPosition = new Vector3(-PartScale / 2, 0, -PartScale / 2);
        FieldVisual.parent = FieldBody;
        FieldVisual.localPosition = new Vector3(.5f, 0, .5f);

        Vector3 nextPosition;
        Vector3 fieldPartScale = Vector3.one;
        Vector3 localRotation = Vector3.zero;
        while (true)
        {
            nextPosition = fieldSpace * RaycastMouseRay();
            fieldPartScale.x = nextPosition.x - startPosition.x;
            fieldPartScale.z = nextPosition.z - startPosition.z;

            localRotation.x = fieldPartScale.z < 0 ? 180 : 0;
            localRotation.z = fieldPartScale.x < 0 ? 180 : 0;
            var PartScaleX = Mathf.Clamp(Mathf.Abs((int)fieldPartScale.x / PartScale * PartScale) + PartScale, 2, LimitScale);
            var PartScaleZ = Mathf.Clamp(Mathf.Abs((int)fieldPartScale.z / PartScale * PartScale) + PartScale, 2, LimitScale);
            fieldPartScale.x = PartScaleX;
            fieldPartScale.z = PartScaleZ;

            preEnable = fieldCollider.IsUnCollid && PartScaleX > 2 && PartScaleZ > 2;
            CheckPossiblePosition(ref isEnable, preEnable);
            if (Input.GetMouseButtonUp(0))
            {
                if (isEnable)
                    break;
                else
                {
                    Initialize();
                    UIManager.Instance.SetActiveButtonWindows(2, 0);
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

        var polePositions = GenerateBatDuk(fieldEntity.transform);
        var cropsTransform = GenerateFieldCrops(fieldEntity.transform);
        fieldEntity.AddCrops(cropsTransform, polePositions);
        fieldEntity.Initialize(FieldData);
        if (isExpand)
        {
            connectEntity.ExpandCropsEntity(fieldEntity);
            Debug.Log("Expand Count : " + connectEntity.CropsCount);
        }
        else
        {
            Debug.Log("Create Count : " + fieldEntity.CropsCount);
        }

        yield return null;
        gameObject.SetActive(false);
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

            var prevSidePosition = RaycastFromUp(vertexPosition[i]);
            var nextSidePosition = RaycastFromUp(vertexPosition[j]);
            var direction = (nextSidePosition - prevSidePosition).normalized;
            var batDukStart = Instantiate(BatDukSide, prevSidePosition + direction, Quaternion.identity);
            var batDukEnd = Instantiate(BatDukSide, nextSidePosition - direction, Quaternion.identity);
            batDukStart.LookAt(nextSidePosition);
            batDukStart.Rotate(-90, 0, 0);
            batDukStart.parent = parent;
            batDukEnd.LookAt(prevSidePosition);
            batDukEnd.Rotate(-90, 0, 0);
            batDukEnd.parent = parent;

            var batDukMid = Instantiate(BatDukMid, Vector3.Lerp(prevSidePosition, nextSidePosition, .5f), Quaternion.identity);
            var batDukScale = BatDukMid.localScale;
            batDukScale.y = Vector3.Distance(prevSidePosition + direction, nextSidePosition - direction) / 2;
            batDukMid.localScale =batDukScale;
            batDukMid.LookAt(nextSidePosition);
            batDukMid.Rotate(-90, 0, 0);
            batDukMid.parent = parent;
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
        var startPosition = FieldVisual.position + (isRotate ? reverseFieldSpace * new Vector3(1, 0, 1) : new Vector3(1, 0, 1));

        var tmpCropsList = new List<Transform>();
        var fieldCropsList = new List<Transform>();
        for (int z = -zScale + 1; z < zScale - 1; z += 2)
        {
            for(int x = -xScale + 1; x < xScale - 1; x += 2)
            {
                var position = RaycastFromUp(reverseFieldSpace * new Vector3(x, 0, z) + startPosition);
                var crops = Instantiate(FieldData.Visual, position, Quaternion.Euler(0, Random.Range(0, 360), 0));
                tmpCropsList.Add(crops);
                crops.parent = parent;
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
        fieldSpace = Quaternion.identity;
        reverseFieldSpace = Quaternion.identity;

        transform.parent = null;
        transform.position = Vector3.zero;
        FieldVisual.parent = FieldBody;
        FieldBody.localScale = InitScale;
        FieldBody.localPosition = Vector3.zero;
        FieldVisual.localPosition = Vector3.up;
    }
    #endregion

    #region Raycast Functions
    private Vector3 RaycastMouseRay()
    {
        var cameraRay = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(cameraRay.origin, cameraRay.direction, out RaycastHit hit, 1000f, RayMask))
            return hit.point;
        return Vector3.zero;
    }

    private Vector3 RaycastFromUp(Vector3 position)
    {
        position.y = 100f;
        if (Physics.Raycast(position, -Vector3.up, out RaycastHit hit, 200f, RayMask))
            return hit.point;
        return Vector3.zero;
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
    #endregion
}
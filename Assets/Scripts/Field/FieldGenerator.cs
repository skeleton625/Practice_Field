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
    [SerializeField] private Transform RotateSpace = null;
    [SerializeField] private DecalProjector FieldDecal = null;
    [Header("Fense Transform Setting")]
    [SerializeField] private Transform Fense = null;
    [SerializeField] private Transform FensePole = null;
    [Header("Field UI Setting")]
    [SerializeField] private ObjectCollider FieldCollider = null;
    [SerializeField] private Material VisualMaterial = null;
    [SerializeField] private Color PossibleColor = Color.white;
    [SerializeField] private Color ImpossibleColor = Color.white;
    [Header("Field Crops Setting")]
    [SerializeField] private CropsData FieldData = null;

    private bool isRotate = false;
    private bool isEnable = false;
    private Camera mainCamera = null;
    private Quaternion fieldSpace = Quaternion.identity;
    private Quaternion reverseFieldSpace = Quaternion.identity;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        isRotate = false;
        fieldSpace = Quaternion.identity;
        reverseFieldSpace = Quaternion.identity;

        transform.parent = null;
        transform.position = Vector3.zero;
        FieldBody.localScale = InitScale;
        FieldBody.localPosition = Vector3.zero;
        FieldVisual.localPosition = Vector3.zero;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
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

        if (isEnable != FieldCollider.IsUnCollid)
        {
            isEnable = FieldCollider.IsUnCollid;
            if (isEnable)
                VisualMaterial.SetColor("_BaseColor", PossibleColor);
            else
                VisualMaterial.SetColor("_BaseColor", ImpossibleColor);
        }
    }

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

    public IEnumerator MakeBuildingCoroutine()
    {
        Vector3 startPosition = Vector3.zero;
        while(true)
        {
            if (Input.GetMouseButtonDown(0))
                break;

            startPosition = fieldSpace * RaycastMouseRay();
            startPosition.x = (int)startPosition.x / PartScale * PartScale + PartScale / 2;
            startPosition.z = (int)startPosition.z / PartScale * PartScale + PartScale / 2;
            transform.localPosition = startPosition;
            transform.localRotation = Quaternion.Euler(0, 0, 0);
            yield return null;
        }

        FieldBody.localPosition = new Vector3(-PartScale / 2, 0, -PartScale / 2);
        FieldVisual.localPosition = new Vector3(.5f, 0, .5f);

        Vector3 nextPosition;
        Vector3 fieldPartScale = Vector3.one;
        Vector3 localRotation = Vector3.zero;
        while (true)
        {
            if (Input.GetMouseButtonUp(0))
                break;

            nextPosition = fieldSpace * RaycastMouseRay();
            fieldPartScale.x = nextPosition.x - startPosition.x;
            fieldPartScale.z = nextPosition.z - startPosition.z;

            localRotation.x = fieldPartScale.z < 0 ? 180 : 0;
            localRotation.z = fieldPartScale.x < 0 ? 180 : 0;
            var PartScaleX = Mathf.Clamp(Mathf.Abs((int)fieldPartScale.x / PartScale * PartScale) + PartScale, PartScale, LimitScale);
            var PartScaleZ = Mathf.Clamp(Mathf.Abs((int)fieldPartScale.z / PartScale * PartScale) + PartScale, PartScale, LimitScale);
            fieldPartScale.x = PartScaleX;
            fieldPartScale.z = PartScaleZ;

            transform.localRotation = Quaternion.Euler(localRotation);
            FieldBody.localScale = fieldPartScale;
            yield return null;
        }
    }

    public IEnumerator StartBuildCoroutine()
    {
        if(isEnable)
        {
            var quaternion = Quaternion.Euler(90, reverseFieldSpace.eulerAngles.y, 0);
            var clone = Instantiate(FieldDecal, FieldVisual.position, quaternion);
            clone.size = new Vector3(FieldBody.localScale.x - 1, FieldBody.localScale.z - 1, 5);
            clone.transform.localScale = clone.size;
            GenerateFieldCrops();
            GenerateFense();
        }

        yield return null;
        gameObject.SetActive(false);
    }

    private void GenerateFense()
    {
        int zScale = (int)FieldBody.localScale.z / 2;
        int xScale = (int)FieldBody.localScale.x / 2;
        var poleDirection = new List<Vector3>();
        var polePosition = new List<Vector3>();
        polePosition.Add(reverseFieldSpace * new Vector3(-xScale, 100, -zScale) + FieldVisual.position);
        polePosition.Add(reverseFieldSpace * new Vector3(-xScale, 100, zScale) + FieldVisual.position);
        polePosition.Add(reverseFieldSpace * new Vector3(xScale, 100, zScale) + FieldVisual.position);
        polePosition.Add(reverseFieldSpace * new Vector3(xScale, 100, -zScale) + FieldVisual.position);
        poleDirection.Add(reverseFieldSpace * new Vector3(-.5f, 0, -.5f));
        poleDirection.Add(reverseFieldSpace * new Vector3(-.5f, 0, .5f));
        poleDirection.Add(reverseFieldSpace * new Vector3(.5f, 0, .5f));
        poleDirection.Add(reverseFieldSpace * new Vector3(.5f, 0, -.5f));

        for(int i = 0; i < 4; i++)
        {
            var j = (i + 1) % 4;

            var prevSidePosition = RaycastFromUp(polePosition[i]);
            var nextSidePosition = RaycastFromUp(polePosition[j]);
            var direction = (nextSidePosition - prevSidePosition).normalized;
            var fenseStart = Instantiate(FensePole, prevSidePosition + direction, Quaternion.identity);
            var fenseEnd = Instantiate(FensePole, nextSidePosition - direction, Quaternion.identity);
            fenseStart.LookAt(nextSidePosition);
            fenseStart.Rotate(-90, 0, 0);
            fenseEnd.LookAt(prevSidePosition);
            fenseEnd.Rotate(-90, 0, 0);

            var fenseMid = Instantiate(Fense, Vector3.Lerp(prevSidePosition, nextSidePosition, .5f), Quaternion.identity);
            var fenseScale = Fense.localScale;
            fenseScale.y = Vector3.Distance(prevSidePosition + direction * 2, nextSidePosition - direction * 2) / 2;
            fenseMid.localScale = fenseScale;
            fenseMid.LookAt(nextSidePosition);
            fenseMid.Rotate(-90, 0, 0);
        }
    }

    private List<Transform> GenerateFieldCrops()
    {
        int zScale = (int)FieldBody.localScale.z / 2;
        int xScale = (int)FieldBody.localScale.x / 2;
        var startPosition = FieldVisual.position + (isRotate ? reverseFieldSpace * new Vector3(.5f, 0, .5f) : new Vector3(.5f, 0, .5f));
        var FieldCropsList = new List<Transform>();
        for(int z = -zScale + 1; z < zScale - 1; z ++)
        {
            for(int x = -xScale + 1; x < xScale - 1; x ++)
            {
                var position = RaycastFromUp(reverseFieldSpace * new Vector3(x, 0, z) + startPosition);
                var clone = Instantiate(FieldData.Visual, position, Quaternion.Euler(0, Random.Range(0, 360), 0));
                FieldCropsList.Add(clone);
            }
        }

        Debug.Log(FieldCropsList.Count / 2);

        return FieldCropsList;
    }
}
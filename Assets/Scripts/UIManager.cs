using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class UIManager : MonoBehaviour
{
    #region SerializeField Variable
    [Header("Field Generator")]
    [SerializeField] private FieldGenerator fieldGenerator = null;
    [SerializeField] private LayerMask RayMask;
    [Header("Building UI Setting")]
    [SerializeField] private DecalProjector OutlineUI = null;
    [Header("Crops UI Setting")]
    [SerializeField] private GameObject CreateWindow = null;
    [SerializeField] private GameObject ExpandWindow = null;
    [SerializeField] private Transform FieldPoleTransform = null;
    [Header("Crops Text Setting")]
    [SerializeField] private Text CreateCropsCount = null;
    [SerializeField] private Text ExpandEntireCount = null;
    [SerializeField] private Text ExpandCurrentCount = null;
    #endregion

    #region Global Private Variable
    private int buildingHash = 0;
    private bool isRotateField = false;
    private bool isMakingField = false;
    private bool isExpandField = false;
    private bool isDeleteField = false;
    private Coroutine fieldCoroutine = null;
    private CropsEntity expandEntity = null;
    private Queue<Transform> poleActiveQueue = null;
    private Queue<Transform> poleTransformQueue = null;
    private Queue<DecalProjector> outlineUIQueue = null;
    private Queue<DecalProjector> outlineUIActiveQueue = null;
    #endregion

    #region Singleton Variable
    public static UIManager Instance = null;
    #endregion

    #region UI Functions
    public void OnClickCreateField()
    {
        if (!isMakingField)
        {
            fieldGenerator.gameObject.SetActive(true);
            fieldCoroutine = StartCoroutine(fieldGenerator.MakeFieldCoroutine());
            isMakingField = true;
        }
    }

    public void OnClickExpandField()
    {
        if (isDeleteField)
            InitializeCoroutine();
        if (!isExpandField)
        {
            if (expandEntity.ParentEntity != null)
            {
                var childEntities = expandEntity.ParentEntity.ExpandEntities;
                foreach (var entity in childEntities)
                    SetActivePoleCollider(entity);
            }
            else
            {
                var childEntities = expandEntity.ExpandEntities;
                foreach (var entity in childEntities)
                    SetActivePoleCollider(entity);
            }

            fieldGenerator.gameObject.SetActive(true);
            fieldCoroutine = StartCoroutine(fieldGenerator.ExpandFieldCoroutine(expandEntity, isRotateField));
            isExpandField = true;
        }
    }

    public void OnClickDeleteField()
    {
        if(!isDeleteField)
        {
            isDeleteField = true;
            fieldCoroutine = StartCoroutine(fieldGenerator.DestroyFieldCoroutine());
            SetDeactiveOutlineUI();
        }
    }

    public void OnClickStartBuild(bool isExpand)
    {
        fieldCoroutine = StartCoroutine(fieldGenerator.StartBuildCoroutine());
        InitializeWindows(isExpand ? 1 : 0);
        InitializeCoroutine();
        fieldGenerator.gameObject.SetActive(false);
    }

    public void ChangeCropsCount(int type, int cropsCount)
    {
        switch(type)
        {
            case 0:
                CreateCropsCount.text = cropsCount.ToString();
                break;
            case 1:
                ExpandCurrentCount.text = cropsCount.ToString();
                break;
            case 2:
                ExpandEntireCount.text = cropsCount.ToString();
                break;
        }
    }
    #endregion

    #region Active Functions
    public void SetActiveCreateWindow()
    {
        if (ExpandWindow.activeSelf)
            return;

        CreateWindow.SetActive(true);
        CreateCropsCount.text = "0";
    }

    public void SetActiveExpandWindow(CropsEntity entity)
    {
        if (CreateWindow.activeSelf)
            return;

        var hashCode = entity.transform.GetHashCode();
        if (buildingHash != hashCode)
        {
            expandEntity = entity;
            buildingHash = hashCode;
            SetDeactiveOutlineUI();
            SetDeactivePoleCollider();
        }
        else
            return;

        isRotateField = entity.transform.rotation == Quaternion.Euler(90, 0, -45);
        ExpandEntireCount.text = entity.CropsCount.ToString();
        ExpandCurrentCount.text = "0";
        ExpandWindow.SetActive(true);

        if(entity.ParentEntity != null)
        {
            var expandEntities = entity.ParentEntity.ExpandEntities;
            foreach (var child in expandEntities)
            {
                var size = child.GetComponent<DecalProjector>().size;
                SetActiveOutlineUI(child.transform, size);
            }
        }
        else
        {
            var expandEntities = entity.ExpandEntities;
            foreach (var child in expandEntities)
            {
                var size = child.GetComponent<DecalProjector>().size;
                SetActiveOutlineUI(child.transform, size);
            }
        }
    }

    public void SetActiveOutlineUI(Transform parent, Vector3 size)
    {
        var outline = outlineUIQueue.Dequeue();
        size.x += 3;
        size.y += 3;
        outline.size = size;
        outline.transform.parent = parent;
        outline.transform.localPosition = Vector3.forward;
        outline.transform.localRotation = Quaternion.identity;
        outline.gameObject.SetActive(true);
        outlineUIActiveQueue.Enqueue(outline);
    }

    private void SetActivePoleCollider(CropsEntity entity)
    {
        Transform pole;
        var polePosition = entity.PolePositions;
        foreach (var position in polePosition)
        {
            if (RaycastTool.RaycastFromUp(position, out RaycastHit hit, RayMask) &&
                hit.transform.CompareTag("Terrain"))
            {
                if (poleTransformQueue.Count > 0)
                {
                    pole = poleTransformQueue.Dequeue();
                    pole.position = hit.point;
                    pole.gameObject.SetActive(true);
                }
                else
                    pole = Instantiate(FieldPoleTransform, hit.point, Quaternion.identity);
                pole.parent = entity.transform;
                poleActiveQueue.Enqueue(pole);
            }
        }
    }
    #endregion

    #region Deactive Functions
    public void SetDeactiveWindows()
    {
        if (CreateWindow.activeSelf)
        {
            InitializeCoroutine();
            InitializeWindows(0);
        }
        else if (ExpandWindow.activeSelf)
        {
            InitializeCoroutine();
            InitializeWindows(1);
        }
        fieldGenerator.gameObject.SetActive(false);
    }

    public void SetDeactiveOutlineUI()
    {
        while (outlineUIActiveQueue.Count > 0)
        {
            var outline = outlineUIActiveQueue.Dequeue();
            outlineUIQueue.Enqueue(outline);
            outline.gameObject.SetActive(false);
            outline.transform.position = Vector3.zero;
            outline.transform.parent = null;
            outline.size = Vector3.forward * 2;
        }
    }

    private void SetDeactivePoleCollider()
    {
        while (poleActiveQueue.Count > 0)
        {
            var pole = poleActiveQueue.Dequeue();
            pole.parent = null;
            pole.position = Vector3.zero;
            pole.gameObject.SetActive(false);
            poleTransformQueue.Enqueue(pole);
        }
    }
    #endregion

    #region Initialize Functions
    private void Awake()
    {
        Instance = this;

        poleActiveQueue = new Queue<Transform>();
        poleTransformQueue = new Queue<Transform>();

        outlineUIQueue = new Queue<DecalProjector>();
        outlineUIActiveQueue = new Queue<DecalProjector>();
        for (int i = 0; i < 40; i++)
        {
            var pole = Instantiate(FieldPoleTransform, Vector3.zero, Quaternion.identity);
            pole.gameObject.SetActive(false);
            poleTransformQueue.Enqueue(pole);
        }

        for (int i = 0; i < 40; i++)
        {
            var outline = Instantiate(OutlineUI, Vector3.zero, Quaternion.Euler(-90, 0, 0));
            outline.gameObject.SetActive(false);
            outlineUIQueue.Enqueue(outline);
        }
    }

    private void InitializeWindows(int type)
    {
        switch (type)
        {
            case 0:
                CreateWindow.SetActive(false);
                break;
            case 1:
                ExpandWindow.SetActive(false);
                break;
        }
        buildingHash = 0;
    }

    public void InitializeCoroutine()
    {
        if (isExpandField)
        {
            isExpandField = false;
            SetDeactiveOutlineUI();
            SetDeactivePoleCollider();
        }
        if (isMakingField)
            isMakingField = false;
        if (isDeleteField)
            isDeleteField = false;

        if(fieldCoroutine != null)
        {
            StopCoroutine(fieldCoroutine);
            fieldCoroutine = null;
        }
    }

    public void InitializeCropsCount(CropsEntity entity = null)
    {
        if (entity == null)
        {
            CreateCropsCount.text = "0";
        }
        else
        {
            ExpandCurrentCount.text = "0";
            ExpandEntireCount.text = entity.CropsCount.ToString();
        }
    }
    #endregion
}

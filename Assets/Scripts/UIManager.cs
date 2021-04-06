using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Field Generator")]
    [SerializeField] private FieldGenerator fieldGenerator = null;
    [SerializeField] private LayerMask RayMask;
    [Header("Crops UI Setting")]
    [SerializeField] private GameObject CreateWindow = null;
    [SerializeField] private GameObject ExpandWindow = null;
    [SerializeField] private Transform FieldPoleTransform = null;
    [Header("Crops Text Setting")]
    [SerializeField] private Text CreateCropsCount = null;
    [SerializeField] private Text ExpandEntireCount = null;
    [SerializeField] private Text ExpandCurrentCount = null;

    private int buildingHash = 0;
    private bool isRotateField = false;
    private bool isMakingField = false;
    private bool isExpandField = false;
    private Coroutine fieldCoroutine = null;
    private CropsEntity expandEntity = null;
    private Queue<Transform> poleActiveQueue = null;
    private Queue<Transform> poleTransformQueue = null;

    public static UIManager Instance = null;

    private void Awake()
    {
        Instance = this;

        poleActiveQueue = new Queue<Transform>();
        poleTransformQueue = new Queue<Transform>();
        for(int i = 0; i < 40; i++)
        {
            var pole = Instantiate(FieldPoleTransform, Vector3.zero, Quaternion.identity);
            pole.gameObject.SetActive(false);
            poleTransformQueue.Enqueue(pole);
        }
    }

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
        if (!isExpandField)
        {
            fieldGenerator.gameObject.SetActive(true);
            fieldCoroutine = StartCoroutine(fieldGenerator.ExpandFieldCoroutine(expandEntity, isRotateField));
            isExpandField = true;
        }
    }

    public void OnClickStartBuild(bool isExpand)
    {
        fieldCoroutine = StartCoroutine(fieldGenerator.StartBuildCoroutine());
        InitializeWindows(isExpand ? 1 : 0);
        InitializeCoroutine();
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
            SetDeactivePoleCollider();
        }
        else
            return;

        isRotateField = entity.transform.rotation == Quaternion.Euler(90, 0, -45);
        ExpandEntireCount.text = entity.CropsCount.ToString();
        ExpandCurrentCount.text = "0";
        ExpandWindow.SetActive(true);

        var polePositions = entity.PolePositions;
        foreach(var position in polePositions)
        {
            var pole = poleTransformQueue.Dequeue();
            pole.position = RaycastTool.RaycastFromUp(position, RayMask);
            pole.gameObject.SetActive(true);
            pole.parent = entity.transform;
            poleActiveQueue.Enqueue(pole);
        }
    }

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
    }

    private void SetDeactivePoleCollider()
    {
        while (poleActiveQueue.Count > 0)
        {
            var pole = poleActiveQueue.Dequeue();
            poleTransformQueue.Enqueue(pole);
            pole.gameObject.SetActive(false);
            pole.position = Vector3.zero;
            pole.parent = null;
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
                SetDeactivePoleCollider();
                break;
        }
        buildingHash = 0;
    }

    public void InitializeCoroutine()
    {
        if (isExpandField)
            isExpandField = false;
        if (isMakingField)
            isMakingField = false;

        if(fieldCoroutine != null)
        {
            StopCoroutine(fieldCoroutine);
            fieldCoroutine = null;
        }
        fieldGenerator.gameObject.SetActive(false);
    }
}

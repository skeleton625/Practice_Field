using System.Collections;
using UnityEngine.UI;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private FieldGenerator fieldGenerator = null;
    [SerializeField] private GameObject CreateWindow = null;
    [SerializeField] private GameObject ExpendWindow = null;
    [SerializeField] private Text CreateCropsCount = null;
    [SerializeField] private Text ExpendEntireCount = null;
    [SerializeField] private Text ExpendCurrentCount = null;

    private int buildingHash = 0;
    private bool isMakingField = false;
    private bool isExpandField = false;
    private bool isDeleteField = false;
    private Coroutine fieldCoroutine = null;

    public bool IsDeleteField { get => isDeleteField; }
    public static UIManager Instance = null;

    private void Awake()
    {
        Instance = this;
    }

    public void OnClickCreateField()
    {
        if (!isMakingField)
        {
            fieldGenerator.gameObject.SetActive(true);
            fieldCoroutine = StartCoroutine(fieldGenerator.MakeBuildingCoroutine());
            isMakingField = true;
        }
    }

    public void OnClickExpandField()
    {
    }

    public void OnClickStartBuild(bool isExpension)
    {
        InitializeWindows(0);
        StartCoroutine(fieldGenerator.StartBuildCoroutine());
    }

    public void OnClickDeleteField()
    {
        isDeleteField = !isDeleteField;
    }

    public void ChangeCropsCount(int type, int cropsCount)
    {
        switch(type)
        {
            case 0:
                CreateCropsCount.text = cropsCount.ToString();
                break;
            case 1:
                ExpendCurrentCount.text = cropsCount.ToString();
                break;
            case 2:
                ExpendEntireCount.text = cropsCount.ToString();
                break;
        }
    }

    public void SetActiveButtonWindows(int type, int hashCode)
    {
        switch (type)
        {
            case 0:
                if (ExpendWindow.activeSelf)
                    return;

                buildingHash = hashCode;
                CreateWindow.SetActive(true);
                CreateCropsCount.text = "0";
                break;
            case 1:
                if (CreateWindow.activeSelf)
                    return;

                buildingHash = hashCode;
                ExpendWindow.SetActive(true);
                ExpendCurrentCount.text = "0";
                break;
            case 2:
                if (CreateWindow.activeSelf)
                {
                    InitializeCoroutine();
                    InitializeWindows(0);
                }

                else if (ExpendWindow.activeSelf)
                {
                    InitializeCoroutine();
                    InitializeWindows(1);
                }
                break;
        }
    }

    private void InitializeWindows(int type)
    {
        
        switch (type)
        {
            case 0:
                isMakingField = false;
                CreateWindow.SetActive(false);
                break;
            case 1:
                isExpandField = false;
                ExpendWindow.SetActive(false);
                break;
        }

        isDeleteField = false;
        buildingHash = 0;
    }

    private void InitializeCoroutine()
    {
        StopCoroutine(fieldCoroutine);
        fieldCoroutine = null;
    }
}

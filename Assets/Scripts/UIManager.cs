using System.Collections;
using UnityEngine.UI;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private FenseGenerator fenseGenerator = null;
    [SerializeField] private GameObject CreateWindow = null;
    [SerializeField] private GameObject ExpendWindow = null;
    [SerializeField] private Text CreateCropsCount = null;
    [SerializeField] private Text ExpendEntireCount = null;
    [SerializeField] private Text ExpendCurrentCount = null;

    private int buildngHash = 0;
    private int preCropsCount = 0;
    private bool isCreateField = false;
    private bool isExpendField = false;
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
        if (!isCreateField)
        {
            fenseGenerator.gameObject.SetActive(true);
            fieldCoroutine = StartCoroutine(fenseGenerator.UnConnectFieldCoroutine());
            isCreateField = true;
        }
    }

    public void OnClickExpendField()
    {
        if(!isExpendField)
        {
            fenseGenerator.gameObject.SetActive(true);
            fieldCoroutine = StartCoroutine(fenseGenerator.ConnectFieldCoroutine(buildngHash));
            isExpendField = true;
        }
    }

    public void OnClickStartBuild(bool isExpension)
    {
        StartCoroutine(StartBuildCoroutine(isExpension));
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

                buildngHash = hashCode;
                CreateWindow.SetActive(true);
                CreateCropsCount.text = "0";
                break;
            case 1:
                if (CreateWindow.activeSelf)
                    return;

                buildngHash = hashCode;
                ExpendWindow.SetActive(true);
                ExpendCurrentCount.text = "0";
                break;
            case 2:
                if (CreateWindow.activeSelf)
                {
                    DisableCoroutine(false);
                    InitializeWindows(0);
                }
                else if (ExpendWindow.activeSelf)
                {
                    DisableCoroutine(true);
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
                isCreateField = false;
                CreateWindow.SetActive(false);
                break;
            case 1:
                isExpendField = false;
                ExpendWindow.SetActive(false);
                break;
        }

        isDeleteField = false;
        buildngHash = 0;
    }

    private void DisableCoroutine(bool isExpension)
    {
        if (fieldCoroutine != null)
        {
            StopCoroutine(fieldCoroutine);
            fieldCoroutine = null;
            fenseGenerator.DestroyFense(isExpension);
            fenseGenerator.ClearFense();
            fenseGenerator.gameObject.SetActive(false);
            fenseGenerator.transform.position = Vector3.zero;
        }
    }

    private IEnumerator StartBuildCoroutine(bool isExpension)
    {
        if (fenseGenerator.PoleCount <= 3)
            yield break;

        if (isCreateField)
            InitializeWindows(0);
        else if (isExpendField)
            InitializeWindows(1);

        if (fieldCoroutine != null)
        {
            StopCoroutine(fieldCoroutine);
            fieldCoroutine = null;
            fenseGenerator.gameObject.SetActive(false);
            fenseGenerator.transform.position = Vector3.zero;
        }
        yield return StartCoroutine(fenseGenerator.GenerateField(isExpension));
        fenseGenerator.gameObject.SetActive(false);
    }
}

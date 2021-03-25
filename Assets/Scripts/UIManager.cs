using System.Collections;
using UnityEngine.UI;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private FenseGenerator fenseGenerator = null;
    [SerializeField] private GameObject CreateWindow = null;
    [SerializeField] private GameObject ExpendWindow = null;
    [SerializeField] private Text CropsCount = null;
    [SerializeField] private Color PossColor = Color.white;
    [SerializeField] private Color ImpossColor = Color.red;

    private int buildngHash = 0;
    private int preCropsCount = 0;
    private bool isCreateField = false;
    private bool isExpendField = false;
    private bool isDeleteField = false;
    private Coroutine fieldCoroutine = null;

    public bool IsDeleteField { get => isDeleteField; }

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
        if(!isExpendField && preCropsCount < 100)
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

    public void ChangeCropsCount(int cropsCount)
    {
        if (cropsCount > 97)
            CropsCount.color = ImpossColor;
        else
            CropsCount.color = PossColor;
        CropsCount.text = cropsCount.ToString();
        preCropsCount = cropsCount;
    }

    public void SetActiveButtonWindows(int type, int hashCode)
    {
        switch (type)
        {
            case 0:
                if (ExpendWindow.activeSelf)
                {
                    DisableCoroutine(true);
                    InitializeWindows(1);
                }

                if (buildngHash != hashCode)
                {
                    buildngHash = hashCode;
                    CreateWindow.SetActive(true);
                }
                else
                {
                    DisableCoroutine(false);
                    InitializeWindows(0);
                }
                break;
            case 1:
                if (CreateWindow.activeSelf)
                {
                    DisableCoroutine(false);
                    InitializeWindows(0);
                }

                if (buildngHash != hashCode)
                {
                    buildngHash = hashCode;
                    ExpendWindow.SetActive(true);
                }
                else
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

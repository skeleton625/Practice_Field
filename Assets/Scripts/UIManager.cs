using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private FenseGenerator fenseGenerator = null;
    [SerializeField] private GameObject BuildingWindow = null;
    [SerializeField] private GameObject CropsWindow = null;

    private bool isCreateField = false;
    private bool isExpendField = false;
    private Coroutine fieldCoroutine = null;

    public void OnClickCreateField()
    {
        if (isCreateField)
        {
            if (fieldCoroutine != null)
            {
                StopCoroutine(fieldCoroutine);
                fieldCoroutine = null;
            }
            fenseGenerator.DestroyFense(false);
            fenseGenerator.ClearFense();
            fenseGenerator.gameObject.SetActive(false);
            isCreateField = false;
        }
        else
        {
            fenseGenerator.gameObject.SetActive(true);
            fieldCoroutine = StartCoroutine(fenseGenerator.UnConnectFieldCoroutine());
            isCreateField = true;
        }
    }

    public void OnClickStartBuild(bool isExpension)
    {
        StartCoroutine(fenseGenerator.GenerateField(isExpension));
    }

    public void OnClickExpendField()
    {
        if (isExpendField)
        {
            if (fieldCoroutine != null)
            {
                StopCoroutine(fieldCoroutine);
                fieldCoroutine = null;
            }
            fenseGenerator.DestroyFense(true);
            fenseGenerator.ClearFense();
            fenseGenerator.gameObject.SetActive(false);
            isExpendField = false;
        }
        else
            fenseGenerator.gameObject.SetActive(true);
        {
            fieldCoroutine = StartCoroutine(fenseGenerator.ConnectFieldCoroutine());
            isExpendField = true;
        }
    }

    public void OnClickDeleteField()
    {

    }

    public void SetActiveButtonWindows(int type, bool isActive)
    {
        switch(type)
        {
            case 0:
                if (isExpendField)
                {
                    if (fieldCoroutine != null)
                    {
                        StopCoroutine(fieldCoroutine);
                        fieldCoroutine = null;
                    }
                    fenseGenerator.ClearFense();
                    CropsWindow.SetActive(false);
                    fenseGenerator.gameObject.SetActive(false);
                    isExpendField = false;
                }

                BuildingWindow.SetActive(isActive);
                if(!isActive)
                {
                    if(fieldCoroutine != null)
                    {
                        StopCoroutine(fieldCoroutine);
                        fieldCoroutine = null;
                    }
                    fenseGenerator.DestroyFense(false);
                    fenseGenerator.ClearFense();
                    fenseGenerator.gameObject.SetActive(false);
                    isCreateField = false;
                }
                break;
            case 1:
                if (isCreateField)
                {
                    if (fieldCoroutine != null)
                    {
                        StopCoroutine(fieldCoroutine);
                        fieldCoroutine = null;
                    }
                    fenseGenerator.ClearFense();
                    BuildingWindow.SetActive(false);
                    fenseGenerator.gameObject.SetActive(false);
                    isCreateField = false;
                }

                CropsWindow.SetActive(isActive);
                if (!isActive)
                {
                    if (fieldCoroutine != null)
                    {
                        StopCoroutine(fieldCoroutine);
                        fieldCoroutine = null;
                    }
                    fenseGenerator.DestroyFense(true);
                    fenseGenerator.ClearFense();
                    fenseGenerator.gameObject.SetActive(false);
                    isCreateField = false;
                }
                break;
        }
    }
}

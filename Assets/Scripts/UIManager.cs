using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private FenseGenerator fenseGenerator = null;
    [SerializeField] private GameObject ButtonWindow = null;

    private bool isCreateFieldChecked = false;
    private bool isExpendFieldChecked = false;
    private bool isDeleteFieldChecked = false;

    private Coroutine fieldCoroutine = null;

    public void OnClickCreateField()
    {
        if(isCreateFieldChecked)
        {
            isCreateFieldChecked = false;
            StopCoroutine(fieldCoroutine);
            fenseGenerator.ClearFense();
            fenseGenerator.gameObject.SetActive(false);
        }
        else
        {
            fenseGenerator.gameObject.SetActive(true);
            fieldCoroutine = StartCoroutine(fenseGenerator.UnConnectFieldCoroutine());
            isCreateFieldChecked = true;
        }
    }

    public void OnClickStartBuild()
    {

    }

    public void OnClickExpendField()
    {
        if (isExpendFieldChecked)
        {
            isExpendFieldChecked = false;
            StopCoroutine(fieldCoroutine);
            fenseGenerator.ClearFense();
        }
        else
        {
            fieldCoroutine = StartCoroutine(fenseGenerator.ConnectFieldCoroutine());
            isExpendFieldChecked = true;
        }
    }

    public void OnClickDeleteField()
    {

    }

    public void SetActiveButtonWindows(bool isActive)
    {
        ButtonWindow.SetActive(isActive);

        if(!isActive)
        {
            isCreateFieldChecked = false;
            isExpendFieldChecked = false;
            isDeleteFieldChecked = false;
            StopCoroutine(fieldCoroutine);
            fenseGenerator.ClearFense();
            fenseGenerator.gameObject.SetActive(false);
        }
    }
}

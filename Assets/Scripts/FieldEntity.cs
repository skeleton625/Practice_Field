using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldEntity : MonoBehaviour
{
    [SerializeField] private float HumanTimer = 0f;
    [SerializeField] private float MonthTimer = 0f;

    private int farmType = 0;
    private int farmCount = 0;
    [SerializeField] private float humanTime = 0f;
    [SerializeField] private float monthTime = 0f;
    private float farmScale = 0f;
    private bool isWorkStarting = false;

    private TestAI workingAI = null;
    private FieldData fieldData = null;
    private List<Transform> grassTransList = null;

    public void Initialize(FieldData fieldData, List<Transform> grassTransList)
    {
        farmType = 0;
        humanTime = HumanTimer;
        monthTime = MonthTimer;
        farmCount = fieldData.FarmingCount[farmType];
        farmScale = grassTransList.Count * fieldData.FarmingScale[farmType];
        this.fieldData = fieldData;
        this.grassTransList = grassTransList;
    }

    public void SetWorking(TestAI workingAI, bool isWorkStarting)
    {
        this.isWorkStarting = isWorkStarting;
        this.workingAI = workingAI;
        SetWorkingPosition();
    }

    private void SetWorkingPosition()
    {
        var grassTrans = grassTransList[Random.Range(0, grassTransList.Count)];
        workingAI.SetDestination(grassTrans.position);
    }

    private void Update()
    {
        if(isWorkStarting)
        {
            if (monthTime > 0)
                monthTime -= Time.deltaTime;
            else
            {
                farmCount--;
                if(farmCount.Equals(0))
                {
                    Debug.Log(string.Format("FarmType : {0}, Left Farm Scale : {1}", farmType, farmScale));
                    switch (farmType)
                    {
                        case 0:
                            PlantGrass();
                            break;
                        case 1:
                        case 2:
                            GrowGrass();
                            break;
                        case 3:
                            HarvestGrass();
                            break;
                    }

                    farmType = (farmType + 1) % 4;
                    farmCount = fieldData.FarmingCount[farmType];
                    farmScale = grassTransList.Count * fieldData.FarmingScale[farmType];
                }
                monthTime += MonthTimer;
            }

            if (humanTime > 0)
                humanTime -= Time.deltaTime;
            else
            {
                SetWorkingPosition();
                humanTime += HumanTimer;
                farmScale -= workingAI.WorkScale;
            }
        }
    }

    private void PlantGrass()
    {
        foreach (var grassTrans in grassTransList)
        {
            grassTrans.localScale = Vector3.one * fieldData.GrassScale;
            grassTrans.gameObject.SetActive(true);
        }
    }

    private void GrowGrass()
    {
        var scale = Vector3.one * (fieldData.GrassScale * (farmType + 1));
        foreach (var grassTrans in grassTransList)
            grassTrans.localScale = scale;
    }

    private void HarvestGrass()
    {
        foreach (var grassTrans in grassTransList)
            grassTrans.gameObject.SetActive(false);
    }
}

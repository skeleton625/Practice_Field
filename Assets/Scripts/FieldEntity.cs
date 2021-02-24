using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldEntity : MonoBehaviour
{
    [SerializeField] private float HumanTimer = 0f;
    [SerializeField] private float MonthTimer = 0f;

    private int farmType = 0;
    private float humanTime = 0f;
    private float monthTime = 0f;
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
        farmScale = grassTransList.Count * fieldData.FarmingScale[farmType];

        this.fieldData = fieldData;
        this.grassTransList = grassTransList;
        Debug.Log(string.Format("Grass Count : {0}", grassTransList.Count));
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
                farmScale = grassTransList.Count * fieldData.FarmingScale[farmType] - workingAI.WorkScale;
                Debug.Log(string.Format("FarmType : {0}, Left Farm Scale : {1}", farmType, farmScale));
                switch (fieldData.FarmingType[farmType])
                {
                    case FarmType.Type1:
                        SetActiveGrass(true);
                        GrowGrass(1);
                        break;
                    case FarmType.Type2:
                        GrowGrass(2);
                        break;
                    case FarmType.Type3:
                        GrowGrass(3);
                        break;
                    case FarmType.Type4:
                        HarvestGrass();
                        break;
                }
                farmType = (farmType + 1) % 12;
                monthTime += MonthTimer;
            }

            if (humanTime > 0)
                humanTime -= Time.deltaTime;
            else
            {
                switch(fieldData.FarmingType[farmType])
                {
                    case FarmType.Type0:
                    case FarmType.Type1:
                    case FarmType.Type2:
                    case FarmType.Type3:
                    case FarmType.Type4:
                        SetWorkingPosition();
                        break;
                }
                humanTime += HumanTimer;
            }
        }
    }

    private void GrowGrass(int scaleCount)
    {
        var scale = Vector3.one * (fieldData.GrassScale * scaleCount);
        foreach (var grassTrans in grassTransList)
            grassTrans.localScale = scale;
    }

    private void HarvestGrass()
    {
        SetActiveGrass(false);
    }
    
    private void SetActiveGrass(bool isActive)
    {
        foreach (var grassTrans in grassTransList)
            grassTrans.gameObject.SetActive(isActive);
    }
}

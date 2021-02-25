using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldEntity : MonoBehaviour
{
    [SerializeField] private float HumanTimer = 0f;
    [SerializeField] private float MonthTimer = 0f;

    private bool isWorkStarting = false;
    private int monthCount = 0;
    private int harvestedCount = 0;
    private float humanTime = 0f;
    private float monthTime = 0f;

    private TestAI workingAI = null;
    private FieldData fieldData = null;
    private List<Transform> grassList = null;

    public void Initialize(FieldData fieldData, List<Transform> grassList)
    {
        monthCount = 0;         // Test Code
        humanTime = HumanTimer;
        monthTime = MonthTimer;
        this.fieldData = fieldData;
        this.grassList = grassList;
        Debug.Log(string.Format("Grass Count : {0}", grassList.Count));
    }

    public void SetWorking(TestAI workingAI, bool isWorkStarting)
    {
        this.isWorkStarting = isWorkStarting;
        this.workingAI = workingAI;
        SetWorkingPosition();
    }

    private void SetWorkingPosition()
    {
        var grassTrans = grassList[Random.Range(0, grassList.Count)];
        workingAI.SetDestination(grassTrans.position);
    }

    private void Update()
    {
        if(isWorkStarting)
        {
            // Clocking Time -> Test Code
            if (monthTime > 0)
                monthTime -= Time.deltaTime;
            else
            {
                monthCount = (monthCount + 1) % 12;
                
                switch (fieldData.FarmingType[monthCount])
                {
                    case FarmType.Type1:
                        SetActiveGrass(true);
                        GrowGrass(1);
                        CalculateLostScale();
                        break;
                    case FarmType.Type2:
                        GrowGrass(2);
                        CalculateLostScale();
                        break;
                    case FarmType.Type3:
                        GrowGrass(3);
                        CalculateLostScale();
                        break;
                    case FarmType.Type4:
                        CalculateLostScale();
                        SetActiveGrass(false);
                        Debug.Log(string.Format("Harvest Count : {0}, Left Count : {0}", harvestedCount, grassList.Count - harvestedCount));
                        harvestedCount = 0;
                        break;
                }
                monthTime += MonthTimer;
            }

            if (humanTime > 0)
                humanTime -= Time.deltaTime;
            else
            {
                switch(fieldData.FarmingType[monthCount])
                {
                    case FarmType.Type0:
                    case FarmType.Type1:
                    case FarmType.Type2:
                        SetWorkingPosition();
                        break;
                    case FarmType.Type3:
                        if(HarvestGrass())
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
        foreach (var grassTrans in grassList)
            grassTrans.localScale = scale;
    }

    private bool HarvestGrass()
    {
        int harvestCount = workingAI.WorkScale / (int)(MonthTimer * fieldData.FarmingScale[monthCount] / HumanTimer);
        int preHarvestCount = harvestedCount + harvestCount;
        for (int i = harvestedCount; i < preHarvestCount; i++)
        {
            if (i < grassList.Count)
                grassList[i].gameObject.SetActive(false);
            else
            {
                harvestedCount += i;
                return false;
            }
        }
        harvestedCount += harvestCount;
        return true;
    }

    private void CalculateLostScale()
    {
        var scale = grassList.Count * fieldData.FarmingScale[monthCount];
        Debug.Log(string.Format("FarmType : {0}, Left Farm Scale : {1}", monthCount, Mathf.Clamp(scale - workingAI.WorkScale, 0, scale)));
    }

    private void SetActiveGrass(bool isActive)
    {
        foreach (var grassTrans in grassList)
            grassTrans.gameObject.SetActive(isActive);
    }
}

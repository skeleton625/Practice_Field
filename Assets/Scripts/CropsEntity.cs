using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CropsEntity : MonoBehaviour
{
    [SerializeField] private float HumanTimer = 0f;
    [SerializeField] private float MonthTimer = 0f;
    [SerializeField] private float CropSpeed = 0f;

    private bool isWorkStarting = false;
    private int harvestedCount = 0;
    private int monthCount = 0;
    private float cropsScale = 0f;
    private float humanTime = 0f;
    private float monthTime = 0f;
    private float workScale = 0f;

    private TestAI workingAI = null;
    private CropsData cropsData = null;
    private List<Transform> cropsList = null;

    public void Initialize(CropsData cropsData, List<Transform> cropsList)
    {
        humanTime = 0;
        monthTime = 0;
        monthCount = 0;
        cropsScale = cropsData.CropsMaxHeight / cropsData.GrowthCount;
        this.cropsData = cropsData;
        this.cropsList = cropsList;
    }

    public void SetWorking(TestAI workingAI, bool isWorkStarting)
    {
        this.isWorkStarting = isWorkStarting;
        this.workingAI = workingAI;
        SetWorkingPosition();
    }

    private void SetWorkingPosition()
    {
        var grassTrans = cropsList[Random.Range(0, cropsList.Count)];
        workingAI.SetDestination(grassTrans.position);
    }

    private void Update()
    {
        if(isWorkStarting)
        {
            humanTime += Time.deltaTime;
            if (humanTime >= HumanTimer)
            {
                switch (cropsData.MonthType[monthCount])
                {
                    case FarmType.Type1:
                    case FarmType.Type2:
                    case FarmType.Type3:
                        SetWorkingPosition();
                        workScale += workingAI.WorkScale;
                        break;
                    case FarmType.Type4:
                        if (HarvestGrass())
                        {
                            SetWorkingPosition();
                            workScale += workingAI.WorkScale;
                        }
                        break;
                }
                Debug.Log(string.Format("FarmType : {0}, Pre WorkScale : {1}", monthCount, workScale));
                humanTime %= HumanTimer;
            }

            // Clocking Time -> Test Code
            monthTime += Time.deltaTime;
            if(monthTime >= MonthTimer)
            {
                monthCount = (monthCount + 1) % 12;

                switch (cropsData.MonthType[monthCount])
                {
                    case FarmType.Type2:
                        SetActiveGrass(true);
                        StartCoroutine(GrowGrass());
                        break;
                    case FarmType.Type3:
                    case FarmType.Type4:
                        StartCoroutine(GrowGrass());
                        break;
                    case FarmType.Type5:
                        SetActiveGrass(false);
                        var scale = cropsList[0].localScale;
                        scale.y = 0;
                        ChangeCropsScale(scale);
                        harvestedCount = 0;

                        break;
                }
                workScale = 0;
                monthTime %= MonthTimer;
            }
        }
    }

    private void ChangeCropsScale(Vector3 scale)
    {
        foreach (var cropsTrans in cropsList)
            cropsTrans.localScale = scale;
    }

    private IEnumerator GrowGrass()
    {
        var prevScale = cropsList[0].localScale;
        var nextScale = prevScale + Vector3.up * cropsScale;
        var rate = Time.deltaTime * CropSpeed;
        for(float i = 0; i < 1; i += rate)
        {
            var scale = Vector3.Lerp(prevScale, nextScale, i);
            ChangeCropsScale(scale);
           yield return null;
        }

        ChangeCropsScale(nextScale);
    }

    private bool HarvestGrass()
    {
        var preHarvestCount = harvestedCount + workingAI.WorkScale;
        for (int i = harvestedCount; i < preHarvestCount; i++)
        {
            if (i < cropsList.Count)
                cropsList[i].gameObject.SetActive(false);
            else
            {
                harvestedCount += i;
                return false;
            }
        }
        harvestedCount += workingAI.WorkScale;
        return true;
    }

    private void SetActiveGrass(bool isActive)
    {
        foreach (var cropsTrans in cropsList)
            cropsTrans.gameObject.SetActive(isActive);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CropsEntity : MonoBehaviour
{
    #region SerializeField Variable
    [SerializeField] private float HumanTimer = 0f;
    [SerializeField] private float MonthTimer = 0f;
    [SerializeField] private float CropSpeed = 0f;
    #endregion

    #region Private Global Variable
    private bool isWorkStarting = false;
    private int harvestedCount = 0;
    private int monthCount = 0;
    private int nextCropsIndex = 0;
    private float cropsScale = 0f;
    private float humanTime = 0f;
    private float monthTime = 0f;
    private float workScale = 0f;

    private TestAI workingAI = null;
    private CropsData cropsData = null;
    private List<Transform> cropsList = null;
    #endregion

    public int CropsCount { get => cropsList.Count; }

    private void Update()
    {
        if (isWorkStarting)
        {
            if (workingAI.IsArrived())
            {
                if (humanTime >= HumanTimer)
                {
                    switch (cropsData.MonthType[monthCount])
                    {
                        case FarmType.Type1:
                        case FarmType.Type2:
                        case FarmType.Type3:
                            GoWorkPosition();
                            break;
                        case FarmType.Type4:
                            if (HarvestCrops())
                                GoWorkPosition();
                            break;
                    }
                    workScale += workingAI.WorkScale;
                    Debug.Log(string.Format("FarmType : {0}, Pre WorkScale : {1}", cropsData.MonthType[monthCount], workScale));
                    humanTime %= HumanTimer;
                }
                else
                    humanTime += Time.deltaTime;
            }    
        }

        // Clocking Time -> Test Code
        monthTime += Time.deltaTime;
        if (monthTime >= MonthTimer)
        {
            monthCount = (monthCount + 1) % 12;

            nextCropsIndex = 0;
            switch (cropsData.MonthType[monthCount])
            {
                case FarmType.Type2:
                    SetActiveCrops(true);
                    StartCoroutine(GrowCrops());
                    break;
                case FarmType.Type3:
                case FarmType.Type4:
                    StartCoroutine(GrowCrops());
                    break;
                case FarmType.Type5:
                    SetActiveCrops(false);
                    var scale = cropsList[0].localScale;
                    scale.y = 0;
                    SetCropsScale(scale);
                    harvestedCount = 0;

                    break;
            }
            workScale = 0;
            monthTime %= MonthTimer;
        }
    }

    #region Public Functions
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
        this.workingAI = workingAI;
        GoWorkPosition();
        StartCoroutine(StartWorkCoroutine());
    }

    public void AddCrops(List<Transform> cropsList)
    {
        this.cropsList.AddRange(cropsList);
    }
    #endregion

    #region Private Functions
    private void SetCropsScale(Vector3 scale)
    {
        foreach (var cropsTrans in cropsList)
            cropsTrans.localScale = scale;
    }

    private void SetActiveCrops(bool isActive)
    {
        foreach (var cropsTrans in cropsList)
            cropsTrans.gameObject.SetActive(isActive);
    }

    private void GoWorkPosition()
    {
        nextCropsIndex = (nextCropsIndex + 1) % cropsList.Count;
        workingAI.SetDestination(cropsList[nextCropsIndex].position);
    }

    private bool HarvestCrops()
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
    #endregion

    #region Coroutine Functions
    private IEnumerator GrowCrops()
    {
        var prevScale = cropsList[0].localScale;
        var nextScale = prevScale + Vector3.up * cropsScale;
        var rate = Time.deltaTime * CropSpeed;
        for (float i = 0; i < 1; i += rate)
        {
            var scale = Vector3.Lerp(prevScale, nextScale, i);
            SetCropsScale(scale);
            yield return null;
        }

        SetCropsScale(nextScale);
    }

    private IEnumerator StartWorkCoroutine()
    {
        while (!workingAI.IsArrived())
            yield return null;
        isWorkStarting = true;
    }
    #endregion
}

﻿using UnityEngine;
using GPUInstancer;

public enum FarmType { Type1, Type2, Type3, Type4, Type5, None };

[CreateAssetMenu(fileName = "New Field Data", menuName = "Datas/FieldData")]
public class CropsData : ScriptableObject
{
    public GPUInstancerPrefab Visual = null;
    public FarmType[] MonthType = null;
    public float CropsWorkScale = 1;
    public float CropsMaxHeight = 0;
    public int GrowthCount = 0;
}

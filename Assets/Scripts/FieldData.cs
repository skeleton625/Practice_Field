using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum FarmType { Type0, Type1, Type2, Type3, Type4, None };

[CreateAssetMenu(fileName = "New Field Data", menuName = "Datas/FieldData")]
public class FieldData : ScriptableObject
{
    public FarmType[] FarmingType = null;
    public int[] FarmingScale = null;
    public float GrassScale;
}

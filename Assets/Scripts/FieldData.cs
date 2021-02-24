using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Field Data", menuName = "Datas/FieldData")]
public class FieldData : ScriptableObject
{
    public int[] FarmingCount = null;
    public float[] FarmingScale = null;
    public float GrassScale;
}

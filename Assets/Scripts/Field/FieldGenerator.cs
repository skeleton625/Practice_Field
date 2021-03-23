using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldGenerator : MonoBehaviour
{
    [SerializeField] private int Scale = 0;
    [SerializeField] private int LimitSize = 0;
    [SerializeField] private Transform FieldBody= null;
    [SerializeField] private Transform FieldSubBody = null;

    [SerializeField] private Transform Rice = null;
    [SerializeField] private Transform RiceBlock = null;
    [SerializeField] private CropsEntity RiceEntity = null;
    [SerializeField] private CropsData fieldData = null;
    [SerializeField] private TestAI workingAI = null;

    private Camera mainCamera = null;
    private Vector3 fieldScale = Vector3.zero;
    private Vector3 startPosition = Vector3.zero;

    private void Start()
    {
        mainCamera = Camera.main;
        fieldScale = FieldSubBody.localScale;
    }

    private void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            startPosition = RaycastMouseRay();
            startPosition.x = (int)startPosition.x / Scale * Scale + 1;
            startPosition.z = (int)startPosition.z / Scale * Scale + 1;
            FieldBody.transform.position = startPosition;
            FieldSubBody.gameObject.SetActive(true);
        }

        if(Input.GetMouseButton(0))
        {
            var nextPosition = RaycastMouseRay();
            fieldScale.x = nextPosition.x - startPosition.x;
            fieldScale.z = nextPosition.z - startPosition.z;
            var rotationX = fieldScale.z < 0 ? 180 : 0;
            var rotationZ = fieldScale.x < 0 ? 180 : 0;
            fieldScale.x = Mathf.Clamp(Mathf.Abs((int)fieldScale.x / Scale * Scale) + Scale, Scale, LimitSize);
            fieldScale.z = Mathf.Clamp(Mathf.Abs((int)fieldScale.z / Scale * Scale) + Scale, Scale, LimitSize);
            FieldBody.rotation = Quaternion.Euler(rotationX, 0, rotationZ);
            FieldSubBody.localScale = fieldScale;
        }

        if(Input.GetMouseButtonUp(0))
        {
            FieldSubBody.gameObject.SetActive(false);
            GenerateGrassField();
        }
    }

    private Vector3 RaycastMouseRay()
    {
        var cameraRay = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(cameraRay.origin, cameraRay.direction, out RaycastHit hit, 1000f))
            return hit.point;
        return Vector3.zero;
    }

    private void GenerateGrassField()
    {
        var entity = Instantiate(RiceEntity, startPosition, Quaternion.identity);
        var fieldTransList = new List<Transform>();
        for(int x = 0; x < fieldScale.x; x += Scale)
        {
            var tmpList = new List<Transform>();
            for(int z = 0; z < fieldScale.z; z += Scale)
            {
                var position = startPosition + new Vector3(x, 0, z);
                var rice = Instantiate(Rice, position, FieldBody.rotation);
                var block = Instantiate(RiceBlock, position, Quaternion.identity);
                block.SetParent(entity.transform);
                rice.SetParent(entity.transform);
                rice.gameObject.SetActive(false);
                tmpList.Add(rice);
            }

            if ((x % 2).Equals(1))
                tmpList.Reverse();
            fieldTransList.AddRange(tmpList);
        }
        entity.transform.rotation = FieldBody.rotation;

        entity.Initialize(fieldData, fieldTransList);
        entity.SetWorking(workingAI, true);
    }
}

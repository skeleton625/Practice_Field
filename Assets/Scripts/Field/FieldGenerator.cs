using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldGenerator : MonoBehaviour
{
    [SerializeField] private int Scale = 0;
    [SerializeField] private int LimitSize = 0;
    [SerializeField] private Transform FieldBody = null;
    [SerializeField] private Transform FieldSubBody = null;
    [SerializeField] private Transform TestParent = null;

    [SerializeField] private Transform Rice = null;
    [SerializeField] private Transform RiceBlock = null;
    [SerializeField] private CropsEntity RiceEntity = null;
    [SerializeField] private CropsData fieldData = null;

    private bool isRotate = false;
    private bool isSelected = false;
    private Camera mainCamera = null;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            isSelected = true;
        if (Input.GetMouseButtonUp(0))
            isSelected = false;
        if(Input.GetKeyDown(KeyCode.R) && !isSelected)
            isRotate = !isRotate;
        if(Input.GetKeyDown(KeyCode.E))
            StartCoroutine(GenerateFieldCoroutine());
    }

    private Vector3 RaycastMouseRay()
    {
        var cameraRay = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(cameraRay.origin, cameraRay.direction, out RaycastHit hit, 1000f))
            return hit.point;
        return Vector3.zero;
    }

    private IEnumerator GenerateFieldCoroutine()
    {
        Vector3 startPosition = Vector3.zero;
        while(!isSelected)
        {
            startPosition = RaycastMouseRay();
            if (isRotate)
            {
                FieldBody.SetParent(TestParent);
                startPosition = Quaternion.Euler(0, -45, 0) * startPosition;
            }
            else
                FieldBody.SetParent(null);
            startPosition.x = (int)startPosition.x / Scale * Scale + 1;
            startPosition.z = (int)startPosition.z / Scale * Scale + 1;
            FieldBody.localPosition = startPosition;
            FieldBody.localRotation = Quaternion.Euler(0, 0, 0);

            yield return null;
        }

        Vector3 fieldScale = Vector3.one;
        Vector3 localRotation = Vector3.zero;
        while (isSelected)
        {
            var nextPosition = (isRotate ? Quaternion.Euler(0, -45, 0) : Quaternion.identity) * RaycastMouseRay();
            fieldScale.x = nextPosition.x - startPosition.x;
            fieldScale.z = nextPosition.z - startPosition.z;

            localRotation.x = fieldScale.z < 0 ? 180 : 0;
            localRotation.z = fieldScale.x < 0 ? 180 : 0;
            var scaleX = Mathf.Clamp(Mathf.Abs((int)fieldScale.x / Scale * Scale) + Scale, Scale, LimitSize);
            var scaleZ = Mathf.Clamp(Mathf.Abs((int)fieldScale.z / Scale * Scale) + Scale, Scale, LimitSize);
            fieldScale.x = scaleX;
            fieldScale.z = scaleZ;

            FieldBody.localRotation = Quaternion.Euler(localRotation);
            FieldSubBody.localScale = fieldScale;
            yield return null;
        }


        var clone = Instantiate(FieldSubBody, FieldSubBody.position, FieldSubBody.rotation);
        FieldSubBody.transform.localScale = new Vector3(Scale, 1, Scale);
        FieldBody.transform.position = Vector3.zero;
    }
}
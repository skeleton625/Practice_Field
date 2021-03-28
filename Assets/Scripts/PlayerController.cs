using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private UIManager uiManager = null;
    [SerializeField] private TerrainGenerator Generator = null;

    private Camera mainCamera = null;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if(Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, 1000f))
            {
                if (hit.transform.CompareTag("Building"))
                {
                    uiManager.SetActiveButtonWindows(0, hit.transform.GetHashCode());
                }
                if (hit.transform.CompareTag("Crops"))
                {
                    if(uiManager.IsDeleteField)
                    {
                        Destroy(hit.transform.gameObject);
                    }
                    else
                    {
                        uiManager.ChangeCropsCount(true, hit.transform.GetComponent<CropsEntity>().CropsCount);
                        uiManager.SetActiveButtonWindows(1, hit.transform.GetHashCode());
                    }
                }
            }    
        }
    }
}

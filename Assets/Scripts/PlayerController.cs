using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private UIManager uiManager = null;
    [SerializeField] private TerrainGenerator Generator = null;

    private int houseHashCode = 0;
    private int cropsHashCode = 0;
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
                    if (hit.transform.GetHashCode().Equals(houseHashCode))
                    {
                        uiManager.SetActiveButtonWindows(0, false);
                        houseHashCode = 0;
                    }
                    else
                    {
                        uiManager.SetActiveButtonWindows(0, true);
                        houseHashCode = hit.transform.GetHashCode();
                    }
                }

                if (hit.transform.CompareTag("Crops"))
                {
                    if (hit.transform.GetHashCode().Equals(cropsHashCode))
                    {
                        uiManager.SetActiveButtonWindows(1, false);
                        cropsHashCode = 0;
                    }
                    else
                    {
                        uiManager.SetActiveButtonWindows(1, true);
                        cropsHashCode = hit.transform.GetHashCode();
                    }
                }
            }    
        }
    }
}

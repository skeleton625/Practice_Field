using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private UIManager uiManager = null;
    [SerializeField] private TerrainGenerator Generator = null;

    private int hashCode = 0;
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
                if(hit.transform.CompareTag("Building"))
                {
                    if (hit.transform.GetHashCode().Equals(hashCode))
                    {
                        uiManager.SetActiveButtonWindows(false);
                        hashCode = 0;
                    }
                    else
                    {
                        uiManager.SetActiveButtonWindows(true);
                        hashCode = hit.transform.GetHashCode();
                    }
                }
            }    
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private LayerMask rayMask = default;
    [SerializeField] private UIManager uiManager = null;
    [SerializeField] private TerrainGenerator Generator = null;

    private void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            if(RaycastTool.RaycastFromMouse(out RaycastHit hit, rayMask))
            {
                if (hit.transform.CompareTag("Building"))
                    uiManager.SetActiveCreateWindow();
                else if (hit.transform.CompareTag("Crops"))
                    uiManager.SetActiveExpandWindow(hit.transform.GetComponent<CropsEntity>());
            }    
        }

        if(Input.GetKeyDown(KeyCode.Escape))
            uiManager.SetDeactiveWindows();
    }
}

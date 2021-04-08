using UnityEngine.EventSystems;
using UnityEngine;

public class RaycastTool
{
    private static Camera mainCamera = null;
    private static EventSystem eventSystem = null;

    public static void Initialize()
    {
        mainCamera = Camera.main;
        eventSystem = EventSystem.current;
    }

    public static bool RaycastFromMouse(ref Vector3 resultPosition, LayerMask rayMask)
    {
        if (eventSystem.IsPointerOverGameObject())
            return false;

        var cameraRay = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(cameraRay.origin, cameraRay.direction, out RaycastHit hit, 1000f, rayMask))
        {
            resultPosition = hit.point;
            return true;
        }
        return false;
    }

    public static bool RaycastFromMouse(out RaycastHit hit, LayerMask rayMask)
    {
        if (eventSystem.IsPointerOverGameObject())
        {
            hit = new RaycastHit();
            return false;
        }

        var cameraRay = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(cameraRay.origin, cameraRay.direction, out hit, 1000f, rayMask))
            return true;
        return false;
    }

    public static bool RaycastFromUp(Vector3 position, out RaycastHit hit, LayerMask rayMask)
    {
        position.y = 100f;
        if (Physics.Raycast(position, -Vector3.up, out hit, 1000f, rayMask))
            return true;
        return false;
    }

    public static Vector3 RaycastFromUp(Vector3 position, LayerMask rayMask)
    {
        position.y = 100f;
        if (Physics.Raycast(position, -Vector3.up, out RaycastHit hit, 200f, rayMask))
            return hit.point;
        return Vector3.zero;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectCollider : MonoBehaviour
{
    [SerializeField] private string CollidTag = "";
    [SerializeField] private Vector3 InitScale = Vector3.zero;

    public bool IsUnCollid { get; private set; }

    public void ClearCollider(Transform parent)
    {
        IsUnCollid = true;
        transform.parent = parent;
        transform.localPosition = new Vector3(0, -.5f, 0);
        transform.localRotation = Quaternion.identity;
        transform.localScale = InitScale;
    }

    private void OnEnable()
    {
        IsUnCollid = true;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(CollidTag))
        {
            IsUnCollid = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(CollidTag))
        {
            IsUnCollid = true;
        }
    }
}

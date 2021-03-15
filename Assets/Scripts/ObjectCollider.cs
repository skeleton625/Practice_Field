using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectCollider : MonoBehaviour
{
    [SerializeField] private string CollidTag = "";

    public bool IsUnCollid { get; private set; }

    private void OnEnable()
    {
        IsUnCollid = true;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(CollidTag))
            IsUnCollid = false;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(CollidTag))
            IsUnCollid = true;
    }
}

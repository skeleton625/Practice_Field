using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class TestAI : MonoBehaviour
{
    [SerializeField] private float workScale = 0f;

    private NavMeshAgent agent = null;

    public float WorkScale{ get => workScale; }

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public void SetDestination(Vector3 position)
    {
        agent.SetDestination(position);
    }
}

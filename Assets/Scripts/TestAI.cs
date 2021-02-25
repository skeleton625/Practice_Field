using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class TestAI : MonoBehaviour
{
    [SerializeField] private int workScale = 0;

    private NavMeshAgent agent = null;

    public int WorkScale{ get => workScale; }

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public void SetDestination(Vector3 position)
    {
        agent.SetDestination(position);
    }
}

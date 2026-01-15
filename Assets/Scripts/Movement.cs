using UnityEngine;
using UnityEngine.AI;
public class Movement : MonoBehaviour {
    //public Transform[] waypoints;
    //private int currentWaypointIndex = 0;
    Vector3 destination;
    private NavMeshAgent agent;

    public void StartDrag() {
        agent.enabled = false;
    }
    public void StopDrag() {
        agent.enabled = true;
        MoveToNextCheckpoint();
    }
    void Start() {

        agent = GetComponent<NavMeshAgent>();
        
        agent.autoBraking = false;
        MoveToNextCheckpoint();
        //if (waypoints.Length > 0) {
        //    MoveToNextCheckpoint();
        //}
    }

    void Update() {
    if (!agent.pathPending && agent.remainingDistance < 0.5f) {
            MoveToNextCheckpoint();
        }    
    }

    void MoveToNextCheckpoint() {
        Vector3 newDestination = Vector3.zero;
        newDestination.x = Random.Range(-4, 4);
        newDestination.y = Random.Range(1, 8);
        agent.destination = newDestination;
        //if (waypoints.Length == 0) return;

        //agent.destination = waypoints[currentWaypointIndex].position;

        //currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
    }
}

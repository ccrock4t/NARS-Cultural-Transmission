using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teacher : MonoBehaviour
{
    [SerializeField]
    public Transform[] waypoints;

    private int waypointIndex = 0;
    private float speed = 5;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Vector3.Distance(this.transform.position, this.waypoints[waypointIndex].position) > 0.5f)
        {
            // not yet at the waypoint
            float distance = speed * Time.deltaTime;
            this.transform.LookAt(this.waypoints[waypointIndex].position);
            this.transform.position = Vector3.MoveTowards(this.transform.position, this.waypoints[waypointIndex].position, distance);
        }
        else
        {
            // at the waypoint, target next waypoint
            waypointIndex = (waypointIndex + 1) % waypoints.Length;
            
        }
    }
}

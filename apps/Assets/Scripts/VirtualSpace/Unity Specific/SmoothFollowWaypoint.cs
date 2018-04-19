using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothFollowWaypoint : MonoBehaviour {
    public Transform Waypoints;
    private Transform[] _waypoints;
    private Transform _transform;
    private int _currentWaypointTarget = 0;
    public float MetersPerSecond = .1f;

    void Start () {
        _transform = transform;
        var waypoints = Waypoints.GetComponentsInChildren<Transform>();
        _waypoints = new Transform[waypoints.Length - 1];
        for (var i = 1; i < waypoints.Length; i++)
        {
            _waypoints[i - 1] = waypoints[i];
        }
	}
	
	void Update () {
        var currentTarget = _waypoints[_currentWaypointTarget];
        var direction = currentTarget.position - transform.position;
        direction.y = 0;

        if (direction.magnitude < 0.3f)
        {
            _currentWaypointTarget = (_currentWaypointTarget + 1) %_waypoints.Length;
            currentTarget = _waypoints[_currentWaypointTarget];
            direction = currentTarget.position - _transform.position;
        }

        //Debug.Log("Going towards " + _currentWaypointTarget + ": " + direction.magnitude);

        var movement = direction.normalized * MetersPerSecond;
        transform.position += movement * Time.deltaTime;
        Debug.DrawRay(_transform.position, direction);

        transform.forward = (Vector3.Lerp(transform.forward, direction, 3 * Time.deltaTime));
        //_transform.LookAt(direction);
	}
}

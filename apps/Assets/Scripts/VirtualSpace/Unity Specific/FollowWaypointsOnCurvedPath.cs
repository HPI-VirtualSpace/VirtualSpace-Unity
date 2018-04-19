using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(SpeedCalculator))]
public class FollowWaypointsOnCurvedPath : MonoBehaviour {
    public Transform Waypoints;
    public float percentsPerSecond = 0.02f; // %2 of the path moved per second
    [SerializeField]
    float _currentPathPercent = 0.0f; //min 0, max 1
    private Transform[] _waypointsArray;
    private SpeedCalculator _speedCalculator;

    void Start()
    {
        _waypointsArray = new Transform[Waypoints.childCount + 1];
        for (int i = 0; i < Waypoints.childCount; i++)
        {
            _waypointsArray[i] = Waypoints.GetChild(i);
        }
        // close the loop
        _waypointsArray[Waypoints.childCount] = Waypoints.GetChild(0);
        _speedCalculator = GetComponent<SpeedCalculator>();
    }

    void Update()
    {
#if !UNITY_EDITOR
        _currentPathPercent += (percentsPerSecond * Time.deltaTime) % 1;
        iTween.PutOnPath(gameObject, _waypointsArray, _currentPathPercent);
        
        var direction = _speedCalculator.Velocity.normalized;
        direction.y = 0;

        transform.forward = direction;
#endif
    }

    void OnDrawGizmos()
    {
        //Visual. Not used in movement
        iTween.DrawPath(_waypointsArray);
    }
}

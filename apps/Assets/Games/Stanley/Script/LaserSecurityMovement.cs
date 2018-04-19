using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserSecurityMovement : MonoBehaviour
{
    public Transform from;
    public Transform to;
    private Transform _transform;
    private Transform _lerpLaserTarget;
    private Vector3 CurrentGoal { get { return _targetIsTo ? to.position : from.position; } }
    public float TravelTime = 2f;

    private bool _targetIsTo;

	void Start ()
	{
	    _transform = transform;

        _lerpLaserTarget = new GameObject().transform;
	    //_lerpLaserTarget.parent = _transform;

	    _lerpLaserTarget.position = from.position;

        _targetIsTo = false;

	    MoveToWaypoint();

	}

	public void MoveToWaypoint () {
	    _targetIsTo = !_targetIsTo;
        //Debug.Log("Moving to next");
	    iTween.MoveTo(_lerpLaserTarget.gameObject,  
                iTween.Hash("position", CurrentGoal, 
                    "time", TravelTime, 
                    "easetype", "linear",
	                "oncompletetarget", gameObject,
                    "oncomplete", "MoveToWaypoint")
            );
    }

    void Update()
    {
        var direction = _lerpLaserTarget.position - _transform.position;
        _transform.forward = direction;
    }
}

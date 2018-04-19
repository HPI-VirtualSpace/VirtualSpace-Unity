using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToadFlyingInFrontOfCam : MonoBehaviour {

    private Transform _cam;

	void Start () {
        _cam = Camera.main.transform;
	}
	
	void Update () {
        var pos = _cam.position;
        pos.y = 0f;
        transform.position = pos;
        var forward = _cam.forward;
        forward.y = 0f;
        transform.forward = forward;
	}
}

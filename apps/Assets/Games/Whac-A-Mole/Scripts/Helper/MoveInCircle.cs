using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveInCircle : MonoBehaviour {

    public float circleSize = 2f;
    public Vector3 circleCenter = Vector3.zero;
    public float secondsPerCircle = 2f;

	void Start () {
		
	}
	
	void Update () {
        float t = (Time.time % secondsPerCircle) / secondsPerCircle * 2 * Mathf.PI;
        transform.position = circleCenter + new Vector3(Mathf.Sin(t), 0f, Mathf.Cos(t)) * circleSize;
	}
}

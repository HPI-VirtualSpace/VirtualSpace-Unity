using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayTestFollow : MonoBehaviour {

    public Transform Camera;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        transform.position = new Vector3(Camera.position.x, transform.position.y, Camera.position.z);
	}
}

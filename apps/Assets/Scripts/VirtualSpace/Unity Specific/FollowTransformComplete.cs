using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowTransformComplete : MonoBehaviour {

    public Transform transformToFollow;


	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        transform.position = transformToFollow.position;
        var rotation = transform.rotation.eulerAngles;
        rotation.x = rotation.z = 0;
        transform.rotation = Quaternion.Euler(rotation);
    }
}

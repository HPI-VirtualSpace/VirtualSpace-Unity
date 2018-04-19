using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIndicatorFollow : MonoBehaviour {

    private float _yOff;

    public Transform Follow;

	void Start () {
        _yOff = (transform.position - Follow.position).y;
	}
	
	void Update () {
        
        transform.position = Follow.position + new Vector3(0f, _yOff, 0f);

	}
}


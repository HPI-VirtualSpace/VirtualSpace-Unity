using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PacmanCherryFaceTowards : MonoBehaviour
{
    public float BackOffset;

    private Transform _camTransform;

	void Start ()
	{
	    var cammain = Camera.main;
        if(cammain != null)
            _camTransform = cammain.transform;
	}
	
	void Update () {
	    if (_camTransform != null)
	    {
	        transform.LookAt(_camTransform);
	        transform.localPosition = Vector3.zero - transform.forward * BackOffset;
	    }
	    else
	    {
	        Start();
	    }
	}
}

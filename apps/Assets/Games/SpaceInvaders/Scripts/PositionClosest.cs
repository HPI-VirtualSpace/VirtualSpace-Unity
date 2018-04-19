using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionClosest : MonoBehaviour
{

    public Vector3 V;
    public BoxCollider B;

	void Start () {
		
	}
	
	void Update ()
	{
	    transform.position = B.bounds.ClosestPoint(B.transform.position + V);
	}
}

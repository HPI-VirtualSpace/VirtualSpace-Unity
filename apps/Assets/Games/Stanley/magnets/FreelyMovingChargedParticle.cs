using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FreelyMovingChargedParticle : ChargedParticle
{
    public float Mass = 1;
    public Rigidbody Rigidbody;

	void Start ()
	{
	    Rigidbody = GetComponent<Rigidbody>();
	    Rigidbody.mass = Mass;
	    Rigidbody.useGravity = false;
	}
}

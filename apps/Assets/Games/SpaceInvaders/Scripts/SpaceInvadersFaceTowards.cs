using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceInvadersFaceTowards : MonoBehaviour {

    public Transform Target;
    
	
	// Update is called once per frame
	void Update () {
        var target = Target.position;
        target.y = transform.position.y;
        transform.LookAt(target);
	}
}

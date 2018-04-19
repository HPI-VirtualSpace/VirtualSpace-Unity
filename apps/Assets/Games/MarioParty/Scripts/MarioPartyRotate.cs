using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarioPartyRotate : MonoBehaviour
{

    public float AnglePerSecond;
    
	void Update () {
		transform.Rotate(Vector3.up, Time.deltaTime * AnglePerSecond);
	}
}

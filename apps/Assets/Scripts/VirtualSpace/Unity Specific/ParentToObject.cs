using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParentToObject : MonoBehaviour {
    public GameObject ViveSystem;
    public string ObjectName;
    public Vector3 PositionOffset;
    public Vector3 RotationOffset;
    private bool found;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        

        if (found) return;

        var refObject = ViveSystem.transform.Find(ObjectName);

        if (refObject == null) return;

        found = true;

        transform.parent = refObject;

        transform.localPosition = PositionOffset;
        transform.localRotation = Quaternion.Euler(RotationOffset);
    }
}

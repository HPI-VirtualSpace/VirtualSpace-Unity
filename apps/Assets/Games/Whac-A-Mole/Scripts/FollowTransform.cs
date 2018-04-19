using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowTransform : MonoBehaviour {
    public Transform follow;

    public bool UseInitialY;
    public float InitialY;

	void Start () {
		if (UseInitialY)
        {
            InitialY = transform.position.y;
        }
	}
	
	void Update () {
        var followPosition = follow.position;
        if (UseInitialY)
        {
            followPosition.y = InitialY;
        }
        transform.position = followPosition;
	}
}

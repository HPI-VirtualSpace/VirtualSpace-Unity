using UnityEngine;
using System.Collections;

public class Follow : MonoBehaviour {

    public Transform ToFollow;
    public bool UseLocal;
    public bool FollowRotation;
    public float PositionFactor = 1f;

    private Vector3 _startPos;
    private Quaternion _startRot;

	// Use this for initialization
	void Start () {
        _startPos = transform.localPosition;
        _startRot = transform.localRotation;
    }
	
	// Update is called once per frame
	void Update () {
        if (!UseLocal)
        {
            transform.position = ToFollow.position*PositionFactor;
            if (FollowRotation) transform.rotation = ToFollow.rotation;
        } else
        {
            transform.localPosition = _startPos + ToFollow.localPosition*PositionFactor;
            if (FollowRotation) transform.localRotation = _startRot * ToFollow.localRotation;
        }
	}
}

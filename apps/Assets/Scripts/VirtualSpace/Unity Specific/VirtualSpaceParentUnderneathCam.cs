using UnityEngine;
using UnityEngine.PostProcessing;

public class VirtualSpaceParentUnderneathCam : MonoBehaviour {

    public Transform ParentTarget;

	void Start ()
	{
	    if (ParentTarget == null)
	        ParentTarget = Camera.main.transform;
        transform.parent = ParentTarget;
        transform.localRotation = Quaternion.identity;
        transform.localPosition = Vector3.zero;
	}
}

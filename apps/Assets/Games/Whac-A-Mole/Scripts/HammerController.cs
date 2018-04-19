using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HammerController : MonoBehaviour
{
    public Transform controllerTarget;
    private Rigidbody _myRigidbody;

    private LinkedList<Vector3> _positions = new LinkedList<Vector3>();
    private Vector3 _sumOfDifferences = Vector3.zero;
    public int NumPositionsForSpeedCalculation = 10;

    public Vector3 Velocity
    {
        get
        {
            if (_positions.Count < 2) return Vector3.zero;
            return _sumOfDifferences / (_positions.Count - 1) / Time.deltaTime;
        }
    }

    void Start () {
	    transform.parent = controllerTarget;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
	 }
	
	// Update is called once per frame
	void Update ()
	{
	    while (_positions.Count >= NumPositionsForSpeedCalculation)
	    {
	        var lastValue = _positions.Last.Value;
	        var beforeLastValue = _positions.Last.Previous.Value;
	        var diff = beforeLastValue - lastValue;
	        _sumOfDifferences -= diff;
	        _positions.RemoveLast();
	    }
	    _positions.AddFirst(transform.position);
	    if (_positions.Count > 1)
	    {
	        var firstValue = _positions.First.Value;
	        var afterFirstValue = _positions.First.Next.Value;
	        var diff = firstValue - afterFirstValue;
	        _sumOfDifferences += diff;
	    }

	    //Debug.Log("hammer speed: " + Velocity);
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PacmanWallsDown : MonoBehaviour
{

    private bool _down;
    private float _acc;

    public Vector3 DownOffset;
    public bool InstaDown = true;
    private Vector3 _startLocalPosition;

	void Start ()
	{
	    _startLocalPosition = transform.localPosition;
	    _acc = 1f;

	}
	
	void Update ()
	{
        var incr = _down ? -(3 * Time.deltaTime) : Time.deltaTime;
	    if (InstaDown)
	        incr *= 1000f;
	    _acc += incr;
        _acc = Mathf.Clamp01(_acc);
	    var position = Vector3.Lerp(_startLocalPosition + DownOffset * (InstaDown ? 1000f : 1f), _startLocalPosition, _acc);
	    transform.localPosition = position;
	}

    public void Move(bool down)
    {
        _down = down;
    }
}

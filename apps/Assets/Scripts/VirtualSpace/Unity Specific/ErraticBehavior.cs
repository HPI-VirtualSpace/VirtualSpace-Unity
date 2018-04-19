using UnityEngine;
using System.Collections;

public class ErraticBehavior : MonoBehaviour
{

    public Bounds Bounds;
    public float Speed = 2f;

    private Vector3 _direction;
    private float _lasttime;
    //private Vector3 _lastPos;
    private Vector3 _lastFrom;
    private float _total;

	// Use this for initialization
	void Start ()
	{
        NewTo();
	}

    private void NewTo()
    {
        _total = Random.Range(1f, 3f);
        _lasttime = Time.time;
        //_lastPos = transform.position;
        var randBounds = new Vector3(
            Random.Range(-0.5f * Bounds.size.x, 0.5f * Bounds.size.x),
            0,
            Random.Range(-0.5f * Bounds.size.z, 0.5f * Bounds.size.z));
        randBounds += Bounds.center;
        //_toPos = randBounds;
        _direction = (randBounds - transform.position).normalized;
        _direction.y = 0f;
        _lastFrom = new Vector3(0f,-100f,0f);

    }

    public void ChangeDirection(Vector3 directionTo, Vector3 infoFrom)
    {
        if (_lastFrom == infoFrom)
            return;
        _direction = -directionTo.normalized;
        _direction.y = 0f;
        _lastFrom = infoFrom;
    }

    // Update is called once per frame
    void Update () {
        var reachedTime = (Time.time - _lasttime) >= _total;
        if(reachedTime || !Bounds.Contains(transform.position))
            NewTo();
        //if(reachedToPos)
        //    NewTo();
        //var factor = (Time.time - _lasttime)/1f;
        //var pos = Vector3.Lerp (_lastPos, _toPos, factor);
        //transform.position = pos;
        

        var pos = transform.position;
        pos += _direction*Speed*Time.deltaTime;
        //if (!Bounds.Contains(pos))
        //{
        //    pos = Bounds.ClosestPoint(transform.position);
        //}
        transform.position = pos;
    }
}

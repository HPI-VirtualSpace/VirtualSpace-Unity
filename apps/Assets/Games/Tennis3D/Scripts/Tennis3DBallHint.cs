using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VirtualSpace;

public class Tennis3DBallHint : MonoBehaviour
{

    public VirtualSpaceHandler Handler;
    public float UpdateRate;
    public ParticleSystem Particles;
    public int Guesses = 10;
    public float Range = 1f;

    private float _nextUpdate;
    private float _y;

	void Start ()
	{
	    _y = transform.position.y;
	}
	
	void Update ()
	{
	    if (Time.time < _nextUpdate)
	        return;

	    _nextUpdate = Time.time + UpdateRate;

	    if (!VirtualSpaceCore.Instance.IsRegistered())
            return;

	    Vector3 center;
	    List<Vector3> area;
	    Handler.SpaceAtTimeWithSafety(UpdateRate, Handler.Settings.Safety, out area, out center);

        if(area.Count < 3)
            return;
	    
	    var xs = area.Select(v => v.x).ToList();
        var zs = area.Select(v => v.z).ToList();
	    var xMin = xs.Min();
	    var xMax = xs.Max();
	    var zMin = zs.Min();
	    var zMax = zs.Max();
        var target = center;
        target.y = _y;
     //   foreach (var aVector3 in area)
	    //{
	    //    Debug.DrawRay(aVector3, Vector3.up, Color.blue, UpdateRate);
	    //}
        for (var i = 0; i < Guesses; i++)
	    {
	        var randX = center.x + Random.Range(xMin, xMax);
	        var randY = center.y + Random.Range(zMin, zMax);
            var tryVector = new Vector3(randX, _y, randY);
            if (!VirtualSpaceHandler.PointInPolygon(tryVector, area, 0f))
	            continue;
	        target = tryVector;
	        break;
	    }
	    transform.position = target;
	    Particles.Emit(1);
	}
}

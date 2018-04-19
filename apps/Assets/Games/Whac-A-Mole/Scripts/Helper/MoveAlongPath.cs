using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveAlongPath : MonoBehaviour {
    private Vector3 _direction;
    public Vector3 direction
    {
        get { return _direction; }
        set { _direction = value.normalized; }
    }
    public float speedInMPerSecond;
    public float totalTravelTimeInSeconds = float.MaxValue;
    public float totalTravelDistanceInMeters = float.MaxValue;

    public float travelTime = 0;
    public float travelDistance = 0;
    private float _lastUpdateTime;

	void Start () {
        _lastUpdateTime = Time.fixedTime;
	}
	
	void Update () {
        float deltaTime = Time.deltaTime;
        Vector3 translationNow = _direction * speedInMPerSecond * deltaTime;
        
        transform.Translate(_direction * speedInMPerSecond * deltaTime);

        travelTime += deltaTime;
        travelDistance += translationNow.magnitude;

        if (travelTime >= totalTravelTimeInSeconds || travelDistance >= totalTravelDistanceInMeters)
        {
            Destroy(this);
            return;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using Games.Tennis3D.Scripts;
using UnityEngine;

public class TennisRacketLogic : MonoBehaviour {
    
    public float MuscleFactor = 1.5f;
    public float BounceFactor = 1.4f;
    public float ClampMuscleVelocityMagnitude = 4f;
    public Transform RacketCenter;
    public Transform Target;
    public float HitIntervalMin = 1f;
    public float RacketRadiusMin = 0.3f;
    public float RacketRadiusMax = 0.8f;
    public float AngleHitMax = 45f;
    public float AdaptationRate = 0.5f;
    public float VeloDistFadeIn = 1.1f;
    public Tennis3DBallLogic TennisBall;
    public float SavedPositionsTime = 0.05f;
    public GameObject FrontCollider;
    public GameObject BackCollider;
    public float AcceptableDistanceForHitMin, AcceptableDistanceForHitMax;

    //private float _switchCountdown;
    //private bool _shouldFrontActive;
    //private bool _isFrontActive;
    private Rigidbody _rigidBodyTennisBall;
    private float _lastHit;
    private Transform _ballTransform;
    private List<Vector3> _lastPositions;
    private List<float> _times;
    private Vector3 _reflected;

    private void Start () {
        _lastPositions = new List<Vector3>();
        _times = new List<float>();
        transform.parent = Target;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        CheckPosition();
        _ballTransform = TennisBall.gameObject.transform;
        _rigidBodyTennisBall = TennisBall.GetComponent<Rigidbody>();

        //BackCollider.SetActive(false);
        //FrontCollider.SetActive(true);
        //_isFrontActive = true;
    }

    private void Update()
    {
        //if (_switchCountdown > 0f)
        //{
        //    _switchCountdown -= Time.deltaTime;
        //    if(_switchCountdown <= 0f)
        //    {
        //        _isFrontActive = _shouldFrontActive;
        //        BackCollider.SetActive(!_isFrontActive);
        //        FrontCollider.SetActive(_isFrontActive);
        //    }
        //}
        //var relBall = _ballTransform.position - RacketCenter.position;
        //relBall.Normalize();
        //var angle = Vector3.Angle(relBall, RacketCenter.forward);
        //var wasFrontActive = _isFrontActive;
        //_shouldFrontActive = angle > 90f;
        //if(wasFrontActive != _shouldFrontActive)
        //{
        //    _switchCountdown = 0.2f;
        //}
        

        CheckBall();
        CheckPosition();
    }

    private void CheckPosition()
    {
        var lastPosition = RacketCenter.position;
        _lastPositions.Add(lastPosition);
        _times.Add(Time.time);
        var index = 0;
        while (index < _times.Count - 1 && Time.time - _times[index] > SavedPositionsTime)
        {
            index++;
        }
        _lastPositions.RemoveRange(0, index);
        _times.RemoveRange(0, index);
    }

    private Vector3 GetVelocity()
    {
        var racketDiff = RacketCenter.position - _lastPositions[0];
        var velocity = racketDiff / (Time.time - _times[0]);
        return velocity;
    }

    [Range(0f, 0.9f)]
    public float Difficulty = 0.3f;
    private void CheckBall()
    {
        //check if hits in last test interval
        var isUpdate = Time.time < _lastHit + HitIntervalMin;
        if (isUpdate)
            return;

        var forward = RacketCenter.forward;
        var velo = GetVelocity();

     //   Debug.DrawRay(RacketCenter.position, velo, Color.red);
        //check if not hitting with side
        var angle = Vector3.Angle(forward, velo);
        if (angle < AngleHitMax)
        {
            //forward *= (velo.magnitude + _rigidBodyTennisBall.velocity.magnitude);
        }
        else if (angle > 180 - AngleHitMax)
        {
            forward *= -1f;// (velo.magnitude + _rigidBodyTennisBall.velocity.magnitude);
        }
        else
            return;

        var plane = new Plane(forward, RacketCenter.position);
        var projected = plane.ClosestPointOnPlane(_ballTransform.position);

        //check distance to racket surface (normal from racket)
        var relVelo = Mathf.Clamp01(velo.magnitude / VeloDistFadeIn);
        var dist2Plane = Vector3.Distance(projected, _ballTransform.position);
        var distThreshold = relVelo * (AcceptableDistanceForHitMax - AcceptableDistanceForHitMin) + AcceptableDistanceForHitMin;
        if (dist2Plane > distThreshold)
            return;
        
        //check if ball not further that racket radius
        var dist2Center = Vector3.Distance(projected, RacketCenter.position);
        if (dist2Center > RacketRadiusMax)
            return;
        var relRacketOffset = (dist2Center - RacketRadiusMin) / (RacketRadiusMax - RacketRadiusMin);
        if (relRacketOffset > 1f - Difficulty)
            return;

        _lastHit = Time.time;

        var refl = Vector3.Reflect(_rigidBodyTennisBall.velocity, forward).normalized;
        Vector3.Angle(refl, forward);
        refl = Vector3.Lerp(refl, forward, 1f- Difficulty);
        _reflected = refl * _rigidBodyTennisBall.velocity.magnitude;

        var resultVelocityOnBall = forward * velo.magnitude * MuscleFactor * (relVelo) + (1f - relVelo) * BounceFactor * _reflected;
        var mag = Mathf.Max(resultVelocityOnBall.magnitude, ClampMuscleVelocityMagnitude);
        resultVelocityOnBall = resultVelocityOnBall.normalized * mag;
       Debug.DrawRay(_ballTransform.position, resultVelocityOnBall, Color.magenta, 2f);

        TennisBall.PlayerHitBall(resultVelocityOnBall, (1f- Difficulty) * AdaptationRate);

    }
}

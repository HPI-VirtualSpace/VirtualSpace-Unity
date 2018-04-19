using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Games.Scripts;
using Games.Scripts;
using UnityEngine;

public enum FlowerState
{
    Happy,
    Hurt,
    Worried
}

public enum FlowerPosition
{
    Up,
    Moving,
    Down
}

public class FlowerController : MonoBehaviour {
    public Animator FaceAnimator;
    public float _lastStateChange;
    public ParticleSystem Dirt;
    
    public FlowerState FlowerState;
    public FlowerPosition PositionState;

    private bool IsUp { get { return _position >= .999; } }
    private bool IsDown { get { return _position <= .001; } }
    private bool WantsToMove { get { return Mathf.Abs(_speed) > .001; } }

    public float MinY = -0.2f;
    public float MaxY = 0.458f;
    public float MinScale = 0.01f;
    public float MaxScale = 0.1f;

    private float _position = 0;
    private float _speed = 0;

    void Start () {
        var position = transform.position;
        position.y = MinY;
        transform.position = position;
        PositionState = FlowerPosition.Down;

        SpawnFlower();
    }

    // spawn outside of area
    // spawn when player wants to go out

    // if not in zone where he should be
    // if walking towards the center but barely/does not have this area
    // spawn distance
    // worried distance

    // show/hide flower
    // spawn little pile
    // depending on player proximity and if he is in unallowed territory

    public float MinWorriedDistance = 1f;
    public float MinUnworriedDistance = 1.1f;

    public Transform Hammer;

    public ScorePopup ScorePopup;

    void Update () {
        Move();

        //if (PositionState == FlowerPosition.Down)
        //{
        //    Debug.Log("Spawn");
        //    SpawnFlower();
        //} else if (PositionState == FlowerPosition.Up) {
        //    Debug.Log("Hide");

        //    HideFlower();
        //}

        // is player close
        var player = Camera.main.transform;
        var playerPosition = player.position;

        var hammerPosition = Hammer.position;

        var flowerToPlayer = playerPosition - transform.position;
        flowerToPlayer.y = 0;
        var flowerToHammer = hammerPosition - transform.position;
        flowerToHammer.y = 0;
        var dist = Math.Min(flowerToPlayer.magnitude, flowerToHammer.magnitude);

        if (FlowerState == FlowerState.Happy && dist < MinWorriedDistance)
        {
            BeWorried();
        }
        else if (FlowerState == FlowerState.Worried && dist > MinUnworriedDistance)
        {
            BeHappy();
        } else if (FlowerState == FlowerState.Hurt && Time.time - _lastHitTime >= SecondsInBetweenHits)
        {
            BeHappy();
        }
        // if is hurt + certain time passed --> happy/idle

        if (IsUp && PositionState == FlowerPosition.Moving && _speed > 0)
        {
            //Debug.Log("All the way up");
            PositionState = FlowerPosition.Up;
            _speed = 0;
        }

        if (IsDown && PositionState == FlowerPosition.Moving && _speed < 0)
        {
            //Debug.Log("All the way down");
            PositionState = FlowerPosition.Down;
            Dirt.Stop();
            _speed = 0;
        }
	}

    public float SpawnSeconds = 1f;
    public float HideSeconds = 1f;


    void Move()
    {
        _position += _speed * Time.deltaTime;

        var yPosition = Mathf.Lerp(MinY, MaxY, _position);
        //Debug.Log("Y pos: " + yPosition);
        var transformPosition = transform.position;
        transformPosition.y = yPosition;
        transform.position = transformPosition;

        var scale = transform.localScale;
        var xzScale = Mathf.Lerp(MinScale, MaxScale, _position);
        scale.x = xzScale;
        scale.z = xzScale;
        transform.localScale = scale;
    }

    void SpawnFlower()
    {
        if (IsUp) return;
        //Debug.Log("Setting parameters");
        Dirt.Play();

        PositionState = FlowerPosition.Moving;

        _speed = (1 - _position) / SpawnSeconds;
        //Debug.Log("New Speed is " + _speed);
    }

    void HideFlower()
    {
        if (IsDown) return;

        PositionState = FlowerPosition.Moving;
        
        _speed = -_position / HideSeconds;
    }

    public void BeHappy()
    {
        //Debug.Log("Be happy");
        FaceAnimator.SetTrigger("Smile");
        FlowerState = FlowerState.Happy;
        _lastStateChange = Time.time;
    }

    public void BeWorried()
    {
        //Debug.Log("Be worried");
        FaceAnimator.SetTrigger("Worried");
        FlowerState = FlowerState.Worried;
        _lastStateChange = Time.time;
    }

    public void BeHurt()
    {
        //Debug.Log("Be hurt");
        FaceAnimator.SetTrigger("Hurt");
        FlowerState = FlowerState.Hurt;
        _lastStateChange = Time.time;
    }

    private float _lastHitTime = float.MinValue;
    public Action<int> FlowerWasHit;
    public float SecondsInBetweenHits = 2f;
    private int _inTrigger = 0;
    private bool firstHit = true;
    void OnTriggerEnter(Collider other)
    {
        //Debug.Log("Collision");
        if (other.CompareTag("Hitter") || other.CompareTag("PlayerBody"))
        {
            //Debug.Log("Last hit was " + (Time.time - _lastHitTime) + " seconds ago");

            if (Time.time - _lastHitTime < SecondsInBetweenHits || _inTrigger > 0) return;

            //Debug.Log("Flower was hit");

            _lastHitTime = Time.time;
            _inTrigger++;

            if (firstHit)
            {
                firstHit = false;
            } else
            {
                BeHurt();

                var hitPosition = other.ClosestPointOnBounds(transform.position);

                var scorePopup = Instantiate(ScorePopup, hitPosition, Quaternion.identity);
                scorePopup.Points = -100;
                if (FlowerWasHit != null)
                    FlowerWasHit.Invoke(-100);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        _inTrigger--;
    }
}

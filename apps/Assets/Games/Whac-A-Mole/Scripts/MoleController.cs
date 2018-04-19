using System;
using Assets.Games.Scripts;
using Games.Scripts;
using UnityEngine;
using Random = UnityEngine.Random;
using EasyButtons;

public class MoleController : MonoBehaviour
{ 
    public ParticleSystem DirtParticles;
    public ParticleSystem HitParticles;
    public ParticleSystem MovingParticles;
    public AudioSource AudioSource;
    public AudioClip HitClip;
    public AudioClip AppearClip;
    public AudioClip[] MockClips;
    public AudioClip[] SneakyClips;
    public Action<int> MoleIsHit;
    public Action<bool> MoleIsDestroyed;
    public ScorePopup ScorePopup;
    public HammerController Hammer;
    public float SimulateHitIn = -1;
    public int Id;
    public static Action<MoleController> MoleSpawned;
    public static Action<MoleController, Vector3> MoleHit;
    public static Action<MoleController> MoleDestroyed;

    private bool IsUp { get { return _position >= .999; } }
    private bool IsDown { get { return _position <= .001; } }
    private bool WantsToMove { get { return Mathf.Abs(_speed) > .001; } }
    
    public GameObject MoleHead;
    public Vector3 MinMole = new Vector3(0, -.24f, 0);
    public Vector3 MaxMole = new Vector3(0, .122f, 0);

    private float _position = 0;
    private float _speed = 0;
    private float _lastHitTime = -1;
    public float SecondsInBetweenHits = 1f;
    private bool _inTrigger = false;

    private float CurrentPosition
    {
        get
        {
            if (IsDown) return 0;
            if (IsUp) return 1;

            return _position;
        }
    }

    private bool _despawnWhenHidden;

    [Button]
    public void HitTheMole()
    {
        SimulateHit();
    }

	void Update () {
	    MoveMole();
	    SetMoleElevation();
	    ControlLifecycle();
	}
    
    void MoveMole()
    {
        if (!WantsToMove) return;
        var newPosition = _position + _speed * Time.deltaTime;
        if (newPosition > 1)
        {
            newPosition = 1;
            _speed = 0;
        }
        else if (newPosition < 0)
        {
            newPosition = 0;
            _speed = 0;
        }
        _position = newPosition;
    }

    void SetMoleElevation()
    {
        var yPosition = Vector3.Lerp(MinMole, MaxMole, _position);
        var molePosition = MoleHead.transform.position;
        molePosition.y = yPosition.y;
        MoleHead.transform.position = molePosition;
    }

    public float ShowHideTime = .5f;
    public float UpTime = .5f;
    private float _upSince = -1;
    public float ShowInTime;
    private float _spawnAt = -1;
    private float _spawnedAt = float.PositiveInfinity;

    public void StartLifecycle()
    {
        _spawnAt = Time.time + ShowInTime;

        if (MoleSpawned != null)
            MoleSpawned(this);
        //Debug.Log("Show in: " + ShowInTime + "Show/Hide: " + ShowHideTime +  " Keep up: " + UpTime);

    }

    void SimulateHit()
    {
        WasHit(Vector3.zero);
    }

    void ControlLifecycle()
    {
        if (_spawnAt <= Time.time)
        {
            SpawnMole(ShowHideTime);
            // mock 

            _spawnedAt = _spawnAt;
            _spawnAt = float.MaxValue;
        }

        if (SimulateHitIn > 0 && _spawnedAt + SimulateHitIn <= Time.time)
        {
            Debug.Log("Hit in " + SimulateHitIn);
            SimulateHit();
            SimulateHitIn = -1;
        }

        if (IsUp && _upSince < 0)
        {
            _upSince = Time.time;
        }
        
        if (IsUp && (Time.time - _upSince) > UpTime)
        {
            _upSince = -1;
            _despawnWhenHidden = true;
            HideMole(ShowHideTime);
        }

        if (IsDown && _despawnWhenHidden)
        {
            //Debug.Log("Destroy");
            Destroy(gameObject);
            if (MoleIsDestroyed != null)
                MoleIsDestroyed.Invoke(_lastHitTime > 0);
            if (MoleDestroyed != null)
                MoleDestroyed(this);
        }
    }

    public bool MovingMole = false;
    public void Move(Vector3 position)
    {
        transform.position = position;
        //MovingParticles.Play();
    }

    public float MockingMinAngle = 80f;
    internal bool Sneaky;

    public void SpawnMole(float spawnTime)
    {
        if (IsUp) return;

        _speed = (1 - CurrentPosition) / spawnTime;

        if (MovingMole)
            MovingParticles.Play();
        //else
        //    DirtParticles.Play();

        AudioSource.PlayOneShot(AppearClip);

        var parent = transform.parent == null ? transform : transform.parent;
        var playerCamera = Camera.main == null ? parent : Camera.main.transform;
        var playerToMole = transform.position - playerCamera.position;
        playerToMole.y = 0;
        
        var angle = Vector3.Angle(playerCamera.forward, playerToMole);
        if (Sneaky && SneakyClips.Length > 0) {
            var clipIndex = Random.Range(0, SneakyClips.Length);
            AudioSource.PlayOneShot(SneakyClips[clipIndex]);
        }
        else if (angle > MockingMinAngle && MockClips.Length > 0)
        {
            var clipIndex = Random.Range(0, MockClips.Length);
            AudioSource.PlayOneShot(MockClips[clipIndex]);
        }
    }

    public void HideMole(float spawnTime)
    {
        if (IsDown) return;

        _speed = -CurrentPosition / spawnTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Hitter")) return;

        WasHit(transform.InverseTransformPoint(other.ClosestPointOnBounds(transform.position)));
    }

    public void WasHit(Vector3 hitPosition)
    {
        if (Time.time - _lastHitTime < SecondsInBetweenHits || _inTrigger) return;
        _lastHitTime = Time.time;
        _inTrigger = true;

        var hitGameObject = Instantiate(HitParticles);
        hitGameObject.transform.parent = transform;
        hitGameObject.transform.localPosition = hitPosition;
        hitGameObject.transform.parent = transform.parent;

        hitGameObject.Play();
        AudioSource.PlayOneShot(HitClip);

        var hammerVelocity = Hammer == null ? 10 : Hammer.Velocity.magnitude;
        var points = hammerVelocity * 100 / 5;
        if (Sneaky) points *= 1.5f;
        //Debug.Log("velocity: " + hammerVelocity);
        //Debug.Log("points: " + points);
        points = Mathf.Clamp(points, 0, 100);
        //Debug.Log("points clamped" + points);

        if (MoleIsHit != null)
            MoleIsHit.Invoke((int) points);
        if (MoleHit != null)
            MoleHit(this, hitPosition);

        var scorePopup = Instantiate(ScorePopup, hitPosition, Quaternion.identity);
        scorePopup.transform.parent = transform;
        scorePopup.transform.localPosition = hitPosition;
        scorePopup.transform.parent = transform.parent;
        scorePopup.Points = (int) points;

        HideMole(ShowHideTime * .5f);

        _despawnWhenHidden = true;
    }

    void OnTriggerExit(Collider other)
    {
        _inTrigger = false;
    }
}

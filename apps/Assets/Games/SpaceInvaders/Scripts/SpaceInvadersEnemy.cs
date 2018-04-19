using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Games.SpaceInvaders.Scripts;

public class SpaceInvadersEnemy : MonoBehaviour
{

    public List<GameObject> Animation1Only;
    public List<GameObject> Animation2Only;
    public float Timestep = 1.2f;
    public float DestroyTime = 4;
    public float DistanceCheck = 3f;
    public float DistanceCheckOffset = 0.1f;

    public SpaceInvadersEnemyShot EnemyShot;
    public float ShotInterval = 2f;
    public float ShotIntervalRandomAdd = 2f;
    [HideInInspector]
    public float FireRateFactor = 1f;


    private bool _toDestroy;
    private float _destroyTime;
    private bool _destroyed;
    private bool _defeated;

    private bool _oldTimestep;
    private float _shootAgain;

    private const float Cool = 0.6f;
    private const float RandCool = 1.0f;
    private static float _cooldownAll;

    // Use this for initialization
    void Start ()
	{
	    _oldTimestep = true;
        _shootAgain = Time.time + Random.Range(0f, ShotIntervalRandomAdd) + ShotInterval;
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (Input.GetKeyDown(KeyCode.A) && Random.Range(0,2)== 0)
            Burst();

        if (_destroyed)
	        return;
        else if (_toDestroy)
        {
            if (Time.time - _destroyTime > DestroyTime)
            {
                _destroyed = true;
            }
            return;
        }
        

        //try to shoot player
        if (Time.time > _shootAgain && Time.time > _cooldownAll)
        {
            //shoot
            _shootAgain = Time.time + (Random.Range(0f, ShotIntervalRandomAdd) + ShotInterval) * FireRateFactor;
            var intersects = Physics.Raycast(transform.position + transform.up, transform.up, DistanceCheck);
            if (intersects)
                return;
            var shot = Instantiate(EnemyShot);
            shot.ValidPosition = EnemyShot.transform.position;
            shot.transform.position = transform.position;
            shot.transform.forward = transform.up;
            shot.transform.SetParent(transform.parent.parent);

            _cooldownAll = Time.time + Cool + Random.Range(0f, RandCool);
        }
        

        var stage1 = Time.time % (Timestep*2) < Timestep;
	    var changed = stage1 != _oldTimestep;
	    _oldTimestep = stage1;
        if (!changed)
	        return;

        foreach (var go in Animation1Only)
	        go.SetActive(stage1);
        foreach (var go in Animation2Only)
            go.SetActive(!stage1);
    }

    public bool Defeated()
    {
        return _defeated;
    }

    public void Burst()
    {
        if(_toDestroy || _destroyed) return;

        _defeated = true;
        _destroyTime = Time.time;
        _toDestroy = true;

        var sound = GetComponent<AudioSource>();
        sound.Play();

        var parent = transform.parent.GetComponent<SpaceInvadersEnemyRow>();
        parent.DeregisterEnemy(this);
        
        foreach (var t in GetComponentsInChildren<Transform>())
        {
            if (t == transform)
                continue;
            if (t.gameObject.GetComponent<BoxCollider>() == null)
            {
                var bc = t.gameObject.AddComponent<BoxCollider>();
            }
            var s = t.gameObject.AddComponent<Shrinker>();
            var rb = t.gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            s.ShrinkOver = DestroyTime;
            //rb.isKinematic = false;
            t.parent = null;
        }

        transform.parent = null;
    }

    public bool CanDestroy()
    {
        return _destroyed;
    }
}

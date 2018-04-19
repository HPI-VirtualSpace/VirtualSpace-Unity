using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MockMoleController : MonoBehaviour {

    public ParticleSystem DirtPile;
    public AudioSource AudioSource;
    public AudioClip SpawnClip;
    public float ShowInTime = 1f;
    private float _startAt = float.MaxValue;
    private float _showingSince = float.MaxValue;
    public float ShowForTime = 2f;

    void Start()
    {
        StartLifecycle();
    }

    void Update () {
	    ControlLifecycle();
    }
    
    public void StartLifecycle()
    {
        _startAt = Time.time + ShowInTime;
    }

    void ControlLifecycle()
    {
        if (_startAt <= Time.time)
        {
            DirtPile.Play();
            AudioSource.PlayOneShot(SpawnClip);
            _startAt = float.MaxValue;
            _showingSince = Time.time;
        }

        if (_showingSince + ShowForTime <= Time.time)
        {
            _showingSince = float.MaxValue;
            Destroy(gameObject);
        }
    }
}

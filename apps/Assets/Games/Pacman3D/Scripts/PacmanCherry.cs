using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PacmanCherry : MonoBehaviour
{

    public GameObject AllMyChildren;
    public GameObject AllMyChildren2;
    public AudioSource Eatensource;
    public AudioSource ShowUp;
    public event Action<PacmanCherry> CherryEaten;
    public event Action<PacmanCherry, bool> CherryReset;

    private bool _eaten;
    private bool _onValid;
    private float _timeEaten;

    private void Start()
    {
        AllMyChildren.SetActive(false);
        AllMyChildren2.SetActive(false);
        _eaten = true;
        _onValid = false;
        _scale = transform.localScale;
    }

    private Vector3 _scale;
    private void Update()
    {
        if (Visible())
        {
            var factor = Mathf.Clamp01(1f- (Time.time - _timeSpawned) / Lifetime);
            transform.localScale = factor * _scale;
        }
    }

    private float _timeSpawned;

    public bool Visible()
    {
        return AllMyChildren.activeSelf;
    }

    public float Lifetime = 4f;
    public void TryReset(bool valid, bool deleteifinvalid)
    {
        if (!_onValid && valid || valid && Time.time - _timeEaten > 10f)
        {
            _timeSpawned = Time.time;
            AllMyChildren.SetActive(true);
            AllMyChildren2.SetActive(true);
            ShowUp.Play();
            _eaten = false;
            _onValid = true;
            if (CherryReset != null)
                CherryReset(this, true);
        }
        if (!valid && (deleteifinvalid || Time.time - _timeSpawned > Lifetime))
        {
            AllMyChildren.SetActive(false);
            AllMyChildren2.SetActive(false);
            _eaten = true;
            _onValid = false;
            if (CherryReset != null)
                CherryReset(this, false);
        }
        _onValid = valid;
    }

    public bool TryEatThis()
    {
        if (_eaten)
            return false;
        AllMyChildren.SetActive(false);
        AllMyChildren2.SetActive(false);
        _eaten = true;
        _timeEaten = Time.time;
        Eatensource.Play();
        if (CherryEaten != null)
            CherryEaten(this);
        return true;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PacmanDrug : MonoBehaviour {

    private bool _eaten;
    private Renderer _rend;

    private void Start()
    {
        _rend = GetComponent<Renderer>();
    }

    private float _eatenTime = -300f;

    public float ValueFactor()
    {
        var justeaten = Time.unscaledTime - _eatenTime < 300f;
        var eatenNow = Time.unscaledTime - _eatenTime < 30f;
        if (justeaten && eatenNow)
            return 2f;
        return justeaten ? 0.75f : 1f;
    }
    
    public bool TryEat()
    {
        if (_eaten || !_rend.enabled)
            return false;
        _rend.enabled = false;
        _eaten = true;
        _eatenTime = Time.unscaledTime;
        return true;
    }

    private void Update()
    {
        if (_eaten && Time.unscaledTime - _eatenTime > 300f)
        {
            _eaten = false;
            _rend.enabled = _onValid;
        }
    }

    private bool _onValid;
    public void OnValidArea(bool valid)
    {
        _onValid = valid;
        _rend.enabled = valid && !_eaten;
    }
}

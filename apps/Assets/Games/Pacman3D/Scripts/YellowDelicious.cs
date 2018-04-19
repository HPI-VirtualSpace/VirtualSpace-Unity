using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.VirtualSpace.Unity_Specific;
using UnityEngine;

public class YellowDelicious : MonoBehaviour {

    private bool _eaten;
    private Renderer _rend;
    public event Action<YellowDelicious> FoodEaten, FoodReset;

    private void Start()
    {
        _rend = GetComponent<Renderer>();
        _onValid = true;
        _eaten = false;
        Reset();
    }

    public bool IsEaten()
    {
        return _eaten;
    }

    private float _timeEaten;
    public bool TryEat()
    {
        if (_eaten || !_onValid)
            return false;
        _rend.enabled = false;
        _eaten = true;
        _timeEaten = Time.time;
        if (FoodEaten != null)
            FoodEaten(this);
        return true;
    }

    public void Reset()
    {
        _onValid = true;
        _rend.enabled = _onValid;
        _eaten = false;
        if (FoodReset != null)
            FoodReset(this);
    }

    private bool _onValid;

    public void OnValidArea(bool valid)
    {
        var changed = !_onValid && valid;// || valid && (Time.time - _timeEaten > 5.5f);
        _onValid = valid;
        if (_eaten && valid && changed)
        {
            Reset();
        }
        _rend.enabled = !_eaten && valid;
    }

    public bool CheckTile(ref PacTile tile)
    {
        var hits = Physics.RaycastAll(new Ray(transform.position, Vector3.down), 1f);
        foreach (var hit in hits)
        {
            var t = hit.transform.parent.GetComponent<PacTile>();
            if (t != null)
            {
                tile = t;
                return true;
            }
        }
        return false;
    }
}

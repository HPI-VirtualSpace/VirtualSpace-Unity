using UnityEngine;
using UnityEngine.UI;

public class PacmanPointJumpToCam : MonoBehaviour
{
    
    public float Lifetime;
    public float Zoffset;
    public Text PointText;

    private Vector3 _start;
    private float _lifetime;
    private Vector3 _end;
    private Transform _cam;
    private bool _init;

    public void Init (Vector3 position, int points)
    {
        _init = true;
        PointText.text = (points > 0 ? "+" : "") + points;
        PointText.color = points > 0 ? Color.yellow : Color.red;
        _lifetime = Lifetime;
        _start = position;
        _cam = Camera.main.transform;
        _end = _cam.position + _cam.forward * Zoffset;
    }
	
	void Update ()
	{
        if (!_init)
            return;
	    _lifetime -= Time.deltaTime;
	    var factor = 1f- _lifetime / Lifetime;
	    factor = Ease(factor);
	    transform.position = Vector3.Lerp(_start, _end, factor);
	    transform.up = Vector3.up;
	    var forward = (_cam.position - transform.position).normalized;
	    forward.y = 0f;
	    transform.forward = forward;
	    
        if(_lifetime < 0f)
            Destroy(gameObject);
	}

    public Easing EasingType;

    public enum Easing
    {
        Quadratic,
        Sinusoidal,
        Circular
    }

    private float Ease(float factor)
    {
        var retVal = 0f;
        switch (EasingType)
        {
            case Easing.Sinusoidal:
                retVal = -0.5f * (Mathf.Cos(Mathf.PI * factor) - 1);
                break;
            case Easing.Quadratic:
                var t = factor * 2f;
                if (t < 1) retVal = 0.5f * t * t;
                else
                {
                    t--;
                    retVal = -0.5f * (t * (t - 2) - 1);
                }
                break;
            case Easing.Circular:
                var tc = factor * 2f;
                if (tc < 1) return -0.5f * (Mathf.Sqrt(1 - tc * tc) - 1);
                tc -= 2;
                retVal = 0.5f * (Mathf.Sqrt(1 - tc * tc) + 1);
                break;
        }
        return retVal;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemyPlayerTennis : MonoBehaviour {

    public Transform Ball;
    public float DudeHeight = 1.42f;
    public Transform Dude;
    public Collider ValidArea;
    public Vector3 DudeOffset = new Vector3(1.2f, 0f, 0f);
    public float ReturnSpeed = 2f;
    public float TimeThres = 0.3f;
    public float CountdownMinusReturn = -1.4f;
    public float CountdownHit = 1f;

    private Vector3 _startPosition;
    private float _countdown;
    private Vector3 _target;
    private Vector3 _from;
    private bool _targetSet;
    private float _countdownTotal;
    private bool _hit;
    private Vector3 _hitPos;
    private float _hitCountdown;

	// Use this for initialization
	void Awake () {
        _countdownTotal = 1f;
        _startPosition = transform.position;
        BackToLine(_startPosition);
    }
	
	// Update is called once per frame
	void Update () {
        if (_target == null)
            return;

        _countdown -= Time.deltaTime;
        var progress = Mathf.Clamp01(1f-_countdown / _countdownTotal);
        progress = Mathf.Sin(Mathf.PI / 2f * progress);
        var newPos = Vector3.Lerp(_from, _target, progress);

        if (_targetSet)
        {
            if (_countdown < TimeThres)
            {
                var relPos = Ball.transform.position + Vector3.forward * 1.3f;
                newPos = Vector3.Lerp(relPos, newPos, _countdown / TimeThres);
            }
        }
        if (_hit)
        {
            newPos = _hitPos;
            _hitCountdown -= Time.deltaTime;
            if (_hitCountdown < 0f)
            {
                _hit = false;
                //if (_countdown < CountdownMinusReturn)
                BackToLine(_startPosition);
            }
        }

        var tmpDist = Mathf.Clamp((-newPos.x + _startPosition.x) / DudeOffset.x, -1f, 1f);

        if (!ValidArea.bounds.Contains(newPos))
        {
            newPos = ValidArea.ClosestPointOnBounds(newPos);
        }
        
        if (!_hit)
        {
            var dudePos = newPos + tmpDist * DudeOffset;
            dudePos.y = 0f;
            Dude.position = dudePos;
        }

        var lookAt = Dude.position;
        lookAt.y = DudeHeight;

        transform.position = newPos;
        transform.LookAt(lookAt);
        Dude.forward = tmpDist >= 0f ? transform.right : -transform.right;
    }

    public void SetTarget(Vector3 position, float time, bool goThere)
    {
        if (!goThere)
        {
            BackToLine(_startPosition);
            return;
        }
        _from = transform.position;
        _target = position;
        _targetSet = true;
        _countdown = time;
        _countdownTotal = time;
    }

    public void Hit()
    {
        _hitPos = Ball.transform.position;
        _hit = true;
        _hitCountdown = CountdownHit;
    }

    public void BackToLine(Vector3 position)
    {
        _hit = false;
        _targetSet = false;
        _from = transform.position;
        _target = position;
        _countdown = ReturnSpeed;
        _countdownTotal = ReturnSpeed;
    }
}

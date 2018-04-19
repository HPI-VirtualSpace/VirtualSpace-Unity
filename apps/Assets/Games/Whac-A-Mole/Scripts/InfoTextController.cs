using EasyButtons;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfoTextController : MonoBehaviour {

    public Action BeingHitEvent;
    private float _initialY;
    public float PlayerOffset = .5f;
    public SimpleHelvetica Text;
    public bool DebugHitAutomatically = false;

    void Start () {
        _initialY = transform.position.y;
	}

    private void Update()
    {
        if (DebugHitAutomatically)
        {
            WasHit();
        }
    }

    public void Move(Vector3 position)
    {
        position.y = _initialY;
        var cameraPosition = Camera.main.transform.position;
        cameraPosition.y = _initialY;

        transform.position = position + (position - cameraPosition).normalized * PlayerOffset;
        transform.LookAt(cameraPosition);
    }

    public void SetStandardText()
    {
        Text.Text = "FOLLOW ME!\nHIT TO START!";
    }

    public void SetScoreText(int high, int best)
    {
        Text.Text = "SCORE: " + high + " BEST: best" +
            "\nFOLLOW ME!\nHIT TO START!";
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hitter"))
        {
            WasHit();
        }
    }

    [Button("WasHit")]
    private void WasHit()
    {
        if (BeingHitEvent != null)
            BeingHitEvent();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Assets;
using Assets.ViveClient;

public class SystemMoves : MonoBehaviour {

    public Transform System;
    public CameraRig CamRig;
    public float XZFactor;
    public int QueueCount = 10;

    private Vector3 _startPosition;
    private List<Vector3> _oldPositions;

    // Use this for initialization
    void Start () {
        _startPosition = System.transform.position;
        _oldPositions = new List<Vector3>();
    }
	
	// Update is called once per frame
	void Update () {
        if (CamRig.Target == null)
            return;
        var systemFlat = System.position;
        systemFlat.y = 0f;
        var movingAverage = GetAverage();
        //var offset = movingAverage - systemFlat;
        System.transform.position = _startPosition + movingAverage * XZFactor;
	}

    private Vector3 GetAverage()
    {
        _oldPositions.Add(CamRig.Target.transform.localPosition);
        while (_oldPositions.Count > QueueCount)
            _oldPositions.RemoveAt(0);
        var avg = Vector3.zero;
        for (var i = 0; i < _oldPositions.Count; i++)
            avg += _oldPositions[i];
        avg /= _oldPositions.Count;
        avg.y = 0f;
        return avg;
    }
}

using System.Collections.Generic;
using UnityEngine;
using RenderHeads.Media.AVProMovieCapture;

public class DeactivateOnStart : MonoBehaviour
{

    public List<GameObject> Activate;
    public List<GameObject> Deactivate;
    public Camera Alternative;
    public float LerpTime = 1f;

    private float _fov3rd, _fov1st;
    private bool _activated;
    private bool _lerpToStatic;
    private float _lerpCurrent;
    private Vector3 _staticPos;
    private Quaternion _staticRot;
    private Vector3 _camPos;
    private Quaternion _camRot;

    private void Awake()
    {
        transform.parent = null;
        _fov3rd = Alternative.fieldOfView;
        _fov1st = Camera.main.fieldOfView;
        _staticPos = Alternative.transform.position;
        _staticRot = Alternative.transform.rotation;
    }

    private void Start()
    {
    }

    private void Update () {
	    if (Input.GetKeyDown(KeyCode.R))
	    {
	        foreach (var o in Activate)
	            o.SetActive(true);
            foreach (var o in Deactivate)
	            o.SetActive(false);
            _activated = true;
           //// Alternative.enabled = true;
            _lerpToStatic = true;
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            _lerpToStatic = !_lerpToStatic;
            if (_lerpToStatic)
            {
                _camPos = Camera.main.transform.position;
                _camRot = Camera.main.transform.rotation;
            }
            Debug.Log("CAM RECORD: " + (_lerpToStatic ? "3RD" : "1ST"));
        }
        if (_activated)
        {
            _lerpCurrent += _lerpToStatic ? Time.unscaledDeltaTime : -Time.unscaledDeltaTime;
            _lerpCurrent = Mathf.Clamp(_lerpCurrent, 0f, LerpTime);
            var factor = _lerpCurrent / LerpTime;
            Alternative.transform.position = Vector3.Lerp(_lerpToStatic ? _camPos : Camera.main.transform.position, _staticPos, factor);
            Alternative.transform.rotation = Quaternion.Lerp(_lerpToStatic ? _camRot : Camera.main.transform.rotation, _staticRot, factor);
            Alternative.fieldOfView = factor * _fov3rd + (1f - factor) * _fov1st;
        }
    }
}

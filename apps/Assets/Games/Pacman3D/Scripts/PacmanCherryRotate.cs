using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PacmanCherryRotate : MonoBehaviour {


    public float RotateSpeed;
    public float UpDown;
    public float UpDownTime;

    void Update()
    {
        transform.localRotation = Quaternion.Euler(0f, RotateSpeed * Time.deltaTime, 0f) * transform.localRotation;
        transform.localPosition = Mathf.Sin(Time.time * Mathf.PI / UpDownTime) * Vector3.up * UpDown;
    }
}

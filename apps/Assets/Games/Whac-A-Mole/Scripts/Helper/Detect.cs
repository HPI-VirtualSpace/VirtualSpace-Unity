using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Detect : MonoBehaviour {
    public delegate void HandleDetection(GameObject me, GameObject other);
    public HandleDetection detectionHandler;

    public void OnTriggerEnter(Collider other)
    {
        //Debug.Log("Collision detected with " + other.name);
        if (detectionHandler != null)
            detectionHandler.Invoke(gameObject, other.gameObject);
    }

    //public void OnCollisionEnter(Collision collision)
    //{
    //    if (detectionHandler != null)
    //        detectionHandler.Invoke(gameObject);
    //}
}

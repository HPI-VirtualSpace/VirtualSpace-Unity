using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivateOnClick : MonoBehaviour
{

    public KeyCode Key;
    public List<GameObject> ActivateDeactivate;
    public List<Camera> ActivateDeactivateCams;
    public bool AlsoMainCam;
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
	    if (Input.GetKeyDown(Key))
	    {
	        foreach (var ad in ActivateDeactivate)
	        {
	            ad.SetActive(!ad.activeSelf);
	        }
	        foreach (var adc in ActivateDeactivateCams)
	        {
	            adc.enabled = !adc.enabled;
	        }
	        if (AlsoMainCam)
	        {
	            Camera.main.enabled = !Camera.main.enabled;
	        }
        }
	}
}

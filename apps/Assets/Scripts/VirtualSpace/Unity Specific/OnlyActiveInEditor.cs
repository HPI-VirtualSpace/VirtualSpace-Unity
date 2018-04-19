using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnlyActiveInEditor : MonoBehaviour {
    
	void Awake () {
		if(!Application.isEditor)
            gameObject.SetActive(false);
	}
}

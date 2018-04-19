using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destructible : MonoBehaviour {

	public GameObject BrokenVersion;

	void OnMouseDown ()
	{
		Instantiate(BrokenVersion, transform.position, transform.rotation);
		Destroy(gameObject);
	}
}

using UnityEngine;
using System.Collections;

public class Shrinker : MonoBehaviour
{

    public float ShrinkOver = 2f;

    private float ShrinkStart;
    private Vector3 ScaleStart;

	// Use this for initialization
	void Start ()
	{
	    ShrinkStart = Time.time;
	    ScaleStart = transform.localScale;

	}
	
	// Update is called once per frame
	void Update ()
	{
	    transform.localScale = ScaleStart * 1.1f * Mathf.Max(1f - (Time.time - ShrinkStart)/ShrinkOver, 0f);
        if (Time.time - ShrinkStart > ShrinkOver)
            Destroy(gameObject);
	}
}

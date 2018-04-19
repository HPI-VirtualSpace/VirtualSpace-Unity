using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceInvadersRepeatSound : MonoBehaviour {

    public List<AudioClip> Clips;
    public AudioSource Source;
    public float Offset;
    public float Repeat;

    private float _last;

	void Start () {
		
	}
	
	void Update () {
        if (Time.time > _last + Repeat + Offset)
        {
            _last = Time.time;
            Source.clip = Clips[Random.Range(0, Clips.Count)];
            Source.Play();
        }
	}
}

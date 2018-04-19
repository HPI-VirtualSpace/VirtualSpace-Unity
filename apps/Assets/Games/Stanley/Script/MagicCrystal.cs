using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MagicCrystal : MonoBehaviour {

    private AudioSource _audioSource;
    public AudioClip MagicPuffSound;

    // Use this for initialization
    void Start () {
        _audioSource = GetComponent<AudioSource>();
	}
	
    public void PlayPuff()
    {
        _audioSource.PlayOneShot(MagicPuffSound);
    }

	// Update is called once per frame
	void Update () {
		
	}
}

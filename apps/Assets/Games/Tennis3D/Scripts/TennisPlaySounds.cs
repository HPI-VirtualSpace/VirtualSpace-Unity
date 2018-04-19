using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TennisPlaySounds : MonoBehaviour {

    public AudioSource Standard;
    public AudioSource Point;

    public AudioClip[] HitSounds;
    public AudioClip PointPlayer;
    public AudioClip PointEnemy;
    public AudioClip Mistake;
    public AudioClip BounceSound;
    public AudioClip NetSound;
    public AudioClip ServeSound;

    public float HitVeloMax;
    public float BounceVeloMax;
    public float NetVeloMax;

    public void PlayHit(float veloMagnitude, bool serve)
    {
        Standard.clip = serve ? ServeSound : HitSounds[Random.Range(0, HitSounds.Length)];
        Standard.pitch = Random.Range(0.9f, 1.1f);
        Standard.volume = Mathf.Clamp01(veloMagnitude / NetVeloMax);
        Standard.Play();
    }

    public void PlayNet(float veloMagnitude)
    {
        Standard.clip = NetSound;
        Standard.pitch = Random.Range(0.9f, 1.1f);
        Standard.volume = Mathf.Clamp01(veloMagnitude / NetVeloMax);
        Standard.Play();
    }

    public void PlayMistake()
    {
        Point.clip = Mistake;
        Point.Play();
    }

    public void PlayPoint(bool playerPoint)
    {
        Point.clip = playerPoint ? PointPlayer : PointEnemy;
        Point.Play();
    }

    public void PlayBounce(float veloMagnitude)
    {
        Standard.clip = BounceSound;
        Standard.pitch = 1f;
        Standard.volume = Mathf.Clamp01(veloMagnitude / NetVeloMax);
        Standard.Play();
    }
}

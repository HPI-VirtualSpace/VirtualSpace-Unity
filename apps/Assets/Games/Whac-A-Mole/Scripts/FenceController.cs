using System.Collections;
using System.Collections.Generic;
using Assets.Games.Scripts;
using Games.Scripts;
using UnityEngine;
using UnityThreading;

public class FenceController : MonoBehaviour
{
    public ScorePopup ScorePopup;
    public WhacBoardController WhacBoardController;
    public float TimeBetweenHits = 1f;

    private float _timeLastHit = float.MinValue;

    void Start()
    {
        WhacBoardController = FindObjectOfType<WhacBoardController>();
    }

    void OnTriggerEnter(Collider other)
    {
        //Debug.Log("Trigger enter");
        if (other.CompareTag("Hitter") || other.CompareTag("PlayerBody"))
        {
            if (Time.time - _timeLastHit <= TimeBetweenHits) return;

            _timeLastHit = Time.time;

            var hitPosition = other.ClosestPointOnBounds(transform.position);

            var scorePopup = Instantiate(ScorePopup, hitPosition, Quaternion.identity);
            scorePopup.Points = Random.Range(-5, -1);
        }
    }
}

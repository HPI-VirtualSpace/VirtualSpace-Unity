using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkThroughRoom : RoomControl
{
    public DetectIfEntered PlayerEnteredTrigger;
    public DetectIfEntered PlayerAlmostFinishedTrigger;
    public DetectIfEntered PlayerFinishedTrigger;

    void OnEnable()
    {
        PlayerEnteredTrigger.OnTriggerEnterAction += UserEntered;
        PlayerAlmostFinishedTrigger.OnTriggerEnterAction += UserAboutToFinish;
        PlayerFinishedTrigger.OnTriggerEnterAction += UserFinished;
    }

    void OnDisable()
    {
        PlayerEnteredTrigger.OnTriggerEnterAction -= UserEntered;
        PlayerAlmostFinishedTrigger.OnTriggerEnterAction -= UserAboutToFinish;
        PlayerFinishedTrigger.OnTriggerEnterAction -= UserFinished;
    }
}

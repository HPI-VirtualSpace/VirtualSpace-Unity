using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LedgeRoomControl : RoomControl
{
    public DetectIfEntered UserEnteredCheckDetector;
    public DetectIfEntered UserAlmostFinishedDetector;
    public DetectIfEntered UserFinishedDetector;

    void OnEnable()
    {
        UserEnteredCheckDetector.OnTriggerEnterAction += UserEntered;
        UserAlmostFinishedDetector.OnTriggerEnterAction += UserAboutToFinish;
        UserFinishedDetector.OnTriggerEnterAction += UserFinished;
    }

    void OnDisable()
    {
        UserEnteredCheckDetector.OnTriggerEnterAction -= UserEntered;
        UserAlmostFinishedDetector.OnTriggerEnterAction -= UserAboutToFinish;
        UserFinishedDetector.OnTriggerEnterAction -= UserFinished;
    }
}

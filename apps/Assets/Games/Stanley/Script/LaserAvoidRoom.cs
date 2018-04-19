using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserAvoidRoom : WalkThroughRoom
{
    public GameObject AvoidGameLasers;

    public override void InitializeRoom()
    {
        AvoidGameLasers.SetActive(true);
    }

    public override void ResetRoom()
    {
        AvoidGameLasers.SetActive(false);
    }
}

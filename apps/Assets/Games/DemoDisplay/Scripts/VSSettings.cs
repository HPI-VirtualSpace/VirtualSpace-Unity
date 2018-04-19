using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VirtualSpace.Shared;

public class VSSettings : MonoBehaviour
{
    public VSManager Manager;

    public StrategySettings StrategySettings;

    void OnEnable()
    {
        Manager.EventHandler.Attach(typeof(StrategySettings), OnStrategySettings);
    }

    void OnDisable()
    {
        Manager.EventHandler.Detach(typeof(StrategySettings), OnStrategySettings);
    }

    private void OnStrategySettings(IMessageBase messageBase)
    {
        StrategySettings = (StrategySettings) messageBase;
    }

    public void SendToBackend()
    {
        if (Manager != null)
            Manager.SendReliable(StrategySettings);
    }
}

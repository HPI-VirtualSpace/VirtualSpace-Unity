using System;
using UnityEngine.Networking;

//client class (display), responsible for deriving state on phone and display similar scene (IK etc.)
namespace Assets.Scripts.VirtualSpace.Unity_Specific
{
    public class DemoDisplayNetworkDiscovery : NetworkDiscovery
    {
        public Action<string, string> BroadcastEvent;
        

        public override void OnReceivedBroadcast(string fromAddress, string data)
        {
            if(BroadcastEvent != null)
                BroadcastEvent.Invoke(fromAddress, data);
        }

    }
}

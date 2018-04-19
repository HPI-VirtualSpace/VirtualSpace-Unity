using System;
using Assets.ViveClient;
using UnityEngine;
using UnityEngine.UI;

public class VirtualSpaceFpsInfo : MonoBehaviour
{
    public Text InfoText;
    public VirtualSpaceHandler Handler;
    public SteamVrReceiver Receiver;
	
	void Update ()
	{
	    var fps = (int)(1f / Time.unscaledDeltaTime);
	    var vivePps = Receiver.GetPps();
	    var vsPps = Handler.GetPps();

	    InfoText.text = "fps: " + fps + Environment.NewLine + "vive pps: " + vivePps + Environment.NewLine + "vs pps: " + vsPps;
	}
}

using System.Collections.Generic;
using UnityEngine;
using Assets.ViveClient;
using VirtualSpaceVisuals;

public class SpaceInvadersController : MonoBehaviour {

    public SteamVrReceiver Receiver;
    public CameraRig CamRig;
    public string Follow;
    public VirtualSpacePreferenceSender VsSender;
    public bool sendPreferences = false;

    private int _followIndex;
    private Transform _follow;
    
	void Update () {
        if (_follow == null || _followIndex != CamRig.PlayerPostfix || _flag)
        {
            //if(CamRig.Target != null)
            //{
                var target = Receiver.GetTrackable(Follow + CamRig.PlayerPostfix);
                if (target != null)
                {
                _flag = false;
                    _follow = target.transform;
                    _followIndex = CamRig.PlayerPostfix;
                    if (sendPreferences)
                        VsSender.UpdatePreferences(new List<string>{target.name});
                }
            //}
        }
        else
        {
            transform.position = _follow.position;
            transform.rotation = _follow.rotation;
        }

    }

    void OnEnable()
    {
        SteamVrReceiver.OnNewTrackingData += GenerateNew;
    }

    void OnDisable()
    {
        SteamVrReceiver.OnNewTrackingData -= GenerateNew;
    }

    private bool _flag;
    private void GenerateNew()
    {
        _flag = true;
    }
}

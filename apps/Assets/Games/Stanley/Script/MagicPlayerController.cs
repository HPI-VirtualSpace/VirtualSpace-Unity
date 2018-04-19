using System.Collections;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using UnityEngine;

public class MagicPlayerController : MonoBehaviour
{
    public bool ShowDamageOverlay = true;
    [SerializeField]
    private int _laserHitCount = 0;
    public PlayMakerFSM DamageOverlayFsm;
    private FsmBool _showDamageOverlay;

    void Start()
    {
        _showDamageOverlay = 
            DamageOverlayFsm.FsmVariables.GetFsmBool("ShowDamageOverlay");
    }
    

    public void HitByLaser(LaserControl laser, Vector3 position)
    {
        _laserHitCount++;
    }

    public void LeftLaser(LaserControl laser)
    {
        _laserHitCount--;
    }

    public void Update()
    {
        if (_laserHitCount > 0 && ShowDamageOverlay)
            _showDamageOverlay.Value = true;
        else
            _showDamageOverlay.Value = false;
    }
}

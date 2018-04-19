using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

//[ExecuteInEditMode]
public class LaserControl : MonoBehaviour
{
    public Transform Emitter;
    public Transform EmissionTarget;
    public Transform EmissionSmoke;
    private MagicPlayerController _player;
    private LineRenderer _laserLine;
    public float LaserWidth = .1f;
    public float MaxLaserLength = 4f;
    private bool _hitPlayerLastRound = false;

    void Start ()
    {
        _laserLine = GetComponent<LineRenderer>();
        _player = FindObjectOfType<MagicPlayerController>();
    }

    void Update () {
        RaycastHit hitInfo;

        RaycastHit[] allHits = Physics.RaycastAll(Emitter.position, Emitter.forward, MaxLaserLength);
        RaycastHit solidHit;
        bool hitSolid = false;

        foreach (var hit in allHits)
        {
            if (!hit.collider.isTrigger)
            {
                solidHit = hit;
                hitSolid = true;

                EmissionTarget.position = solidHit.point;

                EmissionTarget.gameObject.SetActive(true);
                if (solidHit.collider.gameObject.CompareTag("Player"))
                {
                    PlayerInLaser(solidHit);
                }
                else
                {
                    PlayerNotInLaser();
                }

                break;
            }
        }

        if (!hitSolid)
	    {
	        EmissionTarget.position = Emitter.position + Emitter.forward * MaxLaserLength;

            EmissionTarget.gameObject.SetActive(false);

	        PlayerNotInLaser();
        }

        // TODO only do this on updates
        _laserLine.startWidth = LaserWidth;
	    _laserLine.endWidth = LaserWidth;
	    _laserLine.SetPosition(0, Emitter.position);
	    _laserLine.SetPosition(1, EmissionTarget.position);
    }

    private void PlayerInLaser(RaycastHit hitInfo)
    {
        if (!_hitPlayerLastRound)
        {
            _player.HitByLaser(this, hitInfo.point);
            //Debug.Log("Hitting " + hitInfo.collider.name);
            EmissionSmoke.gameObject.SetActive(true);
        }
        _hitPlayerLastRound = true;
    }

    private void PlayerNotInLaser()
    {
        //Debug.Log("Player not in direction");
        EmissionSmoke.gameObject.SetActive(false);

        if (_hitPlayerLastRound)
        {
            _player.LeftLaser(this);
            _hitPlayerLastRound = false;
        }
    }
}

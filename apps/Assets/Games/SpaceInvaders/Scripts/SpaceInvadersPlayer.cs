using System.Collections.Generic;
using Assets.Games.SpaceInvaders.Scripts;
using UnityEngine;

namespace Games.SpaceInvaders.Scripts
{
    public class SpaceInvadersPlayer : MonoBehaviour {

        public SpaceInvadersShot Shot;
        public float CoolDown = 0.2f;
        public bool AutoFire = true;
        public SpaceInvadersGotHit GotHitPlayer;
        public Transform CamTransform;
        public SpaceInvadersPlayerAvatar Avatar;

        private List<SpaceInvadersShot> _shots;
        private float _lastShot;

        // Use this for initialization
        void Start () {
            //Shot.gameObject.SetActive(false);
            _shots = new List<SpaceInvadersShot>();

        }
	
        // Update is called once per frame
        void Update () {
            //Debug.Log(PointInPolygon(CamTransform.position, Area.MeshPoints));
            if ((AutoFire || Input.GetKey(KeyCode.Mouse0)) && 
                Time.time - _lastShot > CoolDown 
                && !GotHitPlayer.IsUnkillable() 
                && !GotHitPlayer.IsDefeated()
                && !Avatar.NoGo)
            {
                //shoot
                var shot = Instantiate(Shot);
                _shots.Add(shot);
                shot.transform.position = transform.position;
                shot.transform.forward = -transform.right;
                shot.gameObject.SetActive(true);
                shot.transform.SetParent(null);
                _lastShot = Time.time;
            }

            var shotsCopy = new List<SpaceInvadersShot>(_shots);
            foreach (var shot in shotsCopy)
            {
                if (!shot.CanDestroy()) continue;
                _shots.Remove(shot);
                Destroy(shot.gameObject);
            }
        }
    }
}

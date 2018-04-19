using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Games.SpaceInvaders.Scripts
{
    public class SpaceInvadersUFO : MonoBehaviour {

        public Transform Kid;
        public float AnimTime = 1f;
        public SpaceInvadersGotHit GotHit;
        public Vector3 KidEnd;
        [HideInInspector] public bool Done;
        public List<SpaceInvadersEnemyRow> Rows;
        public AudioSource AudioCloser;
        public Renderer BeamRenderer;
        public string RecommendedPosition = "RecommendedUFOPosition";
        public SpaceInvadersUfoShoot ShootingUfo;
        private Transform _recTrans;
        private Vector3 _kidStart;
        private bool _started;
        private float _animStarted;

        void Start () {
            Rows = Rows.Where(r => r.gameObject.activeSelf).ToList();
            GotHit.enabled = false;
            _kidStart = Kid.localPosition;
            _recTrans = GameObject.Find(RecommendedPosition).transform;
        }
    
        void Update () {
            if (!_started)
            {
                var done = Rows.All(r => r.Done);
                if (done)
                {
                    GoDown();
                    ShootingUfo.Shoot = true;
                }
            }
            else
            {
                var factor = (Time.time - _animStarted) / AnimTime;
                Kid.localPosition = Vector3.Lerp(_kidStart, KidEnd, factor);
            }

            var closestUnavailablePosition = _recTrans.position;
            var newPos = Vector3.Lerp(transform.position, closestUnavailablePosition, 0.1f);
            newPos.y = 0f;
            transform.position = newPos;
        }

        private void GoDown()
        {
            AudioCloser.Play();
            GotHit.enabled = true;
            _animStarted = Time.time;
            _started = true;
            BeamRenderer.enabled = false;
        }
    }
}

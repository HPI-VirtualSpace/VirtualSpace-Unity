using UnityEngine;

namespace Games.SpaceInvaders.Scripts
{
    public class SpaceInvadersBeam : MonoBehaviour {

        public float Repeat = 0.8f;
        public float Enhance = 2f;
        public Transform RefPosition;
        public SpaceInvadersGotHit UFO;
        private float _yRefStart;
        private float _yLocStart;
        private Renderer _rend;

        void Start () {
            _yRefStart = RefPosition.position.y;
            _yLocStart = transform.localScale.y;
            _rend = GetComponent<Renderer>();
        }
	
        // Update is called once per frame
        void Update () {
            if (!UFO.IsDefeated())
            {
                var factor = Mathf.PingPong(Time.time, Repeat);
                var localScale = Vector3.one * (1f + factor * Enhance);
                var yRefFactor = RefPosition.position.y / _yRefStart;
                localScale.y = yRefFactor * _yLocStart * 1.15f;
                transform.localScale = localScale;
                var position = RefPosition.position;
                position.y = position.y / 2f;
                transform.position = position;
            }
            else
            {
                _rend.enabled = false;
            }

        }
    }
}

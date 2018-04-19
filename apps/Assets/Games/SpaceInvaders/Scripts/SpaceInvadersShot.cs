using UnityEngine;

namespace Games.SpaceInvaders.Scripts
{
    public class SpaceInvadersShot : MonoBehaviour
    {
        public float Lifetime = 10f;
        public float InitTime = 1f;
        public float Speed = 3f;
    
        private bool _destroy;
        private float _timeStarted;
        private bool _enabled;

        private static int _count;

        // Use this for initialization
        void Start()
        {
            _timeStarted = Time.unscaledTime;
            var rigid = GetComponent<Rigidbody>();
            rigid.velocity = transform.forward * Speed;
            _count++;
            gameObject.name = "shotPlayer_" + _count;
        }

        // Update is called once per frame
        void Update()
        {
            if (!_destroy && Time.time - _timeStarted > Lifetime)
                _destroy = true;
            if (!_enabled && Time.time - _timeStarted > InitTime)
            {
                _enabled = true;
                var bc = GetComponent<BoxCollider>();
                bc.enabled = true;
            }

            //transform.position = transform.position + transform.forward * Speed * Time.deltaTime;
        }

        void OnTriggerEnter(Collider colliderEnter)
        {
            if (Time.unscaledTime - _timeStarted < InitTime)
                return;
            _destroy = true;
            var enemy = colliderEnter.gameObject.GetComponent<SpaceInvadersEnemy>();
        
            if (enemy == null)
            {
                var childHit = colliderEnter.gameObject.GetComponentInParent<SpaceInvadersGotHit>();
                var hit = colliderEnter.gameObject.GetComponent<SpaceInvadersGotHit>();
                if (childHit != null)
                    childHit.Evaluate(colliderEnter.gameObject, true);
                else if (hit != null)
                    hit.Evaluate(colliderEnter.gameObject, true);

                return;
            }

            enemy.Burst();

            _destroy = true;
        }

        public bool CanDestroy()
        {
            return _destroy;
        }
    }
}

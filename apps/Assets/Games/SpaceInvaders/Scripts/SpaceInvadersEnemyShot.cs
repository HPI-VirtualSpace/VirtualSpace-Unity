using UnityEngine;
using VirtualSpace.Shared;

namespace Games.SpaceInvaders.Scripts
{
    public class SpaceInvadersEnemyShot : MonoBehaviour
    {
    
        public float Speed = 5f;
        public float TimeBeforeStart = 1f;
        public float TimeBeforeEnd = 10f;
        public float TimeStep = 0.2f;
        //public GameObject Player;
        [HideInInspector]
        public Vector3 ValidPosition;
        public static Polygon Shield;
        public string HitOnly = "Hitter";
        //public static VirtualSpaceHandler Handler;

        private float _lastChecked;
        private float _timeStarted;
        private bool _destroy;
        private bool _enabled;

        private static int _count;
        // Use this for initialization
        void Start()
        {
            _timeStarted = Time.unscaledTime;
            var rigid = GetComponent<Rigidbody>();
            rigid.velocity = transform.forward * Speed;
            gameObject.name = "shotEnemy_" + ++_count;
            SpaceInvadersVisualizeObjects.Visualizer.Add(this);
        }

        // Update is called once per frame
        void Update()
        {
            if (!_destroy && Time.time - _timeStarted > TimeBeforeEnd)
                _destroy = true;
            //if (Time.time + TimeStep > _lastChecked && Shield != null)
            //{
            //    _lastChecked = Time.time;
            //    var unityShield = Handler._TranslateIntoUnityCoordinates(Shield);
            //    var shielded = VirtualSpaceHandler.PointInPolygon(transform.position, unityShield, 0f);
            //    if (shielded)
            //    {
            //        _destroy = true;
            //        //var effect = Instantiate(ShieldedEffect);
            //        //effect.transform.position = transform.position;
            //        //effect.transform.LookAt(Camera.main.transform);
            //        //TODO burst - beam is three cubes
            //    }
            //}

            if (!_enabled && Time.time - _timeStarted > TimeBeforeStart)
            {
                _enabled = true;
                var bc = GetComponent<BoxCollider>();
                bc.enabled = true;
            }
            if (_destroy)
            {
                Destroy(gameObject);
            }

            //transform.position = transform.position + transform.forward* Speed*Time.deltaTime;
        }

        void OnTriggerEnter(Collider collider)
        {
            if (Time.unscaledTime - _timeStarted < TimeBeforeStart || !collider.gameObject.CompareTag(HitOnly))
                return;

            _destroy = true;
            SpaceInvadersVisualizeObjects.Visualizer.Remove(this);

            var childGotHit = collider.gameObject.GetComponentInParent<SpaceInvadersGotHit>();
            var gotHit = collider.gameObject.GetComponent<SpaceInvadersGotHit>();

            if(childGotHit != null)
                childGotHit.Evaluate(collider.gameObject, false);
            else if (gotHit != null)
                gotHit.Evaluate(collider.gameObject, false);
        }

        public bool CanDestroy()
        {
            return _destroy;
        }
    }
}

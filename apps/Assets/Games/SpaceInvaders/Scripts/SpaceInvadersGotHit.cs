using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Games.SpaceInvaders.Scripts
{
    public class SpaceInvadersGotHit : MonoBehaviour
    {

        //public bool BurstAllElseOne;
        public float DestroyTime = 4;
        public bool AllowPlayerDestroy;
        public bool AllowEnemyDestroy = true;
        public AudioSource Audio;
        public float Unkillable;
        public List<GameObject> Show;
        public bool CanBeShielded;
        public bool OneIsBurst;

        //private bool _saveFromHarm;
        private float _destroyTime;
        private bool _destroyed;
        private bool _willDestroy;
        private bool _show;
        private bool _started;
    
        void Update ()
        {
            if (_destroyed) return;
            //_saveFromHarm = CanBeShielded && SpaceInvadersShield.IsShielded(transform.position);
            if (Time.time < Unkillable)
            {
                var show = Mathf.FloorToInt(Time.time * 4f) % 2 == 0;
                if(show != _show)
                {
                    _show = show;
                    foreach(var s in Show)
                    {
                        s.SetActive(_show);
                    }
                }
            } else if (!_started)
            {
                _started = true;
                foreach (var s in Show)
                {
                    s.SetActive(true);
                }
            }


            if (_willDestroy && Time.unscaledTime > _destroyTime + DestroyTime)
                _destroyed = true;
        }

        public void Evaluate(GameObject go, bool isPlayer)
        {
            if (Time.time < Unkillable)
                return;
            //if (_saveFromHarm)
            //    return;
            if (isPlayer && !AllowPlayerDestroy)
                return;
            if (!isPlayer && !AllowEnemyDestroy)
                return;
            if (_willDestroy) return;
            var isChild = gameObject != go;
            if (!isChild)
            {
                _willDestroy = true;
                _destroyTime = Time.unscaledTime;
                var kids = GetComponentsInChildren<Transform>().Where(t => t != transform).ToList();
                foreach (var t in kids)
                {
                    if (t.gameObject.GetComponent<BoxCollider>() == null)
                    {
                        t.gameObject.AddComponent<BoxCollider>();
                    }
                    var rb = t.gameObject.AddComponent<Rigidbody>();
                    rb.useGravity = false;
                    var s = rb.gameObject.AddComponent<Shrinker>();
                    t.SetParent(null);
                    s.ShrinkOver = DestroyTime;
                    rb.isKinematic = false;
                    rb.transform.parent = null;
                }
                if (Audio != null)
                    Audio.Play();
            }
            else
            {
                if (go.GetComponent<BoxCollider>() == null)
                    go.AddComponent<BoxCollider>();
                var rb = go.GetComponent<Rigidbody>() ?? go.AddComponent<Rigidbody>();
                rb.useGravity = false;
                var s = rb.gameObject.AddComponent<Shrinker>();
                s.ShrinkOver = DestroyTime;
                go.transform.SetParent(null);
                rb.isKinematic = false;
            }
        }

        public bool IsDestroyed()
        {
            return _destroyed;
        }

        public bool IsDefeated()
        {
            return _willDestroy;
        }

        public bool IsUnkillable()
        {
            return Time.time < Unkillable;
        }
    }
}

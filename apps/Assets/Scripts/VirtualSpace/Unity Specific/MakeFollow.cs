using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VirtualSpace.Unity_Specific
{
    public class MakeFollow : MonoBehaviour
    {
        public bool Reverse;
        public List<Transform> From;
        public string[] ToAsString;
        public int UpdateRate = 19;

        private Transform[] _to;
        private List<Vector3> _old1FromPositions, _old2FromPositions;
        private List<Quaternion> _old1FromRotations, _old2FromRotations;
        private float _timePassed;
        private bool _found;

        private void Start()
        {
            _to = new Transform[ToAsString.Length];
            _old1FromPositions = From.Select(f => f.position).ToList();
            _old2FromPositions = From.Select(f => f.position).ToList();
            _old1FromRotations = From.Select(f => f.rotation).ToList();
            _old2FromRotations = From.Select(f => f.rotation).ToList();
        }

        void Update ()
        {
            if (!_found)
            {
                for (var i = 0; i  < ToAsString.Length; i++)
                {
                    if(ToAsString[i] == "")
                        continue;
                    var go = GameObject.Find(ToAsString[i]);
                    if (go != null)
                    {
                        ToAsString[i] = "";
                        _to[i] = go.transform;
                    }
                }
                _found = ToAsString.All(tas => tas == "");
                if (_found)
                {
                    if (Reverse)
                    {
                        var tmp = new List<Transform>(_to);
                        _to = From.ToArray();
                        From = tmp.ToList();
                    }
                }
                else return;
            }

            if (UpdateRate <= 0)
            {
                //never interpolate
                for (var i = 0; i < From.Count; i++)
                {
                    _to[i].position = From[i].localPosition;
                    _to[i].rotation = From[i].localRotation;
                }
            }
            else
            {
                //interpolate
                _timePassed += Time.unscaledDeltaTime;
                if (_timePassed > 1f / UpdateRate)
                {
                    _timePassed -= 1f/UpdateRate;
                    _old2FromPositions = _old1FromPositions;
                    _old2FromRotations = _old1FromRotations;
                    _old1FromPositions = From.Select(f => f.localPosition).ToList();
                    _old1FromRotations = From.Select(f => f.localRotation).ToList();
                }
                var timeDelta = Mathf.Clamp01(_timePassed * UpdateRate);
                for (var i = 0; i < From.Count; i++)
                {
                    _to[i].localPosition = Vector3.Lerp(_old2FromPositions[i], _old1FromPositions[i], timeDelta);
                    _to[i].localRotation = Quaternion.Lerp(_old2FromRotations[i], _old1FromRotations[i], timeDelta);
                }
            }
	    
        }
    }
}

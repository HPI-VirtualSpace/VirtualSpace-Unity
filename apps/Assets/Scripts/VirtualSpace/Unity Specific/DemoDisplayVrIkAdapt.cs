using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.VirtualSpace.Unity_Specific
{
    public class DemoDisplayVrIkAdapt : MonoBehaviour
    {
        public float HeightMin, HeightMax, LocalSizeMin, LocalSizeMax;
        public Transform Headmount;
        public Transform Ground;
        public float AllowedOffset;
        public float UpdateRate;
        public float HistoryBuffer = 10;

        private List<float> _history;
        private float _updateTimer;
        private float _startLocalScale, _goalLocalScale;

        void Start () {
            _history = new List<float>();
            var locPos = transform.localPosition;
            locPos.y += Ground.position.y;
            transform.localPosition = locPos;
            _startLocalScale = HeightMin;
            _goalLocalScale = HeightMin;
        }
	
        void Update ()
        {
            _updateTimer -= Time.unscaledDeltaTime;
            if (_updateTimer < 0f)
            {
                if(TryUpdateHeight())
                    _updateTimer = UpdateRate;
            }
            transform.localScale = Vector3.Lerp(Vector3.one * _goalLocalScale, Vector3.one * _startLocalScale, _updateTimer/UpdateRate);
        }

        private bool TryUpdateHeight()
        {
            var angle = Vector3.Angle(Vector3.up, Headmount.up);
            if (angle > AllowedOffset)
                return false;
            var height = Headmount.position.y;
            if(_history.Any())
                _history.RemoveAt(0);
            while (_history.Count < HistoryBuffer)
                _history.Add(height);
            var median = Median(_history);
            var x = (median - HeightMin) / (HeightMax - HeightMin);
            var y = LocalSizeMin + (LocalSizeMax - LocalSizeMin) * x;
            _startLocalScale = _goalLocalScale;
            _goalLocalScale = y;
            return true;
        }

        private float Median(IEnumerable<float> xs)
        {
            var ys = xs.OrderBy(x => x).ToList();
            var mid = (ys.Count - 1) / 2.0f;
            return (ys[(int)(mid)] + ys[(int)(mid + 0.5f)]) / 2f;
        }
    }
}

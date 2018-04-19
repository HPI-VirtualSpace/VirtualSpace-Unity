using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.VirtualSpace.Unity_Specific
{
    public class DemoDisplayInterpolatePosition : MonoBehaviour
    {

        public Transform ToFollow;
        public bool LookAtFollow;
        public Vector3 PositionalOffset;
        //public Quaternion RotationalOffset;
        public bool Clear;
        public static float Lag = 1f;
        public float MinDist = 0.01f;
        public float MinRot = 1f;

        private List<Info> _todo;

        private struct Info
        {
            public float Time;
            public Vector3 Pos;
            public Quaternion Qua;
        }

        void Start()
        {
            _todo = new List<Info>();
        }
        
        private void Update()
        {
            if (ToFollow == null)
                return;

            if (Clear)
            {
                Clear = false;
                _todo.Clear();
            }

            if (!_todo.Any() || 
                Vector3.Distance(_todo.Last().Pos, ToFollow.position) >= MinDist || 
                Quaternion.Angle(_todo.Last().Qua, ToFollow.rotation) >= MinRot)
            {
                var info = new Info
                {
                    Time = Time.unscaledTime,
                    Pos = ToFollow.position,
                    Qua = ToFollow.rotation
                };
                _todo.Add(info);
            }

            var laggedTime = Time.unscaledTime - Lag;

            var lerpToIndex = 0;
            while (lerpToIndex < _todo.Count)
            {
                var isLerpToIndex = _todo[lerpToIndex].Time >= laggedTime;
                if (!isLerpToIndex)
                {
                    lerpToIndex++;
                }
                else break;
            }
            if (lerpToIndex == _todo.Count)
                lerpToIndex = _todo.Count - 1;
            var lerpFromIndex = lerpToIndex - 1;
            if (lerpFromIndex >= 0)
                _todo.RemoveRange(0, lerpFromIndex);
            lerpToIndex -= lerpFromIndex;
            lerpFromIndex = 0;
            if (_todo.Count == 0)
                return;

            //interpolate
            var interpolatedPosition = Vector3.zero;
            var interpolatedRotation = Quaternion.identity;
            if (_todo.Count > 1)
            {
                var delta = (laggedTime - _todo[lerpFromIndex].Time) /
                            (_todo[lerpToIndex].Time - _todo[lerpFromIndex].Time);
                interpolatedPosition = Vector3.Lerp(_todo[lerpFromIndex].Pos, _todo[lerpToIndex].Pos, delta);
                interpolatedRotation = Quaternion.Lerp(_todo[lerpFromIndex].Qua, _todo[lerpToIndex].Qua, delta);
            }
            else
            {
                interpolatedPosition = _todo[0].Pos;
                interpolatedRotation = _todo[0].Qua;
            }
            if (LookAtFollow)
            {
                transform.position = interpolatedPosition;
                transform.rotation = interpolatedRotation;
                var posOff = transform.TransformPoint(PositionalOffset);
                transform.position = posOff;
                transform.LookAt(interpolatedPosition);
            }
            else
            {
                transform.position = PositionalOffset + interpolatedPosition;
                transform.rotation = interpolatedRotation;
            }
        }
    }
}

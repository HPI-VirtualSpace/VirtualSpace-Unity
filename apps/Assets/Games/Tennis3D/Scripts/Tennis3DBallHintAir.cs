using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VirtualSpace;
using VirtualSpace.Shared;


namespace Games.Tennis3D.Scripts
{
    public class Tennis3DBallHintAir : MonoBehaviour {

        public VirtualSpaceHandler Handler;
        public float CognitiveProcessing = 0.4f;
        public Renderer CircleRenderer;
        public float ScaleMin = 1f;
        public GameObject CounterObject;
        public Vector3 Offset;

        private bool _isAnimating;
        private float _animStart;
        private float _animEnd;
        private Vector3 _posEnd;
        private Vector3 _posStart;
        private Vector3 _scaleStart;
        private Vector3 _scaleEnd;
        private bool _init;

        void Start()
        {
            _scaleEnd = Vector3.one * ScaleMin;
            _scaleEnd.y = transform.localScale.y;
            transform.localScale = _scaleEnd;
        }

        public Easing EasingType;
        public enum Easing
        {
            Quadratic,
            Sinusoidal,
            Circular
        }

        private float Ease(float factor)
        {
            var retVal = 0f;
            switch (EasingType)
            {
                case Easing.Sinusoidal:
                    retVal = -0.5f * (Mathf.Cos(Mathf.PI * factor) - 1);
                    break;
                case Easing.Quadratic:
                    var t = factor * 2f;
                    if (t < 1) retVal = 0.5f * t * t;
                    else
                    {
                        t--;
                        retVal = -0.5f * (t * (t - 2) - 1);
                    }
                    break;
                case Easing.Circular:
                    var tc = factor * 2f;
                    if (tc < 1) return -0.5f * (Mathf.Sqrt(1 - tc * tc) - 1);
                    tc -= 2;
                    retVal = 0.5f * (Mathf.Sqrt(1 - tc * tc) + 1);
                    break;
            }
            return retVal;
        }

        private void OnEnabled()
        {
            Handler.OnEventsChanged += CheckWithVirtualSpaceForFastTransitions;
            Handler.OnReceivedNewEvents += CheckWithVirtualSpaceForFastTransitions;
        }

        private void OnDisabled()
        {
            // ReSharper disable once DelegateSubtraction
            Handler.OnEventsChanged -= CheckWithVirtualSpaceForFastTransitions;
            // ReSharper disable once DelegateSubtraction
            Handler.OnReceivedNewEvents -= CheckWithVirtualSpaceForFastTransitions;
        }

        private void CheckWithVirtualSpaceForFastTransitions()
        {
            var actives = Handler.ActiveOrPendingTransitions;
            if (actives == null || actives.Count == 0)
            {
                return;
            }
            var transition = actives.FirstOrDefault(ap => ap.Speed > 0.001f);
            if (transition == null)
            {
                return;
            }
            _isAnimating = true;

            //var turnNow = VirtualSpaceTime.CurrentTurn;
            _animStart = VirtualSpaceTime.ConvertTurnsToSeconds(transition.TurnStart - VirtualSpaceTime.CurrentTurn);
            _animEnd = VirtualSpaceTime.ConvertTurnsToSeconds(transition.TurnEnd - VirtualSpaceTime.CurrentTurn);
            _animStart -= CognitiveProcessing;
            _animEnd += Time.unscaledTime;
            _animStart += Time.unscaledTime;

            //var polyStart = transition.Frames.First().Area.Area;
            var polyEnd = transition.Frames.Last().Area.Area;
            var centerEnd = Handler._TranslateIntoUnityCoordinates(transition.Frames.Last().Position.Position);
            //var areaStart = Handler._TranslateIntoUnityCoordinates(polyStart);
            var areaEnd = Handler._TranslateIntoUnityCoordinates(polyEnd);
            // var unitsStart = ClipperUtility.GetArea(polyStart);
            //var unitsEnd = ClipperUtility.GetArea(polyEnd);
            _posStart = _posEnd;//GetCentroid(areaStart);//, (float) unitsStart);
            _posEnd = GetCentroid(areaEnd);//, (float) unitsEnd);
            _init = true;
            _posEnd += _posEnd + Offset;

            _scaleStart = _scaleEnd;//GetScale(areaStart, _posStart);
            _scaleEnd = GetScale(areaEnd, _posEnd);

            //Debug.Log(VirtualSpaceTime.IsInitialized + " " + _animStart + " " + _animEnd + "   ");
            Debug.DrawRay(_posEnd, Vector3.up * _scaleEnd.magnitude, Color.red, 2f);
            Debug.DrawRay(_posStart, Vector3.up * _scaleStart.magnitude, Color.yellow, 2f);
            Debug.DrawLine(_posEnd, _posStart, Color.magenta, 2f);

        }

        private Vector3 GetScale(List<Vector3> poly, Vector3 centroid)
        {
            var zVal = 3f;
            var xVal = 3f;
            for (var i = 0; i < poly.Count; i++)
            {
                var from = poly[i];
                var to = poly[i + 1 == poly.Count ? 0 : i + 1];
                var heading = to - from;
                var pos = Vector3.Project(centroid - from, heading.normalized) + from;
                if (from.x < centroid.x && to.x > centroid.x ||
                    from.x > centroid.x && to.x < centroid.x)
                {
                    var val = Mathf.Abs(pos.z - centroid.z);
                    zVal = Mathf.Min(val, zVal);
                }
                else if (from.z < centroid.z && to.z > centroid.z ||
                         from.z > centroid.z && to.z < centroid.z)
                {
                    var val = Mathf.Abs(pos.x - centroid.x);
                    xVal = Mathf.Min(val, xVal);
                }
            }
            zVal = Mathf.Max(1f, zVal);
            xVal = Mathf.Max(1f, xVal);
            return new Vector3(zVal, 0f, xVal);
        }

        void Update()
        {
            if(CircleRenderer != null)
                CircleRenderer.enabled = !CounterObject.activeSelf;

            if (VirtualSpaceCore.Instance.IsRegistered())
            {
                if (!_isAnimating)
                {
                    CheckWithVirtualSpaceForFastTransitions();
                }
            }
            if (_isAnimating)
            {
                var factor = Mathf.Clamp01((Time.unscaledTime - _animStart) / (_animEnd - _animStart));
                var ease = Ease(factor);
                var animCenter = Vector3.Lerp(_posStart, _posEnd, ease);
                transform.position = animCenter;
                if (Time.unscaledTime > _animEnd)
                    _isAnimating = false;

                var newScale = Vector3.Lerp(_scaleStart, _scaleEnd, ease);
                newScale.y = transform.localScale.y;
                //transform.localScale = newScale;
            }
            else if (!_init)
            {
                transform.position = Handler.PlayerArea.CenterOfArea + Offset;
            
            }
        }

        public static Vector3 GetCentroid(List<Vector3> poly)
        {
            float accumulatedArea = 0.0f;
            float centerX = 0.0f;
            float centerZ = 0.0f;

            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                float temp = poly[i].x * poly[j].z - poly[j].x * poly[i].z;
                accumulatedArea += temp;
                centerX += (poly[i].x + poly[j].x) * temp;
                centerZ += (poly[i].z + poly[j].z) * temp;
            }

            if (Mathf.Abs(accumulatedArea) < 1E-7f)
                return Vector3.zero;  // Avoid division by zero

            accumulatedArea *= 3f;
            return new Vector3(centerX / accumulatedArea, 0f, centerZ / accumulatedArea);
        }

        // Find the polygon's centroid.
        public Vector3 FindCentroid(List<Vector3> points, float areaUnits)
        {
            // Add the first point at the end of the array.
            var pointsExt = new List<Vector3>(points);
            pointsExt.Add(points.First());

            // Find the centroid.
            float X = 0;
            float Y = 0;
            for (var i = 0; i < points.Count; i++)
            {
                var secondFactor = pointsExt[i].x * pointsExt[i + 1].z -
                                   pointsExt[i + 1].x * pointsExt[i].z;
                X += (pointsExt[i].x + pointsExt[i + 1].x) * secondFactor;
                Y += (pointsExt[i].z + pointsExt[i + 1].z) * secondFactor;
            }

            // Divide by 6 times the polygon's area.
            X /= (6 * areaUnits);
            Y /= (6 * areaUnits);

            // If the values are negative, the polygon is
            // oriented counterclockwise so reverse the signs.
            if (X < 0)
            {
                X = -X;
                Y = -Y;
            }

            return new Vector3(X, 0f, Y);
        }


    }
}

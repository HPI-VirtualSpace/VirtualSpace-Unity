using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using VirtualSpace;
using VirtualSpace.Shared;

namespace Games.SpaceInvaders.Scripts
{
    public class SpaceInvadersShield : MonoBehaviour
    {
        public float MaxSpeed;
        //public float FixedTimestep = 0.1f;
        public float RelativeTimeInFuture;
        //public GameObject ShieldEffect;
        public VirtualSpaceHandler Handler;
       // public float EvalUpdate = 0.1f;
        public GameObject WalkableArea;
        public GameObject Obstacle;
        public float CognitiveProcessing = 0.4f;
        public BoxCollider BoxAdapt;
        public float DistanceToMiddle = 0.4f;
        public Vector3 OriginLevel;
        public float ScaleMin = 1f;

        private bool _isAnimating;
        private float _animStart;
        private float _animEnd;
        private Vector3 _posEnd;
        private Vector3 _posStart;
        private Vector3 _scaleStart;
        private Vector3 _scaleEnd;
        //private List<GameObject> _initObstacles;
        //private static VirtualSpaceHandler staticSpaceHandler;
        //private static List<Vector3> shield;
        //private List<Vector3> _walkable;
        //private Transform _follow;
        // private float _lastEval;

        public float Result;
        [Range(0f, 1f)]
        public float mySliderFloat;

        void Start ()
        {
            _scaleEnd = Vector3.one * ScaleMin;
            _scaleEnd.y = BoxAdapt.transform.localScale.y;
            BoxAdapt.transform.localScale = _scaleEnd;
            //_isAnimating = true;
            //if (VirtualSpaceCore.Instance.IsRegistered())
            //{
            //    List<Vector3> area;
            //    Vector3 center;
            //    Handler.SpaceAtTimeWithSafety(0f, 0f, out area, out center);
            //    Debug.Log(center);
            //    _animStart = Time.unscaledTime;
            //    _animEnd = Time.unscaledTime + 1f;
            //    _posEnd = GetCentroid(area);
            //    _posEnd += (Vector3.Distance(center, transform.position) > 0.01f ? (_posEnd - transform.position).normalized * DistanceToMiddle : Vector3.zero);
            //    _scaleEnd = GetScale(area, _posEnd);
            //}

            //_follow = Camera.main.transform;
            //Time.fixedDeltaTime = FixedTimestep;
            //_initObstacles = new List<GameObject>();
            //SpaceInvadersEnemyShot.Handler = Handler;
            //staticSpaceHandler = Handler;
            //SpaceInvadersEnemyShot.ShieldedEffect = ShieldEffect;
            //_walkable = WalkableArea.GetComponentsInChildren<Transform>().Where(t => t != WalkableArea)
            //    .Select(t => t.position).ToList();
        }

        //public static bool IsShielded(Vector3 position)
        //{
        //    if (shield == null || shield.Count < 3)
        //        return false;
        //    return VirtualSpaceHandler.PointInPolygon(position, shield, 0f);
        //}

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
                        retVal = - 0.5f * (t * (t - 2) - 1);
                    }
                    break;
                case Easing.Circular:
                    var tc = factor*2f;
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
                transition = actives.FirstOrDefault();//TODO
                //Debug.Log("Using first");
            }
            _isAnimating = true;

            // animend
            long turnEnd = transition.TurnEnd;
            if (transition.TurnEnd == long.MaxValue)
            {
                //Debug.Log("too big");
                turnEnd = VirtualSpaceTime.CurrentTurn + VirtualSpaceTime.ConvertSecondsToTurns(2f);
            }
            //Debug.Log("animation end " + turnEnd);

            //var turnNow = VirtualSpaceTime.CurrentTurn;
            _animStart = VirtualSpaceTime.ConvertTurnsToSeconds(transition.TurnStart - VirtualSpaceTime.CurrentTurn);
            _animEnd = VirtualSpaceTime.ConvertTurnsToSeconds(turnEnd - VirtualSpaceTime.CurrentTurn);
            //Debug.Log("animation end " + _animEnd);
            _animStart -= CognitiveProcessing;
            _animEnd += Time.unscaledTime;
            _animStart += Time.unscaledTime;

            //var polyStart = transition.Frames.First().Area.Area;
            var polyEnd = transition.Frames.Last().Area.Area;
            //var areaStart = Handler._TranslateIntoUnityCoordinates(polyStart);
            var areaEnd = Handler._TranslateIntoUnityCoordinates(polyEnd);
            // var unitsStart = ClipperUtility.GetArea(polyStart);
            //var unitsEnd = ClipperUtility.GetArea(polyEnd);
            _posStart = _posEnd;//GetCentroid(areaStart);//, (float) unitsStart);
            _posEnd = GetCentroid(areaEnd);//, (float) unitsEnd);

            _scaleStart = _scaleEnd;//GetScale(areaStart, _posStart);
            _scaleEnd = GetScale(areaEnd, _posEnd);
            //move outwards
            //if (Vector3.Distance(_posStart, OriginLevel) > 0.01f)
            //    _posStart += (_posStart - OriginLevel).normalized * DistanceToMiddle;
            if (Vector3.Distance(_posEnd, OriginLevel) > 0.01f)
                _posEnd += (_posEnd - OriginLevel).normalized * DistanceToMiddle;

            //Debug.Log(VirtualSpaceTime.IsInitialized + " " + _animStart + " " + _animEnd + "   ");
            Debug.DrawRay(_posEnd, Vector3.up * _scaleEnd.magnitude, Color.red, 2f);
            Debug.DrawRay(_posStart, Vector3.up * _scaleStart.magnitude, Color.yellow, 2f);
            Debug.DrawLine(_posEnd, _posStart, Color.magenta, 2f);

            //var frameThen = transition.GetActiveFrames(turnNow + VirtualSpaceTime.ConvertSecondsToTurns(RelativeTimeInFuture))
            //                    .FirstOrDefault() ?? transition.Frames.Last();
            //var validThen = frameThen.Area.Area;
            //var frameNow = transition.GetActiveFrames(turnNow).FirstOrDefault() ?? transition.Frames.First();
            //var validNow = frameNow.Area.Area;
            //var validPolygon = ClipperUtility.Intersection(validThen, validNow).FirstOrDefault() ?? validNow;
            //SpaceInvadersEnemyShot.Shield = validPolygon;
            //List<Vector3> protect = Handler._TranslateIntoUnityCoordinates(validPolygon);
            //foreach (var p in protect)
            //{
            //    center += p;
            //}
            //center /= protect.Count;

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

        void Update ()
        {
            Result = Ease(mySliderFloat);
            if (VirtualSpaceCore.Instance.IsRegistered())
            {
                //check if there is a new state in backend
                if (Handler.GetNewState())
                {
                    EvaluateStates(Handler.State);
                }

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
                newScale.y = BoxAdapt.transform.localScale.y;
                BoxAdapt.transform.localScale = newScale;
            }

            //for (var i = protect.Count; i < _initObstacles.Count; i++)
            //   {
            //       _initObstacles[i].SetActive(false);
            //   }
            //var followPos = center;//_follow.position;
            //   for (var i = 0; i < protect.Count; i++)
            //{
            //    var from = protect[i];
            //    var to = protect[i + 1 == protect.Count ? 0 : i+1];
            //    var heading = to - from;
            //    var pos = Vector3.Project(followPos - from, heading.normalized) + from;
            //    if (_initObstacles.Count <= i)
            //    {
            //        var newObstacle = Instantiate(Obstacle);
            //           _initObstacles.Add(newObstacle);
            //    }
            //    if (heading.magnitude > 0.5f)
            //    {
            //        _initObstacles[i].transform.position = pos;
            //        _initObstacles[i].transform.localRotation = Quaternion.identity;
            //           _initObstacles[i].transform.right = (pos - followPos).normalized;
            //        _initObstacles[i].SetActive(true);
            //    }
            //    else
            //    {
            //        _initObstacles[i].SetActive(false);
            //       }

            //}
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

        private void EvaluateStates(StateInfo info)
        {
            if (!VirtualSpaceCore.Instance.IsRegistered() || info == null)
                return;

            var voting = new TransitionVoting { StateId = info.StateId };
        
            List<List<Vector3>> stateAreasAsList;
            List<Polygon> stateAreas;
            List<Vector3> stateCenters;
            List<Vector3> startAreaAsList;
            Polygon startArea;
            Vector3 startCenter;
            Handler.DeserializeState(info, out stateAreasAsList, out stateAreas, out stateCenters,
                out startCenter, out startArea, out startAreaAsList);

            var distancesToNewCenter = new List<float>();
            var isBiggerClipper = new List<bool>();
            for (var i = 0; i < info.PossibleTransitions.Count; i++)
            {
                distancesToNewCenter.Add(Vector3.Distance(stateCenters[i], startCenter));
                var bigger = ClipperUtility.ContainsWithinEpsilon(stateAreas[i], startArea);
                isBiggerClipper.Add(bigger);
            }

            var valuation = new Dictionary<int, float>();
            for (var i = 0; i < info.PossibleTransitions.Count; i++)
            {
                valuation.Add(i, 0);
            }

            //states with high distance are important
            var distMax = distancesToNewCenter.Max();
            var distMin = distancesToNewCenter.Min();
            for (var i = 0; i < info.PossibleTransitions.Count; i++)
            {
                var val = (distancesToNewCenter[i] - distMin) / (distMax - distMin) * 0.8f;//70% importance
                //Debug.Log(val);
                valuation[i] += val;
            }

            //bigger areas are important
            var areaUnits = stateAreas.Select(ClipperUtility.GetArea).ToList();
            var areaUnitsMax = areaUnits.Max();
            var areaUnitsMin = areaUnits.Min();
            //var biggerThanMax = areaUnits.Select(au => au >= areaUnitsMax * 0.95d).ToList();
            //if (biggerThanMax.Count(bm => true) == 1) // areaUnits.IndexOf(areaUnitsMax)
            for (var i = 0; i < info.PossibleTransitions.Count; i++)
            {
                var val = (float)((areaUnits[i] - areaUnitsMin) / (areaUnitsMax - areaUnitsMin)) * 0.2f;//30% importance
                //Debug.Log(val);
                valuation[i] += val;
            }

            //remove states which are bigger (no travel time)
            for (var i = 0; i < info.PossibleTransitions.Count; i++)
            {
                if (!isBiggerClipper[i])
                    continue;
                valuation[i] = 0;
            }
        
            for (var i = 0; i < info.PossibleTransitions.Count; i++)
            {
                //remove focus state AND switch states //TODO?
                //remove states with no available tiles
                if (info.PossibleTransitions[i] == VSUserTransition.Focus)
                {
                    valuation[i] /= 4f;
                }
            }

            //normalize
            var valueMax = valuation.Values.Max();
            var weights = valuation.Select(v => 0).ToArray();
            var maxWeight = 100;
            for (var i = 0; i < valuation.Count; i++)
            {
                weights[i] = valueMax > 0f ? (int)(maxWeight * valuation[i] / valueMax) : 0;
            }

            //valuate
            for (var i = 0; i < info.PossibleTransitions.Count; i++)
            {
            
                var transition = info.PossibleTransitions[i];
                var vote = new TransitionVote
                {
                    Transition = transition,
                    Value = weights[i]
                };
            
                if (isBiggerClipper[i])
                {
                    vote.PlanningTimestampMs = new List<double> { 0d };
                    vote.ExecutionLengthMs = new List<double> { 0d };
                }
                else
                {
                    vote.PlanningTimestampMs = new List<double>();
                    vote.ExecutionLengthMs = new List<double>();
                
                    vote.PlanningTimestampMs.Add(2* CognitiveProcessing);
                    vote.ExecutionLengthMs.Add(1000d * distancesToNewCenter[i] / MaxSpeed);
                }

                voting.Votes.Add(vote);
            }
            //foreach (var vote in voting.Votes)
            //{
            //    for (var x = 0; x < vote.PlanningTimestampMs.Count; x++)
            //    {
            //        Debug.Log("vote " + vote.Transition + " " + vote.Weight + " " + vote.PlanningTimestampMs[x] + " " + vote.ExecutionLengthMs[x]);
            //    }
            //}

            VirtualSpaceCore.Instance.SendReliable(voting);
        }

    }
}

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using VirtualSpace;
using VirtualSpace.Shared;
using VirtualSpaceVisuals;
using EventType = VirtualSpace.Shared.EventType;

public class VirtualSpaceKeyframe
{
    public Vector3 Position;
    public List<Vector3> Area;
    public float AbsoluteUnityTime;
    public Vector3 DirectionSpeed;

    public VirtualSpaceKeyframe(Vector3 position, List<Vector3> area, float absoluteUnityTime, Vector3 directionSpeed)
    {
        Position = position;
        Area = area;
        AbsoluteUnityTime = absoluteUnityTime;
        DirectionSpeed = directionSpeed;
    }
}

public class VirtualSpaceHandler : MonoBehaviour
{
    #region CodeVariables
    public delegate void Reaction();
    /// <summary>
    /// Events were modified or deleted.
    /// </summary>
    public Reaction OnEventsChanged;
    /// <summary>
    /// Events were added.
    /// </summary>
    public Reaction OnReceivedNewEvents;
    public List<Transition> IncomingTransitions
    {
        get
        {
            lock (_events)
            {
                //Debug.Log("Event count " + _events.Count);
                List<Transition> transitions = new List<Transition>();
                foreach (TimedEvent potentialEvent in _events.Where(
                    event_ => VirtualSpaceTime.CurrentTurn < event_.TurnStart &&
                              event_.EventType == EventType.Transition))
                {
                    transitions.Add((Transition)potentialEvent);
                }
                return transitions;
            }
        }
    }
    public List<Transition> ActiveOrPendingTransitions
    {
        get
        {
            lock (_events)
            {
                List<Transition> transitions = new List<Transition>();
                foreach (TimedEvent potentialEvent in _events.Where(
                    event_ => VirtualSpaceTime.CurrentTurn < event_.TurnEnd &&
                              event_.EventType == EventType.Transition))
                {
                    transitions.Add((Transition)potentialEvent);
                }
                return transitions;
            }
        }
    }
    #endregion

    #region InspectorVariables
    //[Header("Provides information to the script")]
    [Header("NEED TO SET THIS! - Provides information to the script")]
    public VirtualSpaceSettings Settings;
    public VirtualSpaceRendering Rendering;
    //[Header("Is transformed by the script based on the transformation received by the backend")]
    //[Tooltip("Virtual Space coordinates are relative to Reference")]
    //public Transform Reference;
    [Header("DO NOT SET THIS!")]
    public Transform ViveTrackingSystem;
    public Transform PlayerPosition;
    public VirtualSpacePlayerArea PlayerArea;

    private long _lastTurnPlayerAreaUpdated;
    private int _packagesReceived;
    private List<float> _incentivesReceivedTimes;
    private Transform _reference;
    private double _angle;
    private Vector _offset;
    private bool _handleAlloc;
    private Polygon _nice, _must;
    [HideInInspector] public Polygon MustAreaTranslated;
    private readonly List<TimedEvent> _events = new List<TimedEvent>();
    private bool _receivedNewEvents;
    private bool _receivedEventChange;
    #endregion

    #region Initialization

    void Awake()
    {
        if (Rendering == null)
        {
            var go = GameObject.Find("VirtualSpaceSettingsAll");
            if(go != null)
                Rendering = go.GetComponent<VirtualSpaceRendering>();
        }
        if (Settings == null)
        {
            var go = GameObject.Find("VirtualSpaceSettingsSingle");
            if (go != null)
                Settings = go.GetComponent<VirtualSpaceSettings>();
        }

        _incentivesReceivedTimes = new List<float>();
        ViveTrackingSystem.localPosition = new Vector3(0f, Settings.ViveTrackingHeight, 0f);
        _reference = transform;
        PlayerArea.WallMaterial = Rendering.Wall;
        PlayerArea.GetComponent<Renderer>().material = Rendering.Ground;
        var walkableArea = Settings.WalkablePoints.GetComponentsInChildren<Transform>().Where(t => t != Settings.WalkablePoints).ToList();
        var priorityArea = Settings.PriorityPoints.GetComponentsInChildren<Transform>().Where(t => t != Settings.PriorityPoints).ToList();
        foreach (var waTransform in walkableArea)
        {
            waTransform.parent = transform;
        }
        foreach (var paTransform in priorityArea)
        {
            paTransform.parent = transform;
        }
        _nice = new Polygon(walkableArea.Select(t => new Vector(t.localPosition.x, t.localPosition.z)).ToList());
        _must = new Polygon(priorityArea.Select(t => new Vector(t.localPosition.x, t.localPosition.z)).ToList());

        MustAreaTranslated = new Polygon(priorityArea.Select(t => new Vector(t.position.x,t.position.z)).ToList());
        
    }

    void Start()
    {
        if (VirtualSpaceCore.Instance.IsRegistered())
            OnRegistrationSuccess(new RegistrationSuccess() { UserId = VirtualSpaceCore.Instance.UserId});
        VirtualSpaceCore.Instance.AddHandler(typeof(RegistrationSuccess), OnRegistrationSuccess);
        VirtualSpaceCore.Instance.AddHandler(typeof(AllocationGranted), OnAllocationGranted);
        VirtualSpaceCore.Instance.AddHandler(typeof(Incentives), OnIncentives);
        VirtualSpaceCore.Instance.AddHandler(typeof(StateInfo), OnStateInfo);
        VirtualSpaceCore.Instance.AddHandler(typeof(RecommendedTicks), OnRecommendedTicks);
        StartCoroutine(SendPosition());
    }

    public List<float> RecommendedTicksAbsolute
    {
        get
        {
            if (_recommendedTicks == null) return new List<float>(); 
            return _recommendedTicks.Select(rt => rt - VirtualSpaceTime.CurrentTimeInSeconds + Time.unscaledTime).ToList(); ;
        }
    }
    public List<float> RecommendedTicksRelative
    {
        get
        {
            if (_recommendedTicks == null) return new List<float>();
            return _recommendedTicks.Select(rt => rt - VirtualSpaceTime.CurrentTimeInSeconds).ToList();
        }
    }
    private List<float> _recommendedTicks;
    void OnRecommendedTicks(IMessageBase baseMessage)
    {
        var recTicks = (RecommendedTicks) baseMessage;

        _recommendedTicks = recTicks.TickSecondsLeft;
    }

    #endregion

    #region VirtualSpaceHandler
    
    [HideInInspector] public StateInfo State;
    private bool _newState;
    private object _stateLock = new object();

    public void OnStateInfo(IMessageBase baseMessage)
    {
        lock (_stateLock)
        {
            State = (StateInfo)baseMessage;
            Debug.Log("Received new state " + State.StateId);
            _newState = true;
        }
    }

    public void DeserializeState(StateInfo state, 
        out List<List<Vector3>> stateAreaAsList, 
        out List<Polygon> stateArea, 
        out List<Vector3> stateCenters, 
        out Vector3 startPosition,
        out Polygon startArea,
        out List<Vector3> startAreaAsList)
    {
        stateAreaAsList = new List<List<Vector3>>();
        stateArea = new List<Polygon>();
        stateCenters = new List<Vector3>();
        startPosition = _TranslateIntoUnityCoordinates(state.ThisTransitionEndPosition);
        startArea = new Polygon(_TranslateIntoUnityCoordinates(state.ThisTransitionEndArea).Select(v3 => new Vector(v3.x, v3.z)).ToList());
        startAreaAsList = startArea.Points.Select(epp => epp.ToVector3()).ToList();
        for (var i = 0; i < State.PossibleTransitions.Count; i++)// transition in State.PossibleTransitions)
        {
            var endPolygon = new Polygon(_TranslateIntoUnityCoordinates(State.TransitionEndAreas[i]).Select(v3 => new Vector(v3.x, v3.z)).ToList());
            var endCenter = _TranslateIntoUnityCoordinates(State.TransitionEndPositions[i]);
            stateArea.Add(endPolygon);
            stateCenters.Add(endCenter);
            stateAreaAsList.Add(endPolygon.Points.Select(epp => epp.ToVector3()).ToList());
        }
    }

    [HideInInspector]
    public bool GetNewState()
    {
        bool ret;

        lock (_stateLock)
        {
            ret = _newState;
            _newState = false;
        }

        return ret;
    }

    [HideInInspector]
    public bool IsBreached;
    [HideInInspector]
    public List<int> ActiveBreaches = new List<int>();
    [HideInInspector]
    public Dictionary<int, bool> ActiveBreachesOthersFault = new Dictionary<int, bool>();
    [HideInInspector]
    public Dictionary<int, string> ActiveBreachesPlayerIdentifiers = new Dictionary<int, string>();
    [HideInInspector]
    public Dictionary<int, List<string>> ActiveBreacherOtherIdentifiers = new Dictionary<int, List<string>>();
    
    void OnIncentives(IMessageBase messageBase)
    {
        _packagesReceived++;
        Incentives incentives = (Incentives) messageBase;

        List<RevokeEvent> revokeEvents = new List<RevokeEvent>();
        List<TimedEvent> actuationEvents = new List<TimedEvent>();
        foreach (TimedEvent event_ in incentives.Events)
        {
            if (event_ is RevokeEvent)
            {
                revokeEvents.Add((RevokeEvent) event_);
            }
            else
            {
                actuationEvents.Add(event_);
            }
        }

        lock (_events)
        {
            int numRemoved = _events.RemoveAll(event_ =>
                revokeEvents.TrueForOne(
                    revokeEvent => revokeEvent.Id == event_.Id
                )
            );
            if (numRemoved > 0) _receivedEventChange = true;

            foreach (TimedEvent newEvent in actuationEvents)
            {
                TimedEvent eventToOverwrite =
                    _events.Find(existingEvent => newEvent.StrategyId == existingEvent.StrategyId &&
                                                  newEvent.Id == existingEvent.Id);
                if (eventToOverwrite == null)
                {
                    _receivedNewEvents = true;
                    _events.Add(newEvent);
                }
                else
                {
                    //Debug.Log("Overwriting " + eventToOverwrite.Id);
                    _receivedEventChange = true;
                    eventToOverwrite.OverrideWith(newEvent);
                }
            }
        }

    }

    public Action OnAllocation;

    private void OnAllocationGranted(IMessageBase obj)
    {
        AllocationGranted granted = (AllocationGranted) obj;
        _angle = 180f * granted.RotationAroundFirstPoint / Mathf.PI;
        _offset = granted.Offset;
        _handleAlloc = true;
        Debug.Log("OnAllocationGranted finished");
    }

    public int UserId = -1;
    private void OnRegistrationSuccess(IMessageBase p)
    {
        var success = (RegistrationSuccess) p;
        UserId = success.UserId;

        VirtualSpaceCore.Instance.SendReliable(
            new PlayerAllocationRequest()
            {
                RequestId = 0,
                NiceHave = _nice,
                MustHave = _must
            }
        );
    }

    #endregion

    #region VirtualSpaceSender
    public float SendPositionsPerSecond = 10f;
    private IEnumerator SendPosition()
    {
        while (true)
        {
            var camRelative = _reference.InverseTransformPoint(PlayerPosition.position);
            //Debug.Log("Sending " + positionObject.position);
            VirtualSpaceCore.Instance.SendReliable(
                new PlayerPosition(
                    VirtualSpaceTime.CurrentTimeInMillis,
                    camRelative,
                    Vector3.zero
                ));

            yield return new WaitForSeconds(
                1 / SendPositionsPerSecond
            );
        }
    }

    #endregion

    #region PublicInterface

    /// <summary>
    /// First decreases by Safety, then increases by Bloat. Safety only decreases towards other players,
    /// Bloat increases the complete polygon.
    /// </summary>
    public bool BloatedSpaceAtTimeWithSafety(float relativeTime, float safety, float bloat, out List<Vector3> poly, out Vector3 center)
    {
        center = Vector3.zero;
        poly = new List<Vector3>();

        long desiredTurn = VirtualSpaceTime.CurrentTurn + VirtualSpaceTime.ConvertSecondsToTurns(relativeTime);
        //Debug.Log("Current turn: " + VirtualSpaceTime.CurrentTurn);
        //Debug.Log("Looking for turn: " + desiredTurn);
        TimedArea areaEvent = FindAreaAtTurnWithTolerance(desiredTurn, 1);

        var isNull = areaEvent == null || areaEvent.Area == null;
        //Debug.Log(areaEvent == null);
        //if (areaEvent != null && areaEvent.Area != null)
        //Debug.Log(areaEvent.Area.Points.ToPrintableString());
        if (!isNull)
        {
            //Debug.Log("Found event");
            var polyOffset = ClipperUtility.OffsetPolygonForSafety(areaEvent.Area, -safety).FirstOrDefault() ?? areaEvent.Area;
            polyOffset = ClipperUtility.OffsetPolygon(polyOffset, bloat).FirstOrDefault() ?? areaEvent.Area;
            poly = MakeConvexHull(_TranslateIntoUnityCoordinates(polyOffset));
            center = _TranslateIntoUnityCoordinates(polyOffset.Centroid);
        }

        return !isNull;
    }

    public bool SpaceAtTimeWithSafety(float relativeTime, float safety, out List<Vector3> poly, out Vector3 center)
    {
        center = Vector3.zero;
        poly = new List<Vector3>();

        long desiredTurn = VirtualSpaceTime.CurrentTurn + VirtualSpaceTime.ConvertSecondsToTurns(relativeTime);
       //Debug.Log("Current turn: " + VirtualSpaceTime.CurrentTurn);
       //Debug.Log("Looking for turn: " + desiredTurn);
        TimedArea areaEvent = FindAreaAtTurnWithTolerance(desiredTurn, 5);

        var isNull = areaEvent == null || areaEvent.Area == null || !areaEvent.Area.Points.Any();
        //Debug.Log(areaEvent == null);
        //if (areaEvent != null && areaEvent.Area != null)
            //Debug.Log(areaEvent.Area.Points.ToPrintableString());
        if (!isNull)
        {
            //Debug.Log("Found event");
            var polyOffset = ClipperUtility.OffsetPolygonForSafety(areaEvent.Area, -safety).FirstOrDefault() ?? areaEvent.Area;
            if (polyOffset == null && !polyOffset.Points.Any())
            {
                Debug.Log("Result of OffsetPolygonForSafety is invalid");
                return false;
            }
            poly = MakeConvexHull(_TranslateIntoUnityCoordinates(polyOffset));
            center = _TranslateIntoUnityCoordinates(polyOffset.Centroid);
        }

        return !isNull;
    }

    public bool SpaceIntersectionOverTimeWithSafety(float relativeFromSeconds, long relativeToSeconds, float safety,
        out List<Vector3> polygon, out Vector3 center)
    {
        center = Vector3.zero;
        polygon = new List<Vector3>();

        long currentTurn = VirtualSpaceTime.CurrentTurn;
        long absoluteFrom = currentTurn + VirtualSpaceTime.ConvertSecondsToTurns(relativeFromSeconds);
        long absoluteTo = currentTurn + VirtualSpaceTime.ConvertSecondsToTurns(relativeToSeconds);

        Polygon intersection = _SpaceOverTime(absoluteFrom, absoluteTo).FirstOrDefault();
        if (intersection != null)
        {
            intersection = ClipperUtility.OffsetPolygonForSafety(intersection, -safety).FirstOrDefault() ?? intersection;
            polygon = MakeConvexHull(_TranslateIntoUnityCoordinates(intersection));
            center = _TranslateIntoUnityCoordinates(intersection.Center);
        }

        return intersection != null;
    }

    public bool SpaceIntersectionOverTimeWithSafety(Transition transition, float safety, out List<Vector3> poly,
        out Vector3 center)
    {
        return SpaceIntersectionOverTimeWithSafety(transition.TurnStart, transition.TurnEnd, safety, out poly,
            out center);
    }

    public List<VirtualSpaceKeyframe> NextKeyframes()
    {
        List<VirtualSpaceKeyframe> keyframes = new List<VirtualSpaceKeyframe>();
        List<Transition> transitions = ActiveOrPendingTransitions;

        transitions.Sort((a, b) => (int)(a.TurnStart - b.TurnStart));

        foreach (Transition transition in transitions)
        {
            long absoluteTurn = Math.Max(VirtualSpaceTime.CurrentTurn, transition.TurnStart);

            List<TransitionFrame> frames = transition.GetActiveFrames(absoluteTurn).ToList();
            if (frames.Count != 1) Debug.LogWarning("Found " + frames.Count + ". There should be exactly one frame.");
            TransitionFrame frame = frames.FirstOrDefault();
            if(frame == null)
                continue;

            long relativeTurn = absoluteTurn - VirtualSpaceTime.CurrentTurn;
            float absoluteUnityTime = _currentUnityTime + VirtualSpaceTime.ConvertTurnsToSeconds(relativeTurn);
            //TODO threading trouble - fixed here for unit time...

            Vector3 direction = (transition.Frames.Last().Position.Position -
                                 transition.Frames.First().Position.Position).ToVector3();

            //TODO threading trouble - ... but not here (cannot use transforms) -> _TranslateInto...
            VirtualSpaceKeyframe turnKeyframe = new VirtualSpaceKeyframe(
                _TranslateIntoUnityCoordinates(frame.Position.Position),
                _TranslateIntoUnityCoordinates(frame.Area.Area),
                absoluteUnityTime,
                direction.normalized * transition.Speed);

            Debug.Log("Keyframe: ");
            Debug.Log("Position: " + turnKeyframe.Position);
            Debug.Log("Area    : " + turnKeyframe.Area);
            Debug.Log("Unity time: " + turnKeyframe.AbsoluteUnityTime);
            Debug.Log("DirectionSpeed: " + turnKeyframe.DirectionSpeed);

            keyframes.Add(turnKeyframe);
        }

        return keyframes;
    }
    #endregion

    #region public helper

    public int GetPps()
    {
        return _incentivesReceivedTimes.Count;
    }
    #endregion

    #region Update

    public double currentMilliseconds;
    public int currentTurn;
    private float _currentUnityTime;
    public Action<StateInfo> OnStateUpdate;
    public Action OnEventsUpdate;

    void Update()
    {
        currentMilliseconds = VirtualSpaceTime.CurrentTimeInMillis;
        currentTurn = (int)VirtualSpaceTime.CurrentTurn;
        _currentUnityTime = Time.unscaledTime;

        if (_handleAlloc)
        {
            _handleAlloc = false;
            _reference.Rotate(Vector3.up, (float) _angle);
            var pos = _reference.position;
            _reference.position = pos + new Vector3((float) _offset.X, 0f, (float) _offset.Z);
            if (OnAllocation != null)
                OnAllocation.Invoke();
        }

        UpdateDependentComponents();

        for(var i = 0; i < _packagesReceived; i++)
        {
            _incentivesReceivedTimes.Add(Time.unscaledTime);
        }
        _packagesReceived = 0;
        var newTimes = new List<float>();
        foreach (var irt in _incentivesReceivedTimes)
        {
            if (Time.unscaledTime - 1f < irt)
                newTimes.Add(irt);
        }
        _incentivesReceivedTimes = newTimes;

        if (_receivedEventChange || _receivedNewEvents)
        {
            //Debug.Log("EventsUpdate");
            if (OnEventsUpdate != null)
                OnEventsUpdate.Invoke();
        }

        if (_receivedNewEvents)
        {
            //Debug.Log("New Events");

            _receivedNewEvents = false;
            if (OnReceivedNewEvents != null)
                OnReceivedNewEvents.Invoke();
        }
        if (_receivedEventChange)
        {
            //Debug.Log("Changed Events");

            _receivedEventChange = false;
            if (OnEventsChanged != null)
                OnEventsChanged.Invoke();
        }

        lock (_stateLock)
        {
            if (OnStateUpdate != null && State != null)
            {
                OnStateUpdate(State);
                State = null;
            }
        }
    }

    void LateUpdate()
    {
        CleanupEvents();
    }

    public void CleanupEvents()
    {
        List<TimedEvent> toDelete = new List<TimedEvent>();
        lock (_events)
        {
            foreach (TimedEvent event_ in _events)
            {
                if (event_.TurnEnd < VirtualSpaceTime.CurrentTurn - 5)
                {
                    toDelete.Add(event_);
                }
            }
            _events.RemoveRange(toDelete);
        }
    }

    private IEnumerable<TimedArea> _GetAllAreasAtTurn(long turn)
    {
        foreach (TimedEvent event_ in _GetAllEventsAtTurn(turn, EventType.Area))
        {
            yield return (TimedArea) event_;
        }
    }

    private IEnumerable<TimedEvent> _GetAllEventsAtTurn(long turn, EventType eventType)
    {
        foreach (TimedEvent event_ in
            _events.Where(
                potentialEvent => potentialEvent.TurnStart <= turn && turn <= potentialEvent.TurnEnd))
        {
            if (event_.EventType == eventType)
            {
                yield return event_;
            }
            else if (event_ is Transition)
            {
                Transition transition = (Transition) event_;
                foreach (TransitionFrame frame in transition.GetActiveFrames(turn))
                {
                    if (eventType == EventType.Position && frame.Position != null)
                        yield return frame.Position;
                    else if (eventType == EventType.Area && frame.Area != null)
                        yield return frame.Area;
                }
            }
            else
            {
            }
        }
    }

    public void UpdateDependentComponents()
    {
        lock (_events)
        {
            long turnNow = VirtualSpaceTime.CurrentTurn;

            if (turnNow == _lastTurnPlayerAreaUpdated)
                return;
            _lastTurnPlayerAreaUpdated = turnNow;
            foreach (TimedArea area in _GetAllAreasAtTurn(turnNow))
            {
                if (area.Type == IncentiveType.Recommended)
                {
                    PlayerArea.SetNewPositionsWithOffset(area.Area, false);
                    break;
                }
            }
        }
    }

    #endregion

    #region Helper

    public List<Vector3> _TranslateIntoUnityCoordinates(Polygon polygon)
    {
        return polygon.Points.Select(_TranslateIntoUnityCoordinates).ToList();
    }

    public List<Vector3> _TranslateIntoUnityCoordinates(List<Vector3> polygonAsList)
    {
        return polygonAsList.Select(_TranslateIntoUnityCoordinates).ToList();
    }

    private PolygonList _SpaceOverTime(long absoluteFrom, long absoluteTo)
    {
        PolygonList intersection = Polygon.AsRectangle(new Vector(4, 4), new Vector(-2, -2)); // TODO hard-coded
        for (long turn = absoluteFrom; turn < absoluteTo; turn++)
        {
            foreach (TimedArea area in _GetAllAreasAtTurn(turn))
            {
                intersection = ClipperUtility.Intersection(intersection, area.Area);
            }
        }

        return intersection;
    }

    public TimedArea FindAreaAtTurnWithTolerance(long desiredTurn, long turnTolerance = 0)
    {
        lock (_events)
        {
            List<TimedEvent> potentialEvents = _events.FindAll(potentialEvent =>
                (potentialEvent.EventType == EventType.Area || potentialEvent.EventType == EventType.Transition) &&
                potentialEvent.TurnStart - turnTolerance <= desiredTurn &&
                (potentialEvent.TurnEnd == long.MaxValue || desiredTurn <= potentialEvent.TurnEnd + turnTolerance));
            
            foreach (TimedEvent potentialEvent in potentialEvents)
            {
                if (potentialEvent is TimedArea)
                {
                    return (TimedArea) potentialEvent;
                }
                else
                {
                    Transition transition = (Transition) potentialEvent;
                    TransitionFrame frame = transition.GetFrames().Find(potentialFrame => potentialFrame.Area != null &&
                        potentialFrame.Area.TurnStart - turnTolerance <=
                        desiredTurn &&
                        (potentialFrame.Area.TurnEnd == long.MaxValue || desiredTurn <= potentialFrame.Area.TurnEnd + turnTolerance));
                    if (frame != null)
                        return frame.Area;
                }
            }

            return null;
        }
    }

    public TimedArea FindAreaAtTurn(long desiredTurn)
    {
        return FindAreaAtTurnWithTolerance(desiredTurn, 0);
    }

    public Vector3 _TranslateIntoUnityCoordinates(Vector vector)
    {
        return _reference.TransformPoint(vector.ToVector3());
    }

    public Vector3 _TranslateIntoUnityCoordinates(Vector3 vector)
    {
        return _reference.TransformPoint(vector);
    }

    #endregion

    #region PolygonHelper

    public static float DistanceFromPoly(Vector3 pp, List<Vector3> polygon, bool insideMeansZero)
    {
        var inside = PointInPolygon(pp, polygon, 0f);
        if (insideMeansZero && inside)
            return 0f;
        var p = new Vector2(pp.x, pp.z);
        var poly = polygon.Select(mp => new Vector2(mp.x, mp.z)).ToList();
        float result = 10000;

        // check each line
        for (int i = 0; i < poly.Count; i++)
        {
            int previousIndex = i - 1;
            if (previousIndex < 0)
            {
                previousIndex = poly.Count - 1;
            }

            Vector2 currentPoint = poly[i];
            Vector2 previousPoint = poly[previousIndex];

            float segmentDistance = DistanceFromLine(p, previousPoint, currentPoint);

            if (segmentDistance < result)
            {
                result = segmentDistance;
            }
        }

        if (inside)
            result *= -1;

        return result;
    }

    public static void DrawPolygonLines(List<Vector3> polygon, Color color, float duration)
    {
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            Debug.DrawLine(polygon[i], polygon[j], color, duration);
        }
    }

    public static bool PolygonInPolygon(List<Vector3> smaller, List<Vector3> bigger, float tolerance)
    {
        return smaller.All(p => PointInPolygon(p, bigger, tolerance));
    }

    public static bool PointInPolygon(Vector3 point, List<Vector3> polygon, float tolerance)
    {
        if (polygon.Count < 3)
            return false;
        var rev = new List<Vector3>(polygon);
        rev = rev.Select(r => new Vector3(r.x, 0f, r.z)).ToList();
        point.y = 0f;
        // Get the angle between the point and the
        // first and last vertices.
        var maxPoint = rev.Count - 1;
        var totalAngle = Vector3.Angle(rev[maxPoint] - point, rev[0] - point);

        // Add the angles from the point
        // to each other pair of vertices.
        for (var i = 0; i < maxPoint; i++)
        {
            totalAngle += Vector3.Angle(rev[i] - point, rev[i + 1] - point);
        }
        // The total angle should be 2 * PI or -2 * PI if
        // the point is in the polygon and close to zero
        // if the point is outside the polygon.
        totalAngle %= 360f;
        if (totalAngle > 359)
            totalAngle -= 360f;
        return (Mathf.Abs(totalAngle) < 0.001f + tolerance);
    }

    private static float DistanceFromLine(Vector2 p, Vector2 l1, Vector2 l2)
    {
        float xDelta = l2.x - l1.x;
        float yDelta = l2.y - l1.y;

        //	final double u = ((p3.getX() - p1.getX()) * xDelta + (p3.getY() - p1.getY()) * yDelta) / (xDelta * xDelta + yDelta * yDelta);
        float u = ((p.x - l1.x) * xDelta + (p.y - l1.y) * yDelta) / (xDelta * xDelta + yDelta * yDelta);

        Vector2 closestPointOnLine;
        if (u < 0)
        {
            closestPointOnLine = l1;
        }
        else if (u > 1)
        {
            closestPointOnLine = l2;
        }
        else
        {
            closestPointOnLine = new Vector2(l1.x + u * xDelta, l1.y + u * yDelta);
        }


        var d = p - closestPointOnLine;
        return Mathf.Sqrt(d.x * d.x + d.y * d.y); // distance
    }

    // For debugging.
    public static Vector3[] g_MinMaxCorners;
    public static Rect g_MinMaxBox;
    public static Vector3[] g_NonCulledPoints;

    // Find the points nearest the upper left, upper right,
    // lower left, and lower right corners.
    private static void GetMinMaxCorners(List<Vector3> points, ref Vector3 ul, ref Vector3 ur, ref Vector3 ll, ref Vector3 lr)
    {
        // Start with the first point as the solution.
        ul = points[0];
        ur = ul;
        ll = ul;
        lr = ul;

        // Search the other points.
        foreach (Vector3 pt in points)
        {
            if (-pt.x - pt.z > -ul.x - ul.z) ul = pt;
            if (pt.x - pt.z > ur.x - ur.z) ur = pt;
            if (-pt.x + pt.z > -ll.x + ll.z) ll = pt;
            if (pt.x + pt.z > lr.x + lr.z) lr = pt;
        }

        g_MinMaxCorners = new Vector3[] { ul, ur, lr, ll }; // For debugging.
    }

    // Find a box that fits inside the MinMax quadrilateral.
    private static Rect GetMinMaxBox(List<Vector3> points)
    {
        // Find the MinMax quadrilateral.
        Vector3 ul = new Vector3(0, 0), ur = ul, ll = ul, lr = ul;
        GetMinMaxCorners(points, ref ul, ref ur, ref ll, ref lr);

        // Get the coordinates of a box that lies inside this quadrilateral.
        float xmin, xmax, ymin, ymax;
        xmin = ul.x;
        ymin = ul.z;

        xmax = ur.x;
        if (ymin < ur.z) ymin = ur.z;

        if (xmax > lr.x) xmax = lr.x;
        ymax = lr.z;

        if (xmin < ll.x) xmin = ll.x;
        if (ymax > ll.z) ymax = ll.z;

        var result = new Rect(xmin, ymin, xmax - xmin, ymax - ymin);
        g_MinMaxBox = result;    // For debugging.
        return result;
    }

    // Cull points out of the convex hull that lie inside the
    // trapezoid defined by the vertices with smallest and
    // largest X and Y coordinates.
    // Return the points that are not culled.
    private static List<Vector3> HullCull(List<Vector3> points)
    {
        // Find a culling box.
        Rect culling_box = GetMinMaxBox(points);

        // Cull the points.
        List<Vector3> results = new List<Vector3>();
        foreach (Vector3 pt in points)
        {
            // See if (this point lies outside of the culling box.
            if (pt.x <= culling_box.xMin ||
                pt.x >= culling_box.xMax ||
                pt.z <= culling_box.yMin ||
                pt.z >= culling_box.yMax)
            {
                // This point cannot be culled.
                // Add it to the results.
                results.Add(pt);
            }
        }

        g_NonCulledPoints = new Vector3[results.Count];   // For debugging.
        results.CopyTo(g_NonCulledPoints);              // For debugging.
        return results;
    }

    // Return the points that make up a polygon's convex hull.
    // This method leaves the points list unchanged.
    public static List<Vector3> MakeConvexHull(List<Vector3> points)
    {
        if (points == null || !points.Any())
        {
            Debug.LogWarning("MakeConvexHull: Returning emtpy Polygon for undefined or empty polygon.");
            return new List<Vector3>();
        }

        // Cull.
        points = HullCull(points);

        // Find the remaining point with the smallest Y value.
        // if (there's a tie, take the one with the smaller X value.
        Vector3 best_pt = points[0];
        foreach (Vector3 pt in points)
        {
            if ((pt.z < best_pt.z) ||
                ((pt.z == best_pt.z) && (pt.x < best_pt.x)))
            {
                best_pt = pt;
            }
        }

        // Move this point to the convex hull.
        List<Vector3> hull = new List<Vector3>();
        hull.Add(best_pt);
        points.Remove(best_pt);

        // Start wrapping up the other points.
        float sweep_angle = 0;
        for (;;)
        {
            // Find the point with smallest AngleValue
            // from the last point.
            var X = hull[hull.Count - 1].x;
            var Y = hull[hull.Count - 1].z;
            best_pt = points[0];
            float best_angle = 3600;

            // Search the rest of the points.
            foreach (Vector3 pt in points)
            {
                float test_angle = AngleValue(X, Y, pt.x, pt.z);
                if ((test_angle >= sweep_angle) &&
                    (best_angle > test_angle))
                {
                    best_angle = test_angle;
                    best_pt = pt;
                }
            }

            // See if the first point is better.
            // If so, we are done.
            float first_angle = AngleValue(X, Y, hull[0].x, hull[0].z);
            if ((first_angle >= sweep_angle) &&
                (best_angle >= first_angle))
            {
                // The first point is better. We're done.
                break;
            }

            // Add the best point to the convex hull.
            hull.Add(best_pt);
            points.Remove(best_pt);

            sweep_angle = best_angle;

            // If all of the points are on the hull, we're done.
            if (points.Count == 0) break;
        }

        return hull;
    }

    // Return a number that gives the ordering of angles
    // WRST horizontal from the point (x1, y1) to (x2, y2).
    // In other words, AngleValue(x1, y1, x2, y2) is not
    // the angle, but if:
    //   Angle(x1, y1, x2, y2) > Angle(x1, y1, x2, y2)
    // then
    //   AngleValue(x1, y1, x2, y2) > AngleValue(x1, y1, x2, y2)
    // this angle is greater than the angle for another set
    // of points,) this number for
    //
    // This function is dy / (dy + dx).
    private static float AngleValue(float x1, float y1, float x2, float y2)
    {
        float dx, dy, ax, ay, t;

        dx = x2 - x1;
        ax = Math.Abs(dx);
        dy = y2 - y1;
        ay = Math.Abs(dy);
        if (Math.Abs(ax + ay) < 0.001f)
        {
            // if (the two points are the same, return 360.
            t = 360f / 9f;
        }
        else
        {
            t = dy / (ax + ay);
        }
        if (dx < 0)
        {
            t = 2 - t;
        }
        else if (dy < 0)
        {
            t = 4 + t;
        }
        return t * 90;
    }

    #endregion

    public void Tick()
    {
        var tick = new Tick();
        VirtualSpaceCore.Instance.SendReliable(tick);
    }
}
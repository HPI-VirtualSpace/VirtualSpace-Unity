
#define DEBUG_MOLE

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Assets.Games.Scripts;
using Assets.Scripts.VirtualSpace.Unity_Specific;
using JetBrains.Annotations;
using UnityEngine;
using VirtualSpace.Shared;
using VirtualSpace.Unity_Specific;
using VirtualSpaceVisuals;
using Random = UnityEngine.Random;


namespace Games.Scripts
{
    public class WhacGameLogic : MonoBehaviour
    {
        public enum DebugPointPosition
        {
            Recommended,
            ExpectedHit,
            None
        }

        public enum WhacGameEventType
        {
            Suspense,
            NormalPlacement,
            BorderMinigame,
            Movement
        }

        public enum WhacGameState
        {
            BeforeGame,
            MoleSpawn
        }

        private readonly SpawnRhythm _spawnRhythm = new SpawnRhythm();
        private WhacBoardController _board;
        private FlowerController _flowerController;
        private WhacGameEvent _lastEvent;
        private MoleController _startMole;
        private DemoDisplayWhacSpawner Spawner;

        private readonly List<WhacGameEvent> _gameEvents = new List<WhacGameEvent>();
        // keeping track of hit success rate
        private readonly LinkedList<int> _hits = new LinkedList<int>();
        private readonly List<MoleController> _moles = new List<MoleController>();

        private bool _controlGroupChecked;
        [SerializeField] private float _hitSuccessRate;
        // keep game events in history after completion for
        private readonly float _keepInHistoryFor = 2f;
        // when was a mole last spawned
        private float _lastSpawnTime;
        // when will the last mole despawn
        private float _lastMoleTime;
        private long _lastTransitionStart = long.MinValue;
        private float _moleAliveTime; // todo could be local
        private float _moleUpDownTime; // todo could be local

        private float _timeInbetweenSpawns;

        public DebugPointPosition DebugPointType = DebugPointPosition.None;
        public float AllowedAreaOffset = -.2f; // big hammer so spawn moles outside of the actual area
        public float BackToNormalRatePerSecond = .8f;
        public float BestMoleSpawnDistance = .5f;
        public float CognitiveProcessingDelay = .2f;
        public WhacGameState CurrentGameState = WhacGameState.BeforeGame;
        public float CustomHitSuccessRate = -1;
        public float DivisionsMaxDistToRecommended = 2 / 3f;

        public int DivisionsOfBoundaryForPosition = 4;
        public int DivisionsOfBoundaryToRecommended = 3;
        public Transform ExpectedPositionDebugPoint;
        public GameObject Flower;
        public HammerController Hammer;
        public int HitRecordHistorySize = 7;
        public WhacGameState InitialGameState = WhacGameState.BeforeGame;

        /// <summary>
        ///     Should probably be bigger than MoleUpDownTime and smaller than MoleAliveTime - 2 * MoleUpDownTime.
        ///     Should actually depend on the movement speed.
        /// </summary>
        public float LookIntoFuture = .3f;

        public float MinBestDistToPreviousSpawn = .5f;
        public float MinBestViewAngle = 45;
        public float MinRandomQuartile = .8f;
        public float MoleAliveTime = 1.5f;
        public float MoleReachDelay = .3f;
        public GameObject MoleTemplate;
        public float MoleUpDownTime = .25f;
        public float MovementBaseAliveTime = .5f;
        public float MovementBaseInbetweenTime = .5f;
        public float NextSpawnSeconds;

        [Description("Increases the runtime by sorting the potential positions.")] public bool PickRandom = true;

        public float Safety = .2f;
        public bool SimulateHits;
        public float SimulateSuccessRate = .8f;

        public float SuspenseTimeBeforeFastMovement = .8f;
        public float TimeAfterTransition = .3f;
        public float TimeInbetweenSpawns = 1f;
        public float UserBodyHitOffset = .5f;

        public VirtualSpaceHandler VsHandler;
        public VirtualSpaceMenuFollow VsMenu;

        public float DesiredInBetweenSeconds
        {
            get
            {
                float multiplier = 2;
                if (_hitSuccessRate > .9f)
                    multiplier = 1f;
                else if (_hitSuccessRate > .7f)
                    multiplier = 1.3f;
                else if (_hitSuccessRate > .5f)
                    multiplier = 1.7f;

                return multiplier * TimeInbetweenSpawns;
            }
        }

        private void Start()
        {
            //_gameState = WhacGameState.BeforeGame;
            CurrentGameState = InitialGameState;
            _board = FindObjectOfType<WhacBoardController>();
            PlaceFlower();
            SetDefaultMoleValues();
            FindHills();
        }

        private void OnEnable()
        {
            VsHandler.OnEventsUpdate += UpdateGameEvents;
            VsHandler.OnStateUpdate += OnStateUpdate;
            FollowText.BeingHitEvent += StartGame;
        }

        private void OnDisable()
        {
            VsHandler.OnEventsUpdate -= UpdateGameEvents;
            VsHandler.OnStateUpdate -= OnStateUpdate;
            FollowText.BeingHitEvent -= StartGame;
        }

        private void NewSpawner(DemoDisplayWhacSpawner obj)
        {
            Spawner = obj;
        }

        public void InitializeGame()
        {
            NextSpawnSeconds = Time.time;
        }

        private void PlaceFlower()
        {
            //var flower = Instantiate(FlowerTemplate, Vector3.zero, Quaternion.identity);
            _flowerController = Flower.GetComponent<FlowerController>();
            _flowerController.FlowerWasHit += _board.RecordHit;
            _flowerController.Hammer = Hammer.transform;
        }

        private void GetUnityStartStopTime(Transition transition, out float start, out float end)
        {
            start = Time.time +
                    VirtualSpaceTime.ConvertTurnsToSeconds(transition.TurnStart - VirtualSpaceTime.CurrentTurn);
            end = Time.time + VirtualSpaceTime.ConvertTurnsToSeconds(transition.TurnEnd - VirtualSpaceTime.CurrentTurn);
        }


        private void UpdateGameEvents()
        {
            var incoming =
                VsHandler
                    .IncomingTransitions; // should be sorted because we never overwrite events (messes up if we reschedule events

            foreach (var transition in incoming)
                if (transition.TurnStart > _lastTransitionStart)
                {
                    // right/left --> look at transition speed
                    // undefocus --> lift special mode
                    // unfocus --> lift special mode, look at transition speed
                    // adjust spawn time to player hit rate + nearby switches

                    // speed == 0 --> prepare or stop
                    // if prepare, I should check for the type of incoming transition


                    if (transition.TransitionContext != TransitionContext.Animation) continue;

                    var isFastTransition = transition.Speed > .5f;
                    var now = Time.time;
                    float start;
                    float end;
                    GetUnityStartStopTime(transition, out start, out end);

                    var timeToStart = start - now; // should always be higher than CognitiveProcessing

                    //Debug.Log("transition starting in " + timeToStart);

                    WhacGameEvent gameEvent = null;
                    switch (transition.TransitionType)
                    {
                        case VSUserTransition.Stay:
                        case VSUserTransition.Focus:
                            // maybe not, need to care for other players

                            break;
                        case VSUserTransition.Undefocus:
                        {
                            // maybe not, need to care for other players
                            var whacEvent = new WhacGameEvent(start, end, WhacGameEventType.NormalPlacement);
                            _gameEvents.Add(whacEvent);
                            break;
                        }
                        case VSUserTransition.Defocus:
                        {
                            var whacEvent = new WhacGameEvent(start, end, WhacGameEventType.BorderMinigame);
                            _gameEvents.Add(whacEvent);
                            break;
                        }
                        case VSUserTransition.Unfocus:
                        case VSUserTransition.Rotate45Left:
                        case VSUserTransition.Rotate45Right:
                        case VSUserTransition.RotateLeft:
                        case VSUserTransition.RotateRight:
                        {
                            //if (isFastTransition)
                            //{
                            var suspenseEvent = new WhacGameEvent(start - SuspenseTimeBeforeFastMovement, start,
                                WhacGameEventType.Suspense);

                            _gameEvents.Add(suspenseEvent);
                            //}

                            var movementEvent =
                                new WhacGameEvent(start, end, WhacGameEventType.Movement, transition.Speed);
                            _gameEvents.Add(movementEvent);

                            gameEvent = movementEvent;

                            var backToNormalEvent = new WhacGameEvent(end, end + 1, WhacGameEventType.NormalPlacement);
                            _gameEvents.Add(backToNormalEvent);

                            //Debug.Log("Movement:");
                            //Debug.Log("Suspense: start " + (suspenseEvent.StartUnity - Time.time) + " end " + (suspenseEvent.EndUnity - Time.time));
                            //Debug.Log("Movement: start " + (movementEvent.StartUnity - Time.time) + " end " + (movementEvent.EndUnity - Time.time));
                            //Debug.Log("Normal: start " + (backToNormalEvent.StartUnity - Time.time) + " end " + (backToNormalEvent.EndUnity - Time.time));
                            break;
                        }
                        case VSUserTransition.SwitchLeft:
                        case VSUserTransition.SwitchRight:
                            // todo
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    if (gameEvent != null) {
                        var first = transition.Frames.First().Area.Area;
                        var last = transition.Frames.Last().Area.Area;
                        gameEvent.StartArea = first;
                        gameEvent.EndArea = last;
                        var intersection = ClipperUtility.Intersection(first, last);
                        gameEvent.SharedArea = intersection.Any() ? intersection.First() : null;
                    }
                    
                    _lastTransitionStart = transition.TurnStart;
                }
        }

        private void CleanEvents()
        {
            if (!_gameEvents.Any()) return;

            var eventsToDelete = new List<WhacGameEvent>();
            var numEvents = _gameEvents.Count;

            for (var i = 0; i < numEvents; i++)
            {
                var gameEvent = _gameEvents[i];

                if (i != numEvents - 1 && Time.time > gameEvent.EndUnity + _keepInHistoryFor)
                    eventsToDelete.Add(gameEvent);
            }

            _gameEvents.RemoveAll(gameEvent => eventsToDelete.Contains(gameEvent));
        }

        private WhacGameEvent GetActiveGame(float later = 0f)
        {
            if (_gameEvents.Count == 0)
                return new WhacGameEvent(float.MinValue, float.MaxValue, WhacGameEventType.NormalPlacement);

            CleanEvents();

            var numEvents = _gameEvents.Count;
            var considerTime = Time.time + later;

            WhacGameEvent activeEvent = null;
            for (var i = 0; i < numEvents; i++)
            {
                var gameEvent = _gameEvents[i];
                if (activeEvent == null && gameEvent.StartUnity <= considerTime && considerTime <= gameEvent.EndUnity)
                    activeEvent = gameEvent;
            }

            return activeEvent ?? _gameEvents.Last();
        }

        private float TimeToNextEvent()
        {
            var events = _gameEvents.Where(gameEvent => Time.time < gameEvent.StartUnity).ToList();
            if (events.Any())
                return events.First().StartUnity -
                Time.time;

            return 10f;
        }

        private void OnStateUpdate(StateInfo info)
        {
            //if (info.YourCurrentTransition == VSUserTransition.Defocus)
        }

        private void StartGame()
        {
            StartGame(0);
        }

        private int _bestHighscore = -1;
        private int _highscore = -1;
        private float _gameEndAt = -1;
        public float GameRunSeconds = 120f;
        private void StartGame(int score)
        {
            Debug.Log("Start the game");
            _lastMoleTime = Time.time;
            CurrentGameState = WhacGameState.MoleSpawn;
            FollowText.gameObject.SetActive(false);
            SetDefaultMoleValues();
            _highscore = 0;
            _gameEndAt = Time.time + GameRunSeconds; 
        }

        private int _id;
        private MoleController PlaceMole(Vector3 position, float upTime, float showHideTime, float showIn=0, bool skeaky = false)
        {
            var mole = Instantiate(MoleTemplate, position, Quaternion.identity);
            var moleController = mole.GetComponent<MoleController>();
            //moleController.transform.parent = transform;
            moleController.UpTime = upTime;
            moleController.ShowHideTime = showHideTime;
            moleController.ShowInTime = showIn;
            moleController.Hammer = Hammer;
            moleController.SimulateHitIn =
                SimulateHits && Random.Range(0f, 1f) <= SimulateSuccessRate
                    ? Random.Range(showHideTime + upTime / 4, 1.2f * showHideTime + upTime)
                    : -1;
            moleController.Id = ++_id;

            if (CurrentGameState == WhacGameState.BeforeGame)
            {
                moleController.MoleIsHit += StartGame;
                moleController.MovingMole = true;
            }
            else if (CurrentGameState == WhacGameState.MoleSpawn)
            {

                moleController.MoleIsHit += _board.RecordHit;
                
                moleController.MoleIsDestroyed += delegate(bool wasHit)
                {
                    while (_hits.Count > HitRecordHistorySize) _hits.RemoveLast();
                    if (!wasHit)
                    {
                        _board.RecordMiss();
                        _hits.AddFirst(0);
                    }
                    else
                    {
                        _hits.AddFirst(1);
                    }
                    if (CustomHitSuccessRate > 0)
                        _hitSuccessRate = CustomHitSuccessRate;
                    else
                        _hitSuccessRate = _hits.Count < 5 ? 0 : (float) _hits.Average();
                };
            }

            moleController.StartLifecycle();

            return moleController;
        }

        public float SpeedAtTime(float time)
        {
            var relTime = time - Time.time;
            var relTurns = VirtualSpaceTime.ConvertSecondsToTurns(relTime);
            var absTurns = VirtualSpaceTime.CurrentTurn + relTurns;
            var activeTransitions = VsHandler.ActiveOrPendingTransitions;
            var transition = activeTransitions.Find(activeTransition => activeTransition.IsActiveAt(absTurns));
            return transition == null ? 0 : transition.Speed;
        }

        public void SetDefaultMoleValues()
        {
            _timeInbetweenSpawns = TimeInbetweenSpawns;
            _moleAliveTime = MoleAliveTime;
            _moleUpDownTime = MoleUpDownTime;
        }
        
        public float CameraWeight = 1f;
        public float NextAreaWeight = 1f;
        public float DistToPreviousWeight = 1f;
        public float FairLineLengthWeight = 1f;
        public Transform hillParent;
        private List<Vector3> hillSpawnPoints;
        public InfoTextController FollowText;

        private void FindHills()
        {
            hillSpawnPoints = new List<Vector3>();
            foreach (Transform column in hillParent)
            {
                foreach (Transform hill in column)
                {
                    hillSpawnPoints.Add(hill.transform.position);
                }
            }
        } 

        private void PlaceMoles()
        {
            // cleanup destroyed moles
            _moles.RemoveAll(mole => mole == null);

            if (DebugPointType == DebugPointPosition.Recommended)
            {
                List<Vector3> dummy;
                Vector3 currentRecommendedPosition;

                VsHandler.SpaceAtTimeWithSafety(0f, .3f, out dummy, out currentRecommendedPosition);
                ExpectedPositionDebugPoint.position = currentRecommendedPosition;
            }

            var activeGame = GetActiveGame(CognitiveProcessingDelay);
            if (activeGame == null)
            {
                Debug.LogWarning("Active game was null. This should never happen.");
                return;
            }
            
            if (_lastEvent != activeGame)
            {
                _lastEvent = activeGame;
                //Debug.Log("Active game mode: " + activeGame.Type);

                if (activeGame.Type == WhacGameEventType.Movement)
                {
                    var halfDuration = activeGame.Duration / 2;
                    _timeInbetweenSpawns = halfDuration;
                    _lastSpawnTime = Time.time - halfDuration;
                    _moleAliveTime = halfDuration - 2 * MoleUpDownTime;
                }
                else if (activeGame.Type == WhacGameEventType.BorderMinigame)
                {
                    SetDefaultMoleValues();
                    //_timeInbetweenSpawns = .75f;
                }
                else
                {
                    SetDefaultMoleValues();
                    _timeInbetweenSpawns = _spawnRhythm.CurrentOffset();
                    _moleAliveTime = _timeInbetweenSpawns - 2 * MoleUpDownTime;
                    // use a rhythm here
                }
            }

            if (activeGame.Type == WhacGameEventType.Suspense) return;
            
            var timeToNextEvent = TimeToNextEvent();
            var lookAheadForNextArea = timeToNextEvent < 2f ? timeToNextEvent : 10; // next game event in???
            if (activeGame.Type == WhacGameEventType.Movement)
            {
                lookAheadForNextArea = activeGame.Duration / 2;
            }
            
            if (_lastSpawnTime + _timeInbetweenSpawns < Time.time)
            {
                var moleTotalUpTime = _moleAliveTime + 2 * _moleUpDownTime;

                _lastMoleTime = Time.time + moleTotalUpTime;

                var areaSearchOffset = moleTotalUpTime / 3;

                Vector3 positionAtSpawn;
                List<Vector3> areaAtSpawn;
                Vector3 positionAtNextSpawn;
                List<Vector3> areaAtNextSpawn;
                Vector3 dummy;
                List<Vector3> areaForSpawning;

                VsHandler.BloatedSpaceAtTimeWithSafety(areaSearchOffset, Safety, 0, out areaAtSpawn,
                    out positionAtSpawn);
                VsHandler.BloatedSpaceAtTimeWithSafety(areaSearchOffset + lookAheadForNextArea * 1.3f, Safety, 0,
                    out areaAtNextSpawn, out positionAtNextSpawn);
                VsHandler.BloatedSpaceAtTimeWithSafety(areaSearchOffset, Safety, UserBodyHitOffset, out areaForSpawning,
                    out dummy);

                VirtualSpaceHandler.DrawPolygonLines(areaForSpawning, Color.blue, 1f);
                //DrawPolygonLines(areaAtNextSpawn, Color.green, 1f);

                //var positions = FindPossiblePositions(areaForSpawning, positionAtSpawn);
                var positions = hillSpawnPoints;

                PositionValue minPositionValue, maxPositionValue;
                var positionValues = GetPositionEvaluation(positions,
                    areaAtSpawn, areaAtNextSpawn, positionAtSpawn, positionAtNextSpawn,
                    moleTotalUpTime,
                    out minPositionValue,
                    out maxPositionValue);

                if (positionValues.IsEmpty())
                {
                    Debug.LogWarning("Couldn't find positions");
                    return;
                }

                PositionValue bestPositionValue = null;
                var bestValue = float.MinValue;
                foreach (var positionValue in positionValues)
                {
                    positionValue.CameraAngleValue /= maxPositionValue.CameraAngleValue;
                    positionValue.DistanceToCurrentValue /= maxPositionValue.DistanceToCurrentValue;
                    if (maxPositionValue.InNextAreaValue > 0)
                        positionValue.InNextAreaValue /= maxPositionValue.InNextAreaValue;
                    positionValue.DistanceToDeltaValue /= maxPositionValue.DistanceToDeltaValue;
                    positionValue.DistToPreviousMole /= maxPositionValue.DistToPreviousMole;
                    positionValue.WalkLineContainedValue /= maxPositionValue.WalkLineContainedValue;
                    positionValue.TimeRequiredFairness /= maxPositionValue.TimeRequiredFairness;
                    
                    positionValue.Value =
                        CameraWeight * (1 - positionValue.CameraAngleValue)
                        + 2 * NextAreaWeight * (1 - positionValue.InNextAreaValue)
                        + DistToPreviousWeight * positionValue.DistToPreviousMole
                        + FairLineLengthWeight * (1 - positionValue.TimeRequiredFairness)
                    ;

                    if (bestValue < positionValue.Value)
                    {
                        bestValue = positionValue.Value;
                        bestPositionValue = positionValue;
                    }
                }

                var cameraTransform = Camera.main.transform;
                var cameraPosition = cameraTransform.position;

                var finalPositionValue = bestPositionValue;
                if (PickRandom)
                {
                    positionValues.Sort((pV1, pV2) => pV1.Value.CompareTo(pV2.Value));
                    var maxIndex = positionValues.Count - 1;
                    var minIndex = (int) (MinRandomQuartile * maxIndex);
                    var index = Random.Range(minIndex, maxIndex + 1);

                    //for (var i = 0; i < maxIndex; i++)
                    //{
                    //    Color lineColor = Color.green;
                    //    if (i < minIndex)
                    //        lineColor = Color.blue;
                    //    Debug.DrawLine(cameraPosition, positionValues[i].Position, lineColor, 2f);

                    //}

                    finalPositionValue = positionValues[index];
                }

                if (finalPositionValue == null)
                {
                    Debug.Log("Couldn't find good position");
                    return;
                }

                // if final position is at the border of the zone (how to test? defocus?)
                // create a event queue 

                

                var finalPosition = finalPositionValue.Position;

                var finalExepectedUserPosition =
                    finalPosition + (cameraPosition - finalPosition).normalized * UserBodyHitOffset;
                if (DebugPointType == DebugPointPosition.ExpectedHit)
                    ExpectedPositionDebugPoint.position = finalExepectedUserPosition;
                ExpectedPositionDebugPoint.LookAt(finalPosition);

                //Debug.Log("Spawning mole for " + moleTotalUpTime + " during " + activeGame.Type);
                _moles.Add(PlaceMole(finalPosition, _moleAliveTime, _moleUpDownTime));
                _lastSpawnTime = Time.time;
                _spawnRhythm.NextOffset();

                //Debug.Log("CameraAngleValue : " + (1 - finalPositionValue.CameraAngleValue));
                //Debug.Log("InNextAreaValue : " + (1 - finalPositionValue.InNextAreaValue));
                //Debug.Log("MinDistToPreviousValue : " + finalPositionValue.DistToPreviousMole);
                //Debug.Log("FairLineValue : " + finalPositionValue.TimeRequiredFairness);
                //Debug.Log("Final: " + finalPositionValue.Value);
                //Debug.Log("Best: " + bestPositionValue.Value);
                //Debug.Log("Final is " + ((bestPositionValue.Value - finalPositionValue.Value) / finalPositionValue.Value) + " worse than best.");

                //Debug.Log("Have " + _moles.Count + " moles.");
            }
        }

        public Collider NotSpawnZone;
        private List<Vector3> FindPossiblePositions(List<Vector3> areaAtSpawn, Vector3 positionAtSpawn)
        {
            List<Vector3> positions = new List<Vector3>();

            for (var edgeNum = 0; edgeNum < areaAtSpawn.Count; edgeNum++)
            for (float i = 0; i < DivisionsOfBoundaryForPosition; i++)
            {
                var lerp = i / DivisionsOfBoundaryForPosition;
                var boundaryPosition = Vector3.Lerp(
                        areaAtSpawn[edgeNum], areaAtSpawn[(edgeNum + 1) % areaAtSpawn.Count], 
                        lerp);
                for (var j = 0; j < DivisionsOfBoundaryToRecommended; j++)
                {
                    var lerpRecommended = (float) j / DivisionsOfBoundaryToRecommended;
                    var spawnPosition = Vector3.Lerp(boundaryPosition, positionAtSpawn, lerpRecommended);
                    
                    if (NotSpawnZone.bounds.SqrDistance(spawnPosition) > 0)
                        positions.Add(spawnPosition);
                    //else
                    //    Debug.Log("Point " + spawnPosition + ":" + i + " " + j + " in bounds");
                }
            }

            return positions;
        }

        public float ExpectedUserSpeed = 2f;
        public float ExpectedUserAngularSpeed = 90f;
        private List<PositionValue> GetPositionEvaluation(
            List<Vector3> positions, 
            List<Vector3> areaAtSpawn, List<Vector3> areaAtNextSpawn,
            Vector3 positionAtSpawn, Vector3 positionAtNextSpawn,
            float totalMoleUpTime,
            out PositionValue minPositionValue, out PositionValue maxPositionValue)
        {
            var positionValues = new List<PositionValue>();

            var cameraTransform = Camera.main.transform;
            var cameraPosition = cameraTransform.position;
            cameraPosition.y = 0;

            minPositionValue = new PositionValue
            {
                CameraAngleValue = float.MaxValue,
                InNextAreaValue = float.MaxValue,
                DistToPreviousMole = float.MaxValue,
                TimeRequiredFairness = float.MinValue
            };
            maxPositionValue = new PositionValue
            {
                CameraAngleValue = float.MinValue,
                InNextAreaValue = float.MinValue,
                DistToPreviousMole = float.MinValue,
                TimeRequiredFairness = float.MinValue
            };

            foreach (var spawnPosition in positions)
            {
                var expectedUserPosition =
                    spawnPosition + (cameraPosition - spawnPosition).normalized * UserBodyHitOffset;
                
                var playerToExpectedHitPosition = expectedUserPosition - cameraPosition;
                var offsetDistance = playerToExpectedHitPosition.magnitude;
                
                var offsetAngle = Vector3.Angle(cameraTransform.forward, playerToExpectedHitPosition);
                
                // calculate the number of seconds that it will take the user to get there approx.
                var secondsRequiredForMole = offsetAngle / ExpectedUserAngularSpeed + offsetDistance / ExpectedUserSpeed;

                var timeRequiredFairness = secondsRequiredForMole - totalMoleUpTime / 2;
                if (timeRequiredFairness > 0) timeRequiredFairness = timeRequiredFairness * 2;
                else timeRequiredFairness *= -1;

                var hitPolygon = Polygon.AsCircle(.6f, expectedUserPosition, 8);
                var containing = ClipperUtility.ContainsRelative(new Polygon(areaAtSpawn.Select(point => Vector.FromVector3(point)).ToList()), hitPolygon);
                
                // if contains 90% give good score
                var inThisAreaValue = 0f;
                if (containing < .9f)
                {
                    inThisAreaValue =
                        VirtualSpaceHandler.DistanceFromPoly(cameraPosition * 2 / 5 + expectedUserPosition * 3 / 5, areaAtSpawn, true) +
                        VirtualSpaceHandler.DistanceFromPoly(cameraPosition * 1 / 5 + expectedUserPosition * 4 / 5, areaAtSpawn, true) +
                        VirtualSpaceHandler.DistanceFromPoly(expectedUserPosition, areaAtSpawn, true);
                }
                
                // in next area, in the direction of the future
                var inNextAreaValue =
                    VirtualSpaceHandler.DistanceFromPoly(expectedUserPosition, areaAtNextSpawn, true);

                // dist to previous
                var distToPreviousMole = 1;
                if (_moles.Any())
                {
                    var otherMole = _moles.First();
                    var otherMolePosition = otherMole.transform.position;
                    otherMolePosition.y = 0;
                    var dist = Vector3.Distance(spawnPosition, otherMolePosition);
                    distToPreviousMole = dist > .2f ? 1 : 0;
                }

                var positionValue = new PositionValue
                {
                    Position = spawnPosition,
                    CameraAngleValue = offsetAngle < MinBestViewAngle ? MinBestViewAngle : offsetAngle,
                    InNextAreaValue = inThisAreaValue + inNextAreaValue,
                    DistToPreviousMole = distToPreviousMole,
                    TimeRequiredFairness = timeRequiredFairness
                };

                minPositionValue.CameraAngleValue =
                    Math.Min(minPositionValue.CameraAngleValue, positionValue.CameraAngleValue);
                minPositionValue.InNextAreaValue =
                    Math.Min(minPositionValue.InNextAreaValue, positionValue.InNextAreaValue);
                minPositionValue.DistToPreviousMole =
                    Math.Min(minPositionValue.DistToPreviousMole, positionValue.DistToPreviousMole);
                minPositionValue.TimeRequiredFairness =
                    Math.Min(minPositionValue.TimeRequiredFairness, positionValue.TimeRequiredFairness);

                maxPositionValue.CameraAngleValue = 
                    Math.Max(maxPositionValue.CameraAngleValue, positionValue.CameraAngleValue);
                maxPositionValue.InNextAreaValue = 
                    Math.Max(maxPositionValue.InNextAreaValue, positionValue.InNextAreaValue);
                maxPositionValue.DistToPreviousMole =
                    Math.Max(maxPositionValue.DistToPreviousMole, positionValue.DistToPreviousMole);
                maxPositionValue.TimeRequiredFairness =
                    Math.Max(maxPositionValue.TimeRequiredFairness, positionValue.TimeRequiredFairness);

                positionValues.Add(positionValue);
            }

            return positionValues;
        }

        private void Update()
        {
            if (CurrentGameState == WhacGameState.BeforeGame)
            {
                // place mole
                // display permanentely
                // let him follow the centroid
                List<Vector3> currentArea;
                Vector3 currentPosition;

                if (!VsHandler.SpaceAtTimeWithSafety(CognitiveProcessingDelay, 0, out currentArea, out currentPosition))
                    return;

                //if (_startMole == null)
                //    _startMole = PlaceMole(currentPosition, float.MaxValue, _moleUpDownTime, 0);
                if (!FollowText.gameObject.activeSelf)
                {
                    FollowText.gameObject.SetActive(true);
                    // change text
                    if (_highscore >= 0)
                    {
                        FollowText.SetScoreText(_highscore, _bestHighscore);
                    } else
                    {
                        FollowText.SetStandardText();
                    }
                }

                FollowText.Move(currentPosition);
            }
            else
            {
                if (_gameEndAt <= Time.time)
                {
                    Debug.Log("Game finished");
                    // display time on board
                    CurrentGameState = WhacGameState.BeforeGame;
                    _highscore = _board.TotalScore;
                    _bestHighscore = Math.Max(_highscore, _bestHighscore);
                }
                else
                {
                    //UpdateGameParameters();
                    PlaceMoles();
                }
            }

            if (VsMenu.IsControlCondition() && !_controlGroupChecked)
            {
                _controlGroupChecked = true;

                //_flowerController.FlowerWasHit -= _board.RecordHit;
                _flowerController.gameObject.SetActive(false);
            }
        }

        private class PositionValue
        {
            public float CameraAngleValue;
            public float TimeRequiredFairness;
            public float DistanceToCurrentValue;
            public float DistanceToDeltaValue;
            public float InNextAreaValue;
            public float DistToPreviousMole;
            public Vector3 Position;
            public float Value;
            public float WalkLineContainedValue;
        }

        public class WhacGameEvent
        {
            public float EndUnity;
            public float Speed;
            public float StartUnity;
            public float Duration { get { return EndUnity - StartUnity; } }
            public WhacGameEventType Type;
            internal Polygon StartArea;
            internal Polygon EndArea;
            internal Polygon SharedArea;
            internal bool HasShared { get { return SharedArea != null; } }

            public WhacGameEvent(float start, float end, WhacGameEventType type, float speed = 0f)
            {
                StartUnity = start;
                EndUnity = end;
                Type = type;
                Speed = speed;
            }
        }


        private class SpawnRhythm
        {
            private int _nextIndex;
            private readonly List<float> _spawnRhythm = new List<float> {2f, 3f, 2.5f, 1.5f, 2f, 2.5f};

            public float CurrentOffset()
            {
                return _spawnRhythm[_nextIndex];
            }

            public void NextOffset()
            {
                _nextIndex = (_nextIndex + 1) % _spawnRhythm.Count;
            }
        }
    }
}
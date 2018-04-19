using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VirtualSpace.Shared;

namespace VirtualSpace.Badminton
{
    public class BadmintonControl : MonoBehaviour
    {
        public float ExpectedCognitiveProcessingTime = .3f;
        public float InformTimeBeforeFastMovement = 0;
        public float TransitionSpeedConsideredFast = .5f;
        public float ResendStatesAfter = .5f;

        private object _heighestStateAccessor = new object();
        private int _heighestState = -1;
        float _lastTransitionStart; // todo to only handle new ones, use better method later
        BadmintonGamePeriods _gamePeriods;

        [Header("References")]
        public AIPlayer AI;
        [HideInInspector]
        public VirtualSpaceCore Core;
        public VirtualSpaceHandler Handler;

        void Awake()
        {
            _lastTransitionStart = -1;
            _gamePeriods = new BadmintonGamePeriods();
        }

        private void Start()
        {
            Core = VirtualSpaceCore.Instance;
        }

        BadmintonGamePeriod _handledMovement;
        void Update()
        {
            float now = Time.unscaledTime;
            _gamePeriods.Update(now);

            if (_gamePeriods.HaveNewPeriod)
            {

            }

            if (Handler.GetNewState())
            {
                EvaluateStates(Handler.State);
            }

            if (_gamePeriods.TimeToMovement < 1 && _handledMovement != _gamePeriods.NextMovement)
            {
                var nextMovement = _gamePeriods.NextMovement;
                AI.SetVirtualSpacePreferedHitParameter(nextMovement.StartUnity - ExpectedCognitiveProcessingTime,
                    nextMovement.Duration + ExpectedCognitiveProcessingTime);
                _handledMovement = _gamePeriods.NextMovement;
            }
        }

        private void OnEnable()
        {
            Handler.OnEventsUpdate += UpdateGameEvents;
        }

        private void OnDisable()
        {
            Handler.OnEventsUpdate -= UpdateGameEvents;
        }
        
        #region Evaluation
        void EvaluateStates(StateInfo state)
        {
            lock (_heighestStateAccessor)
            {
                if (state.StateId < _heighestState) return;
                if (_heighestState < state.StateId) _heighestState = state.StateId;

                if (_sendPreferencesCoroutine != null)
                {
                    StopCoroutine(_sendPreferencesCoroutine);
                    _sendPreferencesCoroutine = null;
                }

                //Debug.Log("Evaluating state");

                TransitionVoting voting = new TransitionVoting { StateId = state.StateId };

                var aiHitIn = AI.ExpectedToHitNextIn();
                //Debug.Log("Expecting hit in " + aiHitIn);


                if (!float.IsNaN(aiHitIn))
                {
                    foreach (var transition in state.PossibleTransitions)
                    {
                        TransitionVote vote = new TransitionVote();

                        vote.PlanningTimestampMs = new List<double>() { aiHitIn * 1000 };
                        vote.ExecutionLengthMs = new List<double>() { 2000 };
                        vote.Transition = transition;

                        vote.Value = Random.Range(99, 100);

                        List<TimeCondition> conditions;
                        Value valueFunction;
                        CreateTimeConditions(state, out conditions, out valueFunction);

                        vote.TimeConditions = conditions;
                        vote.ValueFunction = valueFunction;

                        if (transition == VSUserTransition.Rotate45Left || transition == VSUserTransition.Rotate45Right)
                            vote.Value = Random.Range(100, 101);

                        voting.Votes.Add(vote);
                    }

                    Core.SendReliable(voting);

                    _sendPreferencesCoroutine = RepeatOnStateUpdate(ResendStatesAfter, state);
                    StartCoroutine(_sendPreferencesCoroutine);
                }
            }
        }

        private void CreateTimeConditions(StateInfo state, out List<TimeCondition> timeConditions, out Value valueFunction)
        {
            var now = VirtualSpaceTime.CurrentTimeInSeconds;

            var aiHitIn = AI.ExpectedToHitNextIn();

            timeConditions = new List<TimeCondition>();

            var executionEndTime = Mathf.Max(now, state.ToSeconds);
            // important because we calculate dynamic tolerance based on the offset from this value

            var proxyPrep = new Variable(VariableTypes.Continuous);
            var intervalNum = new Variable(VariableTypes.Integer);
            var prepSeconds = new Variable(VariableTypes.PreperationTime);
            var execSeconds = new Variable(VariableTypes.ExecutionTime);

            // todo
            var enemyHitTolerance = .05f; // this should be min,max enemy hit time
            // todo
            var ballExchangeTolerance = .2f; // how much the player hits can be influenced

            // ball exchange time
            timeConditions.Add(proxyPrep == aiHitIn + intervalNum * 2);
            timeConditions.Add(prepSeconds >= proxyPrep - (intervalNum * ballExchangeTolerance + enemyHitTolerance));
            timeConditions.Add(prepSeconds <= proxyPrep + (intervalNum * ballExchangeTolerance + enemyHitTolerance));

            timeConditions.Add(execSeconds >= 1.5f);
            timeConditions.Add(execSeconds <= 2.5f);

            valueFunction = 1;// 100 + prepSeconds * (-10);
        }

        private IEnumerator _sendPreferencesCoroutine;

        private IEnumerator RepeatOnStateUpdate(float repeatAfter, StateInfo state)
        {
            yield return new WaitForSeconds(repeatAfter);
            EvaluateStates(state);
        }
        #endregion

        void UpdateGameEvents()
        {
            var incoming = Handler.IncomingTransitions; // should be sorted because we never overwrite events (messes up if we reschedule events

            foreach (var transition in incoming)
            {
                if (transition.TurnStart > _lastTransitionStart)
                {
                    if (transition.TransitionContext != TransitionContext.Animation) continue;

                    var isFastTransition = transition.Speed > TransitionSpeedConsideredFast;
                    var now = Time.time;
                    float start;
                    float end;
                    GetUnityStartStopTime(transition, out start, out end);

                    var timeToStart = start - now; // should always be higher than CognitiveProcessing
                    
                    switch (transition.TransitionType)
                    {
                        case VSUserTransition.Stay:
                            break;
                        case VSUserTransition.Focus:
                            {
                                // maybe not, need to care for other players, wait for the transition time maybe?
                                var whacEvent = new BadmintonGamePeriod(start + 1f, end, BadmintonGamePeriodType.WalkALot);
                                _gamePeriods.Add(whacEvent);
                                break;
                            }
                        case VSUserTransition.Undefocus:
                            {
                                // maybe not, need to care for other players
                                var whacEvent = new BadmintonGamePeriod(start, end, BadmintonGamePeriodType.Normal);
                                _gamePeriods.Add(whacEvent);
                                break;
                            }
                        case VSUserTransition.Defocus:
                            {
                                var whacEvent = new BadmintonGamePeriod(start, end, BadmintonGamePeriodType.ShootHighInCorner);
                                _gamePeriods.Add(whacEvent);
                                break;
                            }
                        case VSUserTransition.Unfocus:
                        case VSUserTransition.Rotate45Left:
                        case VSUserTransition.Rotate45Right:
                        case VSUserTransition.RotateLeft:
                        case VSUserTransition.RotateRight:
                            {
                                //Debug.Log("Movement:");

                                if (InformTimeBeforeFastMovement > 0)
                                {
                                    var informEvent = new BadmintonGamePeriod(start - InformTimeBeforeFastMovement, start, BadmintonGamePeriodType.BeforeMovement);

                                    //Debug.Log("Suspense: start " + (suspenseEvent.StartUnity - Time.time) + " end " + (suspenseEvent.EndUnity - Time.time));

                                    _gamePeriods.Add(informEvent);
                                }

                                var movementEvent = new BadmintonGamePeriod(start, end, BadmintonGamePeriodType.Movement);
                                _gamePeriods.Add(movementEvent);

                                //Debug.Log("Movement: start " + (movementEvent.StartUnity - Time.time) + " end " + (movementEvent.EndUnity - Time.time));
                                //Debug.Log("Normal: start " + (backToNormalEvent.StartUnity - Time.time) + " end " + (backToNormalEvent.EndUnity - Time.time));
                                break;
                            }
                        case VSUserTransition.SwitchLeft:
                        case VSUserTransition.SwitchRight:
                            // todo
                            break;
                        default:
                            throw new System.Exception("Badminton control doesn't know transition type " + transition.TransitionType + ".");
                    }

                    _lastTransitionStart = transition.TurnStart;
                }
            }
        }

        #region Helper
        void GetUnityStartStopTime(Transition transition, out float start, out float end)
        {
            start = Time.time + VirtualSpaceTime.ConvertTurnsToSeconds(transition.TurnStart - VirtualSpaceTime.CurrentTurn);
            end = Time.time + VirtualSpaceTime.ConvertTurnsToSeconds(transition.TurnEnd - VirtualSpaceTime.CurrentTurn);
        }
        #endregion
    }

    public enum BadmintonGamePeriodType
    {
        BeforeMovement,
        Normal,
        Movement,
        WalkALot,
        ShootHighInCorner
    }

    public class BadmintonGamePeriod
    {
        public float StartUnity;
        public float EndUnity;
        public BadmintonGamePeriodType Type;
        public bool Started;
        public float Duration { get { return EndUnity - StartUnity; } }

        public BadmintonGamePeriod(float start, float end, BadmintonGamePeriodType type)
        {
            StartUnity = start;
            EndUnity = end;
            Type = type;
        }
    }

    public class BadmintonGamePeriods
    {
        List<BadmintonGamePeriod> _gamePeriods;
        bool _haveNewPeriod = false;
        public BadmintonGamePeriodType ActivePeriodType;
        public float ActiveDuration;
        public BadmintonGamePeriod NextMovement { get { return _gamePeriods.Find(period => period.Type == BadmintonGamePeriodType.Movement); } }
        private float _now;

        public bool HaveNewPeriod
        {
            get
            {
                var haveNew = _haveNewPeriod;
                _haveNewPeriod = false;
                return haveNew;
            }
        }

        public void Add(BadmintonGamePeriod period)
        {
            _gamePeriods.Add(period);
        }

        public BadmintonGamePeriods()
        {
            _gamePeriods = new List<BadmintonGamePeriod>();

        }

        public void Update(float now)
        {
            _now = now;

            _gamePeriods.RemoveAll(gamePeriod => gamePeriod.EndUnity <= now);

            var fakePeriod = _gamePeriods.IsEmpty() || _gamePeriods[0].StartUnity > now;

            var updatedActive = fakePeriod ? BadmintonGamePeriodType.Normal : _gamePeriods[0].Type;
            _haveNewPeriod = updatedActive != ActivePeriodType;
            ActivePeriodType = updatedActive;
            ActiveDuration = fakePeriod ? (_gamePeriods.IsEmpty() ? 5 : _gamePeriods[0].StartUnity - now /* time to next */) : _gamePeriods[0].Duration;
        }

        public float TimeToMovement
        {
            get
            {
                var period = _gamePeriods.Find(match => match.Type == BadmintonGamePeriodType.Movement);
                if (period == null) return float.MaxValue;
                return period.StartUnity - _now;
            }
        }
    }
}
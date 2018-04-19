using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VirtualSpace;
using VirtualSpace.Shared;
using VirtualSpace.Shared;


namespace Games.Scripts
{
    public class WhacLogic : MonoBehaviour
    {
        /// <summary> 
        /// Seconds stayed over time to value from 0 to 1 
        /// </summary> 
        public AnimationCurve StayCurve;

        public AnimationCurve FocusCurve;

        public VirtualSpaceHandler VsHandler;
        [HideInInspector]
        public VirtualSpaceCore VsCore;

        public WhacGameLogic GameLogic;

        void Start()
        {
            VsCore = VirtualSpaceCore.Instance;

            _random = new System.Random();
        }

        void OnEnable()
        {
            VsHandler.OnStateUpdate += OnStateUpdate;
            if (VsHandler.State != null)
                OnStateUpdate(VsHandler.State);
            VsHandler.OnAllocation += OnAllocation;
        }

        void OnDisable()
        {
            VsHandler.OnStateUpdate -= OnStateUpdate;
            VsHandler.OnAllocation -= OnAllocation;
        }

        public void OnAllocation()
        {
            // todo
            // get the 5 points in walkable
        }

        private const int MillisecondsInSeconds = 1000;
        private readonly List<VSUserTransition> _trackedUserTransitions = new List<VSUserTransition>
            { TransitionHelper.StayTransitions, VSUserTransition.Defocus };
        public float DefocusToStayDegradation = .6f;
        public float SwitchToRotateDegradation = .7f;

        public StateInfo _lastStateInfo;

        private object _heighestStateAccessor = new object();
        private int _heighestState = Int32.MinValue;
        private System.Random _random;

        void OnStateUpdate(StateInfo state)
        {
            lock (_heighestStateAccessor)
            {
                if (state.StateId < _heighestState)
                {
                    Debug.Log("Lower than previous high");
                    return;
                }
                if (_heighestState < state.StateId) _heighestState = state.StateId;
                if (sendPreferencesCoroutine != null)
                    StopCoroutine(sendPreferencesCoroutine);

                var voting = GenerateVote(state);

                _lastStateInfo = state;
                VsCore.SendReliable(voting);

                sendPreferencesCoroutine = RepeatOnStateUpdate(1, state);
                StartCoroutine(sendPreferencesCoroutine);
            }
        }

        private int _highestStateId = -1;
        public int PastTransitionsConsidered = 10;
        private Queue<VSUserTransition> _lastTransitions = new Queue<VSUserTransition>();

        private TransitionVoting GenerateVote(StateInfo state)
        {
            var nowSeconds = VirtualSpaceTime.CurrentTimeInSeconds;
            
            TransitionVoting voting = new TransitionVoting();
            voting.StateId = state.StateId;

            if (_highestStateId < state.StateId)
            {
                _lastTransitions.Enqueue(state.YourCurrentTransition);
                while (_lastTransitions.Count > PastTransitionsConsidered)
                {
                    _lastTransitions.Dequeue();
                }
                _highestStateId = state.StateId;
            }

            var transitionVotes = new List<TransitionVote>();

            // time
            List<TimeCondition> timeConditions;
            List<double> planningTimestampMs;
            List<double> executionTimestampMs;
            Value value;
            CreateTimeConditions(state.ToSeconds, out timeConditions, out value, out planningTimestampMs, out executionTimestampMs);

            // value
            var stayingValue = 1f;
            var focusValue = 0f;
            if (_lastTransitions.Any())
            {
                var countStayTransitions = _lastTransitions.Where(transition => (transition & TransitionHelper.StayTransitions) == transition).Count();
                var countFocusTransitions = _lastTransitions.Where(transition => transition == VSUserTransition.Focus).Count();

                stayingValue = 1 - (float)countStayTransitions / _lastTransitions.Count;
                focusValue = countFocusTransitions == 0 ? 1 : countFocusTransitions == 1 ? .5f : .25f;
            }

            for (int transitionNum = 0; transitionNum < state.PossibleTransitions.Count; transitionNum++)
            {
                var transitionType = state.PossibleTransitions[transitionNum];

                TransitionVote vote = new TransitionVote();
                vote.Transition = transitionType;
                vote.TimeConditions = timeConditions;
                vote.ValueFunction = value;
                vote.PlanningTimestampMs = planningTimestampMs;
                vote.ExecutionLengthMs = executionTimestampMs;

                switch (transitionType)
                {
                    case VSUserTransition.Stay:
                        vote.Value = stayingValue;
                        break;
                    case VSUserTransition.Defocus:
                        vote.Value = stayingValue * DefocusToStayDegradation;
                        break;
                    case VSUserTransition.Focus:
                        vote.Value = focusValue;
                        break;
                    case VSUserTransition.Unfocus:
                        vote.Value = .5;
                        break;
                    case VSUserTransition.Undefocus:
                        vote.Value = 1;
                        break;
                    case VSUserTransition.Rotate45Left:
                    case VSUserTransition.Rotate45Right:
                    case VSUserTransition.RotateLeft:
                    case VSUserTransition.RotateRight:
                        var rotateValue = (1 - stayingValue);
                        vote.Value = rotateValue / 2 + _random.NextDouble() * rotateValue / 2;
                        break;
                    case VSUserTransition.SwitchLeft:
                    case VSUserTransition.SwitchRight:
                        vote.Value = SwitchToRotateDegradation * (1 - stayingValue);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
                // DEBUG
                //if (transitionType != VSUserTransition.Defocus && transitionType != VSUserTransition.Undefocus)
                //{
                //    vote.Value = _random.NextDouble() * .5f;
                //} else
                //{
                //    vote.Value = 1;
                //}

                transitionVotes.Add(vote);
            }

            var maxValue = transitionVotes.Max(vote => vote.Value);
            if (maxValue == 0) maxValue = 1;
            var mulFactorTo100Max = 100 / maxValue;
            transitionVotes.ForEach(vote => vote.Value *= mulFactorTo100Max);

            //string outstring = "";
            //foreach (var vote in transitionVotes)
            //{
            //    outstring += "(Transition: " + vote.TransitionName + ", Value: " + vote.Value + ") ";
            //}
            //Debug.Log(outstring);


            voting.Votes = transitionVotes;
            return voting;
        }

        private IEnumerator sendPreferencesCoroutine;

        private IEnumerator RepeatOnStateUpdate(float repeatAfter, StateInfo state)
        {
            yield return new WaitForSeconds(repeatAfter);
            OnStateUpdate(state);
        }

        private void CreateTimeConditions(float minExecutionSeconds, out List<TimeCondition> timeConditions, out Value value, out List<double> planningTimestampMs, out List<double> executionTimestampMs)
        {
            var now = VirtualSpaceTime.CurrentTimeInSeconds;
            var earliestExecutionTime = Mathf.Max(now, minExecutionSeconds);

            timeConditions = new List<TimeCondition>();

            var offset = (GameLogic.NextSpawnSeconds - Time.time);
            if (offset < 0) offset = GameLogic.DesiredInBetweenSeconds;
            var nextSpawnOffsetMs = offset;
            var timeBetweenSpawn = GameLogic.DesiredInBetweenSeconds;
            var timeTolerance = .5f;

            var proxyPrep = new Variable(VariableTypes.Continuous, "proxyPrep");
            var intervalNum = new Variable(VariableTypes.Integer, "intervalNum");
            var prepSeconds = new Variable(VariableTypes.PreperationTime, "prepSeconds");
            var execSeconds = new Variable(VariableTypes.ExecutionTime, "execSeconds");
            var actualTolerance = new Variable(VariableTypes.Continuous, "actualTolerance");
            var futureOffset = new Variable(VariableTypes.Continuous, "futureOffset");

            timeConditions.Add(intervalNum >= 0);
            timeConditions.Add(futureOffset == intervalNum * timeBetweenSpawn);
            timeConditions.Add(0 <= futureOffset);
            timeConditions.Add(proxyPrep == now + nextSpawnOffsetMs + futureOffset);
            timeConditions.Add(0 <= actualTolerance);
            timeConditions.Add(actualTolerance <= (intervalNum + 1)* timeTolerance);
            timeConditions.Add(prepSeconds >= proxyPrep - actualTolerance);
            timeConditions.Add(prepSeconds <= proxyPrep + actualTolerance);
            timeConditions.Add(execSeconds >= timeBetweenSpawn);

            value = 0;

            //value = futureOffset + actualTolerance;

            planningTimestampMs = new List<double>()
            {
                now + nextSpawnOffsetMs, now + nextSpawnOffsetMs + timeBetweenSpawn, now + nextSpawnOffsetMs + 2 * timeBetweenSpawn
            };
            executionTimestampMs = new List<double>()
            {
                2 * timeBetweenSpawn, 3 * timeBetweenSpawn, 1.5f * timeBetweenSpawn
            };

            //for (int i = 0; i < planningTimestampMs.Count; i++)
            //{
            //    Debug.Log($"prep {planningTimestampMs[i]}, exec {executionTimestampMs[i]}");
            //}
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Assets.ViveClient;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VirtualSpace;
using VirtualSpace.Shared;
using Random = UnityEngine.Random;

#pragma warning disable 618

namespace Games.Tennis3D.Scripts
{
    public class Tennis3DBallLogic : MonoBehaviour
    {
        [Header("--- Enemy / Game Difficulty")]
        public bool PlayerCanDirectlyAccept = true;
        public bool PlayerCanHitAllFieldOnServe = true;
        public bool PlayerCanServeFromAnywhere = true;
        public int MinAmountHitsEnemy = 4;
        public float MinLengthForHit = 1f;
        public float MaxLengthForHit = 4f;
        public float MinLengthForSmash = 1f;
        public float MaxLengthForSmash = 4f;
        public float GravityPlayer = -9.81f;
        public float GravityEnemy = -9.81f;
        public float DesiredWinProbability = 0.6f;
        public float DistanceMax = 12f;
        public float DistanceMin = 6f;
        public float EnemyHitHeight = 0.3f;
        public float EnemyHitHeightMax = 2.5f;
        public float EnemyHitHeightMin = 0.07f;
        public float EnemySmashHeight = 2f;
        public float EnemyXzToYRatio = 0.5f;
        [Range(0f,1f)]
        public float PercentileForMaxHitLengthComputation = 0.9f;

        [Header("--- Other Game Settings")]
        public Transform Viz;
        public bool EnemyAlwaysServes = true;
        [Range(0f, 1f)]
        public float AdaptationOffsetFromGround = 0.9f;
        [Range(0f, 1f)]
        public float AdaptationDistance2Hit = 0.9f;
        [Range(0f, 1f)]
        public float AdaptationSimpleToComplex = 0.5f;
        public float BallResetTime = 1f;
        public float NormalServeCountdown = 2f;
        public Rules RulesApplied = Rules.Badminton;
        public enum Rules
        {
            Tennis,
            Badminton
        }
        public string UntrackedTag = "untracked";
        [Header("--- Backend Settings / Incentive Placement")]
        public Transform Center;
        public float SyncAtSpeed = 0.4f;
        public float MaxSpeed = 0.1f;
        public float CognitiveProcessingTime = 0.5f;
        public float Safety = 0.5f;
        //  public Transform RecommendationPlayingFieldCenter;
        // public float RecommendationNormalContinuation = 1f;
        //public float EstimatedMaxTimeBallExchange = 4f;
        public float EstimatedEnemyHitLength = 2f;
        public float EstimatedPlayerHitLengthAtStart = 2f;
        public float EstimatedPlayerYOffsetOnHit = 1.8f;
        public float EstimatedDistanceToBallOnHit = 0.6f;
        public float EstimatedWaitAfterHitTime = 2f;
        public List<Transform> NiceToHaveSpots;
        public float RandFromDist = 1f;
        public float RandToDist = 2f;
        public int TriesForServe = 10;
        public int TriesForAllField = 10;
        [Header("--- Fixed References (do not change)")]
        public Transform NetHeight;
        public GameObject HitIndicator;
        public ParticleSystem BallIndicator;
        public float BallIndicatorOffset;
        public Tennis3DBallHint Hint;
        public Tennis3DServeHelperSpot HelperSpot;
        public TennisPlaySounds TennisSounds;
        public TrailRenderer Trail;
        public CameraRig CamRig;
        public Collider PlayerRightFieldSide;
        public Collider ValidArea;
        public Collider EnemyArea;
        public Collider PlayerArea;
        public Collider EnemyAreaOuter;
        public Collider PlayerAreaOuter;
        public Collider Floor;
        // public Collider PlayerRacket;
        public Collider Net;
        public Collider PlayerLeftServeArea;
        public Collider PlayerRightServeArea;
        public Collider EnemyLeftServeArea;
        public Collider EnemyRightServeArea;
        public Collider PlayerLeftServeAreaPlay;
        public Collider PlayerRightServeAreaPlay;
        public Collider EnemyLeftServeAreaPlay;
        public Collider EnemyRightServeAreaPlay;
        public Text TextPlayerPoint, TextEnemyPoint, TextPlayerGames, TextEnemyGames, TextPlayerServe, TextEnemyServe;
        public Transform PlayerLeft;
        public Transform PlayerRight;
        public Transform EnemyLeft;
        public Transform EnemyRight;
        //public Collider[] InvalidAreas;
        public EnemyPlayerTennis Enemy;
        public CurvedLineRenderer FrontTrail;
        public List<Transform> FrontTrailPoints;
        public List<Collider> PlayerRacket;
        public ParticleSystem Particles;
        // public float RectSafetyAdd = 0.2f;
        //  public float RectStartTimeThreshold = 3f;
        //public float ServeOffset = 1f;
        //public float RandomTimeOffsetPercentage = 0.3f;
        public VirtualSpaceHandler VirtualSpaceHandler;
        public Transform TennisRacketPosition;

        [HideInInspector] public UnityEvent PlayerScore;
        [HideInInspector] public UnityEvent EnemyScore;
        
        private Vector3 _plannedPointWhereBallHitsGroundAfterEnemyHit;
        private float _plannedImpactDistance2PlayerAfterEnemyHit;
        private List<Vector3> _impactPoints;
        private List<float> _lastPlayerHitDistances;
        private float _resetAtTime;
        private List<float> _lastPlayerYHits;
        private float _gravity;
        private bool _serveMistakeMade;
        private int _hits;
        private List<float> _timesOk;
        private List<float> _timesNotOk;
       // private List<float> _playerHitYOffset;
        private float _timeWhichMightBeOk;
       // private bool _resetBall;
        private Rigidbody _rigidBody;
        private int _timesTouchedGround;
        private bool _playerTouchedLast;
        //private bool _hitNet;
        private int _gamesWon;
        private int _gamesLost;
        private int _scorePlayer;
        private int _scoreEnemy;
        private bool _playerServes;
        private bool _servesFromRight;
        private float _timeToResetBall;
        //private Vector3 _desiredPosition;
        //private float _desiredTime;
        private Vector3 _enemyImpactPoint;
        private float _enemyImpactTime;
        private bool _enemyShouldFail;
        private bool _enemyShouldNotSmash;
        private float _enemyHitLength;
        //private float _enemyServeCountdown;
        private float _forceBallout;
        private bool _forceBalloutPointToPlayer;
        private float _resetPositionAreaTimeOffset;
        private bool _hitGroundPlayer;
        private int _overallPlayerPoints;
        private int _overallEnemyPoints;
        private List<float> _playerHitLengths;
        //private bool _estimatedWillFailOnHit;
        //private bool _isDesiredTarget;
        // private float _serveCountdown;
        private bool _ignoreMistake;
        //private List<float> _enemyShouldHit;
        //private List<float> _enemyShouldMaybeHit;
        private float _timeScaleWarper;

        //private List<EnemyHitKeyframe> HitKeyframes;
        //private struct EnemyHitKeyframe
        //{
        //    public float TimeEnemyHit;
        //    public float TimePlayerReceives;
        //    public bool Obligatory;
        //}

        #region unity script functions
        void Awake ()
        {
            _forceBallout = -1f;
            _timesOk = new List<float>();
       //     _playerHitYOffset = new List<float>();
            _timesNotOk = new List<float>();
            _playerHitLengths = new List<float>();
            _lastPlayerYHits = new List<float>();
            _lastPlayerHitDistances = new List<float>();
            _impactPoints = new List<Vector3>();
            _rigidBody = GetComponent<Rigidbody>();
            _playerServes = Random.Range(0, 2) == 0;
            _playerTouchedLast = !_playerServes;
            ChangeGravity(_playerServes);
            UpdateUi();
            //_resetBall = true;
            BallIndicator.enableEmission = false;
        }

        void Start()
        {
            VirtualSpaceHandler.OnEventsChanged += UpdatePlan;
            VirtualSpaceHandler.OnReceivedNewEvents += UpdatePlan;
            SetFrontTrail(false);
            _timeToResetBall = 1f;
            if (!_playerServes)
                _enemyImpactTime = NormalServeCountdown;
            Hint.gameObject.SetActive(false);

            StateHandlerProperties props = new StateHandlerProperties
            {
                QueueLength = 0,
                MovePriority = .2f,
                TimePriority = .8f
            };
            VirtualSpaceCore.Instance.SendReliable(props);

            DebugFindNextPlayerHitSettings();
        }

        void Update()
        {
            //check if ball should be forced out
            if (_forceBallout > 0f)
            {
                _forceBallout -= Time.deltaTime;
                if (_forceBallout <= 0f)
                {
                    _forceBallout = -1f;
                    Debug.Log("forced");
                    BallOut(_forceBalloutPointToPlayer, false);
                }
            }

            //check if ball should be reset
            if (_timeToResetBall > 0f) //_resetBall && Time.time >= _timeToResetBall)
            {
                _timeToResetBall -= Time.deltaTime;
                if(_timeToResetBall <= 0f)
                    ResetBall();
            }

            //check if player can be helped serving the ball
            if(_playerServes && _hits == 0)
            {
                transform.position = HelperSpot.GetSpot(_resetPositionAreaTimeOffset, false);
                if(Time.time > _resetAtTime + 3f && Vector3.Distance(TennisRacketPosition.position, transform.position) > 0.5f)
                {
                    PlayerHitBall();
                }
            }

            //check if there is a new state in backend
            if (VirtualSpaceHandler.GetNewState())
            {
                EvaluateStates(VirtualSpaceHandler.State);
            }

            //set ball orientation for badminton
            if (_rigidBody.useGravity && RulesApplied == Rules.Badminton && _rigidBody.velocity.magnitude > 0.001f)// && _hits > 0 && _timesTouchedGround == 0 && RulesApplied == Rules.Badminton)
            {
                Viz.forward = _rigidBody.velocity.normalized;
            }

            //check if ball should be hit back
            _enemyImpactTime -= Time.deltaTime;
            var enemyShouldHit = false;
            if (_hits == 0 && !_playerServes)
            {//enemy serves
                // if (_serveCountdown >= 0f && (CamRig.Target == null || !CamRig.Target.CompareTag(UntrackedTag)))
                //{
                    //_serveCountdown -= Time.deltaTime;
                    if (_enemyImpactTime <= 0f)
                        enemyShouldHit = true;
                //}
            }
            else if (
                !_enemyShouldFail  &&// || _estimatedWillFailOnHit) &&
                _playerTouchedLast &&
                EnemyAreaOuter.bounds.Contains(transform.position))
            {//enemy returns
                if (_enemyImpactTime <= 0f)
                {
                    if (RulesApplied == Rules.Tennis)
                    {
                        if (!_enemyShouldNotSmash)
                        {
                            enemyShouldHit = true;
                        }
                        else if (
                            //ball back up
                            (_rigidBody.velocity.y < 0f || transform.position.y >= EnemyHitHeight) &&
                            _timesTouchedGround > 0)
                        {
                            enemyShouldHit = RulesApplied == Rules.Tennis;
                        }
                    }
                    else if (RulesApplied == Rules.Badminton)
                    {
                        enemyShouldHit = true;
                    }
                }
            }
            if (enemyShouldHit)
                EnemyHitBall();

            if (_hits == 0 && _playerServes || !_playerTouchedLast)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    PlayerHitBall();
                }

                DebugPlayerAutoHit();
            }


        }

        [Header("Settings for Player AI to simulate player behavior.")]
        public bool PlayerAutoHit = false;
        public float PlayerMeanHitHeight = 1.7f;
        public float PlayerVarianceHitHeight = .2f;
        //public float PlayerMissProbability = .05f;
        private float _playerNextHitHeight;
        [Tooltip("Shouldn't be too small because a hit might be missed.")]
        public float PlayerHitTolerance = .2f;

        private void DebugPlayerAutoHit()
        {
            if (PlayerAutoHit)
            {
                // check if it's in the field
                if (_rigidBody.velocity.y < 0f 
                        && Math.Abs(transform.position.y - _playerNextHitHeight) < .2f 
                        && PlayerArea.bounds.Contains(transform.position))
                {
                    PlayerHitBall();
                    DebugFindNextPlayerHitSettings();
                }
            }
        }

        private void DebugFindNextPlayerHitSettings()
        {
            //if (Random.Range(0, 1) <= PlayerMissProbability)
            //    _playerNextHitHeight = -1f;
            //else
            _playerNextHitHeight = SampleFromSimulatedNormalDistribution(PlayerMeanHitHeight, PlayerVarianceHitHeight);
        }

        private static float SampleFromSimulatedNormalDistribution(float mean, float std)
        {
            var u1 = 1 - Random.Range(0f, 1f);
            var u2 = 1 - Random.Range(0f, 1f);
            var randStdNormal = (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2));
            var randNormal = mean + std * randStdNormal;
            return randNormal;
        }
        #endregion

        #region virtualspace stuff

        private void UpdatePlan()
        {
            //HitKeyframes.Clear();
            //save time of fast transitions
            //var fastTransitions = VirtualSpaceHandler.IncomingTransitions.Where(it => it.Speed >= SyncAtSpeed);
            //var enemyShouldHit = fastTransitions.Select(it => VirtualSpaceTime.ConvertTurnsToSeconds(it.TurnStart - VirtualSpaceTime.CurrentTurn))
            //    .ToList();
            //var playerShouldReceive = fastTransitions.Select(it => VirtualSpaceTime.ConvertTurnsToSeconds(it.TurnEnd - VirtualSpaceTime.CurrentTurn))
            //    .ToList();
            //give veto
            //check if transition is too long, then subdivide
            //var enemyShouldMaybeHit = new List<float>();
            //for (var i = 1; i < enemyShouldHit.Count; i++)
            //{
            //    var last = enemyShouldHit[i - 1];
            //    var tmp = enemyShouldHit[i];
            //    var diff = tmp - last;
            //    var divideBy = 1;
            //    while (diff / divideBy > EstimatedMaxTimeBallExchange / 2f)
            //        divideBy += 2;
            //    for (var j = 1; j < divideBy; j++)
            //        enemyShouldMaybeHit.Add(last + j * diff / divideBy);
            //}
            //foreach (var esh in enemyShouldHit)
            //{
            //    var keyframe = new EnemyHitKeyframe
            //    {
            //        Time = Time.time + esh,
            //        Obligatory = true
            //    };
            //    Keyframes.Add(keyframe);
            //}
            //foreach (var esmh in enemyShouldMaybeHit)
            //{
            //    var keyframe = new EnemyHitKeyframe
            //    {
            //        Time = Time.time + esmh,
            //        Obligatory = false
            //    };
            //    Keyframes.Add(keyframe);
            //}
            //Keyframes.OrderBy(k => k.Time);
            // _enemyShouldHit.AddRange(addTimes);
            // _enemyShouldHit.Sort();
        }

        private bool TryGettingNextFastTransition(float timeThreshold, out float start, out float end)
        {
            start = 0f;
            end = 0f;

            if (!VirtualSpaceCore.Instance.IsRegistered()) return false;

            var incoming = VirtualSpaceHandler.IncomingTransitions;
            //incoming.ForEach(it => Debug.Log("Speed " + it.Speed));
            var fastTransitions = incoming.Where(it => it.Speed >= SyncAtSpeed).ToList();
            //foreach (var inc in incoming)
            //{
            //    Debug.Log("incoming transition speed: " + inc.Speed);
            //    Debug.Log("sync speed: " + SyncAtSpeed);
            //}
            //if(incoming.Any())
            //    Debug.Log("fast " + incoming.Max(i => i.Speed));
            if (!fastTransitions.Any()) return false;
            //Debug.Log("is fast");
            while (fastTransitions.Any())
            {
                var nextFastTransition = fastTransitions.First();
                fastTransitions.Remove(nextFastTransition);
                if (VirtualSpaceTime.CurrentTurn > nextFastTransition.TurnStart)
                    continue;

                start = VirtualSpaceTime.ConvertTurnsToSeconds(nextFastTransition.TurnStart - VirtualSpaceTime.CurrentTurn);

                if (nextFastTransition.TurnEnd == long.MaxValue)
                {
                    long turnEnd = nextFastTransition.Frames.ElementAt(nextFastTransition.Frames.Count - 2).Position.TurnEnd + 1;
                    end = VirtualSpaceTime.ConvertTurnsToSeconds(turnEnd - VirtualSpaceTime.CurrentTurn);
                }
                else
                {
                    end =
                        VirtualSpaceTime.ConvertTurnsToSeconds(nextFastTransition.TurnEnd - VirtualSpaceTime.CurrentTurn);
                }
                return start < timeThreshold;
            }
            return false;
        }

        private void PlayerHitsAdaptToRequirements()
        {
            if (!VirtualSpaceCore.Instance.IsRegistered())
                return;

            //compute impact point
            //--- ballAtMax/Min, timeAtMax/Min: spot when ball can be accepted
            // ReSharper disable once TooWideLocalVariableScope
            Vector3 ballAtMax, ballAtMin;
            // ReSharper disable once TooWideLocalVariableScope
            float timeAtMax, timeAtMin;
            TryComputeBallAtHeight(EnemyHitHeightMin, _rigidBody.velocity, transform.position, out ballAtMin, out timeAtMin, -_gravity);
            TryComputeBallAtHeight(EnemyHitHeightMax, _rigidBody.velocity, transform.position, out ballAtMax, out timeAtMax, -_gravity);

            //check for next fast transition
            float fastTransitionStart, fastTransitionEnd;
            //ignore, if ball goes 3 other times over net (player-enemey, enemy-player, player-enemy)
            var estimatedTimeForBallExchange = EstimatedMaxTimeBallExchange(1, 1) + timeAtMin;
            var fastTransitionImminent = TryGettingNextFastTransition(estimatedTimeForBallExchange, out fastTransitionStart, out fastTransitionEnd);
            //include offset for cognitive processing (understand instructions etc.)
            fastTransitionStart -= CognitiveProcessingTime;
            fastTransitionEnd -= CognitiveProcessingTime;
            //  Debug.Log(fastTransitionStart + "------" + estimatedTimeForBallExchange+ "---------" + fastTransitionImminent);

            //enemy should not ignore net (might later be reset, in case of quick transitions)
            Net.isTrigger = false;

            //get variables needed for further computation
            //--- ballAtZero, spotAtZero: spot when ball will hit ground
            Vector3 ballAtZero;
            float timeAtZero;
            TryComputeBallAtHeight(0f, _rigidBody.velocity, transform.position, out ballAtZero, out timeAtZero, -_gravity);
            //--- inField, hitsNet: check if ball in field AND over net
            var inField = IsInArea(EnemyArea.bounds, ballAtZero);
            var inOuterField = IsInArea(EnemyAreaOuter.bounds, ballAtZero);

            //var overNet = true;
            //Vector3 tmpPos;
            //float tmpTime;
            //if (TryComputeBallAtHeight(NetHeight.position.y, _rigidBody.velocity, transform.position, out tmpPos, out tmpTime, -_gravity) && PlayerArea.bounds.Contains(tmpPos))
            //{
            //    overNet = false;
            //}
            //--- isSmash: player directly accepted ball (tennis rules only)
            var isSmash = _timesTouchedGround == 0;

            //go through all cases (and set variables)
            if (fastTransitionImminent)
            {
                //everything is quick, need to sync at hit accordingly
                
                //minSuccess = ;
                //enemyComputationSuccess =  &&
                //                          enemyComputationSuccess;
                //set variables for enemy hit
                _enemyImpactTime = -1f;
                _enemyShouldFail = false;
                _enemyHitLength = fastTransitionEnd - fastTransitionStart;
                Net.isTrigger = true;
                _forceBallout = -1f;
                //set misc variables for serve
                _resetPositionAreaTimeOffset = 0f; 

                //check whether enemy should hit during game OR ball should be reset to player OR ball should be reset to enemy
                //(set impact time and impact point here)
                if ((!inField && !inOuterField))// ||!enemyComputationSuccess)
                {
                    //player would not get ball into enemy zone (note: accept if is in outer field!)
                   // Debug.Log("case OUT");
                  //  Debug.Log(enemyComputationSuccess + "!!!");

                    //enemy gets point and will serve, ball will forced out 
                    _forceBallout = fastTransitionStart / 2f;
                    _forceBalloutPointToPlayer = false;
                }
                else if (timeAtMin > 0f && fastTransitionStart > timeAtMin)
                {
                    //ball would be on ground before transition could start
                    Debug.Log("case TOO FAST");

                    //which is fine if there is still enough time to serve then AND the enemy always serves...

                    //try it a little earlier...
                    var doItAnyWay = fastTransitionStart - timeAtMin < 1.5f;
                    if (doItAnyWay)
                    {
                        Debug.Log("WHATEVER");
                        //enemy will hit a little earlier
                        _enemyImpactTime = timeAtMin;
                        _enemyHitLength = fastTransitionEnd - timeAtMin;
                        _enemyImpactPoint = TryComputeBallAtTime(_enemyImpactTime, _rigidBody.velocity, transform.position, -_gravity);
                    }
                    else
                    {
                        //let player serve next time
                        //_resetPositionAreaTimeOffset = fastTransitionEnd - fastTransitionStart;
                        //Debug.Log("NOT WHATEVER " + _resetPositionAreaTimeOffset);
                        //_forceBallout = fastTransitionStart;
                        //_forceBalloutPointToPlayer = true;
                    }
                    
                }
                //else if (timeAtMax > 0f && fastTransitionStart < timeAtMax)
                //{
                //    //player would hit high, so that enemy could not hit it in time
                //    Debug.Log("case TOO SLOW");

                //    //try it a little later...
                //    var doItAnyWay = timeAtMax - fastTransitionStart < fastTransitionEnd - timeAtMax;
                //    if (doItAnyWay)
                //    {
                //       Debug.Log("WHATEVER");
                //        _enemyImpactTime = timeAtMax;
                //        _enemyHitLength = fastTransitionEnd - timeAtMax;
                //        _enemyImpactPoint = TryComputeBallAtTime(_enemyImpactTime, _rigidBody.velocity, transform.position, -_gravity);
                //    }
                //    else
                //    {
                //        //enemy gets point and will serve, ball will forced out 
                //        _forceBallout = fastTransitionStart / 2f;
                //        _forceBalloutPointToPlayer = false;

                //        //better: also visualize missed ball (hit dove?)
                //    }
                //}
                else
                {
                    //we're GOOD, compute enemy stuff accordingly
               //     Debug.Log("case GOOD");
                    _enemyImpactTime = fastTransitionStart;
                    _enemyImpactPoint = TryComputeBallAtTime(_enemyImpactTime, _rigidBody.velocity, transform.position, -_gravity);
                }
                //note: in case where player misses the ball altogether: in update we compute enemies serve wait time
            }
            else if (inOuterField)
            {
                //everything is chill, just apply normal logic

                //check if enemy should fail == player did an amazing hit
                _enemyShouldFail = !inField;
                if (inField && isSmash && _hits > 1)
                {
                    //check if player landed a good shot
                    var dist2Enemy = Vector3.Distance(Enemy.transform.position, ballAtZero);
                    if (dist2Enemy > DistanceMin)
                    {
                        var distRatio = (dist2Enemy - DistanceMin) / (DistanceMax - DistanceMin);
                        var playerRatio = _overallPlayerPoints /
                                          Mathf.Max(1f, _overallEnemyPoints + _overallPlayerPoints);
                        if (distRatio > playerRatio && _hits > MinAmountHitsEnemy)
                        {
                            _enemyShouldFail = true;
                            //_estimatedWillFailOnHit = false;
                        }
                    }
                }

                //compute enemy impact position and time
                //check if recommended tick is imminent, then follow tick logic
                var recommendedTicks = VirtualSpaceHandler.RecommendedTicksRelative;
                if (recommendedTicks.Any() && recommendedTicks.First() < timeAtZero)
                {
                    _enemyImpactTime = recommendedTicks.First();
                    _enemyImpactPoint = TryComputeBallAtTime(recommendedTicks.First(), _rigidBody.velocity, transform.position, -_gravity);
                }
                else
                {
                    //get where enemy should hit and when
                    var couldNotCompute = false;
                    if (RulesApplied == Rules.Tennis)
                    {
                        //check if enemy should smash next hit == player did a shitty hit
                        _enemyShouldNotSmash = true;
                        var velocity = _rigidBody.velocity;
                        var velocityFlat = velocity;
                        velocityFlat.y = 0f;
                        var xzToYRatio = velocity.y / velocityFlat.magnitude;
                        _enemyShouldNotSmash = _hits <= 1 || xzToYRatio <= EnemyXzToYRatio;
                        //check if enemy should smash, compute accordingly
                        if (!_enemyShouldNotSmash) // && !IsInArea(EnemyArea.bounds, impactHenemy))
                        {
                            Vector3 smashVector;
                            float smashTime;
                            if (TryComputeBallAtHeight(EnemySmashHeight, _rigidBody.velocity, transform.position,
                                out smashVector, out smashTime, -_gravity))
                            {
                                _enemyImpactTime = smashTime;
                                _enemyImpactPoint = smashVector;
                            }
                            else
                                _enemyShouldNotSmash = true;
                        }
                        //else if no smash
                        if (_enemyShouldNotSmash)
                        {
                            if (!TryComputeBallAtHeight(EnemyHitHeight, _rigidBody.velocity, transform.position,
                                out _enemyImpactPoint, out _enemyImpactTime, -_gravity))
                                couldNotCompute = true;
                        }
                    }
                    else if (RulesApplied == Rules.Badminton)
                    {
                        _enemyShouldNotSmash = false;

                        Vector3 smashVector;
                        float smashTime;
                        var succeed = true;
                        if (!TryComputeBallAtHeight(EnemySmashHeight, _rigidBody.velocity, transform.position,
                            out smashVector, out smashTime, -_gravity))
                        {
                            succeed = TryComputeBallAtHeight(EnemyHitHeight, _rigidBody.velocity, transform.position,
                                out smashVector, out smashTime, -_gravity);
                        }
                        if (succeed)
                        {
                            _enemyImpactTime = smashTime;
                            _enemyImpactPoint = smashVector;
                        }
                        else
                        {
                            couldNotCompute = true;
                        }
                    }
                    if (couldNotCompute)
                    {
                        _enemyShouldFail = true;
                        return;
                    }
                }

                //compute duration (difficulty) of hit
                var nextEstimatedTimeForBallExchange = EstimatedMaxTimeBallExchange(3, 2);
                float nextFastStart, nextFastEnd;
                var nextFastTransitionImminent = TryGettingNextFastTransition(nextEstimatedTimeForBallExchange, out nextFastStart, out nextFastEnd);
                if (nextFastTransitionImminent)
                {
                    //there will be a fast event, so make hitlength depend on good sync time
                    //time is between 1.5-2.5 times EstimatedBallExchangeTime
                    //p->e is enemyimpacttime, e->p is enemyhitlength(?UNKNWON) on avghitheight, p->e is avgplayerhitlength, e->p is fasttransitionstart - rest
                    var estimatedNextPlayerHitLength = _playerHitLengths.Count > 3 ? GetPercentile(_playerHitLengths, 0.9f) : EstimatedPlayerHitLengthAtStart;
                    _enemyHitLength = nextFastStart - _enemyImpactTime - estimatedNextPlayerHitLength;
                    Debug.Log("prepare");
                    //regardless of whether player would hit area, enemy accepts
                    _enemyShouldFail = false;
                }
                else
                {
                    //there won't be a fast event, so make hitlength depend on difficulty
                    var avgHit = _timesOk.Count > 3 ? _timesOk.Average() : EstimatedEnemyHitLength;
                    var avgMiss = _timesNotOk.Count > 3 ? _timesNotOk.Average() : EstimatedEnemyHitLength;
                    _enemyHitLength = DesiredWinProbability * avgHit + (1f - DesiredWinProbability) * avgMiss;
                    _enemyHitLength = _enemyShouldNotSmash
                        ? Mathf.Clamp(_enemyHitLength, MinLengthForHit, MaxLengthForHit)
                        : Mathf.Clamp(_enemyHitLength, MinLengthForSmash, MaxLengthForSmash);
                }
            }
            
            //set variables
            if (timeAtZero > 0 && timeAtMin > 0f)
                _playerHitLengths.Add(timeAtMin);
        }

        private void EnemyHitsAdaptToRequirements()
        {
            if (!VirtualSpaceCore.Instance.IsRegistered())
                return;

            VirtualSpaceHandler.Tick();

            //get hit length
            var hitLength = _enemyHitLength + CognitiveProcessingTime;

            //get area in future(s)
            Vector3 centerOnHitLength, centerOnWait;
            List<Vector3> polyOnHitLength, polyOnHitLengthAndWait;
            var foundFutureOnHitLength = VirtualSpaceHandler.SpaceAtTimeWithSafety(hitLength, 0f, out polyOnHitLength, out centerOnHitLength);
            var foundFutureOnHitLengthAndWaitTime = VirtualSpaceHandler.SpaceAtTimeWithSafety(hitLength + EstimatedWaitAfterHitTime, 0f, out polyOnHitLengthAndWait, out centerOnWait);

            Debug.DrawRay(centerOnHitLength, Vector3.up, Color.yellow);
            Debug.DrawRay(centerOnWait, Vector3.up, Color.yellow);
            //get estimated y offset for hit
            var estimatedYOffset = _lastPlayerYHits.Count > 3 ? 
                _lastPlayerYHits.Max() * AdaptationOffsetFromGround + (1f - AdaptationOffsetFromGround) * EstimatedPlayerYOffsetOnHit : 
                EstimatedPlayerYOffsetOnHit;
            //compute time at which ball should hit ground (> _enemyHitLength, which is when ball still in the air and ready for player to hit)
            var yForce = GetYForceForTime(hitLength, transform.position.y - estimatedYOffset, -_gravity);
            Vector3 yEndPoint; float endTime;
            if (!TryComputeBallAtHeight(0f, yForce * Vector3.up, Vector3.up * transform.position.y, out yEndPoint, out endTime, -_gravity))
            {
                Debug.Log("couldn't compute time at impact");
                endTime = hitLength;
            }

            //get set estimated hit distance (cam to impact point on ground)
            //_plannedPointWhereBallHitsGroundAfterEnemyHit = Vector3.zero;
            var estimatedHitDistance = _lastPlayerHitDistances.Count > 3 ? 
                _lastPlayerHitDistances.Average() * AdaptationDistance2Hit + (1f-AdaptationDistance2Hit) * EstimatedDistanceToBallOnHit : 
                EstimatedDistanceToBallOnHit;
            estimatedHitDistance = Mathf.Min(estimatedHitDistance, EstimatedDistanceToBallOnHit*1.5f);

            //generate a bunch of random positions and evaluate
            var totalTries = TriesForAllField + TriesForServe + NiceToHaveSpots.Count;
            var velocities = new Vector3[totalTries];
            var values = velocities.Select(v => -100f).ToArray();
            var suggestionsImpact = velocities.Select(v => Vector3.zero).ToArray();
            var suggestionsImpactDistance = velocities.Select(v => 0f).ToArray();
            //var points = new Vector3[totalTries];
            //var valueThreshold = -100f;
            //var validIndices = new List<int>();
            //var plannedImpactPoints = new List<Vector3>();
            for (var i = 0; i < totalTries; i++)
            {
                Vector3 weWantBallToHitGroundHere;
                if (_hits == 0 && i < TriesForServe)
                {
                    weWantBallToHitGroundHere = GetSpot(false, false);
                    _ignoreMistake = false;
                }
                else if (i < TriesForServe + NiceToHaveSpots.Count && i >= TriesForServe)
                {
                    weWantBallToHitGroundHere = NiceToHaveSpots[i-TriesForServe].position;
                    _ignoreMistake = true;
                }
                else
                {
                    if (foundFutureOnHitLength)
                    {
                        //guess wildly
                        var dist = Random.Range(RandFromDist, RandToDist);
                        var angle = Random.Range(0f, 360f);
                        weWantBallToHitGroundHere = centerOnHitLength +
                                               dist * new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    }
                    else
                    {
                        weWantBallToHitGroundHere = GetSpot(false, false);
                    }
                    _ignoreMistake = true;
                }
                weWantBallToHitGroundHere.y = 0f;

                suggestionsImpact[i] = weWantBallToHitGroundHere;

                //check if positions are inside boundaries of field
                var validBounds = _hits <= 1 && !_playerServes && !_ignoreMistake ? (_servesFromRight ? PlayerRightServeArea.bounds : PlayerLeftServeArea.bounds) : PlayerArea.bounds;
                var insideField = validBounds.Contains(weWantBallToHitGroundHere);
                if (!insideField)
                {
                    Debug.DrawRay(weWantBallToHitGroundHere, Vector3.up, Color.black, 1f);
                    // Debug.Log("not in field");
                    continue;
                }

                //get estimated player position
                //get velocity
                velocities[i] = GetVelocity(weWantBallToHitGroundHere, endTime);
                //get position at _enemyHitLength
                var estimatedImpactPositionPlayerRacket = TryComputeBallAtTime(hitLength, velocities[i], transform.position, -_gravity);
                //var estimatedPlayerPositionOnHit = estimatedImpactPositionPlayerRacket;
                estimatedImpactPositionPlayerRacket.y = 0f;
                Debug.DrawRay(estimatedImpactPositionPlayerRacket, Vector3.up, Color.yellow, 1f);
                //guess position of player (will not be underneath ball at that time)
                var impact2Player = Camera.main.transform.position - estimatedImpactPositionPlayerRacket;
                impact2Player.y = 0f;
                var impMag = impact2Player.magnitude;
                suggestionsImpactDistance[i] = impMag;
                impact2Player.Normalize();
                var estimatedPlayerPositionOnHit = estimatedImpactPositionPlayerRacket + impact2Player * Mathf.Min(impMag, estimatedHitDistance);
                estimatedPlayerPositionOnHit.y = 0f;
                var cam2wewant = (Camera.main.transform.position - weWantBallToHitGroundHere);
                cam2wewant.y = 0f;
                var estimatedPlayerPositionOnHitSimple = weWantBallToHitGroundHere +
                                                         cam2wewant.normalized * estimatedHitDistance;
                estimatedPlayerPositionOnHit = AdaptationSimpleToComplex * estimatedPlayerPositionOnHit +
                                               (1f- AdaptationSimpleToComplex) * estimatedPlayerPositionOnHitSimple;

                

                //check if hit will go over net
                Vector3 tmpPos;
                float tmpTime;
                var overNet = TryComputeBallAtHeight(NetHeight.position.y, velocities[i], transform.position,
                                     out tmpPos, out tmpTime, -_gravity) && !EnemyArea.bounds.Contains(tmpPos);
                if (!overNet)
                {
                    Debug.DrawLine(weWantBallToHitGroundHere, tmpPos, Color.red, 1f);
                    continue;
                }
                Debug.DrawRay(weWantBallToHitGroundHere, Vector3.up, Color.grey, 1f);
                Debug.DrawLine(estimatedPlayerPositionOnHit + Vector3.up*0.1f, weWantBallToHitGroundHere + Vector3.up*0.1f, Color.magenta, 1f);

                //so far so good
                values[i] = 0f;

                if (foundFutureOnHitLength)
                {
                    //subtract distance to polygon
                    var distEntry = VirtualSpaceHandler.DistanceFromPoly(estimatedPlayerPositionOnHit, polyOnHitLength, false);
                    var distGround = VirtualSpaceHandler.DistanceFromPoly(weWantBallToHitGroundHere, polyOnHitLength, false);
                    var maxDist = Mathf.Clamp(Mathf.Max(distEntry, distGround), 0f, Safety);
                    values[i] -= maxDist;
                }
                if (foundFutureOnHitLengthAndWaitTime)
                {
                    //subtract distance to polygon
                    var distEntry = VirtualSpaceHandler.DistanceFromPoly(estimatedPlayerPositionOnHit, polyOnHitLengthAndWait, false);
                    var distGround = VirtualSpaceHandler.DistanceFromPoly(weWantBallToHitGroundHere, polyOnHitLengthAndWait, false);
                    var maxDist = Mathf.Clamp(Mathf.Max(distEntry, distGround), 0f, Safety);
                    values[i] -= maxDist;
                }

                //if (values[i] >= valueThreshold)
                //{
                //    if (values[i] >= valueThreshold + 0.1f)
                //    {
                //        validIndices.Clear();
                //        plannedImpactPoints.Clear();
                //    }
                //    validIndices.Add(i);
                //    plannedImpactPoints.Add(weWantBallToHitGroundHere);

                //    var maxVal = validIndices.Select(vi => values[vi]).Min();
                //    if (maxVal > valueThreshold)
                //        valueThreshold = maxVal;

                //    _plannedPointWhereBallHitsGroundAfterEnemyHit = weWantBallToHitGroundHere;
                //    maximumValueIndex = i;
                //}

                Debug.DrawRay(weWantBallToHitGroundHere, Mathf.Abs(values[i]) * Vector3.up, values[i] > 0 ? Color.yellow : Color.green, 1f);
            }

            //get max value entry within threshold (random)
            var maxVal = values.Max();
            var maximumValueIndex = -1;
            //should be far away from old one
            var maxDist2OldImpact = -1f;
            for (var i = 0; i < values.Length; i++)
            {
                if(values[i] < maxVal - 0.1f)
                    continue;
                var dist2OldImpact = Vector3.Distance(suggestionsImpact[i],
                    _plannedPointWhereBallHitsGroundAfterEnemyHit);
                if (dist2OldImpact > maxDist2OldImpact)
                    maximumValueIndex = i;
            }
            _plannedPointWhereBallHitsGroundAfterEnemyHit = suggestionsImpact[maximumValueIndex];
            _plannedImpactDistance2PlayerAfterEnemyHit = suggestionsImpactDistance[maximumValueIndex];
            if (maximumValueIndex >= 0)
            {
                _rigidBody.velocity = velocities[maximumValueIndex];
                _impactPoints.Add(_plannedPointWhereBallHitsGroundAfterEnemyHit);
            }
            else if (foundFutureOnHitLength)
            {
                //Debug.Log(centerOnHitLength + "   " + _enemyHitLength);
                var velo = GetVelocity(centerOnHitLength, hitLength);
                _rigidBody.velocity = Mathf.Abs(velo.magnitude) > 1000 ? Vector3.up : velo;
            }
            else
            {
                Debug.Log("should not happen");
                _rigidBody.velocity = Vector3.up;
            }

            //for (var p = 0; p < points.Length; p++)
            //{
            //    Debug.DrawLine(transform.position, points[p], Color.Lerp(Color.blue, Color.red, values[p]/10), 2f);
            //}
        }

        #endregion

        #region misc

        private float GetPercentile(List<float> list, float percentile)
        {
            var copy = new List<float>(list);
            copy.Sort();
            var floatIndex = copy.Count * percentile;
            var lower = Mathf.FloorToInt(floatIndex);
            var upper = Mathf.FloorToInt(floatIndex);
            if (lower < 0)
                return copy[upper];
            if (upper >= copy.Count)
                return copy[lower];
            floatIndex -= lower;
            return copy[lower] * (1f - floatIndex) + copy[upper] * floatIndex;

        }

        private bool IsInArea(Bounds bounds, Vector3 position)
        {
            var inField = position.x > bounds.center.x - bounds.extents.x &&
                          position.x < bounds.center.x + bounds.extents.x &&
                          position.z > bounds.center.z - bounds.extents.z &&
                          position.z < bounds.center.z + bounds.extents.z;
            return inField;
        }

        private void SetFrontTrail(bool enable)
        {
            if (enable)
            {
                FrontTrail.gameObject.SetActive(true);
                FrontTrailPoints[0].position = transform.position;
                Vector3 position;
                float t;
                var maxHeight = Mathf.Pow(_rigidBody.velocity.y, 2) / (-_gravity - _gravity) + transform.position.y -
                                0.001f;
                if (!TryComputeBallAtHeight(maxHeight, _rigidBody.velocity, transform.position, out position, out t,
                    -_gravity))
                {
                    FrontTrail.gameObject.SetActive(false);
                    return;
                }
                FrontTrailPoints[1].position = position;
                if (!TryComputeBallAtHeight(0f, _rigidBody.velocity, transform.position, out position, out t,
                    -_gravity))
                {
                    FrontTrail.gameObject.SetActive(false);
                    return;
                }
                FrontTrailPoints[2].position = position;
                FrontTrail.Reset();
            }
            else
            {
                FrontTrail.gameObject.SetActive(false);
            }
        }
    
        private Vector3 GetVelocity(Vector3 target, float time)
        {
            var currentHeight = transform.position.y;

            var direction = target - transform.position;
            direction.y = 0f;
            var distance = direction.magnitude;
            direction.Normalize();

            var velocity = direction * distance / time;
            velocity.y = -_gravity * time / 2f - (currentHeight - target.y) / time;

            return velocity;
        }

        private Vector3 GetSpot(bool optimalElseRandom, bool enemyField)
        {
            Vector3 spot;
            if (optimalElseRandom)
            {
                spot = enemyField ? EnemyArea.bounds.center : PlayerArea.bounds.center;
                if (_hits == 0)
                {
                    if (enemyField)
                        spot = _servesFromRight
                            ? EnemyRightServeAreaPlay.bounds.center
                            : EnemyLeftServeAreaPlay.bounds.center;
                    else
                        spot = _servesFromRight
                            ? PlayerRightServeAreaPlay.bounds.center
                            : PlayerLeftServeAreaPlay.bounds.center;
                }
                spot.y = 0f;
            }
            else
            {
                _ignoreMistake = false;
                Bounds box;
                if (_hits == 0)
                {
                    if (enemyField)
                    {
                        box = _servesFromRight ? EnemyRightServeAreaPlay.bounds : EnemyLeftServeAreaPlay.bounds;
                    }
                    else
                    {
                        _ignoreMistake = PlayerRightFieldSide.bounds.Contains(CamRig.transform.position) !=
                                         _servesFromRight;
                        box = PlayerRightFieldSide.bounds.Contains(CamRig.transform.position)
                            ? PlayerRightServeAreaPlay.bounds
                            : PlayerLeftServeAreaPlay.bounds;
                    }
                }
                else
                {
                    box = enemyField ? EnemyArea.bounds : PlayerArea.bounds;
                }
                spot = new Vector3(Random.Range(-box.extents.x, box.extents.x), 0f, Random.Range(-box.extents.z, box.extents.z));
                spot += box.center;
            }

            return spot;
        }

        private static bool TryComputeBallAtHeight(float height, Vector3 force, Vector3 position, out Vector3 end, out float time, float g)
        {
            //get t
            //var g = 9.81f;
            var vDivG = force.y / g;
            var sqrtTerm = vDivG * vDivG + 2 * (position.y - height) / g;
            if (sqrtTerm < 0f)
            {
                time = 0f;
                end = Vector3.zero;
                return false;
            }
            time = Mathf.Sqrt(sqrtTerm) + vDivG;

            var direction = new Vector3(force.x, 0f, force.z);

            end = (new Vector3(position.x, height, position.z)) + direction * time;//TODO air drag?

            return true;
        }

        private float GetYForceForTime(float time, float heightDifference, float g)
        {
            return -heightDifference / time + g*time / 2;
        }

        private static Vector3 TryComputeBallAtTime(float time, Vector3 velocity, Vector3 start, float g)
        {
            var end = velocity * time + start; //TODO air drag?
            end += -0.5f * g * time * time * Vector3.up;
            return end;
        }

        private void ChangeGravity(bool isPlayer)
        {
            _gravity = isPlayer ? GravityPlayer : GravityEnemy;
            Physics.gravity = new Vector3(0, _gravity, 0);
        }

        private void UpdateUi()
        {
            var p = "";
            var e = "";
            if (RulesApplied == Rules.Tennis)
            {
                switch (_scorePlayer)
                {
                    case 0:
                        p = "0";
                        break;
                    case 1:
                        p = "15";
                        break;
                    case 2:
                        p = "30";
                        break;
                    case 3:
                        p = "40";
                        break;
                    default:
                        p = "-";
                        break;
                }
                switch (_scoreEnemy)
                {
                    case 0:
                        e = "0";
                        break;
                    case 1:
                        e = "15";
                        break;
                    case 2:
                        e = "30";
                        break;
                    case 3:
                        e = "40";
                        break;
                    default:
                        e = "-";
                        break;
                }
            }
            else if (RulesApplied == Rules.Badminton)
            {
                p = _scorePlayer.ToString();
                e = _scoreEnemy.ToString();
            }
            if (RulesApplied == Rules.Tennis)
            {
                if (_scoreEnemy >= 4 && _scoreEnemy > _scorePlayer)
                {
                    e = "ADV";
                    p = "-";
                }
                else if (_scorePlayer >= 4 && _scorePlayer > _scoreEnemy)
                {
                    p = "ADV";
                    e = "-";
                }
                else if (_scoreEnemy == 3 && _scorePlayer == 3)
                {
                    p = "-";
                    e = "-";
                }
            }

            TextPlayerGames.text = _gamesWon.ToString();
            TextEnemyGames.text = _gamesLost.ToString();
            TextPlayerPoint.text = p;
            TextEnemyPoint.text = e;
            TextPlayerServe.text = _playerServes ? "X" : "";
            TextEnemyServe.text = !_playerServes ? "X" : "";
            //Text.text = (_playerServes ? "X" : " ") + "   " + p + " : " + e + "   " + (!_playerServes ? "X" : " ") + Environment.NewLine + _gamesWon + " : " + _gamesLost;
        }

        #endregion

        #region trigger
        private void OnTriggerExit(Collider other)
        {
            if (other != ValidArea)
                return;

            if (_timesTouchedGround > 0)
            {
                BallOut(!_hitGroundPlayer, false);
            }
            else
                BallOut(!_playerTouchedLast, _hits == 1);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider == Floor)
            {
                TennisSounds.PlayBounce(_rigidBody.velocity.magnitude);
                ++_timesTouchedGround;
                if (RulesApplied == Rules.Tennis)
                {
                    if (_hits == 1)
                    {
                        if (_timesTouchedGround == 1)
                        {
                            var box = _playerServes ?
                                (_servesFromRight ? EnemyRightServeArea : EnemyLeftServeArea) :
                                (_servesFromRight ? PlayerRightServeArea : PlayerLeftServeArea);
                            var ignore = _ignoreMistake || (_playerServes && PlayerCanHitAllFieldOnServe);
                            //var otherBox = _playerServes ?
                            //    (!_servesFromRight ? EnemyRightServeArea : EnemyLeftServeArea) :
                            //    (!_servesFromRight ? PlayerRightServeArea : PlayerLeftServeArea);
                            if (!ignore && !box.bounds.Contains(transform.position))
                            {
                                //player / enemy hit into wrong spot
                                BallOut(!_playerServes, true);

                            }
                            else
                            {
                                _hitGroundPlayer = !_playerServes;
                            }
                        }
                        else
                        {
                            //ball is out (hit floor twice)
                            BallOut(_playerServes, false);
                        }
                    }
                    else
                    {
                        if (PlayerArea.bounds.Contains(transform.position))
                        {
                            _hitGroundPlayer = true;
                            if (_timesTouchedGround == 2 || _playerTouchedLast)
                            {
                                BallOut(false, false);
                            }
                        }
                        else if (EnemyArea.bounds.Contains(transform.position))
                        {
                            _hitGroundPlayer = false;
                            if (_timesTouchedGround == 2 || !_playerTouchedLast)
                            {
                                BallOut(true, false);
                            }
                        }
                        else
                        {
                            if (_timesTouchedGround > 1)
                            {
                                BallOut(!_hitGroundPlayer, false);
                            }
                            else
                            {
                                BallOut(!_playerTouchedLast, false);
                            }
                        }
                    }
                }
                else if (RulesApplied == Rules.Badminton)
                {
                    var box = _playerTouchedLast ? EnemyArea : PlayerArea;
                    if (_hits == 1)
                    {
                        var ignore = _ignoreMistake || (_playerServes && PlayerCanHitAllFieldOnServe);
                        if (!ignore)
                        {
                            box = _playerServes ?
                                (_servesFromRight ? EnemyRightServeArea : EnemyLeftServeArea) :
                                (_servesFromRight ? PlayerRightServeArea : PlayerLeftServeArea);
                        }
                    }
                    //var otherBox = _playerServes ?
                    //    (!_servesFromRight ? EnemyRightServeArea : EnemyLeftServeArea) :
                    //    (!_servesFromRight ? PlayerRightServeArea : PlayerLeftServeArea);
                    if (!box.bounds.Contains(transform.position))
                    {
                        //player / enemy hit into wrong spot
                        BallOut(!_playerTouchedLast, false);
                    }
                    else
                    {
                        BallOut(_playerTouchedLast, false);
                    }
                }

            }
            //else if (PlayerRacket.Contains(collision.collider))
            //{
            //    HitBall(true);
            //}
            else if (collision.collider == Net)
            { 
                TennisSounds.PlayNet(_rigidBody.velocity.magnitude);
                if (_hits <= 1)
                {
                    BallOut(!_playerServes, RulesApplied == Rules.Tennis);
                }
                //_hitNet = true;
            }
            else
            {
                TennisSounds.PlayBounce(_rigidBody.velocity.magnitude);
                if (RulesApplied == Rules.Tennis)
                {
                    if (_timesTouchedGround == 0)
                    {
                        BallOut(!_playerTouchedLast, _hits == 1);
                    }
                    else
                    {
                        BallOut(!_hitGroundPlayer, _hits == 1);
                    }
                }
                else if (RulesApplied == Rules.Badminton)
                {
                    BallOut(!_playerTouchedLast, false);
                }

                //  Debug.Log("unknown collider " + collision.collider.gameObject.name);
            }
        }
        #endregion

        #region hit ball functions (where velocity is applied)

        private void EnemyHitBall()
        {
            //change gravity
            ChangeGravity(false);

            //change velocity
            RandomChangeVelocity(false);

            //hit the ball (effects, variables and such)
            HitBall(false);
        }

        public void PlayerHitBall(Vector3 velocity, float adaptation)
        {
            //change gravity
            ChangeGravity(true);

            //compute velocity which should be applied
            Vector3 target;
            float time;
            if (TryComputeBallAtHeight(0f, velocity, transform.position, out target, out time, -_gravity))
            {
                var optimalTarget = GetSpot(true, true);
                var adaptedTarget = Vector3.Lerp(target, optimalTarget, adaptation);
                var adaptedTime = time;// _hits == 0 ? 0.8f * adaptation + (1f-adaptation) * 1.3f : 0.9f * adaptation + (1f - adaptation) * 1.6f;//Mathf.Clamp(time * (1 - adaptation) + adaptation * 1.2f, 0.9f, 2f);
                var adaptedVelocity = GetVelocity(adaptedTarget, adaptedTime);
                _rigidBody.velocity = adaptedVelocity;
            }
            else 
                _rigidBody.velocity = velocity;

            //hit the ball (effects, variables and such)
            HitBall(true);
        }

        private void PlayerHitBall()
        {
            //change gravity
            ChangeGravity(true);

            //change velocity
            RandomChangeVelocity(true);

            //hit the ball (effects, variables and such)
            HitBall(true);
        }

        private void RandomChangeVelocity(bool isPlayer)
        {
            //find good velocity which should be applied
            var optimalTarget = GetSpot(false, isPlayer);
            var randomTime = Random.Range(0, 2) == 0
                ? Random.Range(MinLengthForSmash, MaxLengthForSmash)
                : Random.Range(MinLengthForHit, MaxLengthForHit);
            var velocity = GetVelocity(optimalTarget, randomTime);
            _rigidBody.velocity = velocity;
            if (RulesApplied == Rules.Tennis)
            {
                _rigidBody.angularVelocity = new Vector3(Random.Range(0f, 100f), Random.Range(0f, 100f), Random.Range(0f, 100f));
            }
        }

        private void BallOut(bool playersPoint, bool serveMistake)
        {
            //ignore on ballout force overwrite
            if (_forceBallout > 0f || _timeToResetBall > 0f)
                return;
           // Debug.Log("ball out " + Time.time);

            BallIndicator.enableEmission = false;
            SetFrontTrail(false);

            //if (_resetBall)
            //    return;

            _timeToResetBall = BallResetTime;
            //_resetBall = true;

            //check if serve error
            if (_hits == 1 && !_serveMistakeMade && serveMistake)
            {
                _serveMistakeMade = true;
                TennisSounds.PlayMistake();
                return;
            }

            _serveMistakeMade = false;

            if (!playersPoint && !(_playerServes && _hits == 1))
                _timesNotOk.Add(_timeWhichMightBeOk);

            if (playersPoint)
                _overallPlayerPoints++;
            else
                _overallEnemyPoints++;

            TennisSounds.PlayPoint(playersPoint);

            _timesTouchedGround = 0;
            _servesFromRight = !_servesFromRight;
            //_hitNet = false; //do something with this before?

            if (playersPoint)
                _scorePlayer++;
            else
                _scoreEnemy++;


            if (RulesApplied == Rules.Tennis)
            {
                if (_scoreEnemy > _scorePlayer && _scoreEnemy >= 4)
                {
                    if (_scoreEnemy > _scorePlayer + 1)
                    {
                        _gamesLost++;
                        _scorePlayer = 0;
                        _scoreEnemy = 0;
                        _playerServes = !_playerServes;
                    }
                }
                else if (_scoreEnemy < _scorePlayer && _scorePlayer >= 4)
                {
                    if (_scoreEnemy + 1 < _scorePlayer)
                    {
                        _gamesWon++;
                        _scorePlayer = 0;
                        _scoreEnemy = 0;
                        _playerServes = !_playerServes;
                    }
                }
            }
            else if (RulesApplied == Rules.Badminton)
            {
                if (playersPoint != _playerServes)
                    _playerServes = !_playerServes;

                var enemyWon = (_scoreEnemy >= 21 && _scoreEnemy > _scorePlayer + 1) || _scoreEnemy == 30;
                var playerWon = (_scorePlayer >= 21 && _scorePlayer > _scoreEnemy + 1) || _scorePlayer == 30;
                if (enemyWon)
                {
                    _gamesLost++;
                    _scorePlayer = 0;
                    _scoreEnemy = 0;
                    _playerServes = true;
                }
                else if (playerWon)
                {
                    _gamesLost++;
                    _scorePlayer = 0;
                    _scoreEnemy = 0;
                    _playerServes = false;
                }
            }

            UpdateUi();
        }

        private float EstimatedMaxTimeBallExchange(int playerCount, int enemyCount)
        {
            var estimatedPlayerHitLength = _playerHitLengths.Count > 3 ? GetPercentile(_playerHitLengths, PercentileForMaxHitLengthComputation) : EstimatedPlayerHitLengthAtStart;
            //var hitsAndMissed = new List<float>();
            //hitsAndMissed.AddRange(_timesOk);
            //hitsAndMissed.AddRange(_timesNotOk);
            var estimatedEnemyHitLength = EstimatedEnemyHitLength;//hitsAndMissed.Count > 3 ? GetPercentile(hitsAndMissed, minToMax) : EnemyStartHitTime;
          //  Debug.Log("estimate : " + estimatedPlayerHitLength + " " + estimatedEnemyHitLength);
            return playerCount * estimatedPlayerHitLength + enemyCount * estimatedEnemyHitLength;
        }

        private void ResetBall()
        {
            //Debug.Log("try reset ball");
            //if (_forceBallout > 0f)
            //    return;
            transform.forward = Vector3.down;
            _resetAtTime = Time.time;

         //   Debug.Log("reset ball " + Time.time);
            BallIndicator.enableEmission = false;
            SetFrontTrail(false);

            Trail.enabled = false;
            Trail.Clear();
            Particles.enableEmission = true;
            //_resetBall = false;
            var enemyPosition = _servesFromRight ? EnemyRight.position : EnemyLeft.position;
            enemyPosition.y = 0f;
            Enemy.BackToLine(enemyPosition);
            _hits = 0;
            Vector3 resetPosition;
            float start, end;
            var fastImminent = TryGettingNextFastTransition(
                EstimatedMaxTimeBallExchange(1, 1), out start, out end);
            _playerServes = _playerServes && !fastImminent;
            if (_playerServes)
            {
                if (PlayerCanServeFromAnywhere)
                    resetPosition = HelperSpot.GetSpot(_resetPositionAreaTimeOffset, true);
                else
                {
                    resetPosition = _servesFromRight ? PlayerRight.position : PlayerLeft.position;
                }
            }
            else
            {
                resetPosition = _servesFromRight ? EnemyRight.position : EnemyLeft.position;
                
                if (fastImminent)
                {
                    _enemyHitLength = end - start;
                    _enemyImpactTime = start - CognitiveProcessingTime;
                }
                else
                {
                    _enemyHitLength = EstimatedEnemyHitLength;
                    _enemyImpactTime = NormalServeCountdown;
                }
            }
            _rigidBody.useGravity = false;
            _rigidBody.velocity = Vector3.zero;
            _rigidBody.angularVelocity = Vector3.zero;
            transform.position = resetPosition;
            //if (!_playerServes)
            //{
            //    _serveCountdown = _enemyServeCountdown;
            //    //    //float start, end;
            //    //if (TryGettingNextFastTransition(EstimatedMaxTimeBallExchange, out start, out end) && end-start > 0f)
            //    //{
            //    //    //change countdown in accordance with backend data
            //    //    _serveCountdown = start;
            //    //    _enemyHitLength = end - start;
            //    //    Debug.Log("quick serve   " + start + " " + _enemyHitLength);
            //    //}
            //}
        }

        private void CreateTimeConditions(StateInfo state, out List<TimeCondition> timeConditions, out Value valueFunction,
            out List<double> Execs, out List<double> Preps)
        {
            var now = VirtualSpaceTime.CurrentTimeInSeconds;
            var earliestNextTime = state.ToSeconds;

            timeConditions = new List<TimeCondition>();
            
            // important because we calculate dynamic tolerance based on the offset from this value
            float timeUntilNextExecutionMin;
            if (!_playerTouchedLast)
            {
                float tmpTime;
                Vector3 tmpPos;
                if (!TryComputeBallAtHeight(EstimatedPlayerYOffsetOnHit, _rigidBody.velocity, transform.position,
                    out tmpPos, out tmpTime, -_gravity))
                {
                    TryComputeBallAtHeight(0f, _rigidBody.velocity, transform.position,
                        out tmpPos, out tmpTime, -_gravity);
                }
                timeUntilNextExecutionMin = tmpTime + EstimatedMaxTimeBallExchange(1, 0);
            }
            else
            {
                timeUntilNextExecutionMin = _enemyImpactTime;
            }
            var ballExchangeTime = EstimatedMaxTimeBallExchange(1, 1);

            var proxyPrep = new Variable(VariableTypes.Continuous, "proxyPrep");
            var intervalNum = new Variable(VariableTypes.Integer, "intervalNum");
            var prepSeconds = new Variable(VariableTypes.PreperationTime);
            var execSeconds = new Variable(VariableTypes.ExecutionTime);
            var actualTolerance = new Variable(VariableTypes.Continuous, "actualTolerance");
            var futureOffset = new Variable(VariableTypes.Continuous, "futureOffset");
            
            // todo
            var enemyHitTolerance = .05f; // this should be min,max enemy hit time
            // todo
            var ballExchangeTolerance = .5f; // how much the player hits can be influenced

            timeConditions.Add(intervalNum >= 0);
            timeConditions.Add(futureOffset == intervalNum * ballExchangeTime);
            timeConditions.Add(proxyPrep == now + timeUntilNextExecutionMin + futureOffset);
            timeConditions.Add(0 <= futureOffset);
            timeConditions.Add(futureOffset <= 50);
            timeConditions.Add(prepSeconds >= proxyPrep - actualTolerance);
            timeConditions.Add(prepSeconds <= proxyPrep + actualTolerance);
            timeConditions.Add(0 <= actualTolerance);
            timeConditions.Add(actualTolerance <= (intervalNum + 1) * ballExchangeTolerance + enemyHitTolerance);

            timeConditions.Add(execSeconds >= 1.5f);
            timeConditions.Add(execSeconds <= 5f);

            Execs = new List<double> { 2f, 3f, 4f };
            var nextHitTime = now + timeUntilNextExecutionMin; // it is not if queue is longer than 1
            Preps = new List<double> { nextHitTime, nextHitTime + ballExchangeTime, nextHitTime + 2 * ballExchangeTime };

            //valueFunction = futureOffset + actualTolerance;
            valueFunction = 0;
        }

        public List<VSUserTransition> DebugTransitions = new List<VSUserTransition>();
        private void EvaluateStates(StateInfo info)
        {
            if (!VirtualSpaceCore.Instance.IsRegistered() || info == null)
                return;

            //Logger.Debug($"Receive at turn: {VirtualSpaceTime.CurrentTurn}, Millis: {VirtualSpaceTime.CurrentTimeInMillis}");
            //Vector startPosition;
            //Polygon startPolygon;
            //_map.GetPlayerStatus(info.SystemRotationState, info.SystemPlayerInFocusState, info.SystemPlayerNum,
            //    out startPosition, out startPolygon);
            //if (info.SystemPlayerNum == 0)
            //{
            //    Logger.Debug($"=============");
            //    Logger.Debug($"{info.SystemPlayerNum} at {startPosition}");
            //}
            //var rand = new System.Random((int)DateTime.Now.Ticks);

            while(_impactPoints.Count > 100)
                _impactPoints.RemoveAt(0);

            TransitionVoting voting = new TransitionVoting {StateId = info.StateId};
            //info.YourCurrentState == 
            var areaUnits = new List<double>();
            var overlapsWithBase = new List<double>();
            var newAreaDeltas = new List<double>();
            var impactPoints = new List<int>();
            List<List<Vector3>> stateAreasAsList;
            List<Polygon> couldBeAreas;
            List<Vector3> stateCenters;
            List<Vector3> startAreaAsList;
            Polygon beforeArea;
            Vector3 startCenter;
            VirtualSpaceHandler.DeserializeState(info, out stateAreasAsList, out couldBeAreas, out stateCenters, 
                out startCenter, out beforeArea, out startAreaAsList);
            var startOverlap = ClipperUtility.GetArea(ClipperUtility.Intersection(VirtualSpaceHandler.MustAreaTranslated, beforeArea));
            var thisAreaAtStartOfTransitionList = beforeArea.Points;

            for (var i = 0; i < couldBeAreas.Count; i++)
            {
                //Debug.Log(info.PossibleTransitions[i] + " size: " + couldBeAreas[i].Dimension);
                if (DebugTransitions.Contains(info.PossibleTransitions[i]))
                    VirtualSpaceHandler.DrawPolygonLines(
                        couldBeAreas[i].Points.Select(point => point.ToVector3()).ToList(), Color.red, 2f);
            }

            for (var i = 0; i < info.PossibleTransitions.Count; i++)// VSUserTransition transition in info.PossibleTransitions)}
            {
                var area = stateAreasAsList[i];
                var couldBeArea = couldBeAreas[i];
                var couldBeAreaDimension = ClipperUtility.GetArea(couldBeArea);
                var numPointsInEndPolygon = _impactPoints.Count(ip => VirtualSpaceHandler.PointInPolygon(ip, area, 0.5f));
                var overlapWithBaseline = ClipperUtility.GetArea(ClipperUtility.Intersection(VirtualSpaceHandler.MustAreaTranslated, couldBeArea));
                var percentageCompletelyNewArea = 
                    ClipperUtility.GetArea(ClipperUtility.Difference(couldBeArea, beforeArea)) / couldBeAreaDimension;

                areaUnits.Add(couldBeAreaDimension);
                overlapsWithBase.Add(overlapWithBaseline);
                newAreaDeltas.Add(percentageCompletelyNewArea);
                impactPoints.Add(numPointsInEndPolygon);
                //if (info.PossibleTransitions[i] != VSUserTransition.Stay) continue;
                //stayOverlap = overlapWithBaseline;
            }
            
            var valuation = new Dictionary<int, float>();
            for (var i = 0; i < info.PossibleTransitions.Count; i++)
            {
                valuation.Add(i, 0);
            }

            //states with low overlap to current area are important
            var newAreasMax = newAreaDeltas.Max();
            var newAreasMin = newAreaDeltas.Min();
            var newAreaDelta = newAreasMax - newAreasMin;
            if (newAreasMax - newAreasMin > 0)
            {
                var weight = 0.25f;//25% importance
                for (var i = 0; i < info.PossibleTransitions.Count; i++)
                {
                    var val = (float)((newAreaDeltas[i] - newAreasMin) / newAreaDelta);

                    Debug.Log(info.PossibleTransitions[i] + " overlap with previous: " + val);
                    
                    valuation[i] += (1 - val) * weight;
                }
            }

            //states with overlap to base are important
            var overlapsDelta = overlapsWithBase.Select(o => Mathf.Max(0f, (float) (o - startOverlap))).ToList();
            var maxOverlapDelta = overlapsDelta.Max();
            var minOverlapDelta = overlapsDelta.Min();
            var overlapDelta = maxOverlapDelta - minOverlapDelta;
            if (maxOverlapDelta - minOverlapDelta > 0f)
            {
                var relOverlaps = overlapsDelta.Select(o => (o - minOverlapDelta) / overlapDelta).ToList();
                var weight = .3f;//30% importance
                for (var i = 0; i < info.PossibleTransitions.Count; i++)
                {
                    //if (overlaps[i] > startOverlap)
                    var val = relOverlaps[i];

                    Debug.Log(info.PossibleTransitions[i] + " overlap with baseline: " + val);
                    
                    //Debug.Log(val);
                    valuation[i] += val * weight;
                }
            }
            
            //bigger areas are important
            var areaUnitsMax = areaUnits.Max();
            var areaUnitsMin = areaUnits.Min();
            //var biggerThanMax = areaUnits.Select(au => au >= areaUnitsMax * 0.95d).ToList();
            //if (biggerThanMax.Count(bm => true) == 1) // areaUnits.IndexOf(areaUnitsMax)
            for (var i = 0; i < info.PossibleTransitions.Count; i++)
            {
                var val = (float) ((areaUnits[i] - areaUnitsMin) / (areaUnitsMax - areaUnitsMin)) * 0.15f;//15% importance
                //Debug.Log(val);
                valuation[i] += val; 
            }
            
            //little impact density is important
            var densities = new List<double>();
            for (var i = 0; i < info.PossibleTransitions.Count; i++)
            {
                var density = impactPoints[i] / areaUnits[i];
                if (info.PossibleTransitions[i] == VSUserTransition.Defocus)
                    density = double.MaxValue;
                densities.Add(density);
            }
            var densityMin = densities.Min();
            for (var i = 0; i < info.PossibleTransitions.Count; i++)
            {
                var val = Math.Abs(densities[i]) < 0.0001f ? 0f : (float) (densityMin / densities[i]) * 0.3f;//30% importance
                //Debug.Log(val);
                valuation[i] += val; 
            }
            //favoredIndices.Add(densities.IndexOf(densityMin));

            //normalize
            var valueMax = valuation.Values.Max();
            var weights = valuation.Select(v => 0).ToArray();
            var maxWeight = 100;
            for (var i = 0; i < valuation.Count; i++)
            {
                weights[i] = valueMax > 0f ? (int) (maxWeight * valuation[i] / valueMax) : 0;
                if (info.PossibleTransitions[i] == VSUserTransition.Defocus)
                    weights[i] /= 2;
            }

            //valuate
            var timeBackAndForth = EstimatedMaxTimeBallExchange(2, 2);
            for (var i = 0; i < info.PossibleTransitions.Count; i++)
            {
                var transition = info.PossibleTransitions[i];
                var vote = new TransitionVote
                {
                    Transition = transition,
                    Value = weights[i]
                };

                var isBiggerClipper = ClipperUtility.ContainsWithinEpsilon(couldBeAreas[i], beforeArea);
                if (transition == VSUserTransition.Stay)
                {
                    foreach (var sa in startAreaAsList)
                    {
                        Debug.DrawRay(sa + Vector3.up * 1f, Vector3.up * 1f, Color.white, 1f);

                    }
                    foreach (var sa in stateAreasAsList[i])
                    {
                        Debug.DrawRay(sa + Vector3.up * 1f, Vector3.up * 1f, Color.black, 1f);
                    }
                }
                
                List<TimeCondition> conditions;
                Value value;
                CreateTimeConditions(info, out conditions, out value, out vote.ExecutionLengthMs, out vote.PlanningTimestampMs);
                vote.TimeConditions = conditions;
                vote.ValueFunction = value;
                //Debug.Log("badminton: " + transition + " plan " + vote.PlanningTimestampMs[0] + " / exec " + vote.ExecutionLengthMs[0]);

                voting.Votes.Add(vote);
            }
            //foreach (var vote in voting.Votes)
            //{
            //    Debug.Log("vote " + vote.Transition + " " + vote.Value);
            //}

            VirtualSpaceCore.Instance.SendReliable(voting);
        }

        private void HitBall(bool isPlayer)
        {
            //change gravity
            _rigidBody.useGravity = true;

            //change velocity if backend is connected
            if (isPlayer)
            {
                //compute enemies future position etc.
                PlayerHitsAdaptToRequirements();
            }
            else
            {
                //compute enemy hit based on difficulty, virtualspace etc.
                EnemyHitsAdaptToRequirements();
            }

            EvaluateStates(VirtualSpaceHandler.State);


            //set hint
            Hint.gameObject.SetActive(isPlayer);

            //play sound
            TennisSounds.PlayHit(_rigidBody.velocity.magnitude, _hits == 0);

            //compute impact point
            float tmpTime;
            Vector3 tmpPos;
            TryComputeBallAtHeight(0f, _rigidBody.velocity, transform.position, out tmpPos, out tmpTime, -_gravity);

            //visual effects
            var go = Instantiate(HitIndicator);
            go.transform.position = transform.position;
            if (_hits == 0)
            {
                Trail.enabled = true;
                Particles.enableEmission = false;
            }
            if (!isPlayer)
            {
                //enemy avatar swings bat
                Enemy.Hit();
                //enable front trail
                SetFrontTrail(true);
                //emission
                BallIndicator.enableEmission = true;
                var biPos = tmpPos;
                biPos.y = BallIndicatorOffset;
                BallIndicator.transform.position = biPos;
                
                //save into heights
                _lastPlayerYHits.Insert(0, transform.position.y);
                if (_lastPlayerYHits.Count > 5)
                    _lastPlayerYHits.RemoveAt(5);

                //save into impact distances
                if (_plannedPointWhereBallHitsGroundAfterEnemyHit != Vector3.zero)
                {
                    var flatCam = Camera.main.transform.position;
                    var flatBall = transform.position;
                    flatBall.y = 0f;
                    flatCam.y = 0f;
                    _lastPlayerHitDistances.Insert(0, Vector3.Distance(flatCam, flatBall));
                    if (_lastPlayerHitDistances.Count > 5)
                        _lastPlayerHitDistances.RemoveAt(5);
                }
            }
            else
            {
                //make enemy run to estimated position
                if (!_enemyShouldFail)
                    Enemy.SetTarget(_enemyImpactPoint, _enemyImpactTime, !_enemyShouldFail);// !_enemyShouldFail || _estimatedWillFailOnHit);
                //disable front trail
                BallIndicator.enableEmission = false;
                SetFrontTrail(false);
            }
            
            //change timescale variable
            if (isPlayer)
                _timeScaleWarper = 0f;

            //apply rules (might return here)
            if (RulesApplied == Rules.Tennis && _timesTouchedGround == 0 && _hits == 1)
            {
                if (isPlayer && !_playerServes && !PlayerCanDirectlyAccept)
                {
                    BallOut(false, false);
                    return;
                }
                else if (!isPlayer && _playerServes)
                {
                    BallOut(true, false);
                    return;
                }
            }
            if (_hits > 0 && isPlayer == _playerTouchedLast)
            {
                BallOut(!isPlayer, _hits == 1 && RulesApplied == Rules.Tennis);
                return;
            }

            //change helper variables
            _hits++;
            _timesTouchedGround = 0;
            _playerTouchedLast = isPlayer;

            //change handicap variables
            if (isPlayer)
            {
                _timesOk.Add(_timeWhichMightBeOk);
            }
            else
            {
                _timeWhichMightBeOk = tmpTime;
            }
        }

        #endregion
    }
}

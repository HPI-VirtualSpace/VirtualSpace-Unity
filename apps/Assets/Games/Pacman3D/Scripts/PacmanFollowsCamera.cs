using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Assets.Scripts.VirtualSpace.Unity_Specific;
using UnityEngine;
using VirtualSpace;
using VirtualSpace.Shared;
using VirtualSpaceVisuals;
using Random = UnityEngine.Random;

namespace Games.Pacman3D.Scripts
{
    public class PacmanFollowsCamera : MonoBehaviour
    {
        [Header("backend related")]
        //public int NeighborDistanceMaxOnTileChange = 2;
        public float IgnoreUnavailableAbove = 2f;
        public float TimeFrameWhereHoppingIsAllowed = 2f;
        //public float EvalUpdate = 0.1f;
        public int WalkedList = 18;

        [Header("study related")]
        public VirtualSpaceMenuFollow Menu;

        [Header("game related")]
        public GameObject PointViz;
        public float CognitiveProcessingTime = 0.3f;
        public PacmanWallsDown WallsDown;
        public Vector3 TileSize;
        public Transform Camera;
        public GameObject Tiles;
        public Transform RayTest;
        public float Speed = 5f;
        public CanvasStuff Ui;
        public float WallErrorInterval = 1f;
        public int WallDecrement = -5;
        public int CherryIncrement = 10;
        public GameObject Walls;
        public AudioClip[] EatSounds;
        public AudioClip SpecialSound;
        public AudioClip GhostEatenSound;
        public AudioSource EatSource;
        public AudioSource WallSource;
        public AudioSource DieSource;
        public AudioSource WonSource;
        public AudioSource StartSource;
        //public Animator[] Animators;
        public GameObject Food;
        // [HideInInspector] public UnityEvent MadeSpecial;
        // [HideInInspector] public UnityEvent MadeNormal;
        public List<PacmanGhost> Ghosts;
        public float RayTestY = 0.06f;
        public float GhostTime = 10f;
        public int GameStartCountdown = 3;
        public int GhostEatenIncrement = 10;
        public VirtualSpaceHandler VirtualSpaceHandler;
        public Transform WholeArea;
        public GameObject Control;
        
        private List<PacmanCherry> _cherries;
       // private Dictionary<int, List<YellowDelicious>> _tilesToFood;
        private List<Vector3> _wholeArea;
        private PacmanDrug _drug;
        private float _timeBeforeStart;
        private int _eatSoundIndex;
        private List<AnotherBrick> _walls;
        private bool _wallsAreRed;
        private float _wallHitLast;
        private int _currentTileIndex;
        private int _lastTileIndex;
        private List<PacTile> _tiles;
        private List<int> _tileIndices;
        private List<Transform> _tileTransforms;
        private List<List<int>> _neighborIndices;
        private List<Collider> _collider;
        private bool _gameOver;
        //private bool _newEvent;
        //private Vector3 _newEventOrigin;
        //private int _newEventIndex;
        //private float _newEventTime;
        private int _foodCount;
        private int _foodEaten;
        private int _points;
        private bool _won;
        private bool _alreadyInit;
        private bool _alreadyPunishedForOutside;
        private bool _figureOutGhosts;
        private float _distanceNeighbors;
        //private float _recheckGhostsCountdown;
        private bool _controlGroupChecked;
        private List<int> _lastTilesWalked;
        private bool _playerOkay;
        private List<int> _lastAvailableTiles;

        private void Awake () {
            _won = true;
            _lastAvailableTiles = new List<int>();
            _postimes = new List<PosTime>();
            _playerOkay = true;
            Ghosts = Ghosts.Where(g => g.gameObject.activeSelf).ToList();
            _lastTilesWalked = new List<int>();
            _drug = Food.GetComponentInChildren<PacmanDrug>();
            _foodCount = Food.GetComponentsInChildren<YellowDelicious>().Length;
            End(1f);
            _walls = Walls.GetComponentsInChildren<AnotherBrick>().ToList();
            _tiles = Tiles.GetComponentsInChildren<PacTile>().ToList();
            _tileTransforms = _tiles.Select(t => t.gameObject.transform).ToList();
            _tileIndices = new  List<int>();
            for (var i = 0; i < _tiles.Count; i++)
                _tileIndices.Add(i);
            _collider = _tiles.Select(t => t.GetCollider()).ToList();
            _neighborIndices = new List<List<int>>();
            //_recheckGhostsCountdown = -1f;
            _wholeArea = WholeArea.GetComponentsInChildren<Transform>().Where(t => t != WholeArea).Select(t => t.position).ToList();

            var targetPos = Camera.position;
            targetPos.y = transform.position.y;
            transform.position = targetPos;

            foreach (var t in _tiles)
            {
                var neighbors = t.Neighbors;
                var indices = neighbors.Select(n => _tiles.IndexOf(n)).ToList();
                
                _neighborIndices.Add(indices);
                foreach (var n in t.Neighbors)
                {
                    if (!n.Neighbors.Contains(t))
                        Debug.LogWarning("[Pacman3D] neighbor conflict " + n.name + " does not contain " + t.name);
                }
            }


            _distanceNeighbors = Vector3.Distance(_tileTransforms[0].position,
                _tileTransforms[_neighborIndices[0][0]].position);

            //_tilesToFood = new Dictionary<int, List<YellowDelicious>>();
            //foreach (var tile in _tileIndices)
            //{
            //    _tilesToFood.Add(tile, new List<YellowDelicious>());
            //}
            //foreach (var food in Food.GetComponentsInChildren<YellowDelicious>())
            //{
            //    var tile = _tiles[0];
            //    if (food.CheckTile(ref tile))
            //    {
            //        _tilesToFood[_tiles.IndexOf(tile)].Add(food);
            //    }
            //}

            _cherries = _tiles.Select(t => t.GetComponentInChildren<PacmanCherry>()).ToList();
        }

        private void OnEnabled()
        {
            VirtualSpaceHandler.OnEventsChanged += SetFlagFigureOutGhost;
            VirtualSpaceHandler.OnReceivedNewEvents += SetFlagFigureOutGhost;
        }

        private void OnDisabled()
        {
            // ReSharper disable once DelegateSubtraction
            VirtualSpaceHandler.OnEventsChanged -= SetFlagFigureOutGhost;
            // ReSharper disable once DelegateSubtraction
            VirtualSpaceHandler.OnReceivedNewEvents -= SetFlagFigureOutGhost;
        }

        private void SetFlagFigureOutGhost()
        {
            _figureOutGhosts = true;
        }
        
        public Transform DebugArea1;
        public Transform DebugArea2;
        private bool _is1;
        private float _debugTimeSwitch = 5f;
        public bool DebugMode = true;
        public float TimeConsideredForSpeedComp = 0.4f;
        //private float _lastTimeTurned;
        public struct PosTime
        {
            public Vector3 pos;
            public float time;
        }


        private List<PosTime> _postimes;

        private float GetSpeed()
        {
            if (!_postimes.Any())
                return Speed;
            var first = _postimes.FirstOrDefault();
            var dist = first.pos - Camera.position;
            dist.y = 0f;
            var mag = dist.magnitude;
            var timediff = Time.unscaledTime - first.time;
            if (timediff < 0.01f)
                return Speed;
            var cmpSpeed = mag / timediff;
            return Mathf.Max(Speed, cmpSpeed);
        }

        private void FigureOutWhereToSendGhosts(bool forceGhostResend)
        {
            var transitionEnds = float.MaxValue;
            var transitionStarts = float.MaxValue;
            var futureArea = new List<Vector3>();
            var futureAreaSteps = new List<Vector3>[5];
            var futurePoint = Vector3.zero;
            var presentCenter = Vector3.zero;
            var futureSet = false;
            var presentSet = false;
            var willBeArea = new List<Vector3>();
            for (var i = 0; i < futureAreaSteps.Length; i++)
            {
                futureAreaSteps[i] = new List<Vector3>();
            }
            var presentArea = _wholeArea;
            //var presentAreaPlusOffset = new List<Vector3>();
            if (!DebugMode)
            {
                if (VirtualSpaceCore.Instance.IsRegistered())
                {
                    Vector3 will;
                    VirtualSpaceHandler.SpaceAtTimeWithSafety(1f, 0f, out willBeArea,
                        out will);


                    presentSet = VirtualSpaceHandler.SpaceAtTimeWithSafety(0f, 0f, out presentArea, out presentCenter);
                    //for (var i = 0; i < futureAreaSteps.Length; i++)
                    //{
                        //List<Vector3> tmp;
                        //Vector3 tmpPos;
                        ////travel time + processing + min standing on time
                        //var success =
                        //    VirtualSpaceHandler.SpaceAtTimeWithSafety(
                        //        (_distanceNeighbors / Speed + CognitiveProcessingTime) * (i + 1), 0f, out tmp,
                        //        out tmpPos);
                        //futureAreaSteps[i] = tmp;
                    //}

                    //get keyframes from backend
                    var actives = VirtualSpaceHandler.ActiveOrPendingTransitions;
                    if (actives.Count == 0)
                    {
                        //starting - send ghosts anywhere
                    }
                    else
                    {
                        var currentTransition = actives.FirstOrDefault(a => a.Speed > 0.1f);
                        if (currentTransition != null)
                        {
                            if (currentTransition.TurnEnd == long.MaxValue)
                            {
                                //no transitions - send ghosts anywhere
                            }
                            else
                            {
                                futureArea = VirtualSpaceHandler._TranslateIntoUnityCoordinates(currentTransition.Frames.Last()
                                        .Area.Area);
                                futurePoint = VirtualSpaceHandler._TranslateIntoUnityCoordinates(currentTransition.Frames.Last()
                                    .Position.Position);
                                futureSet = true;
                            }
                        }
                    }
                }
            }
            else
            {
                var dbgArea1 = DebugArea1.GetComponentsInChildren<Transform>().Where(t => t != DebugArea1)
                    .Select(t => t.position).ToList();
                var dbgArea2 = DebugArea2.GetComponentsInChildren<Transform>().Where(t => t != DebugArea2)
                    .Select(t => t.position).ToList();
                futureArea = _is1 ? dbgArea1 : dbgArea2;
                presentArea = _is1 ? dbgArea2 : dbgArea1;
                for (var i = 0; i < futureAreaSteps.Length; i++)
                {
                    List<Vector3> tmp;
                    //travel time + processing + min standing on time
                    tmp = (_distanceNeighbors / Speed) * (i + 1) > _debugTimeSwitch ? futureArea : presentArea;
                    futureAreaSteps[i] = tmp;
                }
            }
            //get tiles available in future and present
            var futureTiles = GetAvailableTiles(futureArea, false);
            var presentTiles = GetAvailableTiles(presentArea, false);
            var willBeTiles = GetAvailableTiles(willBeArea, false);

            //_recheckGhostsCountdown = _distanceNeighbors / GetSpeed() + CognitiveProcessingTime;

            List<int> availableTiles;
            //var availableTimeSteps = futureAreaSteps.Select(fat => GetAvailableTiles(fat, false)).ToList();
            //availableTimeSteps.Insert(0, presentTiles);
            //for (var step = 0; step < availableTimeSteps.Count; step++)
            //{
            //    var notavailable = _tileIndices
            //        .Where(ti =>
            //            !availableTiles.Contains(ti) &&
            //            !availableTimeSteps[step].Contains(ti)).ToList();
            //    var distanceToNow = CreateDistanceEvaluation(_currentTileIndex, notavailable);
            //    //check tiles added
            //    for (var c = 0; c < availableTimeSteps[step].Count; c++)
            //    {
            //        var check = availableTimeSteps[step][c];
            //        if (availableTiles.Contains(check))
            //            continue;
            //        var dist = distanceToNow[check];
            //        if (dist != step)
            //            continue;
            //        availableTiles.Add(check);
            //    }
            //}

            //var pathToFuture = new List<int>();
            //if (futureTiles.Any() && (!futureTiles.Contains(_lastTileIndex) ||
            //                          !futureTiles.Contains(_currentTileIndex)))
            //{
            //    pathToFuture = GetClosestPathToTiles(futureTiles,
            //        futureTiles.Contains(_currentTileIndex) ? _lastTileIndex : _currentTileIndex, availableTiles);
            //    pathToFuture = pathToFuture.Where(p => !futureTiles.Contains(p)).ToList();
            //    pathToFuture.AddRange(futureTiles);
            //    pathToFuture = pathToFuture.Where(pf => availableTiles.Contains(pf)).ToList();

            //}
            availableTiles = willBeTiles;
            availableTiles.AddRange(presentTiles.Where(pt => futureTiles.Contains(pt) && !availableTiles.Contains(pt)));

            var pathToFutureMiddle = new List<int>();
            var pathToPresentMiddle = new List<int>();
            if (futureSet) // && !_cherries.Any(c => c.Visible()))
            {
                var from = futureTiles.Contains(_currentTileIndex) ? _lastTileIndex : _currentTileIndex;
                if(!GetPathFromPointToArea(futurePoint, futureTiles, from, out pathToFutureMiddle))
                    pathToFutureMiddle.Clear();
                for(var i = 0; i  < pathToFutureMiddle.Count-1; i++)
                {
                    var t = i + 1;
                    Debug.DrawLine(_tileTransforms[pathToFutureMiddle[i]].position + Vector3.up * 4f, _tileTransforms[pathToFutureMiddle[t]].position + Vector3.up * 4f, Color.red, 1f);
                }
                //futureTiles = futureTiles.Where(ft => availableTiles.Contains(ft)).ToList();
            }
            else
            {
                if (presentSet) //!pathToFutureMiddle.Any() && presentSet)
                {
                    var from = presentTiles.Contains(_currentTileIndex) ? _lastTileIndex : _currentTileIndex;
                    if(!GetPathFromPointToArea(presentCenter, availableTiles, from, out pathToPresentMiddle))
                        pathToPresentMiddle.Clear();
                    for (var i = 0; i < pathToPresentMiddle.Count-1; i++)
                    {
                        var t = i + 1;
                        Debug.DrawLine(_tileTransforms[pathToPresentMiddle[i]].position + Vector3.up * 4f, _tileTransforms[pathToPresentMiddle[t]].position + Vector3.up * 4f, Color.green, 1f);
                    }
                    availableTiles.AddRange(pathToPresentMiddle.Where(pfm => !availableTiles.Contains(pfm)));
                }
            }
            //var inside2Center = pathToFutureMiddle.Where(p => availableTiles.Contains(p)).ToList();
            //var outside2Center = pathToFutureMiddle;


            //set available tiles
            //var displayPath = onlyPath.Any();//!futureTiles.Contains(_currentTileIndex) && pathToFuture.Any();
            //if(displayPath)
            //    availableTiles.AddRange(pathToFuture.Where(ptf => !availableTiles.Contains(ptf)));
            //availableTiles = displayPath
            //    ? pathToFuture
            //    : availableTiles;

            //set control condition if necessary
            if (Menu.IsControlCondition() && !_controlGroupChecked && Control != null)
            {
                var food = Food.GetComponentsInChildren<YellowDelicious>()
                    .Where(f => !VirtualSpaceHandler.PointInPolygon(f.transform.position, presentArea, 0f));
                var drugs = Food.GetComponentsInChildren<PacmanDrug>()
                    .Where(f => !VirtualSpaceHandler.PointInPolygon(f.transform.position, presentArea, 0f));
                Control.SetActive(true);
                gameObject.transform.parent.gameObject.SetActive(false);
                _controlGroupChecked = true;
                foreach (var f in food)
                {
                    var go = f.gameObject;
                    Destroy(go);
                    //f.TryEat();
                }
                foreach (var d in drugs)
                {
                    _controlGroupChecked = true;
                    Destroy(d.gameObject);//.SetActive(false);
                }
                foreach (var c in _cherries)
                {
                    _controlGroupChecked = true;
                    Destroy(c.gameObject);//.SetActive(false);
                }
            }

            //check if player is okay == in available tiles
            var playerOkay = presentTiles.Contains(_currentTileIndex) || presentTiles.Contains(_lastTileIndex);
            if (playerOkay != _playerOkay)
            {
                //WallsDown.Move(!playerOkay);
                foreach (var g in Ghosts)
                {
                    g.gameObject.SetActive(playerOkay);
                }
                var foodx = Food.GetComponentsInChildren<YellowDelicious>().ToList();
                foreach (var f in foodx)
                {
                    f.OnValidArea(playerOkay);
                }
                var drugsx = Food.GetComponentsInChildren<PacmanDrug>().ToList();
                foreach (var d in drugsx)
                {
                    d.OnValidArea(playerOkay);
                }
                
                //foreach (var c in _cherries)
                //{
                //    c.gameObject.SetActive(playerOkay);
                //}
            }
            _playerOkay = playerOkay;


            //check if available tiles is different from last
            var same = _lastAvailableTiles.Count == availableTiles.Count;
            if (same)
            {
                if (!_lastAvailableTiles.All(lat => availableTiles.Contains(lat)))
                {
                    same = false;
                }
            }
            //send ghosts if available tiles changed or player position changed
            if (!same || forceGhostResend)
            {
                //if (forceGhostResend)//TODO _playerOkay || )
                //{
                    _lastAvailableTiles = availableTiles;
                    SendGhostToAvailable(!futureTiles.Any(), forceGhostResend);
                //}

                //if (!same)
                //{
                    //set available tile visibility
                    //for (var n = 0; n < _tiles.Count; n++)
                    //    _tiles[n].SetActiveTile(_lastAvailableTiles.Contains(n));

                    //getting lewd with the food...
                    var path = futureSet && _playerOkay ? 
                        (futureTiles.Contains(_currentTileIndex) ? 
                            new List<int>() : 
                            pathToFutureMiddle.Where(pfm => pathToFutureMiddle.IndexOf(pfm) == pathToFutureMiddle.Count-1)) : 
                       (_playerOkay ? 
                            new List<int>() : 
                            pathToPresentMiddle);
                    foreach (var p in _tileIndices)
                    {
                        if(_cherries[p] != null)
                            _cherries[p].TryReset(path.Contains(p), presentTiles.Contains(_currentTileIndex) && !_lastAvailableTiles.Contains(p));
                    }
                        
                //}
            }
        }

        private void SendGhostToAvailable(bool relaxMode, bool forceMove)
        {

            //iterate through ghosts and assign tasks
            var distances = CreateDistanceEvaluation(_currentTileIndex, new List<int>());
            var ghosts = Ghosts.Where(g => g.gameObject.activeSelf).ToList();

            ghosts = ghosts.Where(g => forceMove || !g.IsOccupied() || _lastAvailableTiles.Contains(FindGhostIndex(g))).ToList();
            var ghostPositions = ghosts.Select(FindGhostIndex).ToList();

            var neighborsAvailable = new List<int>();
            foreach (var t in _tileIndices)
            {
                if (distances[t] > 2)
                    continue;
                neighborsAvailable.Add(t);
            }

            //get closest unavailable tile(s)
            var closestTiles = GetNeighboringTiles(_lastAvailableTiles);
            var fleeing = ghosts.Any(g => g.IsFleeing());
            //if (fleeing)
            //{
            //    targetTiles = _lastAvailableTiles.Where(tt => _neighborIndices[tt].All(n => _lastAvailableTiles.Contains(n)))
            //        .ToList();
            //}
            //else
            //{
            //    //get closest unavailable tile(s)
            //    var closestTiles = GetNeighboringTiles(_lastAvailableTiles);
            //    targetTiles = closestTiles;
            //}
            foreach (var closeTile in closestTiles)
            {
                if (!ghosts.Any())
                    break;
                var distance = distances[closeTile];
                var ghostSendIndex = closeTile;

                Debug.DrawRay(_tileTransforms[ghostSendIndex].position, Vector3.one * 0.5f, Color.magenta, 0.4f);
                if (relaxMode)
                {
                    var ghostDistanceWeWant = distance == 0 ? 0 : 2 * distance - 1;
                    for (var i = 0; i < distances.Count; i++)
                    {
                        if (distances[i] != ghostDistanceWeWant || _lastAvailableTiles.Contains(i))
                            continue;
                        ghostSendIndex = i;
                        distance = distances[i];
                        break;
                    }
                }
                var ghostSent = ghosts.FirstOrDefault(g => g.GetHash() == ghostSendIndex);
                var ghostIndex = ghostSent == null ? GetClosestGhostIndex(ghosts, ghostSendIndex) : ghosts.IndexOf(ghostSent);
                if (ghostIndex >= 0)
                {
                    var shouldflee = fleeing && distance > 1;
                    List<int> ghostCrumbs;
                    bool ghostNoDirectWay;
                    var avoidTiles = _lastAvailableTiles.Contains(ghostPositions[ghostIndex]) ? neighborsAvailable : _lastAvailableTiles;//neighborsAvailable;
                    if (shouldflee)
                        avoidTiles = new List<int>();
                    var val = CreateDistanceEvaluation(ghostPositions[ghostIndex], avoidTiles);
                    FindPathFromTo(ghostPositions[ghostIndex], ghostSendIndex, out ghostCrumbs,
                        out ghostNoDirectWay, false, val);
                    var crumbsPath = ghostCrumbs.Select(i => _tileTransforms[i].position).ToList();
                    if (fleeing && !shouldflee)
                        ghosts[ghostIndex].StartNormal();
                    //ghosts[ghostIndex].StartNormal();
                    var time = (distance) * _distanceNeighbors / _avgSpeed;
                    var forceTeleport = time <= 0f;
                    ghosts[ghostIndex].GoTo(crumbsPath, time, ghostNoDirectWay || forceTeleport, ghostSendIndex,
                        _distanceNeighbors, _tileTransforms[ghostPositions[ghostIndex]].position, false);
                    ghosts.RemoveAt(ghostIndex);
                    ghostPositions.RemoveAt(ghostIndex);
                }

            }
            //ghosts left: send anywhere in unavailable area
            var ghostInts = _tileIndices.Where(ti => !_lastAvailableTiles.Contains(ti)).ToList();
            if (Control == null)
                ghostInts = _tileIndices;
            while (ghosts.Count > 0)
            {
                
                TrySentGhostToRandomAvailable(ghosts[0], ghostInts, true, false);
                ghosts.RemoveAt(0);
            }

            //else //TODO intentionally kill player?
            //{
            //    //Debug.Log("KILL KILL KILL !!!");
            //    //catch player - game over
            //    var ghostIndex = GetClosestGhostIndex(ghosts, _currentTileIndex);
            //    List<int> ghostCrumbs;
            //    bool ghostNoDirectWay;
            //    FindPathFromTo(ghostPositions[ghostIndex], _currentTileIndex, out ghostCrumbs,
            //        out ghostNoDirectWay, new List<int>(), false);
            //    var crumbsPath = ghostCrumbs.Select(i => _tileTransforms[i].position).ToList();
            //    ghosts[ghostIndex].StartNormal();
            //    ghosts[ghostIndex].GoTo(crumbsPath, -1, ghostNoDirectWay, _currentTileIndex,
            //        _distanceNeighbors, _tileTransforms[ghostPositions[ghostIndex]].position, false);
            //}

            //}
        }

        private bool GetPathFromPointToArea(Vector3 to, List<int> availableTiles, int from, out List<int> pathToPoint)
        {
            pathToPoint = new List<int>();
            var infos = Physics.RaycastAll(new Ray(to + Vector3.up, Vector3.down), 5f);
            var indx = -1;
            foreach (var info in infos)
            {
                if(!_collider.Contains(info.collider))
                    continue;
                indx = _collider.IndexOf(info.collider);
            }
            if (indx >= 0)
            {
                indx = availableTiles.Contains(indx) ? indx: GetClosestIndex(availableTiles, indx);
                pathToPoint = GetCleanList(from, indx);//new List<int> { indx },from, availableTiles);
                return true;
            }
            return false;
        }

        private List<int> GetNeighboringTiles(ICollection<int> area)
        {
            var ghostSendIndices = new List<int>();
            foreach (var tile in area)
            {
                var neighbors = _neighborIndices[tile];
                ghostSendIndices.AddRange(neighbors);
            }
            ghostSendIndices = ghostSendIndices.Distinct().ToList();
            ghostSendIndices = ghostSendIndices.Where(gs => !area.Contains(gs)).ToList();
            var distances = CreateDistanceEvaluation(_currentTileIndex, new List<int>());
            ghostSendIndices = ghostSendIndices.OrderBy(d => distances[d]).ToList();
            return ghostSendIndices;
        }

        private float GetPathDistance(List<int> tiles)
        {
            var distance = 0f;
            var path = tiles.Select(t => _tileTransforms[t].position).ToList();
            for (var i = 1; i < path.Count; i++)
                distance += Vector3.Distance(path[i - 1], path[i]);
            return distance;
        }

        private List<int> GetAvailableTiles(List<Vector3> area, bool drawDebugRays)
        {
            //get tiles in future area
            var tilesWeWantUserToBeOn = new List<int>();
            for (var n = 0; n < _tiles.Count; n++)
            {
                var available = IsAvailable(n, area, true);
                //if(available)
                //    Debug.Log("getavailable " +_tiles[n]+ " is true");
                if (available)
                    tilesWeWantUserToBeOn.Add(n);
                //if(drawDebugRays)
                //    Debug.DrawRay(_tileTransforms[n].position, Vector3.up * 5f, available ? Color.green : Color.red, 0.4f);
            }
            //Debug.Log(tilesWeWantUserToBeOn.Count);
            return tilesWeWantUserToBeOn;
        }

        private List<int> GetClosestPathToTiles(List<int> tilesWeWantUserToBeOn, int from, List<int> overlapIfSame)
        {
            //compute distances future area tiles to current tile
            var distances = CreateDistanceEvaluation(from, new List<int>());

            //get tile in future area which is closest to current tile
            var smallestDistance = int.MaxValue;
            var smallestIndex = -1;
            var same = new List<int>();
            for (var i = 0; i < _tiles.Count; i++)
            {
                if (!tilesWeWantUserToBeOn.Contains(i))
                    continue;
                if (distances[i] > smallestDistance)
                    continue;
                if (distances[i] == smallestDistance)
                {
                    same.Add(i);
                }
                else
                {
                    smallestIndex = i;
                    smallestDistance = distances[i];
                    same.Clear();
                }
            }

            if (smallestIndex == -1)
                return new List<int>();

            var list = GetCleanList(from, smallestIndex);
            var over = overlapIfSame.Count(o => list.Contains(o));
            for (var i = 0; i < same.Count; i++)
            {
                var altList = GetCleanList(from, same[i]);
                var altOver = overlapIfSame.Count(o => altList.Contains(o));
                if(altOver<over)
                    continue;
                over = altOver;
                list = altList;
            }
            return list;
        }

        private List<int> GetCleanList(int from, int to)
        {
            //compute path from current tile to closest tile in future area
            List<int> pathToAvailableArea;
            bool noDirectWay;
            var val = CreateDistanceEvaluation(from, new List<int>());
            FindPathFromTo(from, to, out pathToAvailableArea, out noDirectWay, true, val);

            //remove area tiles from path
            //pathToAvailableArea.RemoveAll(tilesWeWantUserToBeOn.Contains);
            //add current tile
            if (!pathToAvailableArea.Contains(from))
                pathToAvailableArea.Insert(0, from);
            //add all tiles from last tile to this tile (might move around corners etc.)
            if (_lastTileIndex != from)
            {
                List<int> pathLastToCurrent;
                bool pathLastToCurrentNoDirectWay;
                var values = CreateDistanceEvaluation(_lastTileIndex, new List<int>());
                FindPathFromTo(_lastTileIndex, from, out pathLastToCurrent, out pathLastToCurrentNoDirectWay, true, values);
                if (!pathLastToCurrent.Contains(_lastTileIndex))
                    pathLastToCurrent.Insert(0, _lastTileIndex);
                pathLastToCurrent = pathLastToCurrent.Where(pltc => !pathToAvailableArea.Contains(pltc)).ToList();
                pathToAvailableArea.InsertRange(0, pathLastToCurrent);
            }
            
            //now we have the valid path from user to next area
            return pathToAvailableArea;
        }

        private int GetClosestIndex(List<int> indices, int tileInQuestion)
        {
            if (indices.Count == 0)
                return tileInQuestion;
            //pick closer ghost 
            var avoid = new List<int>();
            var distanceFromClosestUnavailable = CreateDistanceEvaluation(tileInQuestion, avoid);
            var dists = indices.Select(gi => distanceFromClosestUnavailable[gi]).ToList();
            var min = 0;
            var minDist = dists[0];
            for (var i = 1; i < indices.Count; i++)
            {
                if (dists[i] >= minDist)
                    continue;
                minDist = dists[i];
                min = i;
            }

            //if (min == -1)
            //{

            //    var tilePos = _tileTransforms[tileInQuestion].position;
            //    for (var i = 1; i < ghosts.Count; i++)
            //    {
            //        if (dists[i] >= minDist)
            //            continue;
            //        minDist = dists[i];
            //        min = i;
            //    }
            //}
            return min;
        }

        private int GetClosestGhostIndex(List<PacmanGhost> ghosts, int tileInQuestion)
        {
            var ghostIndices = ghosts.Select(FindGhostIndex).ToList();
            return GetClosestIndex(ghostIndices, tileInQuestion);
        }

        //private int SendClosestGhostToTileClosestToPlayer(List<int> distances, List<int> toBeAvoided, float inSeconds, ref List<PacmanGhost> ghosts, ref List<int> ghostPositions)
        //{
        //    var currentClosestUnavailableDistance = -1;
        //    var currentClosestUnavailable = -1;
        //    for (var n = 0; n < _tiles.Count; n++)
        //    {
        //        if (toBeAvoided.Contains(n) || _currentTileIndex == n || (currentClosestUnavailable >= 0 && distances[n] >= currentClosestUnavailableDistance))
        //            continue;

        //        currentClosestUnavailable = n;
        //        currentClosestUnavailableDistance = distances[n];
        //    }
        //    if (currentClosestUnavailable >= 0)
        //    {
        //        List<int> c;
        //        bool noDirectWay;
        //        //pick closer ghost 
        //        var avoid = new List<int> { _currentTileIndex };
        //        avoid.AddRange(toBeAvoided);
        //        var distanceFromClosestUnavailable = CreateDistanceEvaluation(currentClosestUnavailable, toBeAvoided);
        //        var dists = ghostPositions.Select(gp => distanceFromClosestUnavailable[gp]).ToList();
        //        var min = 0;
        //        var minDist = dists[0];
        //        for (var i = 1; i < ghosts.Count; i++)
        //        {
        //            if (dists[i] >= minDist)
        //                continue;
        //            minDist = dists[i];
        //            min = i;
        //        }
        //        FindPathFromTo(ghostPositions[min], currentClosestUnavailable, out c, out noDirectWay, avoid);
        //        var crumbsPath = c.Select(i => _tiles[i].transform.position).ToList();
        //        ghosts[min].StartNormal();
        //        ghosts[min].GoTo(crumbsPath, inSeconds, noDirectWay, c.Last(), _distanceNeighbors, _tileTransforms[ghostPositions[min]].position);
        //        ghosts.RemoveAt(min);
        //        ghostPositions.RemoveAt(min);
        //    }
        //    return currentClosestUnavailable;
        //}

        //private void SyncWithBackendSend()
        //{
        //    if (!VirtualSpaceCore.Instance.IsRegistered())
        //        return;

        //    //var valueDict = new Dictionary<int, float>();
        //    //add current index
        //    //valueDict.Add(_currentTileIndex, 1f);
        //    //var done = new List<int> { _currentTileIndex };

        //    var posCurrent = _tiles[_currentTileIndex].transform.position;
        //    var nDist1 = FindRecursiveNeighbors(_currentTileIndex, 1).Where(n => n != _currentTileIndex).ToList();
        //    var pos = nDist1.Select(item => _tiles[item].gameObject.transform.position).ToList();
        //    var valMin = -1f;
        //    var minIndex = -1;
        //    for (var n = 0; n < nDist1.Count; n++)
        //    {
        //        var val = Vector3.Angle(pos[n] - posCurrent, Camera.forward);
        //        if (!(valMin < 0f) && !(val < valMin)) continue;
        //        valMin = val;
        //        minIndex = nDist1[n];
        //    }

        //    //call this on every new tile enter!
        //    if(minIndex < 0)
        //        return;
        //    var requestPosition = _tiles[minIndex].transform.position;
        //    VirtualSpaceHandler.RequestPosition(requestPosition, 0f, 0f, 1, 1);
        //}

        //private void SpaceEvent(UnavailableSpaceListLegacy us)
        //{
        //    if (us.closestUnavailablePosition == null || us.closestUnavailablePosition.t >= IgnoreUnavailableAbove)
        //    {
        //        return;
        //    }
        //    _newEvent = true;
        //    _newEventOrigin = new Vector3(us.closestUnavailablePosition.x, RayTestY, us.closestUnavailablePosition.z);
        //    _newEventTime = us.closestUnavailablePosition.t;
        //}

        private void Start()
        {
            foreach (var ghost in Ghosts)
            {
                ghost.StopMovement(true, true);
            }
        }

        private bool IsAvailable(int tileIndex, List<Vector3> flatArea, bool isDebug)
        {
            if (flatArea.Count < 3)
            {
                //Debug.Log(_tiles[tileIndex].name + "!!!");
                return false;
            }
            var pos = _tiles[tileIndex].transform.position;
            //var size = TileSize;
            
            //var p1 = new Vector3(pos.x - size.x * 0.5f, 0f, pos.z - size.z * 0.5f);
            //var p2 = new Vector3(pos.x - size.x * 0.5f, 0f, pos.z + size.z * 0.5f);
            //var p3 = new Vector3(pos.x + size.x * 0.5f, 0f, pos.z - size.z * 0.5f);
            //var p4 = new Vector3(pos.x + size.x * 0.5f, 0f, pos.z + size.z * 0.5f);
            //var p1In = VirtualSpaceHandler.PointInPolygon(p1, flatArea, 0f);
            //var p2In = VirtualSpaceHandler.PointInPolygon(p2, flatArea, 0f);
            //var p3In = VirtualSpaceHandler.PointInPolygon(p3, flatArea, 0f);
            //var p4In = VirtualSpaceHandler.PointInPolygon(p4, flatArea, 0f);
            //Debug.Log(_tiles[tileIndex].name + " " + p1In + " " + p2In + " " + p3In + " " + p4In);
            //Debug.DrawRay(p1, Vector3.up * 10, p1In ? Color.white : Color.grey, 1);
            //Debug.DrawRay(p2, Vector3.up * 10, p2In ? Color.white : Color.grey, 1);
            //Debug.DrawRay(p3, Vector3.up * 10, p3In ? Color.white : Color.grey, 1);
            //Debug.DrawRay(p4, Vector3.up * 10, p4In ? Color.white : Color.grey, 1);
            var available = -VirtualSpaceHandler.DistanceFromPoly(pos, flatArea, false) > TileSize.magnitude/2f ;
            //var available = p1In && p2In && p3In && p4In;
            //if(isDebug && available)
            //    Debug.Log("isavailable " + available + "  "+ _tiles[tileIndex].name);
            return available;
        }

        private void TrySentGhostToRandomAvailable(PacmanGhost ghost, List<int> availableArea, bool force, bool teleport)
        {
            if ((ghost.AreYouMoving() && !force) || availableArea.Count == 0)
                return;
            //move away if spot is cleared
            var ghostIndex = FindGhostIndex(ghost);

            var unavailableTiles = _tileIndices.Where(ti => !availableArea.Contains(ti)).ToList();
            var random = Random.Range(0, availableArea.Count - 1);
            var tileIndex = availableArea[random];
            List<int> crumbs;
            bool noDirectWay;
            var values = CreateDistanceEvaluation(ghostIndex, unavailableTiles);
            FindPathFromTo(ghostIndex, tileIndex, out crumbs, out noDirectWay, false, values);
            var crumbsPath = crumbs.Select(i => _tiles[i].transform.position).ToList();
            ghost.GoTo(crumbsPath, -1f, noDirectWay || teleport, tileIndex, _distanceNeighbors, _tileTransforms[ghostIndex].position, false);

            //Debug.Log("planb " + ghost.name + " path " + _tiles[ghostIndex].name + " " + _tiles[tileIndex].name);
            //foreach (var gc in crumbs)
            //{
            //    Debug.Log(_tiles[gc].name);
            //}
            
        }

        //private void TrySendToDedicated(PacmanGhost ghost)
        //{
        //    //check if an event was received and do computation on main thread
        //    if (_newEvent)
        //    {
        //        _newEvent = false;

        //        //RaycastHit info;
        //        //if (!Physics.Raycast(_newEventOrigin, Vector3.down, out info, 10))
        //        //    return;

        //        //var coll = info.collider;
        //        //if (!_collider.Contains(coll))
        //        //    return;

        //        var index = _newEventIndex;// _collider.IndexOf(coll);

        //        List<Vector3> c;
        //        bool noDirectWay;
        //        FindPathFromTo(FindGhostIndex(ghost), index, out c, out noDirectWay, true);
        //        ghost.StartNormal();
        //        ghost.GoTo(c, _newEventTime, noDirectWay);
        //    }
        //}

        private int FindGhostIndex(PacmanGhost ghost)
        {
            var ghostPos = ghost.transform.position;
            var minI = 0;
            var minDist = Vector3.Distance(ghostPos, _tileTransforms[0].position);
            for (var i = 1; i < _tileTransforms.Count; i++)
            {
                var dist = Vector3.Distance(ghostPos, _tileTransforms[i].position);
                if (dist < minDist)
                {
                    minI = i;
                    minDist = dist;
                }
            }
            return minI;
            //var coll = ghost.GetTilePosition();
            //return _collider.Contains(coll) ? _collider.IndexOf(coll) : 0;
        }
    
        private void FindPathFromTo(int start, int end, out List<int> crumbs, out bool noDirectWay, bool containFirst, List<int> values)
        {
            crumbs = new List<int>();
            var currentValue = values[end];
            noDirectWay = currentValue > values.Count;
            //var str = "path " + start+ " / " +  end + " : ";
            if (noDirectWay)
            {
                crumbs.Add(end);
                //Debug.Log(str+end);
                return;
            }
            var tmp = end;
            while (true)
            {
                crumbs.Add(tmp);
                //str += tmp + " ";
                if (containFirst ? currentValue == 0 : currentValue <= 1)
                    break;
                var neighbors = _neighborIndices[tmp];
                // ReSharper disable once ForCanBeConvertedToForeach
                for (var ni = 0; ni < neighbors.Count; ni++)
                {
                    if (currentValue <= values[neighbors[ni]])
                        continue;
                    currentValue = values[neighbors[ni]];
                    tmp = neighbors[ni];
                }
                currentValue = values[tmp];
            }
            //Debug.Log(str);
            crumbs.Reverse();
        }

        private List<int> FindRecursivePathValue(List<int> values, int index, int value, List<int> indicesToAvoid)
        {
            var neighbors = _neighborIndices[index];

            foreach(var n in neighbors)
            {
                if (indicesToAvoid.Any() && indicesToAvoid.Contains(n))
                    continue;

                if (values[n] > value)
                {
                    values[n] = value;
                    values = FindRecursivePathValue(values, n, value + 1, indicesToAvoid);
                }
            }
            return values;
        }

        private List<int> CreateDistanceEvaluation(int index, List<int> indicesToAvoid)
        {
            var values = new List<int>();
            for(var i = 0; i < _tiles.Count; i++)
            {
                values.Add(int.MaxValue);
            }
            values[index] = indicesToAvoid.Contains(index) ? int.MaxValue : 0;
            values = FindRecursivePathValue(values, index, 1, indicesToAvoid);
            return values;
        }

        private float _avgSpeed;
        void Update()
        {
            var unsc = Time.unscaledTime;
            _postimes.Add(new PosTime() {pos = Camera.position, time = unsc });
            var upto = 0;
            for (var i = 0; i < _postimes.Count; i++)
            {
                if (unsc - _postimes[i].time > TimeConsideredForSpeedComp)
                    continue;
                upto = i - 1;
                break;
            }
            if (upto > 0)
            {
                _postimes.RemoveRange(0, upto);
            }
            _avgSpeed = _avgSpeed * 0.9f + GetSpeed() * 0.1f;

            var camPos = Camera.position;
            var oldTileIndex = _currentTileIndex;
            var outsideMaze = false;

            //check if there is a new state in backend
            if (VirtualSpaceHandler.GetNewState())
            {
                EvaluateStates(VirtualSpaceHandler.State);
            }

            //debug areas...
            if (DebugMode)
            {
                _debugTimeSwitch -= Time.unscaledDeltaTime;
                if (_debugTimeSwitch < 0f)
                {
                    _is1 = !_is1;
                    _debugTimeSwitch = 10f;
                }
            }

            //look for closest index and second closest
            RaycastHit info;
            if (Physics.Raycast(RayTest.position, Vector3.down, out info, 10))
            {
                if (_collider.Contains(info.collider))
                {
                    _currentTileIndex = _collider.IndexOf(info.collider);
                }
                else
                {
                    //weird...
                    Debug.Log("Pacman: something wrong with getting current tile index");
                }
            
                if (_gameOver)
                {
                    _timeBeforeStart -= Time.deltaTime;
                    if (_timeBeforeStart <= 0)
                    {
                        StartGame();
                    }
                    else if (_timeBeforeStart < GameStartCountdown)
                    {
                        var intDown = Mathf.Ceil(_timeBeforeStart);
                        var msg = intDown <= 1 ? "Start!" : (intDown - 1).ToString(CultureInfo.InvariantCulture);
                        Ui.Display(-1, msg, false, true);
                        if (!_alreadyInit)
                        {
                            _alreadyInit = true;
                            foreach (var ghost in Ghosts)
                            {
                                ghost.StopMovement(true,true);
                            }
                        }
                    } 
                }
                else
                {
                    //TrySendToDedicated(GhostBlock1);
                    //TrySendToDedicated(GhostBlock2);
                    //TrySentGhostToRandomAvailable(GhostPlay1);
                }
            }
            else
            {
                //ERROR: player outside maze
                outsideMaze = true;
            }

            if ((
                    (outsideMaze && !_alreadyPunishedForOutside) || 
                    (oldTileIndex != _currentTileIndex && !_neighborIndices[oldTileIndex].Contains(_currentTileIndex))) &&
                !_wallsAreRed && _playerOkay)
            {
                //ERROR: player went through wall
                Ui.SaveLastDisplay();
                if (!_gameOver)
                {
                    _points = Mathf.Max(0, _points + WallDecrement);
                    Ui.Display(_points, "WALL -" + Mathf.Abs(WallDecrement).ToString(), true, false);
                    MakeNew(WallDecrement, _tileTransforms[_currentTileIndex].position);
                } else
                {
                    Ui.Display(-1, "WALL", true, false);
                }
                _alreadyPunishedForOutside = true;
                _wallHitLast = Time.time;
                _wallsAreRed = true;
                foreach (var wall in _walls)
                {
                    wall.ChangeMaterial(true);
                }
                WallSource.Play();
            }
            else if (_wallsAreRed && (Time.time - _wallHitLast) > WallErrorInterval && !outsideMaze)
            {
                Ui.Display(_gameOver ? -1 : _points, "", false, false);
                _wallsAreRed = false;
                _alreadyPunishedForOutside = false;
                foreach (var wall in _walls)
                {
                    wall.ChangeMaterial(false);
                }
            }

            var neighbors = _neighborIndices[_currentTileIndex];
            var distances = neighbors.Select(GetTileDistance).ToList();
            var closestNeighborIndex = neighbors[0];
            var min = distances[0];
            for (var i = 1; i < distances.Count; i++)
            {
                if (distances[i] >= min)
                    continue;
                min = distances[i];
                closestNeighborIndex = neighbors[i];
            }
            var playerPosChanged = false;
            if (oldTileIndex != _currentTileIndex)
            {
                _lastTileIndex = oldTileIndex;
                //_tiles[oldTileIndex].SetActiveTile(false);
                //_tiles[_currentTileIndex].SetActiveTile(true);
                EvaluateStates(VirtualSpaceHandler.State);
                playerPosChanged = true;
                //SyncWithBackendSend();

                _lastTilesWalked = _lastTilesWalked.Where(l => l != _currentTileIndex).ToList();
                _lastTilesWalked.Add(_currentTileIndex);
                while(_lastTilesWalked.Count > WalkedList)
                    _lastTilesWalked.RemoveAt(0);
            }

            if (_gameOver && !_won && _timeBeforeStart > GameStartCountdown)
                return;

            var camAtFloorLevel = camPos;
            camAtFloorLevel.y = _tileTransforms[closestNeighborIndex].position.y;
            camAtFloorLevel -= _tileTransforms[_currentTileIndex].position;

            var toNeighborVector = _tileTransforms[closestNeighborIndex].position - _tileTransforms[_currentTileIndex].position;
            var toNeighborVectorProjected = Vector3.Project(camAtFloorLevel, toNeighborVector.normalized);
            var playerPos = _tileTransforms[_currentTileIndex].position + toNeighborVectorProjected;
            var targetPos = new Vector3(playerPos.x, transform.position.y, playerPos.z);

            var process = toNeighborVectorProjected.magnitude / toNeighborVector.magnitude;
            //Debug.Log(process);
            if (process < 0.3f && _lastTileIndex != _currentTileIndex)
            {
                playerPosChanged = true;
                _lastTileIndex = _currentTileIndex;
            }
            //if (_recheckGhostsCountdown > 0f)
            //{
            //    _recheckGhostsCountdown -= Time.unscaledDeltaTime;
            //    if (_recheckGhostsCountdown <= 0f)
            //        _figureOutGhosts = true;
            //}
            if (_figureOutGhosts || playerPosChanged)
            {
                _figureOutGhosts = false;
            }
            FigureOutWhereToSendGhosts(playerPosChanged);//TODO where to place
            //Debug.Log(" ff + " + _lastTileIndex + " " + _currentTileIndex);

            //var angle = Vector3.Angle((targetPos - transform.position).normalized, transform.forward);
            //Debug.Log(angle);
            //if(angle < 170 || Time.unscaledTime-_lastTimeTurned > 0.3f)
            transform.LookAt(targetPos);
            //if (angle > 170)
             //   _lastTimeTurned = Time.unscaledTime;

            var currentSpeed = _avgSpeed * Time.deltaTime;//TODO avgspeed = speed
            var toTarget = targetPos - transform.position;
            var distanceToTarget = toTarget.magnitude;
            if (outsideMaze)
            {
                var newPos = camPos;
                newPos.y = transform.position.y;
                transform.position = newPos;

                //UI.Display(-1, "OUTSIDE TRACKING", true, false, true);
            }
            else if (true)//TODO if avatar ... distanceToTarget < currentSpeed)
            {
                transform.position = targetPos;
            }
            else if(distanceToTarget < 10000)
            {
                //Debug.Log(transform.position);
                //Debug.Log(targetPos);
                var lerpedPos = Vector3.Lerp(transform.position, targetPos, currentSpeed / distanceToTarget);
                if (!float.IsNaN(lerpedPos.x))
                    transform.position = lerpedPos;
            }
        }

        private void MakeNew(int points, Vector3 position)
        {
            var go = Instantiate(PointViz);
            var jmp = go.GetComponent<PacmanPointJumpToCam>();
            jmp.Init(position, points);
        }

        private void EvaluateStates(StateInfo info)
        {
            if (!VirtualSpaceCore.Instance.IsRegistered() || info == null)
                return;

            TransitionVoting voting = new TransitionVoting { StateId = info.StateId };

            var foodInArea = new List<int>();
            var availableTiles = new List<List<int>>();
            var numberUnwalkedTiles = new List<int>();
            var unwalkedTiles = _tileIndices.Where(ti => !_lastTilesWalked.Contains(ti)).ToList();
            var areaUnits = new List<double>();
            //var currentlyAvailableTiles = new List<int>();

            List<List<Vector3>> stateAreasAsList;
            List<Polygon> stateAreas;
            List<Vector3> stateCenters;
            List<Vector3> startAreaAsList;
            Polygon startArea;
            Vector3 startCenter;
            VirtualSpaceHandler.DeserializeState(info, out stateAreasAsList, out stateAreas, out stateCenters,
                out startCenter, out startArea, out startAreaAsList);

            var availableFoodPositions = Food.GetComponentsInChildren<YellowDelicious>().Where(af => !af.IsEaten()).Select(af => af.gameObject.transform.position).ToList();
            
            for (var i = 0; i < info.PossibleTransitions.Count; i++)
            {
                var area = stateAreasAsList[i];
                var tilesInArea = new List<int>();
                for (var t = 0; t < _tiles.Count; t++)
                {
                    if(IsAvailable(t, area, false))
                        tilesInArea.Add(t);
                }
                availableTiles.Add(tilesInArea);
                var foodAvailable = availableFoodPositions.Count(afp => VirtualSpaceHandler.PointInPolygon(afp, area, 0f));
                foodInArea.Add(foodAvailable);

                var unwalked = unwalkedTiles.Count(t => VirtualSpaceHandler.PointInPolygon(_tileTransforms[t].position, area, 0f));
                numberUnwalkedTiles.Add(unwalked);


                var areaU = ClipperUtility.GetArea(stateAreas[i]);

                areaUnits.Add(areaU);
            }

            var valuation = new Dictionary<int, float>();
            for (var i = 0; i < info.PossibleTransitions.Count; i++)
            {
                valuation.Add(i, 0);
            }

            //states with food are important
            var foodMax = foodInArea.Max();
            var foodMin = foodInArea.Min();
            for (var i = 0; i < info.PossibleTransitions.Count; i++)
            {
                var val = (float) (foodInArea[i] - foodMin) / (foodMax - foodMin) * 0.8f;
                //Debug.Log(val);
                valuation[i] += val;
            }

            //states with unwalked tiles are important
            var unwalkedMax = numberUnwalkedTiles.Max();
            var unwalkedMin = numberUnwalkedTiles.Min();
            for (var i = 0; i < info.PossibleTransitions.Count; i++)
            {
                var val = (float)(numberUnwalkedTiles[i] - unwalkedMin) / (unwalkedMax - unwalkedMin) * 0.1f;
                //Debug.Log(val);
                valuation[i] += val;
            }

            //bigger areas are important
            var areaUnitsMax = areaUnits.Max();
            var areaUnitsMin = areaUnits.Min();
            //var biggerThanMax = areaUnits.Select(au => au >= areaUnitsMax * 0.95d).ToList();
            //if (biggerThanMax.Count(bm => true) == 1) // areaUnits.IndexOf(areaUnitsMax)
            for (var i = 0; i < info.PossibleTransitions.Count; i++)
            {
                var val = (float)((areaUnits[i] - areaUnitsMin) / (areaUnitsMax - areaUnitsMin)) * 0.1f;//40% importance
                //Debug.Log(val);
                valuation[i] += val;
            }

            var nothingAvailable = new List<int>();
            for (var i = 0; i < info.PossibleTransitions.Count; i++)
            {
                //remove focus state AND switch states //TODO?
                //remove states with no available tiles
                if (info.PossibleTransitions[i] == VSUserTransition.Focus ||
                    info.PossibleTransitions[i] == VSUserTransition.SwitchLeft ||
                    info.PossibleTransitions[i] == VSUserTransition.SwitchRight ||
                    availableTiles[i].Count == 0)
                {
                    nothingAvailable.Add(i);
                    valuation[i] = 0;
                }
            }

            //normalize
            var valueMax = valuation.Values.Max();
            var weights = valuation.Select(v => 0).ToArray();
            var maxWeight = 100 * (_drug == null ? 1f : _drug.ValueFactor());
            for (var i = 0; i < valuation.Count; i++)
            {
                weights[i] = valueMax > 0f ? (int)(maxWeight * valuation[i] / valueMax) : 0;
            }

            //valuate
            var timeForOneTilehop = (_distanceNeighbors / (Speed/2f) + CognitiveProcessingTime) * 1000f;//in milliseconds
            var distances = CreateDistanceEvaluation(_currentTileIndex, new List<int>());
            for (var i = 0; i < info.PossibleTransitions.Count; i++)
            {
                var transition = info.PossibleTransitions[i];
                var vote = new TransitionVote
                {
                    Transition = transition,
                    Value = weights[i]
                };


                var isBiggerClipper = ClipperUtility.ContainsWithinEpsilon(stateAreas[i], startArea);
                //var isBiggerThanCurrent = global::VirtualSpaceHandler.PolygonInPolygon(startAreaAsList, stateAreasAsList[i], 0.1f);
                if (isBiggerClipper || nothingAvailable.Contains(i))
                {
                    //Debug.Log("pacman: biggerthancurrent");
                    vote.PlanningTimestampMs = new List<double> {0d};
                    vote.ExecutionLengthMs = new List<double> { 0d };
                }
                else
                {
                    //Debug.Log("pacman: smallerthancurrent");
                    //var smallestDistance = float.MaxValue;
                    var smallestDistance = availableTiles[i].Select(at => distances[at]).Min();
                    var allWithSmallestDistance = availableTiles[i].Where(at => distances[at] == smallestDistance).ToList();
                    var randomWithSmallestDistance = allWithSmallestDistance[Random.Range(0, allWithSmallestDistance.Count)];
                    //var closestTile = -1;
                    //var crumbsForClosestTile = new List<int>();
                    List<int> crumbs;
                    bool noDirectWay;
                    var values = CreateDistanceEvaluation(_currentTileIndex, new List<int>());
                    FindPathFromTo(_currentTileIndex, randomWithSmallestDistance, out crumbs, out noDirectWay, true, values);
                    //for (var t = 0; t < availableTiles[i].Count; t++)
                    //{

                    //    List<int> crumbs;
                    //    bool noDirectWay;
                    //    FindPathFromTo(_currentTileIndex, t, out crumbs, out noDirectWay, new List<int>());
                    //    var distance = 0f;
                    //    if(noDirectWay)
                    //        continue;
                    //    var crumbsPath = crumbs.Select(c => _tiles[c].transform.position).ToList();
                    //    for (var c = 1; c < crumbs.Count; c++)
                    //    {
                    //        distance += Vector3.Distance(crumbsPath[c-1], crumbsPath[c]);
                    //    }
                    //    if (distance < smallestDistance)
                    //    {
                    //        crumbsForClosestTile = crumbs;
                    //        smallestDistance = distance;
                    //        closestTile = t;
                    //    }
                    //}
                    vote.PlanningTimestampMs = new List<double>();
                    vote.ExecutionLengthMs = new List<double>();

                    //set some standard values, if everything else fails
                    vote.PlanningTimestampMs.Add(5000);
                    vote.ExecutionLengthMs.Add(5000);

                    //var end = crumbs.LastOrDefault();
                    for (var c = 0; c < crumbs.Count - 1; c++)
                    {
                        //Debug.Log("c " + c);
                        //check if that tile is still in the current area (maybe does not apply for tiles at end of crumb line)
                        var inPolygon = VirtualSpaceHandler.PointInPolygon(_tiles[c].transform.position,
                            startAreaAsList, 0f);
                        //Debug.DrawRay(_tiles[c].transform.position, Vector3.up * 5, !inPolygon ? Color.red : Color.green, 5f);
                        //Debug.Log("c " + c + " " + inPolygon);
                        if (!inPolygon)
                            continue;

                        //if yes, check if tile connected to closest tile in straight line
                        //var thisToEnd = (_tiles[c].transform.position - _tiles[end].transform.position).normalized;
                        //var angle = Vector3.Angle(Vector3.forward, thisToEnd);
                        //while (angle < 0) angle += 90;
                        //angle %= 90;
                        //var diff = Mathf.Abs(90 - angle);
                        //Debug.DrawLine(Vector3.up*2+ _tiles[c].transform.position, Vector3.up * 2 + _tiles[end].transform.position, diff > 0.001f ? Color.red : Color.green, 5f);
                        //Debug.Log("c " + c + " " + diff);
                        //if (diff > 0.001f)
                        //    continue;

                        //if yes, tiles until that as planning time, tiles after as execution time
                        vote.PlanningTimestampMs.Add(c * timeForOneTilehop);
                        vote.ExecutionLengthMs.Add((crumbs.Count-c)* timeForOneTilehop);
                        //Debug.Log("c " + c + " " + vote.PlanningTimestampMs.Last() + " " + vote.ExecutionLengthMs.Last());
                    }
                }

                voting.Votes.Add(vote);
            }
            //foreach (var vote in voting.Votes)
            //{
            //    for (var x = 0; x < vote.PlanningTimestampMs.Count; x++)
            //    {
            //        Debug.Log("vote " + vote.Transition + " " + vote.Value + " " + vote.PlanningTimestampMs[x] + " " + vote.ExecutionLengthMs[x]);
            //    }
            //}
            VirtualSpaceCore.Instance.SendReliable(voting);
        }

        private float GetTileDistance(int index)
        {
            var tilePos = _tileTransforms[index].position;
            return Vector3.Distance(new Vector3(Camera.position.x, tilePos.y, Camera.position.z), tilePos);
        }

        private List<int> FindRecursiveNeighbors(int index, int amount)
        {
            var list = new List<int>();
            var neighbors = _neighborIndices[index];
            list.AddRange(neighbors);
            if (amount > 1)
            {
                foreach (var neighbor in neighbors)
                {
                    var recursiveNeighbors = FindRecursiveNeighbors(neighbor, amount - 1);
                    list.AddRange(recursiveNeighbors.Where(n => !list.Contains(n)));
                }
            }
            return list;
        }

        //private static Vector2 Rect2Position(VirtualSpace.RectLegacy rect, Vector3 position)
        //{
        //    float x;
        //    if (position.x < rect.lower_X)
        //    {
        //        x = rect.lower_X;
        //    }
        //    else if (position.x > rect.upper_X)
        //    {
        //        x = rect.upper_X;
        //    }
        //    else
        //    {
        //        x = position.x;
        //    }

        //    float z;
        //    if (position.z < rect.lower_Z)
        //    {
        //        z = rect.lower_Z;
        //    }
        //    else if (position.z > rect.upper_Z)
        //    {
        //        z = rect.upper_Z;
        //    }
        //    else
        //    {
        //        z = position.z;
        //    }
        //    return new Vector2(x, z);
        //}

        private void OnTriggerStay(Collider other)
        {
            if (_gameOver || !_enteredEnemy)
                return;

            var ghost = other.GetComponent<PacmanGhost>();

            if (ghost != null)
            {
                if (!ghost.IsFleeing())
                {
                    _gameOverCountdown -= Time.deltaTime;
                    if (_gameOverCountdown < 0f)
                    {
                        _enteredEnemy = false;
                        Ui.Display(_points, "GAME OVER!", true, true);
                        DieSource.Play();
                        ghost.StopMovement(false, true);
                        _won = false;
                        End(2f);
                    }
                    
                }
            }
            
        }

        private bool _enteredEnemy;
        private float _gameOverCountdown;
        private void OnTriggerEnter(Collider other)
        {
            if (_gameOver)
                return;

            var ghost = other.GetComponent<PacmanGhost>();

            if (ghost != null)
            {
                if (ghost.IsFleeing())
                {
                    _points += GhostEatenIncrement;
                    Ui.Display(_points, "GHOST +" + GhostEatenIncrement, false, true);
                    EatSource.clip = GhostEatenSound;
                    EatSource.Play();
                    ghost.StopMovement(false, false);
                    ghost.InitPosition(transform.position);
                }
                else
                {
                    _enteredEnemy = true;
                    _gameOverCountdown = 1f;
                    //DieSource.Play();
                    //_won = false;
                    //End(2f);
                    //Ui.Display(_points, "GAME OVER!", true, true);
                    //ghost.StopMovement(false, true);
                }
                return;
            }

            var drug = other.GetComponent<PacmanDrug>();
            if (drug != null)
            {
                var eaten = drug.TryEat();
                if (eaten)
                {
                    MakeEatSound(true);
                    foreach (var g in Ghosts)
                    {
                        g.StartFlee(GhostTime);
                    }
                }
                return;
            }

            var food = other.GetComponent<YellowDelicious>();
            if (food != null)
            {
                var eaten = food.TryEat();
                if (eaten)
                {
                    MakeEatSound(false);
                    _foodEaten++;
                    _points++;
                    MakeNew(1, food.transform.position);
                    if (_foodEaten == _foodCount)//TODO _foodCount)
                    {
                        Ui.Display(_points, "YOU WON!", false, true);
                        _won = true;
                        End(2.5f);
                        WonSource.Play();
                        foreach (var g in Ghosts)
                        {
                            g.StopMovement(true, true);
                        }
                    } else
                        Ui.Display(_points, "MJAMM! +1", false, true);
                }
                return;
            }

            var cherry = other.GetComponent<PacmanCherry>();
            if (cherry != null)
            {
                var eaten = cherry.TryEatThis();
                if (eaten)
                {
                    MakeEatSound(true);
                    _points += CherryIncrement;
                    MakeNew(CherryIncrement, cherry.transform.position);
                    Ui.Display(_points, "CHERRY UP!", false, true);
                }
                return;
            }
        }

        private void MakeEatSound(bool special)
        {
            if (special)
            {
                EatSource.clip = SpecialSound;
            }else
            {
                if (++_eatSoundIndex == EatSounds.Length)
                    _eatSoundIndex = 0;
                EatSource.clip = EatSounds[_eatSoundIndex];
            }
        
            EatSource.Play();
        }

        private void End(float factor)
        {
            _gameOver = true;
            _timeBeforeStart = GameStartCountdown * factor;
        }

        private void StartGame()
        {
            _controlGroupChecked = false;
            _gameOver = false;
            _alreadyInit = false;
            _foodEaten = 0;
            _points = 0;
            var food = Food.GetComponentsInChildren<YellowDelicious>();
            foreach (var f in food)
                f.Reset();
            StartSource.Play();
            //foreach (var a in Animators)
            //    a.Play("same");
            foreach (var g in Ghosts)
            {
                g.StartNormal();
                g.InitPosition(transform.position);
            }
        }
    }
}

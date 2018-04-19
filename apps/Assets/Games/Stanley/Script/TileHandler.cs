using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VirtualSpace;
using VirtualSpace.Shared;
using Random = UnityEngine.Random;

public class TileHandler : MonoBehaviour
{
    // references
    public VirtualSpaceHandler VsHandler;
    private VirtualSpaceCore _vsCore;

    public PlayMakerFSM GameLogic;

    // state
    //private List<Tile> _tiles;
    //private Tile[,] _tilesArray;
    private int _numRows;
    private int _numColumns;
    private List<int> _freeTileIds;
    private List<int> _ownedTileIds;
    private int _currentPlayerTileId = -1;
    private int _initialPlayerTileId;

    public Transform ExpectedCenter;

    // initialize
    private bool _receivedTileProperties = false;
    private bool _initializedProperties = false;

    private bool _receivedTileStatusOnce;
    
    // Interface
    public Action OnStart;

    //public Tile CurrentPlayerTile
    //{
    //    get
    //    {
    //        if (_tiles == null ||
    //            _currentPlayerTileId < 0 || _currentPlayerTileId > _tiles.Count)
    //            return null;
    //        return _tiles[_currentPlayerTileId];
    //    }
    //}
    //[SerializeField]
    //public Vector3 CurrentTilePosition
    //{
    //    get
    //    {
    //        return CurrentPlayerTile.UnityCenter;
    //    }
    //}

    public Vector DesiredInitialTileDirection = new Vector(-1, -1);
    private int _numIndices;

    void Start ()
	{
	    VsHandler = FindObjectOfType<VirtualSpaceHandler>();

	    _vsCore = VirtualSpaceCore.Instance;

	    //_vsCore.AddHandler(typeof(TilesInfo), OnTilesProperties);
	    //_vsCore.AddHandler(typeof(TileStatusInfo), OnTileStatusInfo);
	    //_vsCore.AddHandler(typeof(TilesGranted), OnTilesGranted);
    }

    // initialize
    void OnTilesProperties(IMessageBase baseMessage)
    {
        //var tilesInfo = (TilesInfo) baseMessage;
        //_tiles = tilesInfo.Tiles;

        //_numRows = tilesInfo.Tiles.Max(tile => tile.RowNum) + 1;
        //_numColumns = tilesInfo.Tiles.Max(tile => tile.ColumnNum) + 1;
        //_numIndices = _tiles.Count;

        _receivedTileProperties = true;
    }

    public Vector3 Translate(Vector vector, float rotation)
    {
        return vector.Rotate(rotation).ToVector3();
    }

    public List<Vector3> Translate(List<Vector> list, float rotation)
    {
        return list.Select(vector => Translate(vector, rotation)).ToList();
    }

    public int TransformIndex(int index)
    {
        return (index - _initialPlayerTileId + _numIndices) % _numIndices;
    }
    
    public void InitializeProperties()
    { 
        //_tilesArray = new Tile[_numRows, _numColumns];
        
        //var initialPlayerTile = CurrentPlayerTile;
        //var initialCenter = initialPlayerTile.Area.Center;
        
        //var translationRadians = (float)initialCenter.Angle(DesiredInitialTileDirection);

        //var desiredClockwiseNormal = new Vector(
        //    -DesiredInitialTileDirection.Z,
        //    DesiredInitialTileDirection.X);

        //var quadrantMultiplier = 1;
        //if (desiredClockwiseNormal * initialCenter < 0)
        //{
        //    quadrantMultiplier = -1;
        //}

        //translationRadians *= quadrantMultiplier;
        
        //VsHandler.ChangeCoordinateSystem(translationRadians, Vector.Zero, true);

        //Debug.Log("Angle offset: " + translationRadians);

        //translationRadians *= -1;

        //foreach (var tile in _tiles)
        //{
            // we do not use vsUandler translation because the translation there is 
            // independent on the tile handler
            // in fact, strategies should be responsible for assigning this
            // todo make this amazingly easy to use and understand
            // todo this should probably change the VS Handler?
            // rotate based on the initial tile
            // this rotation needs to be applied somewhere of course
            // todo rotate our system
            // todo apply index offset to all incoming indices so we can assume that we are the center of the world

            //tile.Id -= _initialPlayerTileId;
            // todo tile and row num depend on rotation 
            
        //    tile.UnityArea = Translate(tile.Area, translationRadians);
        //    tile.UnityCenter = Translate(tile.Area.Center, translationRadians);
            
        //    _tilesArray[tile.RowNum, tile.ColumnNum] = tile;
        //}

        //ExpectedCenter.position = CurrentPlayerTile.UnityCenter;
    }

    // only report on granted?
    // regular update about the status of the tiles
    void OnTileStatusInfo(IMessageBase baseMessage)
    {
        //var tilesInfo = (TileStatusInfo)baseMessage;

        //_freeTileIds = tilesInfo.FreeTileIds;
        //_ownedTileIds = tilesInfo.OwnedTileIds;

        //if (!_receivedTileStatusOnce)
        //{
        //    _initialPlayerTileId = _currentPlayerTileId = _ownedTileIds.First();
        //    _receivedTileStatusOnce = true;
        //}

        //_receivedTileStatus = true;
    }

    void OnTilesGranted(IMessageBase baseMessage)
    {
        //Debug.Log("Granted received");
        //var tilesInfo = (TilesGranted)baseMessage;

        //if (tilesInfo.TileIds == null || tilesInfo.TileIds.Count == 0)
        //{
        //    Debug.LogWarning("received empty granted list");
        //    return;
        //}

        //// change current tile, only one in request so far
        //_currentPlayerTileId = tilesInfo.TileIds.First();

        //if (_requestGrantedCallback == null)
        //{
        //    Debug.LogWarning("Callback is null");
        //}
        
        //if (_requestGrantedCallback != null)
        //    _requestGrantedCallback.Invoke();
        //_requestGrantedCallback = null;
    }

    // request not granted...
    // rerequest

    enum RequestType
    {
        CounterClockwise
    }

    private RequestType _lastRequestType;
    private Action _requestGrantedCallback;

    public void Release(int previousId)
    {
        //TilesRelease request = new TilesRelease
        //{
        //    TileIds = new List<int> { previousId }
        //};
        //Debug.Log("Releasing tile " + previousId);
        
        //_vsCore.SendReliable(request);
    }

    public void RequestCounterclockwise(Action callback)
    {
        //CurrentPlayerTile
        // request tile
        //_requestGrantedCallback = callback;

        //_lastRequestType = RequestType.CounterClockwise;
        
        //var requestTile = 
        //    _tilesArray[1 - CurrentPlayerTile.ColumnNum, CurrentPlayerTile.RowNum];

        //Debug.Log("Requesting tile " + requestTile.Id);

        //TilesRequest request = new TilesRequest
        //{
        //    TileIds = new List<int> { requestTile.Id }
        //};

        //_vsCore.SendReliable(request);
    }

    private bool _receivedTileStatus;
    private bool _ready;
    void Update ()
    {
        if (_receivedTileProperties && _receivedTileStatusOnce && !_initializedProperties)
        {
            InitializeProperties();
            _initializedProperties = true;
        }

        if (!_initializedProperties) return;
        
        if (!_ready)
        {
            Debug.Log("Ready");
            GameLogic.Fsm.Event("ReceivedStartupInformation");
            if (OnStart != null)
                OnStart.Invoke();
            _ready = true;
        }

        if (_receivedTileStatus)
        {
            // received new status
        }
    }
}


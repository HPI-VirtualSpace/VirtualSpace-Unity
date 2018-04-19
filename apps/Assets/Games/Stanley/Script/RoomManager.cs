using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VirtualSpace.Shared;

// this needs to manage switching rooms as well as asking for space from the tile manager

public class RoomManager : MonoBehaviour
{
    public TileHandler TileHandler;

    // room structure
    public List<RoomControl> Rooms;
    public List<GameObject> Lasers;

    void OnEnable()
    {
        Debug.Log("OnEnable Room Manager called");

        OnInitialTile();
        //TileHandler.OnStart += OnInitialTile;
    }

    void OnDisable()
    {
        //TileHandler.OnStart -= OnInitialTile;
    }
    
    private const float DistanceToZeroToBeZero = .2f;
    private readonly Vector3 DebugRayElevation = new Vector3(0, .5f, 0);

    private LinkedList<int> _idHistory = new LinkedList<int>();

    // on the initial tile we need to do the mapping
    // tile index to our index
    // or better tile position, to our position
    void OnInitialTile()
    {

    }

    private void ActivateActiveRoom()
    {

    }
    
    public void Entered(RoomControl roomControl)
    {
        // deactivate previous
        // release


        if (_idHistory.Count >= 2)
        {
            var previousId = _idHistory.Last.Previous.Value;

            TileHandler.Release(previousId);

            Lasers[previousId].SetActive(true);

            // reset the room
        }
        
    }

    // request tile from tile manager
    public void AboutToFinish(RoomControl roomControl)
    {
        Debug.Log("About to finish called by " + roomControl.GetType());
        TileHandler.RequestCounterclockwise(OnRequestGranted);
    }

    private bool _canGoRight;
    public void OnRequestGranted()
    {
        _canGoRight = true;
    }

    private bool _userFinished;
    public void UserFinished(RoomControl roomControl)
    {
        Debug.Log("User finished called by " + roomControl.GetType());

        _userFinished = true;
    }

    // release tile when in next room

    public void Update()
    {

        if (_userFinished && _canGoRight)
        {
            // enable next room
            ActivateActiveRoom();

            _userFinished = false;
            _canGoRight = false;
        }
    }

    //private static void GetInnerBoundaries(List<Vector3> ownedArea,
    //    out Vector3 zeroPoint, out Vector3 leftBoundary, out Vector3 rightBoundary)
    //{
    //    var zeroIndex = ownedArea.FindIndex(vector =>
    //        Vector3.Distance(vector, Vector3.zero) < DistanceToZeroToBeZero);
    //    zeroPoint = ownedArea[zeroIndex];
    //    var numCoords = ownedArea.Count;
    //    var beforeIndex = (zeroIndex - 1 + numCoords) % numCoords;
    //    var afterIndex = (zeroIndex + 1) % numCoords;
    //    leftBoundary = ownedArea[beforeIndex] - zeroPoint;
    //    rightBoundary = ownedArea[afterIndex] - zeroPoint;
    //}

    //private void DebugOrientation()
    //{
    // boundaries
    //Vector3 zeroPoint;
    //Vector3 leftBoundary;
    //Vector3 rightBoundary;
    //GetInnerBoundaries(ownedArea, out zeroPoint, out leftBoundary, out rightBoundary);

    //Debug.Log("Tile center: " + ownedCenter);
    //Debug.Log("zero: " + zeroPoint + " left: " + leftBoundary + " right: " + rightBoundary);

    //Debug.DrawRay(zeroPoint + DebugRayElevation, leftBoundary, Color.green, 10);
    //Debug.DrawRay(zeroPoint + DebugRayElevation, rightBoundary, Color.red, 10);
    //Debug.DrawRay(zeroPoint + DebugRayElevation, Vector3.right, Color.gray, 10);

    //{
    //    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
    //    cube.name = "TileCenterCube";
    //    cube.transform.parent = transform;
    //    cube.transform.position = ownedCenter;
    //}
    //{
    //    var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
    //    cylinder.name = "BottomLeftTransformedCylinder";
    //    cylinder.transform.position = TileHandler.VsHandler._TranslateIntoUnityCoordinates(new Vector(-1.5, -1.5));
    //    cylinder.transform.parent = transform;
    //    //Debug.Log("cylinder position " + cylinder.transform.position);
    //}
    //}

}

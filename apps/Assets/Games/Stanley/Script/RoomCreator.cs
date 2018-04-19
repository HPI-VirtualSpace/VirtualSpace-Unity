using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WallType
{
    Door,
    Socket,
    Closed
}

public class RoomCreator : MonoBehaviour
{
    [Header("Generation Templates")]
    public GameObject Floor;

    public GameObject WallWithoutDoorOnLeft;
    public GameObject WallWithDoorSocket;
    public GameObject WallWithDoor;

    //-> room dimensions
    public Vector2 RoomDimensions;

    [Header("Debug Generation")]
    public Vector2 Center;

    public bool CreateOnce;


    void Start()
    {
        // debug
        //{
        //    // bottom left
        //    WallType[] wallTypes = new WallType[]
        //    {
        //        WallType.Closed, WallType.Door, WallType.Closed, WallType.Closed
        //    };
        //    CreateRoom(new Vector2(-1, -1), RoomDimensions, wallTypes);
        //}
        //{
        //    // bottom right
        //    WallType[] wallTypes = new WallType[]
        //    {
        //        WallType.Closed, WallType.Closed, WallType.Closed, WallType.Socket
        //    };
        //    CreateRoom(new Vector2(1, -1), RoomDimensions, wallTypes);
        //}

        // need info for room -> door socket? door? wall?
        // start at initial tile
        // but first, i need dorsies
    }
    
	void Update () {
	    //if (CreateOnce)
	    //{
	    //    CreateRoom(Center, RoomDimensions);
	    //    CreateOnce = false;
	    //}	
	}

    public Room CreateRoom(Vector3 center, WallType[] wallTypes)
    {
        return CreateRoom(new Vector2(center.x, center.z), RoomDimensions, wallTypes);
    }

    Room CreateRoom(Vector2 center, Vector2 dimensions, WallType[] wallTypes)
    {
        Vector3 center3 = new Vector3(center.x, 0, center.y);
        Vector3 dimensions3 = new Vector3(dimensions.x, 0, dimensions.y);
        return CreateRoom(center3, dimensions3, wallTypes);
    }

    GameObject GetWallTemplate(WallType wallType)
    {
        switch (wallType)
        {
            case WallType.Socket:
                return WallWithDoorSocket;
            case WallType.Door:
                return WallWithDoor;
            default:
                return WallWithoutDoorOnLeft;
        }
    }

    Room CreateRoom(Vector3 center, Vector3 dimensions, WallType[] wallTypes)
    {
        var room = new GameObject();
        room.name = "GeneratedRoom";
        var roomComponent = room.AddComponent<Room>();
        room.transform.position = center;
        room.transform.parent = transform;

        Vector3 floorY = new Vector3(0, Floor.transform.position.y, 0);
        var floor = Instantiate(Floor, center + floorY, Quaternion.identity);
        floor.transform.parent = room.transform;

        // create four walls at the right positions!!!!
        // two overlapping walls should be seemless

        // top (maxz, zerox)
        {
            var wallTemplate = GetWallTemplate(wallTypes[0]);
            Vector3 topOffset = new Vector3(0, 0, dimensions.z / 2);
            Vector3 topRotation = new Vector3(0, 90, 0);
            Vector3 doorY = new Vector3(0, wallTemplate.transform.position.y, 0);
            var wallTop = Instantiate(wallTemplate, center + doorY + topOffset, Quaternion.Euler(topRotation));
            wallTop.transform.parent = room.transform;
        }
        // right (maxx, zeroz)
        {
            var wallTemplate = GetWallTemplate(wallTypes[1]);
            Vector3 rightOffset = new Vector3(dimensions.x / 2, 0, 0);
            Vector3 rightRotation = new Vector3(0, 180, 0);
            Vector3 doorY = new Vector3(0, wallTemplate.transform.position.y, 0);
            var wallTop = Instantiate(wallTemplate, center + doorY + rightOffset, Quaternion.Euler(rightRotation));
            wallTop.transform.parent = room.transform;
        }
        // bottom
        {
            var wallTemplate = GetWallTemplate(wallTypes[2]);
            Vector3 bottomOffset = new Vector3(0, 0, -dimensions.z / 2);
            Vector3 bottomRotation = new Vector3(0, 270, 0);
            Vector3 doorY = new Vector3(0, wallTemplate.transform.position.y, 0);
            var wallTop = Instantiate(wallTemplate, center + doorY + bottomOffset, Quaternion.Euler(bottomRotation));
            wallTop.transform.parent = room.transform;
        }
        // left
        {
            var wallTemplate = GetWallTemplate(wallTypes[3]);
            var wallTop = Instantiate(wallTemplate, wallTemplate.transform.position + center, wallTemplate.transform.localRotation);
            wallTop.transform.parent = room.transform;
        }

        return roomComponent;
    }
}

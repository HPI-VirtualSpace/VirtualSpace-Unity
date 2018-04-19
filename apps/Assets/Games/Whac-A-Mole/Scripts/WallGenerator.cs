using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class IListExtensions
{
    /// <summary>
    /// Shuffles the element order of the specified list.
    /// </summary>
    public static void Shuffle<T>(this IList<T> ts)
    {
        var count = ts.Count;
        var last = count - 1;
        for (var i = 0; i < last; ++i)
        {
            var r = UnityEngine.Random.Range(i, count);
            var tmp = ts[i];
            ts[i] = ts[r];
            ts[r] = tmp;
        }
    }
}

public class WallGenerator : MonoBehaviour {
    List<GameObject> wallComponents;

    public GameObject wallGameObject;
    public List<GameObject> fillerGameObjects;

    Bounds boundingBox;
    List<Bounds> fillerBoundingBoxes;
    Dictionary<GameObject, float> bounds;

    public List<Vector3> WallEdges = new List<Vector3>() { new Vector3(-2, 0, -2), new Vector3(2, 0, -2), new Vector3(2, 0, 2), new Vector3(-2, 0, 2) };


    private void Awake()
    {
        wallComponents = new List<GameObject>();

        bounds = new Dictionary<GameObject, float>();

        boundingBox = wallGameObject.GetComponentInChildren<Renderer>().bounds;
        bounds[wallGameObject] = boundingBox.size.x;

        fillerBoundingBoxes = new List<Bounds>();
        foreach (GameObject fillerObject in fillerGameObjects)
        {
            fillerBoundingBoxes.Add(fillerObject.GetComponentInChildren<Renderer>().bounds);
            //Debug.Log("Bounds: " + fillerObject.GetComponentInChildren<Renderer>().bounds);
            bounds[fillerObject] = fillerObject.GetComponentInChildren<Renderer>().bounds.size.x / 4;
        }

        RedrawWalls();
    }

    void RedrawWalls()
    {
        foreach (GameObject wallComponent in wallComponents)
        {
            Destroy(wallComponent);
        }

        int numNodes = WallEdges.Count;
        if (numNodes <= 1) return;

        for (int i = 0; i < numNodes - 1; i++)
        {
            GenerateWall(WallEdges[i], WallEdges[i + 1]);
        }
        //Debug.Log("nodes[numNodes - 1] = " + nodes[numNodes - 1].transform.position);
        GenerateWall(WallEdges[numNodes - 1], WallEdges[0]);
    }

    public Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
    {
        Vector3 dir = point - pivot; // get point direction relative to pivot
        dir = Quaternion.Euler(angles) * dir; // rotate it
        point = dir + pivot; // calculate rotated point
        return point; // return it
    }

    private void GenerateWall(Vector3 point1, Vector3 point2)
    {
        Vector3 wallVector = point2 - point1;
        Vector3 wallDirection = wallVector.normalized;
        
        Quaternion wallRotation = Quaternion.LookRotation(wallDirection) * Quaternion.Euler(new Vector3(0, 90, 0));

        float remainingLength = wallVector.magnitude;
        List<GameObject> gameObjectsToPlace = new List<GameObject>();
        while (remainingLength > 0)
        {
            if (remainingLength - boundingBox.size.x >= 0)
            {
                gameObjectsToPlace.Add(wallGameObject);
                
                remainingLength -= boundingBox.size.x;
            }
            else
            {
                bool foundFiller = false;
                for (int i = 0; i < fillerGameObjects.Count; i++) {
                    float size = fillerBoundingBoxes[i].size.x / 4;

                    if (remainingLength - size >= 0)
                    {
                        gameObjectsToPlace.Add(fillerGameObjects[i]);

                        remainingLength -= size;
                        foundFiller = true;
                    }
                }
                if (!foundFiller)
                    remainingLength = 0;
            }
        }

        gameObjectsToPlace.Shuffle();

        float placedLength = 0;
        foreach (GameObject objectToPlace in gameObjectsToPlace)
        {
            Vector3 placementPoint = point1
                + wallDirection * placedLength;

            GameObject wallComponent = Instantiate(objectToPlace, placementPoint, wallRotation);
            wallComponent.transform.parent = transform;
            wallComponents.Add(wallComponent);
            placedLength += bounds[objectToPlace];
        }
    }

    void Update () {
		
	}
}

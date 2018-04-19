using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tennis3DFrontHint : MonoBehaviour
{

    public CurvedLineRenderer CurvedLineRenderer;
    public LineRenderer Renderer;
    public GameObject Antagonist;
    public Transform EnemyRacket;
    public Transform EndPoint;
    public float Height;
    public Transform[] FrontTrailPoints;

	void Update ()
	{
        var enabled = !Antagonist.activeSelf && (Time.time - Mathf.FloorToInt(Time.time)) * 0.8f > 0.4f;
	    Renderer.enabled = enabled;

        if (enabled)
        {
            var middle = EnemyRacket.position + (EndPoint.position - EnemyRacket.position) * 0.5f + Vector3.up * Height;
            FrontTrailPoints[0].position = EnemyRacket.position;
            FrontTrailPoints[1].position = middle;
            FrontTrailPoints[2].position = EndPoint.position;

            CurvedLineRenderer.Reset();
        }
    }
}

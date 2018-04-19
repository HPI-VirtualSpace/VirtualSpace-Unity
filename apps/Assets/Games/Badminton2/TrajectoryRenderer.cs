using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualSpace.Badminton
{
    [RequireComponent(typeof(LineRenderer))]
    public class TrajectoryRenderer : MonoBehaviour
    {
        LineRenderer _renderer;

        void Start()
        {
            _renderer = GetComponent<LineRenderer>();
        }

        public void SetPoints(Vector3[] points)
        {
            _renderer.positionCount = points.Length;
            _renderer.SetPositions(points);
        }
    }
}
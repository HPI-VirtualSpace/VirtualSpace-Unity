using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VirtualSpaceVisuals;

namespace Assets.Games.SpaceInvaders.Scripts
{
    public class SpaceInvadersFancyFloorTile : MonoBehaviour
    {
        public List<SpaceInvadersFancyFloorTile> Neighbors;
        public float Resolve = 1f;
        public float NeighborsTell = 0.8f;
        public VirtualSpacePlayerArea Area;

        private static int Count;

        private int _count;
        private float _res;
        private Renderer _rend;
        private bool _told;
        public Color NoAlpha;
        public Color Alpha;
        public Color NoAlphaRed, AlphaRed;
        [HideInInspector]
        public bool Nogo;

        void Start ()
        {
            _res = -1f;
            _count = -1;
            _rend = GetComponent<Renderer>();
            _rend.material.color = NoAlpha;
        }
	
        void Update () {
            if (_res >= 0f)
            {
                _res -= Time.deltaTime;
                if (!_told && _res <= NeighborsTell)
                {
                    _told = true;
                    foreach (var n in Neighbors)
                    {
                        n.TriggerInternal(_count);
                    }
                }
                _rend.material.color = Nogo ? Color.Lerp(NoAlphaRed, AlphaRed, _res / Resolve)  : Color.Lerp(NoAlpha, Alpha, _res / Resolve);
            }
        }

        public void Trigger()
        {
            Count++;
            TriggerInternal(Count);
        }

        private void TriggerInternal(int count)
        {
            if (count <= _count)
                return;
            Nogo = Area.MeshPoints.Count < 3 || !PointInPolygon(transform.position, Area.MeshPoints);
            _count = count;
            _res = Resolve;
            _told = false;
        }

        public static bool PointInPolygon(Vector3 point, List<Vector3> polygon)
        {
            var rev = new List<Vector3>(polygon);
            point.y = 0f;
            // Get the angle between the point and the
            // first and last vertices.
            var maxPoint = rev.Count - 1;
            var totalAngle = Vector3.Angle(rev[maxPoint] - point, rev[0] - point);

            // Add the angles from the point
            // to each other pair of vertices.
            for (var i = 0; i < maxPoint; i++)
            {
                totalAngle += Vector3.Angle(rev[i] - point, rev[i + 1] - point);
            }
            // The total angle should be 2 * PI or -2 * PI if
            // the point is in the polygon and close to zero
            // if the point is outside the polygon.
            return (360 - Mathf.Abs(totalAngle) < 0.000001);
        }
    }
}

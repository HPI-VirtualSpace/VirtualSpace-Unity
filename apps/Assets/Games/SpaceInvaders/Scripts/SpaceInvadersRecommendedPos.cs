using System.Collections.Generic;
using UnityEngine;
using VirtualSpace;

namespace Games.SpaceInvaders.Scripts
{
    public class SpaceInvadersRecommendedPos : MonoBehaviour
    {

        //public float X, Y;
        public VirtualSpaceHandler Handler;
        public float UpdateRate;
        //public float Distance = 1f;
        //public bool ShouldBeInside;
        //public List<Vector3> Polygon;

        public Transform Standard;

        private float _next;
	
        void Update () {
            if (!(Time.time > _next)) return;
            _next = Time.time + UpdateRate;
            SetCenter();
            //while(!TrySetRandomPoint() && ++count < 5){}
        }

        private void SetCenter()
        {
            //var random = new Vector3(Random.Range(-X,X), 0f, Random.Range(-Y,Y));
            Vector3 center;
            if (!VirtualSpaceCore.Instance.IsRegistered())
            {
                center = Standard.position;
            }
            else
            {
                List<Vector3> presentArea;
                Vector3 presentCenter;
                Handler.SpaceAtTimeWithSafety(0f, 0f, out presentArea, out presentCenter);
                center = presentCenter;
                //foreach (var poly in Polygon)
                //{
                //    center += poly;
                //}
                //center /= Polygon.Count;
            }

            transform.position = center;
            //var inside = PointInPolygon(random, NoGo.MeshPoints);
            //if (inside == ShouldBeInside)
            //{
            //    transform.position = random;//(random - transform.position).normalized * Distance;
            //    return true;
            //}
            //return false;
        }
    
    }
}

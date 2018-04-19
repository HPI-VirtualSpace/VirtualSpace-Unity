using System.Collections.Generic;
using UnityEngine;

namespace Games.Tennis3D.Scripts
{
    public class Tennis3DServeHelperSpot : MonoBehaviour {

        public Transform Follow;
        public Vector3 OffsetFromGround;
        public VirtualSpaceHandler Handler;
        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.G))
                transform.position = GetSpot(0f, true);
        }
        public Vector3 GetSpot(float time, bool goThereImmediately)
        {
            var campos = Camera.main.transform.position;
            var pos = campos;
            pos.y = 0f;
            pos += OffsetFromGround;
            return pos;
            //List<Vector3> poly;
            //Vector3 pos = Vector3.one;
            ////Debug.Log("dfffff" + pos);
            //if (!Handler.SpaceAtTimeWithSafety(time, 0f, out poly, out pos))
            //{
            //    //Debug.Log("nothing found");
            //    pos = Follow.position;
            //    pos.y = 0f;
            //}
            //else
            //{
            //    //Debug.Log("sdfasdfsadf" + pos);
            //}

            //pos += OffsetFromGround;
            //transform.position = goThereImmediately ? pos : Vector3.Slerp(transform.position, pos, 0.5f);
            //return pos;
        }
    }
}

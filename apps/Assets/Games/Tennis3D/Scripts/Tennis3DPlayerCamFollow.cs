using UnityEngine;

namespace Games.Tennis3D.Scripts
{
    public class Tennis3DPlayerCamFollow : MonoBehaviour {

        void Update ()
        {
            var camPos = Camera.main.transform.position;
            var followPos = camPos;
            followPos.y = 0f;
            var camRot = Camera.main.transform.rotation.eulerAngles;
            var followRot = camRot;
            followRot.x = 0f;
            followRot.z = 0f;
            transform.rotation = Quaternion.Euler(followRot);
            transform.position = followPos;
        }
    }
}

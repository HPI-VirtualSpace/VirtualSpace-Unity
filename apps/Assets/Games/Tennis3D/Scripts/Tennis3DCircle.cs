using UnityEngine;

namespace Games.Tennis3D.Scripts
{
    public class Tennis3DCircle : MonoBehaviour {
    
        void Update ()
        {
            transform.localScale = Vector3.one * (Mathf.PingPong(Time.time, 0.8f) / 0.8f + 0.2f) * 0.1f;
        }
    }
}

using UnityEngine;

namespace Games.SpaceInvaders.Scripts
{
    public class SpaceInvadersRotating : MonoBehaviour {

        public float RotationSpeed = 1f;
        public SpaceInvadersGotHit UFO;

        void Start () {
		
        }

        void Update () {
            if(!UFO.IsDefeated())
                transform.Rotate(Vector3.up * RotationSpeed * Time.deltaTime);
        }
    }
}

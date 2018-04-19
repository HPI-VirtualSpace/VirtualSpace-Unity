using UnityEngine;

namespace Assets.Games.Scripts
{
    public class LookAtMainCamera : MonoBehaviour {
        private Transform LookedAt;

        void Start()
        {
            LookedAt = Camera.main == null ? null : Camera.main.transform;
        }

        void Update ()
        {
            if (LookedAt == null)
                return;
            Vector3 lookPosition = LookedAt.position;
            lookPosition.y = transform.position.y;

            transform.LookAt(lookPosition);
        }
    }
}

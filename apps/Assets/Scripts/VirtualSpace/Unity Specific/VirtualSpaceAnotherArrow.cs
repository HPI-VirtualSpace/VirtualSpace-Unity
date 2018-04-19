using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace VirtualSpaceVisuals
{
    public class VirtualSpaceAnotherArrow : MonoBehaviour
    {
        //public Transform ReferenceTransform;
        public float DistanceHudToCamera = 0.2f;
        public float PictureRotOffset = 270;
        public float SizeMax = 0.7f;
        public float Radius = 70f;
        [Range(0f, 1f)]
        public float Size;
        public float BounceExtend = 0.5f;
        public float BounceTime = 0.3f;

        [HideInInspector]
        public Vector3 CenterPoint;
        public RawImage ArrowImage;
        public RectTransform ArrowRect;

        void Start()
        {
            var cam = Camera.main.transform;
            var rot = transform.localRotation;
            var pos = transform.localPosition;
            transform.SetParent(cam);
            transform.localRotation = rot;// Quaternion.identity;
            transform.localPosition = new Vector3(0f, 0f, DistanceHudToCamera);

            StartCoroutine(Adapting());
        }

        private void Adapt(Transform reference)
        {
            var refPosition = reference.position;
            refPosition.y = 0f;
            CenterPoint.y = 0f;
            var vector = (CenterPoint - refPosition).normalized;
            Debug.DrawRay(refPosition, CenterPoint - refPosition);
            var angle = Vector3.Angle(vector, -reference.right) *
                        (Vector3.Cross(vector, -reference.right).y < 0f ? -1 : 1);
            var anglePi = Mathf.PI * (angle + 180f) / 180f;
            var locPos = new Vector3(Mathf.Cos(anglePi), Mathf.Sin(anglePi), 0f);
            ArrowRect.localPosition = locPos * Radius;
            ArrowRect.localScale = Size * SizeMax * Vector3.one * (1f + Mathf.PingPong(Time.unscaledTime, BounceTime) * BounceExtend);
            ArrowRect.localRotation = Quaternion.Euler(0f, 0f, angle + PictureRotOffset);
        }

        private IEnumerator Adapting()
        {
            while (true)
            {
                yield return null;

                Adapt(Camera.main.transform);
            }
            // ReSharper disable once IteratorNeverReturns
        }
    }
}

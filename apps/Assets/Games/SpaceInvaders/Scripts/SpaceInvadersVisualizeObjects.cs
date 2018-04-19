using System.Collections.Generic;
using System.Linq;
using Assets.Games.SpaceInvaders.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace Games.SpaceInvaders.Scripts
{
    public class SpaceInvadersVisualizeObjects : MonoBehaviour
    {
        public GameObject ShotVisual;
        public GameObject BonusVisual;
        public Transform ReferencePosition;
        public Transform BonusPosition;
        public float PictureRotOffset = 180f;
        public float SizeMax = 5f;
        public float DistanceMax = 2f;
        public float DistanceMin = 0.5f;
        public float Radius = 500f;

        private List<SpaceInvadersEnemyShot> _shotPositions;
        private List<RawImage> _shotImages;
        private List<RectTransform> _shotRects;
        private RawImage _bonusImage;

        public static SpaceInvadersVisualizeObjects Visualizer;

        void Awake () {
            _shotPositions = new List<SpaceInvadersEnemyShot>();
            _shotImages = new List<RawImage>();
            _shotRects = new List<RectTransform>();
            Visualizer = this;
        }

        public void Add(SpaceInvadersEnemyShot shot)
        {
            _shotPositions.Add(shot);
        }

        public void Remove (SpaceInvadersEnemyShot shot)
        {
            _shotPositions.Remove(shot);
        }
	
        void Update () {
            CheckShots();
            MoveShotVisuals();
        }

        private void CheckShots()
        {
            _shotPositions = _shotPositions.Where(sp => sp != null).ToList();
            while (_shotPositions.Count > _shotImages.Count)
            {
                var ri = Instantiate(ShotVisual);
                ri.transform.SetParent(transform);
                _shotRects.Add(ri.GetComponent<RectTransform>());
                _shotImages.Add(ri.GetComponent<RawImage>());
            }
            while (_shotPositions.Count < _shotImages.Count)
            {
                var index = _shotImages.Count - 1;
                var last = _shotImages[index];
                _shotRects.RemoveAt(index);
                _shotImages.RemoveAt(index);
                Destroy(last.gameObject);
            }
        }

        private void CheckBonus()
        {
        
        }

        private void MoveShotVisuals()
        {
            var refPosition = ReferencePosition.position;
            refPosition.y = 0f;
            var vectors = _shotPositions.Select(sp => (sp.transform.position - refPosition).normalized);
            var angles = vectors.Select(v => Vector3.Angle(v, -ReferencePosition.right) *
                                             (Vector3.Cross(v, -ReferencePosition.right).y < 0f ? -1 : 1)).ToList();
            var distances = _shotPositions.Select(sp => Vector3.Distance(sp.transform.position, refPosition)).ToList();
            for (var i = 0; i < angles.Count; i++)
            {
                var hits = Physics.RaycastAll(new Ray(_shotPositions[i].transform.position, _shotPositions[i].transform.forward), DistanceMax);
                var colliderPlayer = SpaceInvadersPlayerAvatar.PreHitTestCollider;
                var willHit = hits.Any(h => h.collider == colliderPlayer);
                if (willHit)
                {
                    var angle = angles[i];
                    var anglePi = Mathf.PI * (angle + 180f) / 180f;
                    var locPos = new Vector3(Mathf.Cos(anglePi), Mathf.Sin(anglePi), 0f);
                    _shotRects[i].localPosition = locPos * Radius;
                    _shotRects[i].gameObject.name = _shotPositions[i].name;
                    _shotRects[i].localScale = (1f - Mathf.Clamp01((distances[i] - DistanceMin) / (DistanceMax - DistanceMin))) * SizeMax *
                                               Vector3.one;
                    _shotRects[i].localRotation = Quaternion.Euler(0f, 0f, angle + PictureRotOffset);
                } else
                {
                    _shotRects[i].localScale = Vector3.zero;
                }
            }
        }
    }
}

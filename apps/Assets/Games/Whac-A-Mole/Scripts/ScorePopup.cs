using Assets.Games.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace Games.Scripts
{
    public class ScorePopup : MonoBehaviour {
        public Text Text;
        public Color NegativeColor;
        public Color PositiveColor;

        public int MinFontSize = 20;
        public int MaxFontSize = 40;
        public int MinPoints = 10;
        public int MaxPoints = 100;

        public int Points;

        public float UpwardsSpeed = .5f;
        public float FadeoutSeconds = 2f;

        private float _spawnTime;
        
        public float DisplaceTowardsPlayer = 0.05f;
        void Start ()
        {
            Text.text = (Points <= 0 ? "" : "+") + Points;

            var lerp = (Points - MinPoints) / (MaxPoints - MinPoints);
            Mathf.Clamp(lerp, 0, 1);
            var fontSize = (int) Mathf.Lerp(MinFontSize, MaxFontSize, lerp);
            Text.fontSize = fontSize;
            Text.color = Points < 0 ? NegativeColor : PositiveColor;

            var parent = transform.parent == null ? transform : transform.parent;
            var playerCamera = Camera.main == null ? parent : Camera.main.transform;

            // change position a little towards the player
            var textPosition = transform.position;
            var cameraPosition = playerCamera.position;
            cameraPosition.y = transform.position.y;
            //var meToCamera = cameraPosition - textPosition;
            //meToCamera = meToCamera.normalized * DisplaceTowardsPlayer;
            //transform.position += meToCamera;
            //transform.LookAt(cameraPosition);

            transform.up = Vector3.up;
            var forward = (cameraPosition - transform.position).normalized;
            forward.y = 0f;
            transform.forward = forward;

            _spawnTime = Time.time;
        }
	
        void Update () {
            if (Time.time - _spawnTime >= FadeoutSeconds)
            {
                Destroy(gameObject);
            }

            var alpha = 255 - 255 * (Time.time - _spawnTime) / FadeoutSeconds;
            byte byteAlpha = (byte) alpha;
            //Debug.Log("alpha: " + alpha + " byteAlpha: " + byteAlpha);
            var yOffset = Time.deltaTime * .5f;

            var position = transform.position;
            position.y += yOffset;
            transform.position = position;

            var faceColor = Text.color;
            // var outlineColor = Text.outlineColor;

            faceColor.a = byteAlpha;
            // outlineColor.a = byteAlpha;

            Text.color = faceColor;
            //Text.outlineColor = outlineColor;
        }
    }
}

using UnityEngine;

namespace Assets.Scripts.VirtualSpace.Unity_Specific
{
    public class DemoDisplayCamera : MonoBehaviour
    {
        public Camera Cam;
        public RenderTexture Tex;

        void Start ()
        {
            Cam.enabled = true;
            Cam.targetTexture = Tex;
        }

        public void SetTex(Vector2 offset, Vector2 position)
        {
            Cam.rect = new Rect(offset, position);
        }

        public void Toggle(string layer)
        {
            Cam.cullingMask ^= 1 << LayerMask.NameToLayer(layer);
        }

        public void Show(string layer)
        {
            Cam.cullingMask |= 1 << LayerMask.NameToLayer(layer);
        }

        public void Hide(string layer)
        {
            Cam.cullingMask &= ~(1 << LayerMask.NameToLayer(layer));
        }
    }
}

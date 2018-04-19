using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace VirtualSpaceVisuals
{
    public class VirtualSpaceMenuItem : MonoBehaviour
    {
        public Text Text;
        public bool Hover;
        public Color Selected;
        public Color NotSelected;
        public Collider Collider;
        [HideInInspector]
        public bool HitTest;
        public float MaxDistRay;

        private Transform _cam;

        private void Start()
        {
            _cam = Camera.main.transform;
        }

        private void Update()
        {
            if(!HitTest)
                return;

            var hits = Physics.RaycastAll(new Ray(_cam.position, _cam.forward), MaxDistRay);
            Debug.DrawRay(_cam.position, _cam.forward);
            var hit = hits.Select(h => h.collider).Contains(Collider);
            
            Hover = hit;
            Text.color = hit ? Selected : NotSelected;
            //if (hit)
            //{
            //    Debug.Log("Option was hit");
            //}
        }

        //public void OnPointerEnter(PointerEventData eventData)
        //{
        //    Hover = true;
        //    Text.color = Selected;
        //}
        //public void OnPointerLeave(PointerEventData eventData)
        //{
        //    Hover = false;
        //    Text.color = NotSelected;
        //}
    }
}

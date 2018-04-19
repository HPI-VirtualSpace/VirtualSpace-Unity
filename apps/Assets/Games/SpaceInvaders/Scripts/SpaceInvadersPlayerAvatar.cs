using Assets.ViveClient;
using UnityEngine;
using System.Collections.Generic;
using VirtualSpaceVisuals;

namespace Assets.Games.SpaceInvaders.Scripts
{
    public class SpaceInvadersPlayerAvatar : MonoBehaviour
    {
        public BoxCollider SaveBox;
        public VirtualSpaceHandler Handler;
        public Transform Controller;
        //public CameraRig CamRig;
        public List<Renderer> Renderer;
        public Color Green, Red;
        //public BoxCollider PreHitTest;
        [HideInInspector]
        public bool NoGo
        {
            set
            {
                if (value != _nogo)
                {
                    foreach (var rend in Renderer)
                    {
                        if(rend != null)
                            rend.material.color = _nogo ? Green : Red;
                    }
                }
                _nogo = value;
            }
            get { return _nogo; }
        }
        public static BoxCollider PreHitTestCollider;

        private bool _nogo;
        private bool _init;

        void Update()
        {
            if (!_init)
            {
                _init = true;
                PreHitTestCollider = SaveBox;
            }
            var local = Controller.localPosition;
            local.y = 0f;
            transform.localPosition = local;
            NoGo = !SaveBox.bounds.Contains(Camera.main.transform.position) || !SaveBox.bounds.Contains(Controller.position);

            //var camRot = _follow.transform.localRotation.eulerAngles;
            //camRot.x = 0f;
            //camRot.z = 0f;
            //transform.localRotation = Quaternion.Euler(camRot + RotOffset);

            //var head2Hand = Controller.transform.position - CamRig.Target.transform.position;
            //head2Hand.y = 0f;
            //transform.right = -head2Hand.normalized;

            var forward = Controller.forward;
            forward.y = 0f;
            transform.right = forward;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.VirtualSpace.Unity_Specific
{
    public class DemoDisplayPlayer : NetworkBehaviour
    {
        public DemoDisplayCamera Cam;
        public string ComponentName = "DemoDisplayComponent";
        public DemoDisplayComponent Component;
        public Vector3 Offset;

        private List<Transform> _clientObjectsToFollow;
        private List<Transform> _followObjects;
        private List<DemoDisplayInterpolatePosition> _interpolateObjects;
        private int _firstPersonIndex;
        private int _camIndex;

        private bool _init;
        
        void TryInit()
        {
            //Debug.Log("init player");
            if (isLocalPlayer)
                Component = GameObject.Find(ComponentName).GetComponent<DemoDisplayComponent>();
            else if (Component == null)
                return;

            _init = true;

            _followObjects = new List<Transform>();
            _interpolateObjects = new List<DemoDisplayInterpolatePosition>();
            _firstPersonIndex = Component.FirstPersonIndex;
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.parent == transform)
                {
                    if (_followObjects.Count < Component.FollowObjects.Count)
                    {
                        _followObjects.Add(child);
                    }
                    else if(_interpolateObjects.Count < _followObjects.Count)
                    {
                        var inter = child.GetComponent<DemoDisplayInterpolatePosition>();
                        _interpolateObjects.Add(inter);
                        if (child.GetComponent<DemoDisplayCamera>() != null)
                            _camIndex = _interpolateObjects.Count - 1;
                    }
                    
                    var idAdapt = child.GetComponent<DemoDisplayVrIkAdapt>();
                    if (idAdapt != null)
                    {
                        idAdapt.Headmount = _followObjects[_firstPersonIndex];
                        idAdapt.Ground = _followObjects[_camIndex];
                    }
                }
            }
            if (_interpolateObjects.Count != _followObjects.Count)
            {
                Debug.LogError("DemoDisplayPlayer: array sizes do not match, prefab invalid " + _interpolateObjects.Count + " " + _followObjects.Count);
            }

            if (isLocalPlayer)
            {
                _clientObjectsToFollow = Component.FollowObjects;
            }
            else
            {
                for (var i = 0; i < transform.childCount; i++)
                {
                    transform.GetChild(i).gameObject.SetActive(true);
                }
                for (var i = 0; i < _interpolateObjects.Count; i++)
                {
                    _interpolateObjects[i].ToFollow = _followObjects[i];
                }
            }
        }

        private bool _hasUpdate;
        private bool _isFirstPerson;
        private Vector3 _posOff;//, _rotOff;
        public void UpdateInfo(Vector3 positionalOffset, bool isFirstPerson)//Vector3 rotationalOffset, 
        {
            _hasUpdate = true;
            _isFirstPerson = isFirstPerson;
            _posOff = positionalOffset;
            //_rotOff = rotationalOffset;
        }

        public void TrySet()
        {
            if (!_hasUpdate)
                return;
            _hasUpdate = false;
            for (var i = 0; i < _interpolateObjects.Count; i++)
            {
                //var rotOff = Quaternion.identity;
                var posOff = Vector3.zero;
                var follow = _followObjects[i];
                var lookAtFollow = false;
                var clear = false;
                if (i == _camIndex)
                {
                    clear = true;
                    if (!_isFirstPerson)
                    {
                        //rotOff = Quaternion.Euler(_rotOff);
                        posOff = _posOff;
                        lookAtFollow = true;
                    }
                    else
                    {
                        follow = _followObjects[_firstPersonIndex].transform;
                    }
                }
                _interpolateObjects[i].PositionalOffset = posOff;
                //_interpolateObjects[i].RotationalOffset = rotOff;
                _interpolateObjects[i].ToFollow = follow;
                _interpolateObjects[i].LookAtFollow = lookAtFollow;
                _interpolateObjects[i].Clear = clear;
            }
        }

        void Update()
        {
            if (!_init)
            {
                TryInit();
                return;
            }

            if (isLocalPlayer)
            {
                for (var i = 0; i < _clientObjectsToFollow.Count; i++)
                {
                    _followObjects[i].localPosition = _clientObjectsToFollow[i].position;
                    _followObjects[i].localRotation = _clientObjectsToFollow[i].rotation;
                }
            }
            else
            {
                TrySet();
            }
        }
    }
}

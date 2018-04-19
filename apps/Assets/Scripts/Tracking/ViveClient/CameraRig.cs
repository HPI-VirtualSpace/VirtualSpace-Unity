using System;
using System.Collections;
using System.Linq;
using Assets.ViveClient;
using UnityEngine;
using VirtualSpaceVisuals;

namespace Assets.ViveClient
{
    public class CameraRig : MonoBehaviour {
        [HideInInspector]
        public bool Tracked;
        [HideInInspector] public GameObject Target;
        private int _playerIndex;
        [HideInInspector] public int PlayerPostfix;

        public string HmdPrefix = "HMD";
        public SteamVrReceiver Receiver;
       // public Text DebugText;
        //public Transform DebugFollow;
        public VirtualSpacePreferenceSender VsSender;

        private bool _isCalibrated;
        private const float TimeForCalibration = 3.0f;
        private float _calibratingTime;
        private const float AngleOffsetThreshold = 3.0f;
        private Quaternion _angleAxisAddRot;
        private Transform _camTrans;
       // private float _timeFade;

        [HideInInspector] public VideoSettings CurrentVideoSetting;

        public enum VideoSettings
        {
            LowMobile,
            HighMobile,
            EditorPreview
        }

        public void Awake()
        {
            Tracked = true;
            _playerIndex = -1;
            PlayerPostfix = -1;
            SetGraphicsQuality(VideoSettings.HighMobile);
            UnityEngine.XR.InputTracking.disablePositionalTracking = true;
        }

        public void Start()
        {
            transform.SetParent(null, true);
            _camTrans = Camera.main.transform;
            _camTrans.SetParent(transform, false);
            _camTrans.localPosition = Vector3.zero;
            transform.position = Vector3.up;
        }

        //private void Update()
        //{
        //    //if(Input.GetMouseButtonDown(0))
        //    //    StartFollowingNext();
        //    if(_timeFade > 0f)
        //    {
        //        _timeFade -= Time.deltaTime;
        //        if (_timeFade <= 0f)
        //        {
        //            DebugText.text = "";
        //        }
        //    }
        //}

        public void StartFollowingNext()
        {
           // if(DebugFollow == null)
           // {
            _angleAxisAddRot = Quaternion.identity;
            var targets = Receiver.GetAllTrackables().Where(r => r.name.StartsWith(HmdPrefix)).ToList();
            targets = targets.OrderBy(go => go.name).ToList();
            if (targets.Count == 0)
            {
                Debug.Log("Couldn't find trackables with HMD prefix");
                return;
            }
            //if (Target != null)
            //    targets.Remove(Target);


            if (++_playerIndex >= targets.Count)
                _playerIndex = 0;
            Target = targets[_playerIndex];
            var post = Target.name;
            post = post.Substring(HmdPrefix.Length);
            PlayerPostfix = Int32.Parse(post);
            VsSender.UpdatePreferences(Target.name);
            Debug.Log("Attaching camera to " + Target.name);
            //if (DebugText != null)
            //{
            //    DebugText.text = Target.name;
            //    _timeFade = 1f;
            //}
            //}
            //else
            //{
            //    Target = DebugFollow.gameObject;
            //}
            TryStartFollowing();
        }

        public void TryStartFollowingFromPostfix(string name)
        {
            _angleAxisAddRot = Quaternion.identity;
            var targets = Receiver.GetAllTrackables().Where(r => r.name.StartsWith(HmdPrefix)).ToList();
            //var postFixString = postfix.ToString();
            var target = targets.FirstOrDefault(t => t.name == name);//t => t.name.Length == (HmdPrefix.Length + postFixString.Length) &&
                                                                     //t.name.EndsWith(postFixString));
            if (target == null)
            {
                return;
            }
            var post = target.name.Substring(HmdPrefix.Length);
            PlayerPostfix = Int32.Parse(post);
            Target = target;
            VsSender.UpdatePreferences(Target.name);

            //PlayerPostfix = postfix;
            VsSender.UpdatePreferences(Target.name);

            TryStartFollowing();
        }

        private void TryStartFollowing()
        {
            if (Target == null)
                return;
            StopAllCoroutines();
            StartCoroutine(Following(Target));
        }
        
        public void SetGraphicsQuality(VideoSettings vs)
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            if (Application.isEditor)
            {
                vs = VideoSettings.EditorPreview;
            }
            switch (vs)
            {
                case VideoSettings.EditorPreview:
                    UnityEngine.XR.XRSettings.eyeTextureResolutionScale = 1f;
                    Application.targetFrameRate = -1;
                    QualitySettings.vSyncCount = 0;
                    break;
                case VideoSettings.HighMobile:
                    UnityEngine.XR.XRSettings.eyeTextureResolutionScale = 0.7f;
                    Application.targetFrameRate = 60;
                    QualitySettings.vSyncCount = 1;
                    break;
                case VideoSettings.LowMobile:
                    UnityEngine.XR.XRSettings.eyeTextureResolutionScale = 0.4f;
                    Application.targetFrameRate = 30;
                    QualitySettings.vSyncCount = 2;
                    break;
                default:
                    UnityEngine.XR.XRSettings.eyeTextureResolutionScale = 1f;
                    break;
            }
            CurrentVideoSetting = vs;
        }

        private IEnumerator Following(GameObject target)
        {
            while (true)
            {
                yield return null;

                TrySetToTarget(target);
            }
            // ReSharper disable once IteratorNeverReturns
        }

        private void TrySetToTarget(GameObject target)
        {
            if (target == null)
                return;
            transform.position = target.transform.position;
            _camTrans.localPosition = Vector3.zero;
            Tracked = !target.CompareTag("untracked");
            if (!Tracked) { }
            else
            {
                if (Application.isEditor)
                {
                    transform.rotation = target.transform.rotation * _angleAxisAddRot;//since cam doesn't move

                    var cam = transform.GetChild(0);
                    cam.localRotation = Quaternion.identity;
                }
                else
                {
                    //constantly correct orientation from optitrack -- problematic with drop outs
                    Quaternion twist;
                    Quaternion swing;
                    var targetRot = target.transform.rotation * _angleAxisAddRot;// angleAxisAddRot;// Quaternion.Euler(eulerOffset);
                    SwingTwistDecomposition(targetRot, Vector3.up, out twist, out swing);

                    var child = _camTrans;//transform.GetChild(0);

                    Quaternion cameraTwist;
                    Quaternion cameraSwing;
                    SwingTwistDecomposition(child.transform.localRotation, Vector3.up, out cameraTwist, out cameraSwing);

                    var offset = Quaternion.Angle(transform.rotation, twist * Quaternion.Inverse(cameraTwist));

                    if (offset > AngleOffsetThreshold)
                    {
                        _isCalibrated = false;
                        _calibratingTime = 0.0f;
                    }
                    else if (_calibratingTime < TimeForCalibration)
                    {
                        _calibratingTime += Time.deltaTime;
                    }
                    else//if (!isCalibrated && offset < angleOffsetJumpBack)
                    {
                        transform.rotation = twist * Quaternion.Inverse(cameraTwist);
                        _isCalibrated = true;
                    }

                    if (!_isCalibrated)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, twist * Quaternion.Inverse(cameraTwist), 0.01f);
                    }

                    //Debug.Log("local cam " + child.localPosition);
                    //Debug.Log("global cam " + child.position);
                }
            }
            transform.position = target.transform.position;
        }

        //private void ResetOrientation(){
        //    Quaternion twist;
        //    Quaternion swing;
        //    SwingTwistDecomposition(Target.transform.rotation, Vector3.up, out twist, out swing);

        //    var child = transform.GetChild(0);

        //    Quaternion cameraTwist;
        //    Quaternion cameraSwing;
        //    SwingTwistDecomposition(child.transform.localRotation, Vector3.up, out cameraTwist, out cameraSwing);

        //    transform.rotation = twist * Quaternion.Inverse(cameraTwist);
        //}

        //private static void SetTintColor(Material mat, Color c) {
        //	mat.SetColor("_TintColor", c);
        //}

        private void SwingTwistDecomposition(Quaternion q, Vector3 v, out Quaternion twist, out Quaternion swing){
            var rotationAxis = new Vector3(q.x, q.y, q.z);
            var projection = Vector3.Project(rotationAxis, v);
            var magnitude = Mathf.Sqrt(Mathf.Pow(projection.x, 2) + Mathf.Pow(projection.y, 2) + Mathf.Pow(projection.z, 2) +Mathf.Pow(q.w, 2));
            twist = new Quaternion(projection.x/magnitude, projection.y/magnitude, projection.z/magnitude, q.w/magnitude);
            var twistConjugated = new Quaternion(-projection.x/magnitude, -projection.y/magnitude, -projection.z/magnitude, q.w/magnitude);
            swing = q * twistConjugated;
        }
    }
}

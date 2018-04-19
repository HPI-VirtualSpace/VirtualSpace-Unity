using Assets.ViveClient;
using Assets.ViveClient;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System;

namespace VirtualSpaceVisuals
{
    public class VirtualSpaceMenuFollow : MonoBehaviour
    {
        public CameraRig CamRig;
        public GameObject Menu;
        public VirtualSpaceMenuItem PlayerText;
        public VirtualSpaceMenuItem RotationText;
        public VirtualSpaceMenuItem SceneText;
        public VirtualSpaceMenuItem QualityText;
        public VirtualSpaceMenuItem DebugText;
        public VirtualSpaceMenuItem ControlText;
        public VirtualSpaceMenuItem QuitText;
        public ReadSceneNames SceneNames;
        public SteamVrReceiver TrackingUdp;
        public AsyncTcpSocket TrackingTcp;
        public GameObject DebugObject;
        public float TryLoadAfterStart = 3f;
        public VirtualSpaceHandler Handler;
        public VirtualSpacePlayerArea Area;
        public VirtualSpacePreferenceSender PrefSender;

        private Vector3 _lastHandlerPos;
        private int _sceneIndex;
        private Transform _cam;
        private bool _hasTriedToLoad;
        private float _startTime;
        private bool _control;
        private bool _loadNewScene;
        private bool _controlSet;

        public bool IsControlCondition()
        {
            return _control;
        }

        void Start ()
        {
            _startTime = Time.time;
            transform.parent = null;
            Menu.SetActive(false);
            _cam = Camera.main.transform;

            var sceneName = SceneManager.GetActiveScene().name;
            _sceneIndex = SceneNames.scenes.ToList().IndexOf(sceneName);
            PrefSender.UpdatePreferenceSceneColor(sceneName, Handler.Settings.BackendColor);

            CamRig.StartFollowingNext();

            //LoadPrefs();
            //ResetVisuals();
        }

        private void ResetVisuals()
        {
            SceneText.Text.text = "scene: " + SceneNames.scenes[_sceneIndex];//SceneManager.GetActiveScene().name;
            RotationText.Text.text = "rotation: " + Handler.transform.rotation.eulerAngles.y;
            PlayerText.Text.text = "player: " + CamRig.PlayerPostfix;
            DebugText.Text.text = "debug: " + (DebugObject.activeSelf ? "on" : "off");
            ControlText.Text.text = "control: " + (_control ? "true" : "false");
            QualityText.Text.text = "quality: " + (CamRig.CurrentVideoSetting == CameraRig.VideoSettings.HighMobile ? "high" : "low");
        }

        private void SavePrefs()
        {
            PlayerPrefs.SetInt("QualitySetting", (int) CamRig.CurrentVideoSetting);
            PlayerPrefs.SetInt("DebugSetting", DebugObject.activeSelf ? 1 : 0);
            if(CamRig.Target != null)
                PlayerPrefs.SetString("FollowSetting", CamRig.Target.name);
        }

        private void LoadPrefs()
        {
            var videoSetting = (CameraRig.VideoSettings) PlayerPrefs.GetInt("QualitySetting");
            var debugSetting = PlayerPrefs.GetInt("DebugSetting") == 1;
            var followSetting = PlayerPrefs.GetString("FollowSetting");

            CamRig.SetGraphicsQuality(videoSetting);
            DebugObject.SetActive(debugSetting);
            CamRig.TryStartFollowingFromPostfix(followSetting);
        }

        private void SetControl()
        {
            if (_controlSet)
                return;
            if (_control)
            {
                _controlSet = true;
                   _lastHandlerPos = Handler.transform.position;
                _lastHandlerPos.y = 0f;
                var newCenter = Area.CenterOfArea;
                newCenter.y = 0f;
                var offset = newCenter - _lastHandlerPos;
                Handler.transform.position = _lastHandlerPos - offset;
            }
            else
            {
                Handler.transform.position = _lastHandlerPos;
            }
        }
	
        void Update ()
        {
            transform.position = _cam.position;
            var rotation = _cam.rotation.eulerAngles;
            rotation.x = 0f;
            rotation.z = 0f;
            transform.rotation = Quaternion.Euler(rotation);

            if (!_hasTriedToLoad && Time.time- _startTime > TryLoadAfterStart)
            {
                _hasTriedToLoad = true;
                LoadPrefs();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _hasTriedToLoad = true;

                Menu.SetActive(!Menu.activeSelf);
                PlayerText.HitTest = Menu.activeSelf;
                RotationText.HitTest = Menu.activeSelf;
                SceneText.HitTest = Menu.activeSelf;
                QuitText.HitTest = Menu.activeSelf;
                ControlText.HitTest = Menu.activeSelf;
                QualityText.HitTest = Menu.activeSelf;
                DebugText.HitTest = Menu.activeSelf;

                if (!Menu.activeSelf)
                {
                    //var newScene = SceneManager.GetActiveScene().name != SceneNames.scenes[_sceneIndex];
                    if (_loadNewScene)
                    {
                        SceneManager.LoadScene(_sceneIndex);
                        TrackingUdp.Quit();
                        TrackingTcp.Quit();
                    }
                }
                else
                {
                    ResetVisuals();
                }
            }

            if (Application.isEditor)
            {
                if (Input.GetKeyDown(KeyCode.P))
                {
                    SwitchPlayer();
                }
                if (Input.GetKeyDown(KeyCode.T))
                {
                    RotateEnvironment();
                }
                if (Input.GetKeyDown(KeyCode.S))
                {
                    SetNextScene();
                }
                if (Input.GetKeyDown(KeyCode.C))
                {
                    SetControlConditionOfScene();
                }
            }

            if (Menu.activeSelf)
            {
                //Debug.Log("Menu active");
                if (Input.GetMouseButtonDown(0))
                {
                    if (PlayerText.Hover)
                    {
                        //Debug.Log("PlayerText active");

                        SwitchPlayer();
                    }
                    else if (RotationText.Hover)
                    {
                        RotateEnvironment();
                    }
                    else if (SceneText.Hover)
                    {
                        //Debug.Log("SceneText active");

                        SetNextScene();
                    }
                    else if (DebugText.Hover)
                    {
                        DebugObject.SetActive(!DebugObject.activeSelf);

                        SavePrefs();
                        ResetVisuals();
                    }
                    else if (ControlText.Hover)
                    {
                        SetControlConditionOfScene();
                    }
                    else if (QualityText.Hover)
                    {
                        var setting = CamRig.CurrentVideoSetting != CameraRig.VideoSettings.HighMobile
                            ? CameraRig.VideoSettings.HighMobile
                            : CameraRig.VideoSettings.LowMobile;
                        CamRig.SetGraphicsQuality(setting);

                        SavePrefs();
                        ResetVisuals();
                    }
                    else if (QuitText.Hover)
                    {
                        Application.Quit();
                    }
                } 
            }
        }

        private void SetControlConditionOfScene()
        {
            _control = true;
            SetControl();

            SavePrefs();
            ResetVisuals();
        }

        private void SetNextScene()
        {
            var nScenes = SceneNames.scenes.Length;
            if (++_sceneIndex >= nScenes) _sceneIndex = 0;
            var s = SceneNames.scenes[_sceneIndex];
            SceneText.Text.text = "scene: " + s;
            _loadNewScene = true;

            ResetVisuals();
        }

        private void SwitchPlayer()
        {
            CamRig.StartFollowingNext();
            PlayerText.Text.text = "player: " + CamRig.PlayerPostfix;

            SavePrefs();
            ResetVisuals();
        }

        private void RotateEnvironment()
        {
            //Debug.Log("Rotating environment");
            var handlerRotation = Handler.transform.rotation;
            handlerRotation *= Quaternion.Euler(0, 90, 0);
            Handler.transform.rotation = handlerRotation;

            ResetVisuals();
        }
    }
}

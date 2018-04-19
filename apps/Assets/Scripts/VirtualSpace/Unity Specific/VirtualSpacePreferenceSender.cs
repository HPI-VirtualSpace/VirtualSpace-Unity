using System.Collections.Generic;
using UnityEngine;
using VirtualSpace;
using VirtualSpace.Shared;

namespace VirtualSpaceVisuals
{
    public class VirtualSpacePreferenceSender : MonoBehaviour
    {
        public bool SendPreferences = true;
        [HideInInspector] public string Main;
        [HideInInspector] public List<string> Others;
        [HideInInspector] public string Scene;
        [HideInInspector] public ColorPref Color;
        public float SendAfter = 1f;

        private bool _canSend;

        void Start () {
            Others = new List<string>();
            Main = "";
        }

        public void UpdatePreferenceSceneColor(string scene, ColorPref colorPref)
        {
            Scene = scene;
            Color = colorPref;
            SendAgain();
        }

        public void UpdatePreferences(string main)
        {
            Main = main;
            SendAgain();
        }

        public void UpdatePreferences(List<string> others)
        {
            Others = others;
            SendAgain();
        }

        private void Update()
        {
            if (SendAfter > 0f)
            {

                SendAfter -= Time.unscaledDeltaTime;
                if (SendAfter <= 0f)
                {
                    _canSend = true;
                    SendAgain();
                }
            }
        }

        private void SendAgain()
        {
            if (!_canSend)
                return;
            if (!SendPreferences)
            {
                Debug.Log("would send prefs...");
                return;
            }
            VirtualSpaceCore.Instance.SendReliable(new PreferencesMessage()
            {
                preferences = new PlayerPreferences()
                {
                    MainTrackableIdentifier = Main,
                    OtherTrackableIdentifiers = Others,
                    SceneName = Scene,
                    Color = Color
                }
            });
        }
    }
}

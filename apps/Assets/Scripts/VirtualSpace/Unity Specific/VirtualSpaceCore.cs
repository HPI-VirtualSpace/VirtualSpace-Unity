using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using VirtualSpace.Shared;

using VirtualSpace.Utility;

namespace VirtualSpace
{
    public class VirtualSpaceCore : Singleton<VirtualSpaceCore>
    {
        private static ClientWorker _worker;
        private bool Initialized;
        public int ReceiveInterval;
        public int SentInterval;

        // guarantee this will be always a singleton only - can't use the constructor!
        private VirtualSpaceCore() { } 

        
        internal void Initialize(string serverIP, int serverPort)
        {
            if (Initialized) return;

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name == "DemoDisplay")
                {
                    Debug.Log("VirtualSpace: Not initializing because we are the demo display");
                    return;
                }
            }
            
            Debug.Log("VirtualSpace: Initializing. Connecting to " + serverIP + ":" + serverPort);

            _worker = new ClientWorker(serverIP, serverPort, SystemInfo.deviceUniqueIdentifier);
            _worker.Start();

            Initialized = true;

            Debug.Log("VirtualSpace: Initialized. Doesn't mean it's connected");

            // _worker.PacketSendAnalyzer.OnIntervalUpdate += OnPacketSendIntervalUpdate;
            // _worker.PacketReceivedAnalyzer.OnIntervalUpdate += OnPacketReceivedIntervalUpdate;
        }

        //private void OnEnable()
        //{
        //    if (_worker != null)
        //    {
        //        _worker.PacketSendAnalyzer.OnIntervalUpdate += OnPacketSendIntervalUpdate;
        //        _worker.PacketReceivedAnalyzer.OnIntervalUpdate += OnPacketReceivedIntervalUpdate;
        //    }
        //}

        //private void OnDisable()
        //{
        //    _worker.PacketSendAnalyzer.OnIntervalUpdate -= OnPacketSendIntervalUpdate;
        //    _worker.PacketReceivedAnalyzer.OnIntervalUpdate -= OnPacketReceivedIntervalUpdate;
        //}

        //private void OnPacketReceivedIntervalUpdate(int currentInterval, Dictionary<Type, SendInfo> infos)
        //{
        //    ReceiveInterval = currentInterval;
        //    ReceiveInfos = infos;
        //}

        //private void OnPacketSendIntervalUpdate(int currentInterval, Dictionary<Type, SendInfo> infos)
        //{
        //    SentInterval = currentInterval;
        //    SentInfos = infos;
        //}

        private void Update()
        {
            if (!Initialized)
            {
                return;
            }
            //_worker.ReceiveOnce();
            VirtualSpaceTime.SetUnityTime(Time.unscaledTime);

            TimeMessage message = _worker.LastTimeMessage;

            if (message == null)
            {
                return;
            }

            double timeBeforeSync = VirtualSpaceTime.CurrentTimeInMillis;
            VirtualSpaceTime.Update(message.Millis, message.TripTime);
            double timeAfterSync = VirtualSpaceTime.CurrentTimeInMillis;

            _worker.LastTimeMessage = null;

            double timeOffset = Math.Abs(timeAfterSync - timeBeforeSync);
            if (timeOffset > 20)
                Debug.LogWarning("Time shifted by " + String.Format("{0:0.00}", Math.Abs(timeOffset)) + " since the last update");

            //Debug.Log("bla");
        }

        protected override void OnDestroyHelper()
        {
            if (Initialized)
                _worker.Stop();
        }

        public int UserId
        {
            get
            {
                return _worker.PlayerID;
            }
        }
        public bool IsRegistered()
        {
            return Initialized && _worker.IsRegistered();
        }

        public void AddHandler(Type type, Action<IMessageBase> handler)
        {
            _worker.AddHandler(type, handler);
        }

        public void RemoveHandler(Type type, Action<IMessageBase> handler)
        {
            _worker.RemoveHandler(type, handler);
        }

        public void SendUnreliable(MessageBase message)
        {
            _worker.SendUnreliable(message);
        }

        public void SendReliable(MessageBase message)
        {
            _worker.SendReliable(message);
        }
    }
}
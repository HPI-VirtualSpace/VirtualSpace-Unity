using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.VirtualSpace.Unity_Specific
{
    public class DemoDisplayClient : NetworkBehaviour
    {

        public VirtualSpaceHandler VirtualSpaceHandler;
        public DemoDisplayNetworkDiscovery Discovery;
        public DemoDisplayNetworkManager Manager;

        private bool _serverConnected;

        public string GetAddress()
        {
            return Manager.networkAddress;
        }

        void Start()
        {
            Debug.Log("Initializing Demo Display (Client)");
            Discovery.Initialize();
            Discovery.StartAsClient();

            Manager.useGUILayout = false;
        }

        void OnEnable()
        {
            Discovery.BroadcastEvent += OnReceivedBroadcast;
            Manager.ServerConnectedEvent += ServerConnected;
            Manager.ServerDisconnectedEvent += ServerDisconnected;
        }

        void OnDisable()
        {
            Discovery.BroadcastEvent -= OnReceivedBroadcast;
            Manager.ServerConnectedEvent -= ServerConnected;
            Manager.ServerDisconnectedEvent -= ServerDisconnected;
        
        }

        private void OnReceivedBroadcast(string address, string data)
        {
            Discovery.StopBroadcast();
            Debug.Log("Initializing Demo Display (Client) " + address);
            Manager.networkAddress = address;
            Manager.StartClient();
        }

        void OnDestroy()
        {
            if(Discovery.running)
                Discovery.StopBroadcast();
            else
                Manager.StopClient();
            // NetworkServer.Reset();
            //Network.Disconnect();
            // NetworkTransport.Shutdown();
            // NetworkTransport.Init();
        }

        void ServerConnected()
        {
            _serverConnected = true;
        }

        void Update()
        {
            if (_serverConnected)
            {
                if (VirtualSpaceHandler.UserId >= 0)
                {
                    var msg = new SceneLoadMsg { SceneName = SceneManager.GetActiveScene().name, UserId = VirtualSpaceHandler.UserId };
                    short id = 133;
                    Manager.client.Send(id, msg);
                    Debug.Log("Demo Display: client (this) has connected to server, sending " + msg.SceneName);
                    _serverConnected = false;
                }
            }
        }

        void ServerDisconnected()
        {
            Debug.Log("Demo Display: client (this) restart");
            Manager.StopClient();
            Discovery.Initialize();
            Discovery.StartAsClient();
        }
    }

    public class SceneLoadMsg : MessageBase
    {
        public string SceneName;
        public int UserId;
    }
}

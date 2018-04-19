using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//client class (display), responsible for deriving state on phone and display similar scene (IK etc.)
namespace Assets.Scripts.VirtualSpace.Unity_Specific
{
    public class DemoDisplayServer : MonoBehaviour
    {
        [Header("Dynamic Settings")]
        public Vector3 CameraOffsetPosition;
        // public Vector3 CameraOffsetRotation;
        public bool IsFirstPerson;
        
        [Header("Static Settings")]
        public DemoDisplayNetworkDiscovery Discovery;
        public DemoDisplayNetworkManager Manager;
        public VSManager VsManager;
        public RawImage[] Backgrounds;
        public Transform VsCam;
        public string ToggleCameraLayerFirstPerson = "DemoDisplayIK";
        public RenderTexture RenderTex;
        public List<Vector2> Positions;
        public List<Vector2> Offsets;
        public Vector3 SceneOffset;
        public string ServerSceneName = "DemoDisplay";

        //private Dictionary<DemoDisplayComponent, Info> _componentToInfo;
        private Dictionary<int, Info> _connectionIdToInfo;
        private Scene _main;
        private List<LoadInfo> _waitForBuild;
        private int _loadCount;
        private Vector3 _lastCamOffsetPos;//_lastCamOffsetRot;
        private bool _lastIsFirstPerson;
        private bool _loading;
        private Material _mat;

        private struct Info
        {
            public DemoDisplayPlayer DdPlayer;
            public DemoDisplayCamera DdCamera;
            public Scene Scene;
            public Camera Camera;
            public int CamIndex;
        }

        private struct LoadInfo
        {
            public NetworkConnection Connection;
            public string SceneToLoad;
            public int UserId;
        }

        void Start()
        {
            _lastIsFirstPerson = IsFirstPerson;
            //_lastCamOffsetPos = CameraOffsetPosition;
            //_lastCamOffsetRot = CameraOffsetRotation;
            _waitForBuild = new List<LoadInfo>();
            RenderTexture.active = RenderTex;
            //_componentToInfo = new Dictionary<DemoDisplayComponent, Info>();
            _connectionIdToInfo = new Dictionary<int, Info>();
            _main = SceneManager.GetActiveScene();
           // NetworkTransport.Init();
            Discovery.Initialize();
            Discovery.StartAsServer();
            Manager.StartServer();
        }

        void Update()
        {
            //check load queue
            if (_waitForBuild.Any() && !_loading)
            {
                var conn = _waitForBuild[0].Connection;
                var sceneToLoad = _waitForBuild[0].SceneToLoad;
                var contains = _connectionIdToInfo.ContainsKey(conn.connectionId);
                var hasUser = VsManager.Users.HasUser(_waitForBuild[0].UserId);
                if (sceneToLoad != ServerSceneName &&
                    hasUser &&
                    (!contains || _connectionIdToInfo[conn.connectionId].Scene.name != sceneToLoad))
                {
                    TryRemove(conn);
                    _loading = true;
                    _mat = VsManager.Users.GetUser(_waitForBuild[0].UserId).AreaMaterial;
                    SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);
                }
            }

            //check if settings changed
            if (_lastIsFirstPerson != IsFirstPerson || _lastCamOffsetPos != CameraOffsetPosition)// || _lastCamOffsetRot != CameraOffsetRotation)
            {
                var camChanged = _lastIsFirstPerson != IsFirstPerson;
                _lastCamOffsetPos = CameraOffsetPosition;
                //_lastCamOffsetRot = CameraOffsetRotation;
                _lastIsFirstPerson = IsFirstPerson;
                VsCam.localPosition = CameraOffsetPosition;
                VsCam.LookAt(VsCam.parent);
                var locrot = VsCam.localRotation.eulerAngles;
                locrot.z = 0f;
                VsCam.localRotation = Quaternion.Euler(locrot);

                foreach (var v in _connectionIdToInfo.Values)
                {
                    v.DdPlayer.UpdateInfo(CameraOffsetPosition, IsFirstPerson); //CameraOffsetRotation, IsFirstPerson);
                    if(camChanged)
                        v.DdCamera.Toggle(ToggleCameraLayerFirstPerson);
                }
            }
        }

        public void OnSceneMessageReceived(NetworkConnection conn, string sceneToLoad, int userid)
        {
            var loadInfo = new LoadInfo
            {
                Connection = conn,
                SceneToLoad = sceneToLoad,
                UserId = userid
            };
            _waitForBuild.Add(loadInfo);
        }

        public void TryRemove(NetworkConnection conn)
        {
            var contains = _connectionIdToInfo.ContainsKey(conn.connectionId);
            //Debug.Log("try remove " + conn.connectionId + " " + contains);
            if (contains)
            {
                //Debug.Log("remove");
                SceneManager.UnloadSceneAsync(_connectionIdToInfo[conn.connectionId].Scene);
                var index = _connectionIdToInfo[conn.connectionId].CamIndex;
                _connectionIdToInfo.Remove(conn.connectionId);
                Backgrounds[index].material = null;
            }
        }

        //private DemoDisplayComponent GetComponentFromAddress(string fromAddress)
        //{
        //    var component = _componentToInfo.Keys.FirstOrDefault(m => fromAddress == m.GetAddress());
        //    return component;
        //}

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == ServerSceneName)
                return;
            _loadCount++;
            var conn = _waitForBuild[0].Connection;
            var theRoots = scene.GetRootGameObjects();
            DemoDisplayComponent component = null;
            foreach (var obj in theRoots)
            {
                obj.transform.position = obj.transform.position + SceneOffset * _loadCount;
                //Debug.Log(obj.gameObject.name + " " + obj.transform.position);
                if (component == null)
                {
                    component = obj.GetComponentInChildren<DemoDisplayComponent>();
                }
            }
            SceneManager.SetActiveScene(scene);
            var indices = _connectionIdToInfo.Values.Select(v => v.CamIndex).ToList();
            var camIndex = 0;
            for (var i = 0; i < 4; i++)
            {
                if (indices.Contains(i)) continue;
                camIndex = i;
                break;
            }

            //var clientObjects = component.ClientObjects;
            //enable network identity objects

            NetworkServer.SetClientReady(conn);
            NetworkServer.SpawnObjects();
            var player = Instantiate(component.PlayerPrefab, Vector3.zero, Quaternion.identity);
            player.transform.position = player.transform.position + SceneOffset * _loadCount;
            var ddplayer = player.GetComponent<DemoDisplayPlayer>();
            ddplayer.Offset = SceneOffset * _loadCount;
            Camera cam = null;
            DemoDisplayCamera ddcam = null;
            if (ddplayer != null)
            {
                ddplayer.Component = component;
                ddcam = ddplayer.Cam;
                cam = ddcam.Cam;
            }
            ddplayer.UpdateInfo(CameraOffsetPosition, IsFirstPerson); //CameraOffsetRotation, 
            NetworkServer.AddPlayerForConnection(conn, player, 0);
            NetworkServer.SpawnObjects();
            NetworkServer.SetClientNotReady(conn);
            var info = new Info
            {
                DdCamera = ddcam,
                DdPlayer = ddplayer,
                Scene = scene,
                CamIndex = camIndex,
                Camera = cam
            };
            _connectionIdToInfo.Add(conn.connectionId, info);
            //var addressIndex = _connectionIdToInfo.Keys.ToList().IndexOf(conn.connectionId);
            if(ddcam != null)
                ddcam.SetTex(Offsets[camIndex], Positions[camIndex]);
            Backgrounds[camIndex].material = _mat;
            SceneManager.SetActiveScene(_main);
            _waitForBuild.RemoveAt(0);
            _loading = false;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            NetworkServer.UnregisterHandler(133);
            Manager.ClientConnectedEvent -= ClientConnected;
            Manager.ClientDisconnectedEvent -= ClientDisconnected;
            Manager.ClientWantsPrefab -= ClientWantsPrefab;
        }

        private void SceneLoadMsgHandler(NetworkMessage netmsg)
        {
            var msg = netmsg.ReadMessage<SceneLoadMsg>();
            Debug.Log("Demo Display: received >>" + msg.SceneName + "<< from " + netmsg.conn.address);
            OnSceneMessageReceived(netmsg.conn, msg.SceneName, msg.UserId);
        }

        void OnEnable()
        {
            NetworkServer.RegisterHandler(133, SceneLoadMsgHandler);
            SceneManager.sceneLoaded += OnSceneLoaded;
            Manager.ClientConnectedEvent += ClientConnected;
            Manager.ClientDisconnectedEvent += ClientDisconnected;
            Manager.ClientWantsPrefab += ClientWantsPrefab;
        }

        private void ClientWantsPrefab(NetworkConnection conn, short id)
        {
            //var info = _addressToInfo[conn.address];
            //var index = Scenes.IndexOf(info.scene.name);
            //var prefab = Prefabs[index];
            //var player = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            //NetworkServer.AddPlayerForConnection(conn, player, id);
        }

        void OnDestroy()
        {
            Manager.StopServer();
            //if(Discovery.running)
            //    Discovery.StopBroadcast();
            NetworkServer.Shutdown();
            //Network.Disconnect();
            //NetworkTransport.Shutdown();
            //NetworkTransport.Init();
        }


        void ClientConnected(NetworkConnection conn)
        {
            Debug.Log("Demo Display: server (this) connected to client " + conn.address + " " + conn.connectionId);
            //NetworkServer.SetClientReady(conn);
            //NetworkServer.SpawnObjects();
        }

        void ClientDisconnected(NetworkConnection conn)
        {
            Debug.Log("Demo Display: server (this) disconnected from client " + conn.address + " " + conn.connectionId);
            NetworkServer.DestroyPlayersForConnection(conn);
            TryRemove(conn);
        }

        public void StartServer()
        {

            Discovery.broadcastData = SceneManager.GetActiveScene().name;
        }
    }
}

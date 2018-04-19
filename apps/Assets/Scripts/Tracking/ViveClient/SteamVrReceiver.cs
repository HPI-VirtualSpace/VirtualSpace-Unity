using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.ViveClient
{
    [RequireComponent(typeof(AsyncTcpSocket))]
    public class SteamVrReceiver : MonoBehaviour
    {
        [Header("Server IP")]
        public string MappingServerIp = "192.168.1.116";
        public string MulticastAddress = "226.0.0.1";

        [Header("Tracking Properties")]
        public int PortTrackingData = 33000;
        public string SocketBufferSize = "0x100000";
        public int DataBufferSize = 20;

        [Header("Mapping Properties")]
        public int PortMappingData = 11000;
        public char ServerMessageEntriesSeparator = ';';
        public char ServerMessageEntryValueSeparator = '-';
        public string RequestForNewDictionary = "REQUIRETABLE";

        public delegate void NewTrackingDataAction();
        public static event NewTrackingDataAction OnNewTrackingData;

        private static char _serverMessageEntriesSeparator;
        private static char _serverMessageEntryValueSeparator;
        private static Dictionary<int, string> _id2name;

        private float _lastReceived;
        private Dictionary<string, GameObject> gameObjectDictionary = new Dictionary<string, GameObject>();
        private Dictionary<string, GameObject> createdObjectDictionary;
        private List<float> _receivedPackagesTimes = new List<float>();
        private int _socketBufsize = 0x100000;
        private AsyncTcpSocket _asyncTcpSocket;
        private Thread _udpThread;

        private static Socket _socketTracking;
        private static byte[] _bufferTracking;
        private static Socket _socketMapping;
        private static byte[] _bufferMapping;
        private static TrackedObjectData[][] _dataBuffer;
        private static int _dataBufferHead;
        public static int _receivedPackages;
        private static bool _checkCreatedObjects;
        private static bool _threadRunning;
        private static bool _sendRequest;
        private static int _lastHead;

        private static readonly object syncLock = new object();
        private static string RequestString;

        struct TrackedObjectData
        {
            public int id;
            public Vector3 pos;
            public Quaternion rot;
        }

        private void Awake()
        {
            Restart();
        }

        void Restart()
        {
            //set up udp for tracking
            if (Reset)
            {
                Quit();
            }
            if(_socketTracking == null || Reset)
            {
                _socketTracking = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _socketTracking.Bind(new IPEndPoint(IPAddress.Any, PortTrackingData));
                IPAddress mulIP = IPAddress.Parse(MulticastAddress);
                _socketTracking.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(mulIP, IPAddress.Any));
                _socketTracking.Blocking = true;
                _socketBufsize = (int)new System.ComponentModel.Int32Converter().ConvertFromString(SocketBufferSize);
                _socketTracking.ReceiveBufferSize = _socketBufsize;
                _bufferTracking = new byte[_socketBufsize];
                _dataBuffer = new TrackedObjectData[DataBufferSize][];
                _dataBufferHead = -1;
                StartReceiveThread();
                _receivedPackagesTimes = new List<float>();
                _id2name = new Dictionary<int, string>();
                GC.Collect();
            }

            Reset = false;
            _lastReceived = Time.unscaledTime;
            //set up tcp for mapping
            gameObjectDictionary = new Dictionary<string, GameObject>();
            createdObjectDictionary = new Dictionary<string, GameObject>();
            RequestString = RequestForNewDictionary;
            _serverMessageEntriesSeparator = ServerMessageEntriesSeparator;
            _serverMessageEntryValueSeparator = ServerMessageEntryValueSeparator;
            _asyncTcpSocket = gameObject.GetComponent<AsyncTcpSocket>();
            _asyncTcpSocket.Port = PortMappingData;
            _asyncTcpSocket.ServerIp = MappingServerIp;
            AsyncTcpSocket.OnReceiveMessage += DeserializeTcpMessage;
        }

        private static void DeserializeTcpMessage(string msg)
        {
            var newDictionary = new Dictionary<int, string>();
            msg = msg.Remove(0, RequestString.Length);
            var split = msg.Split(_serverMessageEntriesSeparator);
            foreach (var splitString in split)
            {
                var splitAgain = splitString.Split(_serverMessageEntryValueSeparator);
                int key;
                if (splitAgain.Length != 2 || !int.TryParse(splitAgain[0], out key)) continue;
                var value = splitAgain[1];
                newDictionary.Add(key, value);
            }
            _id2name = newDictionary;
            _checkCreatedObjects = true;
        }

        private static string SerializeTcpMessage(Dictionary<int, string> dictionary)
        {
            var resultString = "";
            var stringList = dictionary.Select(d => d.Key + "" + _serverMessageEntryValueSeparator + "" + d.Value).ToList();
            for (var s = 0; s < stringList.Count; s++)
            {
                resultString += stringList[s];
                if (s < stringList.Count - 1)
                    resultString += _serverMessageEntriesSeparator;
            }
            return resultString;
        }

        private void StartReceiveThread()
        {
            try
            {
                _threadRunning = true;
                _udpThread = new Thread(ReceiveLoop)
                {
                    Priority = System.Threading.ThreadPriority.Highest
                };
                _udpThread.Start();
            }
            catch (Exception ex)
            {
                Debug.Log("error starting thread: " + ex.Message);
            }

            //Debug.Log("ViveTracking: Started UDP receive thread");

        }

        private void StopReceiveLoop()
        {
            _threadRunning = false;

            Thread.Sleep(100);
            if (_udpThread != null)
            {
                _udpThread.Abort();
                // serialThread.Join();
                Thread.Sleep(100);
                _udpThread = null;
                //Debug.Log("ViveTracking: Stopped UDP receive thread");

            }
        }
        
        private void ReceiveRead()
        {
            try
            {
                //Debug.Log("bytes test ");
                int bytesReceived = _socketTracking.Receive(_bufferTracking);
                //Debug.Log("bytes " + bytesReceived);
                if (bytesReceived == 0) return;
                var msg = new byte[bytesReceived];
                Array.Copy(_bufferTracking, msg, bytesReceived);
                lock (syncLock)
                {
                    if (++_dataBufferHead >= DataBufferSize)
                        _dataBufferHead = 0;
                    _receivedPackages++;
                    var tod = FromByteArray<TrackedObjectData>(msg);
                    _dataBuffer[_dataBufferHead] = tod;
                    //Debug.Log("package received " + _dataBufferHead);
                }
            }
            catch
            {
                //Debug.LogError(e.Message);
            }
        }

        private void ReceiveLoop()
        {
            while (_threadRunning)
                ReceiveRead();
        }

        public int GetPps()
        {
            return _receivedPackagesTimes.Count;
        }

        public void Quit()
        {
            if (_threadRunning)
                StopReceiveLoop();
            if (_socketTracking != null)
            {
                _socketTracking.Close();
            }
        }

        private void OnDisable()
        {
            AsyncTcpSocket.OnReceiveMessage -= DeserializeTcpMessage;
        }

        private void OnApplicationQuit()
        {
            Debug.Log("close socket");
            Quit();
        }

        private float _lastSent;
        public float PPS;
        public bool Reset;
        void Update()
        {
            if(Time.unscaledTime - _lastReceived > 2f)
            {
                Reset = true;
            }
            if (Reset)
            {
                Restart();
                return;
            }
            //ReceiveRead();
           
            if (_sendRequest && AsyncTcpSocket.IsConnected() && Time.unscaledTime - _lastSent > 1f)
            {
                _lastSent = Time.unscaledTime;
                _sendRequest = false;
                AsyncTcpSocket.Send(RequestForNewDictionary);
            }
            if (_checkCreatedObjects)
            {
                CheckCreatedObjects();
                _checkCreatedObjects = false;
            }
            TrackedObjectData[] lastTrackedData;
            if (GetLastTrackingData(out lastTrackedData))
            {
                //Debug.Log("package time : " + (Time.time - _last));
                _lastReceived = Time.unscaledTime;
                InterpretTrackedObjectData(lastTrackedData);
            }
            else
            {
                _sendRequest = true;
            }
            for(var i = 0; i < _receivedPackages; i++)
            {
                _receivedPackagesTimes.Add(Time.unscaledTime);
            }
            _receivedPackages = 0;
            var newReceivedPackagesTimes = new List<float>();
            foreach (var rpt in _receivedPackagesTimes)
            {
                if(Time.unscaledTime -1f < rpt)
                    newReceivedPackagesTimes.Add(rpt);
            }
            _receivedPackagesTimes = newReceivedPackagesTimes;
            //Debug.Log("frame rate : " + Time.deltaTime);
            PPS = GetPps();
        }

        private bool GetLastTrackingData(out TrackedObjectData[] trackData)
        {
            trackData = new TrackedObjectData[0];
            var change = false;
            if (_dataBufferHead >= 0)
            {
                lock (syncLock)
                {
                    change = _lastHead != _dataBufferHead;
                    _lastHead = _dataBufferHead;
                    trackData = _dataBuffer[_dataBufferHead];
                }
            }
            return change;
        }

        private void CheckCreatedObjects()
        {
            var copy = new Dictionary<int, string>(_id2name);
            foreach(var id2Name in copy)
            {
                var setName = id2Name.Value;//data.name;
                                            //add to dict if does not exist
                if (!gameObjectDictionary.ContainsKey(setName))
                {
                    GameObject go;
                    var child = transform.Find(setName);
                    if (child != null)
                        go = child.gameObject;
                    else
                    {
                        go = new GameObject(setName);
                        go.transform.parent = transform;
                        createdObjectDictionary.Add(setName, go);
                    }
                    gameObjectDictionary.Add(setName, go);
                    go.tag = "untracked";
                }
            }

            //remove unused gameobjects
            var createdCopy = new Dictionary<string, GameObject>(createdObjectDictionary);
            foreach (var entry in createdCopy)
            {
                var isInDict = copy.ContainsValue(entry.Key);
                if (!isInDict)
                {
                    createdObjectDictionary.Remove(entry.Key);
                    gameObjectDictionary.Remove(entry.Key);
                    Destroy(entry.Value);
                }
            }

            //TODO throw created event
            if(OnNewTrackingData!=null) OnNewTrackingData();
        }

        private static byte[] ToByteArray<T>(T[] source) where T : struct
        {
            GCHandle handle = GCHandle.Alloc(source, GCHandleType.Pinned);
            try
            {
                IntPtr pointer = handle.AddrOfPinnedObject();
                byte[] destination = new byte[source.Length * Marshal.SizeOf(typeof(T))];
                Marshal.Copy(pointer, destination, 0, destination.Length);
                return destination;
            }
            finally
            {
                if (handle.IsAllocated)
                    handle.Free();
            }
        }
       
        private static T[] FromByteArray<T>(byte[] source) where T : struct
        {
            T[] destination = new T[source.Length / Marshal.SizeOf(typeof(T))];
            GCHandle handle = GCHandle.Alloc(destination, GCHandleType.Pinned);
            try
            {
                IntPtr pointer = handle.AddrOfPinnedObject();
                Marshal.Copy(source, 0, pointer, source.Length);
                return destination;
            }
            finally
            {
                if (handle.IsAllocated)
                    handle.Free();
            }
        }

        private void InterpretTrackedObjectData(TrackedObjectData[] tod)
        {
            var tracked = new List<string>();
            foreach (var data in tod)
            {
                GameObject trackedObj;
                if (_id2name.ContainsKey(data.id))
                {
                    var setName = _id2name[data.id];//data.name;
                    //add to dict if does not exist
                    if (!gameObjectDictionary.TryGetValue(setName, out trackedObj))
                    {
                        Debug.Log("ViveTracking: game object not yet created");
                        continue;
                    }
                    //update transform
                    trackedObj.transform.localPosition = data.pos;
                    trackedObj.transform.localRotation = data.rot;

                    trackedObj.tag = "tracked";
                    tracked.Add(setName);
                    //Debug.Log("ViveTracking:" + data.id);
                }
                else
                {
                    _sendRequest = true;
                   // Debug.Log("ViveTracking: missing entry " + data.id);
                   // Debug.Log("ViveTracking: please request dictionary from server");
                }
            }

            //tag untracked objects
            foreach (var untracked in gameObjectDictionary)
            {
                if (tracked.Contains(untracked.Key))
                    continue;
                untracked.Value.tag = "untracked";
            }
        }

        public GameObject GetTrackable(string trackableName)
        {
            GameObject returnValue;
            gameObjectDictionary.TryGetValue(trackableName, out returnValue);
            return returnValue;
        }

        public GameObject[] GetAllTrackables()
        {
            return gameObjectDictionary.Values.ToArray();
        }
    }
}
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.VirtualSpace.Unity_Specific
{
    public class DemoDisplayNetworkManager : NetworkManager {
        internal Action<NetworkConnection> ClientConnectedEvent;
        internal Action<NetworkConnection> ClientDisconnectedEvent;
        internal Action ServerDisconnectedEvent;
        internal Action ServerConnectedEvent;
        internal Action<NetworkConnection, short> ClientWantsPrefab;

        public override void OnServerConnect(NetworkConnection conn)
        {
           // Debug.Log("Manager client connect " + conn.address);
            if (ClientConnectedEvent != null)
                ClientConnectedEvent(conn);
        }

        public override void OnServerDisconnect(NetworkConnection conn)
        {
           // Debug.Log("Manager client disconnect " + conn.address);
            if (ClientDisconnectedEvent != null)
                ClientDisconnectedEvent(conn);
        }

        public override void OnClientDisconnect(NetworkConnection conn)
        {
            //Debug.Log("Manager server disconnect " + conn.address);
            if (ServerDisconnectedEvent != null)
                ServerDisconnectedEvent();
        }

        public override void OnClientConnect(NetworkConnection conn)
        {
           // Debug.Log("Manager server connect " + conn.address);
            ClientScene.AddPlayer(conn, 0);
            if (ServerConnectedEvent != null)
                ServerConnectedEvent();
        }

        public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
        {
            if (ClientWantsPrefab != null)
                ClientWantsPrefab(conn, playerControllerId);
        }
    }
}

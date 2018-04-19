using UnityEngine;

namespace VirtualSpace.Messaging
{
    public class VirtualSpaceInitializer : MonoBehaviour
    {
        public string ServerIP = "192.168.1.116";
        public int ServerPort = 8683;
        public bool RunInEditorBackground = true;

        private void Awake()
        {
            if (Application.isEditor && RunInEditorBackground)
            {
                Application.runInBackground = true;
            }
            VirtualSpaceCore.Instance.Initialize(ServerIP, ServerPort);
        }
    }
}
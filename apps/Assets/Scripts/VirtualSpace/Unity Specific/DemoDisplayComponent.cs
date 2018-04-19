using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

//class in client and server, responsible for syncing right objects and disabling tracking etc. on display side
namespace Assets.Scripts.VirtualSpace.Unity_Specific
{
    public class DemoDisplayComponent : NetworkBehaviour
    {
        public List<GameObject> ServerSwitchObjects;
        public List<GameObject> ClientSwitchObjects;
        public List<Transform> FollowObjects;
        public int FirstPersonIndex;
        public GameObject PlayerPrefab;
        //private bool _isClient;
        //private bool _runsAsClient;

        void Awake()
        {
            var hasDemoScene = false;
            for(var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name == "DemoDisplay")
                    hasDemoScene = true;
            }
            var isServer = Application.isEditor && hasDemoScene;

            if (!isServer)
            {
                foreach (var co in ClientSwitchObjects)
                {
                    co.SetActive(!co.activeSelf);
                    //if(!co.activeSelf)
                    //    Destroy(co);
                }
            }
            else
            {
                foreach (var co in ServerSwitchObjects)
                {
                    co.SetActive(!co.activeSelf);
                    if (!co.activeSelf)
                        Destroy(co);
                }
            }

        }

       
    }
}

        
    


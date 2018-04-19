using System.Collections.Generic;
using System.Linq;
using Assets.ViveClient;
using UnityEngine;
using UnityEngine.UI;

namespace VirtualSpaceVisuals
{
    public class VirtualSpaceBreachVisuals : MonoBehaviour
    {
        public CameraRig CamRig;
        public SteamVrReceiver Receiver;
        public float PlayerHeight = 1f;
        public float PlayerWidth = 0.5f;
        public float ObjectWidth = 0.2f;
        public Material PlayerMaterial;
        public Text BreachText;
        public float TextLocalScale;
        public float BounceExtend = 0.5f;
        public float BounceTime = 0.3f;
        public string CylinderId = "HMD";
        public string SphereId = "Ufo";
        public VirtualSpaceHandler Handler;
        [HideInInspector] public bool Visualize;
        [HideInInspector] public List<Vector3> YourArea;
        [HideInInspector] public float SafetyPoly;

        private List <GameObject> _otherVisuals;
        private List<GameObject> _playerVisuals;

        private bool _generateNew;
        private GameObject[] _allTrackables;
        private Dictionary<GameObject, GameObject> _handhelds;
        private List<GameObject> _players;
        private GameObject _lastTarget;
        private GameObject _ownHandheld;

        void Start()
        {
            _playerVisuals = new List<GameObject>();
            _otherVisuals = new List<GameObject>();
        }

        void OnEnable()
        {
            SteamVrReceiver.OnNewTrackingData += GenerateNewLists;
        }


        void OnDisable()
        {
            SteamVrReceiver.OnNewTrackingData -= GenerateNewLists;
        }

        private void GenerateNewLists()
        {
            _generateNew = true;
        }
        
        void Update ()
        {
            BreachText.enabled = false;
            foreach (var v in _playerVisuals)
                v.SetActive(false);
            foreach (var v in _otherVisuals)
                v.SetActive(false);
            
            if (!Visualize || CamRig.Target == null)
                return;
            var currentTarget = CamRig.Target.name;
            var differentTarget = _lastTarget != null && _lastTarget != CamRig.Target;
            _lastTarget = CamRig.Target;
            var currentTargetNumber = CamRig.PlayerPostfix.ToString();

            _generateNew = _generateNew || differentTarget;
            if (_generateNew)
            {
                _generateNew = false;

                _allTrackables = Receiver.GetAllTrackables();
                _players = _allTrackables
                    .Where(t => t.name != currentTarget && t.name.Contains(CylinderId)).ToList();
                _handhelds = new Dictionary<GameObject, GameObject>();
                var allhandhelds = _allTrackables
                    .Where(t =>
                        t.name.Substring(SphereId.Length, t.name.Length - SphereId.Length) != currentTargetNumber
                        && t.name.Contains(SphereId)).ToList();
                foreach (var t in _players)
                {
                    var playerId = t.name.Substring(CylinderId.Length, t.name.Length - CylinderId.Length);
                    var handheld = allhandhelds.FirstOrDefault(ahh => ahh.name == SphereId + playerId);
                    if (handheld != null)
                        _handhelds.Add(t, handheld);
                }
                _ownHandheld = _allTrackables.FirstOrDefault(t => t.name == SphereId + currentTargetNumber);
            }
            

            //check if player breached
            var playerdist = VirtualSpaceHandler.DistanceFromPoly(CamRig.Target.transform.position, YourArea, false) + PlayerWidth + SafetyPoly;
            var playerBreached = playerdist > 0f;
            if (!playerBreached)
            {
                if (_ownHandheld != null && Handler.Settings.CheckHandheldBreach)
                {
                    var handheldDist = VirtualSpaceHandler.DistanceFromPoly(_ownHandheld.transform.position, YourArea, false) + ObjectWidth + SafetyPoly;
                    playerBreached = handheldDist > 0f;
                }
            }

            var playersCopy = new List<GameObject>(_players);
            var handheldsCopy = new List<GameObject>();
            foreach (var p in _players)
            {
                GameObject handheld;
                if (_handhelds.TryGetValue(p, out handheld))
                {
                    handheldsCopy.Add(handheld);
                }
            }
            if (playerBreached)
            {
                //player has breached - visualize all other players
            }
            else
            {
                //visualize players in your area
                foreach (var t1 in _players)
                {
                    var pd = VirtualSpaceHandler.DistanceFromPoly(t1.transform.position, YourArea, false) - PlayerWidth - SafetyPoly;
                    var otherBreached = pd < 0f;
                    if (!otherBreached)
                    {
                        //var playerName = t1.name;
                        //var id = t1.name.Substring(CylinderId.Length, playerName.Length - CylinderId.Length);
                        GameObject handheld;
                        if (_handhelds.TryGetValue(t1, out handheld))
                        {
                            var handheldDist = VirtualSpaceHandler.DistanceFromPoly(handheld.transform.position, YourArea, false) - ObjectWidth - SafetyPoly;
                            otherBreached = handheldDist < 0f;
                        }
                    }
                    if (otherBreached) continue;

                    playersCopy.Remove(t1);
                    if(_handhelds.ContainsKey(t1))
                        handheldsCopy.Remove(_handhelds[t1]);
                }
            }

            //check if others have breached
            //var playerIdentifiers = VirtualSpaceHandler.ActiveBreaches.Select(ab => VirtualSpaceHandler.ActiveBreachesPlayerIdentifiers[ab]).ToList();
            //var players = trackables.Where(t => playerIdentifiers.Contains(t.name)).ToList();
            //var otherIdentifiers = new List<string>();
            //foreach (var ab in VirtualSpaceHandler.ActiveBreaches)
            //{
            //    var others = VirtualSpaceHandler.ActiveBreacherOtherIdentifiers[ab] ?? new List<string>();
            //    otherIdentifiers.AddRange(others);
            //}
            //var handhelds = trackables.Where(t => otherIdentifiers.Contains(t.name)).ToList();
            
            //create player visuals
            for (var i = 0; i < playersCopy.Count; i++)
            {
                if (_playerVisuals.Count <= i)
                {
                    var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    visual.GetComponent<Collider>().isTrigger = true;
                    visual.transform.parent = transform;
                    visual.transform.localScale = new Vector3(PlayerWidth, PlayerHeight, PlayerWidth);
                    visual.GetComponent<Renderer>().material = PlayerMaterial;
                    _playerVisuals.Add(visual);
                }
                _playerVisuals[i].SetActive(true);
                var position = _players[i].transform.position;
                position.y = PlayerHeight/2f;
                _playerVisuals[i].transform.position = position;
            }
            //create object visuals
            for (var i = 0; i < handheldsCopy.Count; i++)
            {
                if (_otherVisuals.Count <= i)
                {
                    var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    visual.GetComponent<Collider>().isTrigger = true;
                    visual.transform.parent = transform;
                    visual.transform.localScale = ObjectWidth * Vector3.one;
                    visual.GetComponent<Renderer>().material = PlayerMaterial;
                    _otherVisuals.Add(visual);
                }
                _otherVisuals[i].SetActive(true);
                var position = handheldsCopy[i].transform.position;
                _otherVisuals[i].transform.position = position;
            }
            
            
            if (playersCopy.Count > 0)
            {
                var theOnlyOneToBlameIsYou = playerBreached;//VirtualSpaceHandler.ActiveBreaches.Select(b => VirtualSpaceHandler.ActiveBreachesOthersFault[b]).Any(c => c == false);
                BreachText.enabled = true;
                BreachText.text = theOnlyOneToBlameIsYou ? (playerdist > 0f ? Handler.Settings.BreachMessage : Handler.Settings.BreachHandheld) : Handler.Settings.BreachOther;
                BreachText.rectTransform.localScale = TextLocalScale * Vector3.one * (1f + Mathf.PingPong(Time.unscaledTime, BounceTime) * BounceExtend);

            }//}
        }
    }
}

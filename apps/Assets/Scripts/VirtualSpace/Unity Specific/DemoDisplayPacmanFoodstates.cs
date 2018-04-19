using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.VirtualSpace.Unity_Specific
{
    public class DemoDisplayPacmanFoodstates : NetworkBehaviour {
        
        public string MazeName = "maze";
        public GameObject[] FollowGhosts;
        private YellowDelicious[] _food;
        private PacmanCherry[] _cherries;
        private PacmanGhost[] _ghosts;

        private List<DelayedAction> _delayedActions;
        private bool[] _oldGhostState;

        struct DelayedAction
        {
            public int Index;
            public int Type;
            public bool Flag;
            public float Time;
        }

        void Start()
        {
            _delayedActions = new List<DelayedAction>();
            var go = GameObject.Find(MazeName);
            _food = go.GetComponentsInChildren<YellowDelicious>();
            _cherries = go.GetComponentsInChildren<PacmanCherry>();
            _ghosts = go.GetComponentsInChildren<PacmanGhost>();
            _oldGhostState = _ghosts.Select(g => true).ToArray();

            if (isLocalPlayer)
            {
                foreach (var food in _food)
                {
                    food.FoodEaten += FoodEaten;
                }
                foreach (var food in _food)
                    food.FoodReset += FoodReset;
            }
        }

        void OnDestroy()
        {
            if (isLocalPlayer)
            {
                foreach (var food in _food)
                    food.FoodEaten -= FoodEaten;
                foreach (var food in _food)
                    food.FoodReset -= FoodReset;
                foreach (var cherry in _cherries)
                    cherry.CherryEaten -= CherryEaten;
                foreach (var cherry in _cherries)
                    cherry.CherryReset -= CherryReset;
            }
        }
        
        [Client]
        private void FoodEaten(YellowDelicious f)
        {
            var index = _food.ToList().IndexOf(f);
            CmdUpdateFood(index, false);
        }

        [Client]
        private void FoodReset(YellowDelicious f)
        {
            var index = _food.ToList().IndexOf(f);
            CmdUpdateFood(index, true);
        }

        [Client]
        private void CherryEaten(PacmanCherry p)
        {
            var index = _cherries.ToList().IndexOf(p);
            CmdUpdateCherry(index, false);
        }

        [Client]
        private void CherryReset(PacmanCherry p, bool visible)
        {
            var index = _cherries.ToList().IndexOf(p);
            CmdUpdateCherry(index, visible);
        }

        public void Update()
        {
            if (!isLocalPlayer)
            {
                while (_delayedActions.Any())
                {
                    var action = _delayedActions[0];
                    if (_delayedActions[0].Time > Time.unscaledTime - DemoDisplayInterpolatePosition.Lag)
                        break;
                    _delayedActions.RemoveAt(0);
                    switch (action.Type)
                    {
                        case 0:
                            if (action.Flag)
                                _food[action.Index].Reset();
                            else
                                _food[action.Index].TryEat();
                            break;
                        case 1:
                            FollowGhosts[action.Index].SetActive(action.Flag);
                            break;
                        case 2:
                            _cherries[action.Index].TryReset(action.Flag, true);
                            break;
                    }
                }
            }
            else
            {
                var ghostStates = _ghosts.Select(g => g.gameObject.activeSelf).ToArray();
                for (var i = 0; i < ghostStates.Length; i++)
                {
                    if(ghostStates[i] != _oldGhostState[i])
                        CmdUpdateGhost(i, ghostStates[i]);
                }
                _oldGhostState = ghostStates;
            }
        }

        [Command]
        public void CmdUpdateFood(int i, bool show)
        {
            var action = new DelayedAction
            {
                Time = Time.unscaledTime,
                Flag = show,
                Index = i,
                Type = 0
            };
            _delayedActions.Add(action);
        }

        [Command]
        public void CmdUpdateGhost(int i, bool active)
        {
            var action = new DelayedAction
            {
                Time = Time.unscaledTime,
                Flag = active,
                Index = i,
                Type = 1
            };
            _delayedActions.Add(action);
        }

        [Command]
        public void CmdUpdateCherry(int i, bool show)
        {
            var action = new DelayedAction
            {
                Time = Time.unscaledTime,
                Flag = show,
                Index = i,
                Type = 2
            };
            _delayedActions.Add(action);
        }
    }
}

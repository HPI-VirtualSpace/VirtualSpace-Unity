using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Games.Scripts;
using Assets.Scripts.VirtualSpace.Unity_Specific;
using UnityEngine;
using UnityEngine.Networking;

namespace VirtualSpace.Unity_Specific
{
    public class DemoDisplayWhacSpawner : NetworkBehaviour
    {
        private List<DelayedAction> _delayedActions;
        public Transform LookAtAlternative;
        public Transform RemoteHammer;
        struct DelayedAction
        {
            public Action Action;
            public float Time;
        }
        
        private void Start()
        {
            _delayedActions = new List<DelayedAction>();

            if (isLocalPlayer)
            {
                MoleController.MoleSpawned += MoleSpawned;
                MoleController.MoleDestroyed += MoleDestroyed;
                MoleController.MoleHit += MoleHit;
            }
            else
            {
                //MoleSpawnParent.MoleLookAt = LookAtAlternative;
            }
        }

        private void OnDestroy()
        {
            if (isLocalPlayer)
            {
                MoleController.MoleSpawned -= MoleSpawned;
                MoleController.MoleDestroyed -= MoleDestroyed;
                MoleController.MoleHit -= MoleHit;
            }
        }

        [Client]
        private void MoleSpawned(MoleController obj)
        {
            //Debug.Log("MoleSpawned");
            CmdSpawnMole(obj.transform.position, obj.Id, obj.ShowInTime, obj.ShowHideTime, obj.UpTime);
        }

        [Client]
        private void MoleHit(MoleController obj, Vector3 hitPosition)
        {
           // Debug.Log("MoleHit");
            CmdHitMole(obj.Id, hitPosition);
        }

        [Client]
        private void MoleDestroyed(MoleController obj)
        {
            //Debug.Log("MoleDestroyed");
            CmdDestroyMole(obj.Id);
        }

        public void Update()
        {
            if (!isLocalPlayer)
            {
                while (_delayedActions.Any())
                {
                    var delayedAction = _delayedActions[0];
                    if (delayedAction.Time + DemoDisplayInterpolatePosition.Lag > Time.unscaledTime )
                        break;
                    Debug.Log(delayedAction.Time + " " + DemoDisplayInterpolatePosition.Lag + " " + Time.unscaledTime + " " + delayedAction);
                    _delayedActions.RemoveAt(0);
                    if (delayedAction.Action != null) delayedAction.Action();
                }

            }
        }

        private List<MoleController> _moles = new List<MoleController>();
        public MoleController MoleTemplate;
        [Command]
        private void CmdSpawnMole(Vector3 position, int id, float showIn, float showHideTime, float upTime)
        {
            //Debug.Log("CmdSpawnMole");
            var action = new DelayedAction();
            action.Time = Time.unscaledTime;

            action.Action = delegate()
            {
                var mole = Instantiate(MoleTemplate);
                mole.transform.position = position + GetComponent<DemoDisplayPlayer>().Offset;
                _moles.Add(mole);

                mole.Id = id;
                mole.ShowInTime = showIn;
                mole.ShowHideTime = showHideTime;
                mole.UpTime = upTime;

                mole.StartLifecycle();
            };

            _delayedActions.Add(action);
        }

        public MoleController MoleWithId(int id)
        {
            var mole = _moles.Find(potentialMole => potentialMole.Id == id);
            return mole;
        }

        [Command]
        private void CmdDestroyMole(int id)
        {
            //Debug.Log("CmdDestroyMole");
            //var action = new DelayedAction();
            //action.Time = Time.unscaledTime;

            //action.Action = delegate()
            //{
            //    var mole = MoleWithId(id);
            //};

            //_delayedActions.Add(action);
        }

        [Command]
        private void CmdHitMole(int id, Vector3 hitPosition)
        {
            //Debug.Log("CmdHitMole");
            var action = new DelayedAction();
            action.Time = Time.unscaledTime;

            action.Action = delegate ()
            {
                var mole = MoleWithId(id);
                mole.WasHit(hitPosition);
            };

            _delayedActions.Add(action);
        }

        [Command]
        private void CmdSetScore(string score, string instruction)
        {
            Debug.Log("score " + score);
        }
    }
}

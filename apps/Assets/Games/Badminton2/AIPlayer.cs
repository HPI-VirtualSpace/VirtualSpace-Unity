using System.Collections.Generic;
using UnityEngine;
using VirtualSpace.Shared;

namespace VirtualSpace.Badminton
{

    public class AIPlayer : MonoBehaviour
    {
        [Header("Badminton properties")]
        public Transform MinOwnField;
        public Vector3 MinOwnFieldPosition { get { return MinOwnField.transform.position; } }
        public Transform MaxOwnField;
        private Vector3 MaxOwnFieldPosition { get { return MaxOwnField.transform.position; } }
        public Transform MinOtherField;
        private Vector3 MinOtherFieldPosition { get { return MinOtherField.transform.position; } }
        public Transform MaxOtherField;
        private Vector3 MaxOtherFieldPosition { get { return MaxOtherField.transform.position; } }

        [Header("AI properties")]
        [Range(.1f, 2.5f)]
        public float MinHitHeight;
        [Range(.1f, 2.5f)]
        public float MaxHitHeight;
        public Vector3 HitOutOfFieldOffset = new Vector3(1f, 0, 1f);
        
        // todo this should be determined dynamically, and it's only important for the AI
        public float ExpectedEnemyPlayerHitHeight
            { get { return Mathf.Lerp(OtherPlayer.MinHitHeight, OtherPlayer.MaxHitHeight, .5f); } }
        public float ExpectedTimeUntilEnemyHits = 1.5f;

        [Header("Virtual Space Properties")]
        public bool UseVirtualSpaceAreas;
        public VirtualSpaceHandler Handler;
        public float Safety = .3f;
        private float _nextVSPreferedHitTime;
        private float _nextVSPreferedHitDuration;

        [Header("Script References")]
        public ShuttleShockControl Shuttleshock;
        public AIPlayer OtherPlayer;

        public float DebugHitIn;
        public bool ShowTrajectoryWhenHitting;
#if DEBUG_EXPECTATIONS
        private List<float> _listOfExpectedHitTimes = new List<float>();
        private float _lastExpectation;
#endif

        void Update()
        {
            var position = Shuttleshock.transform.position;

            if (IsHittable(position) && (!UseVirtualSpaceAreas || IsCloseToVirtualSpacePrefered(position)))
            {
                Hit();
            }

            DebugHitIn -= Time.deltaTime;
            if (-10 < DebugHitIn && DebugHitIn < 0)
            {
                HitToRandomTarget();
                DebugHitIn = -11;
            }

#if DEBUG_EXPECTATIONS
            if (UseVirtualSpaceAreas)
            {
                var newExpectation = ExpectedToHitNextIn();
                var expectDelta = Mathf.Abs(_lastExpectation - newExpectation);

                if (expectDelta > 3 * Time.deltaTime)
                    Debug.Log("Expectation changed by delta: " + expectDelta);

                _lastExpectation = newExpectation;
            }
#endif
        }

        public float ExpectedToHitNextIn()
        {
            float expectedTime;
            if (Shuttleshock.LastHitByMe(this))
            {
                expectedTime = OtherPlayer.ExpectedToHitNextIn() + OtherPlayer.ExpectedTimeUntilEnemyHits;
            }
            else
            {
                expectedTime = Shuttleshock.HeightOnDownwardsSlopeReachedInSeconds(MaxHitHeight);
            }

#if DEBUG_EXPECTATIONS
            if (UseVirtualSpaceAreas)
            {
                _listOfExpectedHitTimes.Add(Time.time + expectedTime);
            }
#endif

            return expectedTime;
        }

        private void Hit()
        {
            Shuttleshock.SetDisplayTrajectory(ShowTrajectoryWhenHitting);

#if DEBUG_EXPECTATIONS
            if (UseVirtualSpaceAreas)
            {
                var now = Time.time;
                Debug.Log("Hitting now. Offset of predictions to follow.");
                foreach (var time in _listOfExpectedHitTimes)
                {
                    var expectedDelta = Mathf.Abs((now - time));
                    if (expectedDelta > .15f)
                        Debug.Log(expectedDelta);
                }
                _listOfExpectedHitTimes.Clear();
            }
#endif

            if (UseVirtualSpaceAreas)
                HitToVirtualSpaceTarget();
            else
                HitToRandomTarget();
        }

        private bool IsHittable(Vector3 position)
        {
            if (Shuttleshock.LastHitByMe(this)) return false;

            var isInHitHeight = IsInRange(position.y, MinHitHeight, MaxHitHeight);

            if (!isInHitHeight) return false;
            
            var isInPolygon = IsInRange(position, MinOwnFieldPosition - HitOutOfFieldOffset, MaxOwnFieldPosition + HitOutOfFieldOffset);

            return isInPolygon;
        }

        #region VirtualSpace
        private bool IsCloseToVirtualSpacePrefered(Vector3 position)
        {
            var nextPreferedHitHeight = Shuttleshock.HeightAtAbsoluteTime(_nextVSPreferedHitTime);

            var clampedPreferedHeight = Mathf.Clamp(nextPreferedHitHeight, MinHitHeight, MaxHitHeight);

            var deltaToPrefered = Mathf.Abs(clampedPreferedHeight - position.y);
            var isCloseToPreferedHeight = deltaToPrefered < 0.1;

            //if (isCloseToPreferedHeight)
            //{
            //    Debug.Log("Hitting now");
            //    Debug.Log("prefered height: " + clampedPreferedHeight);
            //    Debug.Log("delta to prefered: " + deltaToPrefered);
            //}

            return isCloseToPreferedHeight;
        }

        public void SetVirtualSpacePreferedHitParameter(float NextHitTime, float NextHitDuration)
        {
            // todo this shouldn't change too often
            // based on those parameters, find a trajectory that goes over the net


            _nextVSPreferedHitTime = NextHitTime;
            _nextVSPreferedHitDuration = NextHitDuration;

            Vector3 positionAtSpawn;
            List<Vector3> areaAtSpawn;

            Handler.SpaceAtTimeWithSafety(
                _nextVSPreferedHitTime - VirtualSpaceTime.CurrentTimeInSeconds +
                _nextVSPreferedHitDuration, Safety, out areaAtSpawn, out positionAtSpawn);

            var hitsNet = Shuttleshock.ShotHitsNet(
                Shuttleshock.PositionAtTime(_nextVSPreferedHitTime - (VirtualSpaceTime.CurrentTimeInSeconds - Shuttleshock.TimePassedSinceLastHit)),
                positionAtSpawn,
                _nextVSPreferedHitDuration);

            if (hitsNet)
            {
                Debug.Log("Shot would hit net");
            }

        }

        void HitToVirtualSpaceTarget()
        {
            Vector3 positionAtSpawn;
            List<Vector3> areaAtSpawn;

            var flightDuration = _nextVSPreferedHitTime - Time.time + _nextVSPreferedHitDuration;

            Debug.Log("hitting, delta now to prefered: " + (_nextVSPreferedHitTime - Time.time));
            // todo get the end of a transition, this needs to be the length of the execution time
            // todo get point similar to whac
            Handler.SpaceAtTimeWithSafety(_nextVSPreferedHitDuration, Safety, out areaAtSpawn, out positionAtSpawn);

            VirtualSpaceHandler.DrawPolygonLines(areaAtSpawn, Color.red, 2f);

            var leveledPositionAtSpawn = positionAtSpawn.Copy();
            positionAtSpawn.y = ExpectedEnemyPlayerHitHeight;

            Debug.DrawLine(positionAtSpawn, leveledPositionAtSpawn, Color.red, 2f);

            Shuttleshock.ShootFromCurrentPositionTo(this, positionAtSpawn, ExpectedTimeUntilEnemyHits);
        }
        #endregion

        // AI HIT METHOD
        void HitToRandomTarget()
        {
            // this should of course check if it's hitting the bounds
            var xPosition = Random.Range(MinOtherFieldPosition.x, MaxOtherFieldPosition.x);
            var zPosition = Random.Range(MinOtherFieldPosition.z, MaxOtherFieldPosition.z);
            var yPosition = ExpectedEnemyPlayerHitHeight;
            var pointTo = new Vector3(xPosition, yPosition, zPosition);

            Shuttleshock.ShootFromCurrentPositionTo(this, pointTo, ExpectedTimeUntilEnemyHits);
        }


        #region Helper
        private bool IsInRange(float it, float smaller, float larger)
        {
            return smaller <= it && it <= larger;
        }

        private bool IsInRange(Vector3 it, Vector3 smaller, Vector3 larger)
        {
            return CompleteSmallerOrEqual(smaller, it) &&
                CompleteSmallerOrEqual(it, larger);
        }

        private bool CompleteSmallerOrEqual(Vector3 a, Vector3 b)
        {
            return a.x <= b.x && a.z <= b.z;
        }
        #endregion

    }
}
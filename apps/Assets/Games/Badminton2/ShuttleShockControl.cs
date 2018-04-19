using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualSpace.Badminton
{
    public class ShuttleShockControl : MonoBehaviour
    {
        // REFERENCES
        public BadmintonTrajectory Trajectory;
        public AIPlayer Player1;
        public AIPlayer Player2;

        // STATUS
        private float _timestampLastServe;
        private AIPlayer _lastHitHyPlayer;
        private float _timePassedSinceHit = 0f;
        public float TimePassedSinceLastHit {
            get {
                return _timePassedSinceHit;
            }
        }


        public float NetHeight = 1.5f;
        public float NetZValue = 3.72f;

        // HELPER
        public float Now { get { return Time.unscaledTime; } }

        void Update()
        {
            // sometimes a collider is not hit but the ball stops nonetheless
            if (IsOnGround() && LastServeIsOutOfDeltaTime()) LetNextPlayerServe();

            MoveAlongTrajectory();
        }

        // STATE SETTER
        private void MoveAlongTrajectory()
        {
            _timePassedSinceHit += Time.unscaledDeltaTime;

            var newPosition = Trajectory.PositionAtTime(_timePassedSinceHit);

            if (newPosition.y >= 0)
            {
                transform.position = newPosition;
            }
        }

        // STATE GETTER
        private bool IsOnGround()
        {
            return transform.position.y < .01;
        }

        private bool LastServeIsOutOfDeltaTime()
        {
            return Time.time - _timestampLastServe > .5f;
        }

        public bool LastHitByMe(AIPlayer player)
        {
            return _lastHitHyPlayer == player;
        }

        // TODO racket
        //public void HitWithRacket(Vector3 velocity, Vector3 racketOrientation, float racketDampFactor)
        //{

        //}

        // TRAJECTORY DISPLAY PROXY (todo pretty?)
        public void SetDisplayTrajectory(bool show)
        {
            Trajectory.SetDisplayTrajectory(show);
        }

        // HITTING
        public Vector3 PositionAtTime(float t)
        {
            return Trajectory.PositionAtTime(_timePassedSinceHit);
        }

        public void ShootFromCurrentPositionTo(AIPlayer player, Vector3 pointTo, float timeAt)
        {
            ShootFromTo(player, transform.position, pointTo, timeAt);
        }

        public bool ShotHitsNet(Vector3 pointFrom, Vector3 pointTo, float timeAt) {
            var velocity = Trajectory.VelocityForPointAtTimeFrom(pointFrom, pointTo, timeAt);

            var netPosition = Trajectory.PositionAtZ(NetZValue, pointFrom, velocity);

            return netPosition.y < NetHeight;
        }

        private void ShootFromTo(AIPlayer player, Vector3 pointFrom, Vector3 pointTo, float timeAt)
        {
            Debug.Log("Shuttlecock hit by player " + player.name);
            _lastHitHyPlayer = player;

            Trajectory.Parameters.StartPosition = pointFrom;
            PlaceOnStartOfTrajectory();

            Trajectory.Parameters.Velocity = Trajectory.VelocityForPointAtTimeFrom(pointFrom, pointTo, timeAt);
        }

        // RESET
        private void PlaceOnStartOfTrajectory()
        {
            _timePassedSinceHit = 0;
        }

        // CALCULATION PROXIES
        public float HeightOnDownwardsSlopeReachedInSeconds(float height)
        {
            return Trajectory.TimeToDownwardsHeight(height) - _timePassedSinceHit;
        }

        public float HeightAtAbsoluteTime(float time)
        {
            return Trajectory.PositionAtTime(time - Now + _timePassedSinceHit).y;
        }

        private void OnCollisionEnter(Collision collision)
        {
            Debug.Log("Collided with " + collision.collider.name + "(" + collision.collider.tag + ")");
            // todo exclude racket
            LetNextPlayerServe();
        }

        private void LetNextPlayerServe()
        {
            // todo this needs to check for the side, assign points, etc.
            _timestampLastServe = Time.time;
            AIPlayer playerToServe = LastHitByMe(Player1) ? Player2 : Player1;
            playerToServe.DebugHitIn = .5f;
        }
    }
}
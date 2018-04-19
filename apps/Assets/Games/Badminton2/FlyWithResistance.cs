using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualSpace.Badminton
{
    [RequireComponent(typeof(Rigidbody))]
    public class FlyWithResistance : MonoBehaviour
    {

        public BadmintonTrajectory Trajectory;
        private Rigidbody _rigidbody;
        public float AirResistanceFactor = .2f;
        private TrailRenderer _trailRenderer;

        void Start()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _trailRenderer = GetComponent<TrailRenderer>();

            ShootOut();
        }

        void ShootOut()
        {
            transform.position = Trajectory.Parameters.StartPosition;
            _trailRenderer.Clear();
            _rigidbody.velocity = Trajectory.Parameters.Velocity;
        }

        void FixedUpdate()
        {
            var airResistance = -AirResistanceFactor * _rigidbody.velocity * _rigidbody.velocity.magnitude;

            _rigidbody.velocity += Trajectory.Parameters.BallGravity * Time.fixedDeltaTime;
            _rigidbody.velocity += airResistance * Time.fixedDeltaTime;

            if (transform.position.y < 0)
            {
                ShootOut();
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            Debug.Log("Collision entered with " + collision.collider.name + " : " + collision.collider.tag);
            ShootOut();
        }
    }
}
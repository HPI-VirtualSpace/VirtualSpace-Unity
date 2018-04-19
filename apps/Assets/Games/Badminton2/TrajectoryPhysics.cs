using System;
using UnityEngine;

namespace VirtualSpace.Badminton
{
    public interface TrajectoryPhysics
    {
        void SetParameters(TrajectoryParameters parameters);

        float TimeToZValue(float z, Vector3 startPosition, Vector3 initialVelocity);

        float TimeToHeightOnDownwardsSlope(float height = 0);

        Vector3 PositionAtTime(float timeAtPoint);

        Vector3 InitialVelocityForPointAtTimeFromTo(Vector3 pointFrom, Vector3 pointTo, float timeAt);
    }

    public abstract class ZeroPointFinder
    {
        protected Func<float, float> _function;
        protected float _minGuess;
        protected float _maxGuess;

        public void SetFunction(Func<float, float> function)
        {
            _function = function;
        }

        public void SetGuesses(float minGuess, float maxGuess)
        {
            _minGuess = minGuess;
            _maxGuess = maxGuess;
        }

        public abstract float ZeroPoint();
    }

    // implementation based on this: http://numerik.uni-hd.de/~lehre/SS12/numerik0/2-nullstellen.pdf
    public class IntervalWrapping : ZeroPointFinder
    {
        private int _numIterations = 20;
        private float _epsilon = 0.001f;

        public override float ZeroPoint()
        {
            var a = _minGuess;
            var fA = _function(a);
            var b = _maxGuess;
            var fB = _function(b);

            float x = 0;
            float fX = 0;

            for (int i = 0; i < _numIterations; i++)
            {
                x = (a + b) / 2;
                fX = _function(x);

                if (Math.Abs(fX) < _epsilon)
                {
                    //Debug.Log("Converged with f(" + x + ") = " + fX + " at iteration " + i);
                    return x;
                }

                if (fA * fX < 0)
                {
                    b = x;
                    fB = fX;
                } else
                {
                    a = x;
                    fA = fX;
                }
            }

            Debug.LogWarning("Converged with f(" + x + ") = " + fX + " due to iteration limit " + _numIterations);
            return x;
        }
    }

    public class NoAirResistanceTrajectory : TrajectoryPhysics
    {
        private TrajectoryParameters _parameters;
        
        public void SetParameters(TrajectoryParameters parameters)
        {
            _parameters = parameters;
        }

        public float TimeToHeightOnDownwardsSlope(float height = 0)
        {
            var A = _parameters.BallGravity.y / 2;
            var B = _parameters.Velocity.y;
            var C = _parameters.StartPosition.y - height;

            var timeOfImpact = (
                    -B -
                    Mathf.Sqrt(B * B - 4 * A * C)
                )
                / (2 * A);

            return timeOfImpact;
        }

        public Vector3 PositionAtTime(float timeAtPoint)
        {
            return _parameters.StartPosition +
                timeAtPoint * _parameters.Velocity +
                (timeAtPoint * timeAtPoint) * _parameters.BallGravity / 2;
        }

        public Vector3 InitialVelocityForPointAtTimeFromTo(Vector3 pointFrom, Vector3 pointTo, float timeAt)
        {
            return (pointTo - pointFrom - timeAt * timeAt * _parameters.BallGravity / 2) / timeAt;
        }

        public float TimeToZValue(float z, Vector3 startPosition, Vector3 initialVelocity)
        {
            throw new NotImplementedException();
        }
    }

    // TODO split in static and dynamic parameters
    // TODO temporary pamaters g and vt whenever the static parameter change
    // implementation based on this: http://farside.ph.utexas.edu/teaching/336k/Newtonhtml/node29.html
    public class WithSimplifiedAirResistanceTrajectory : TrajectoryPhysics
    {
        private TrajectoryParameters _parameters;
        private ZeroPointFinder _zeroFinder;

        public WithSimplifiedAirResistanceTrajectory()
        {
            _zeroFinder = new IntervalWrapping();
        }

        public void SetParameters(TrajectoryParameters parameters)
        {
            _parameters = parameters;
        }

        public float TimeToHeightOnDownwardsSlope(float height = 0)
        {
            return TimeToHeightOnDowanwardsSlope(height, _parameters.StartPosition, _parameters.Velocity);
        }
        
        public float TimeToHeightOnDowanwardsSlope(float h, Vector3 s0, Vector3 v0)
        {
            var g = -_parameters.BallGravity.y;
            var vt = _parameters.Mass * g / _parameters.C;

            Func<float, float> yTrajectoryMinusHeight = delegate (float t)
            {
                return s0.y
                    + vt / g * (v0.y + vt) * (1 - Mathf.Exp(-g * t / vt)) - vt * t
                    - h;
            };

            // get the time of the highest point of trajectory
            var highTime = (-1) * (vt / g) * (float)Math.Log(vt / (v0.y + vt));

            _zeroFinder.SetFunction(yTrajectoryMinusHeight);
            _zeroFinder.SetGuesses(highTime, 10);

            var timeGuess = _zeroFinder.ZeroPoint();

            //Debug.Log("High time was " + highTime);
            //Debug.Log("Guessing height " + height + " will be reached in " + timeGuess);

            return timeGuess;
        }

        public float TimeToZValue(float z)
        {
            return TimeToZValue(z, _parameters.StartPosition, _parameters.Velocity);
        }

        public float TimeToZValue(float z, Vector3 s0, Vector3 v0)
        {
            var g = -_parameters.BallGravity.y;
            var vt = _parameters.Mass * g / _parameters.C;

            Func<float, float> zTrajectory = delegate (float t)
            {
                return s0.z
                    + (v0.z * vt / g) * (1 - Mathf.Exp(-g * t / vt))
                    - z;
            };

            _zeroFinder.SetFunction(zTrajectory);
            _zeroFinder.SetGuesses(0.001f, 10);

            return _zeroFinder.ZeroPoint();
        }

        public Vector3 PositionAtTime(float timeAtPoint)
        {
            return PositionAtTime(timeAtPoint, _parameters.StartPosition, _parameters.Velocity);
        }

        public Vector3 PositionAtTime(float t, Vector3 s0, Vector3 v0)
        {
            var g = -_parameters.BallGravity.y;
            var vt = _parameters.Mass * g / _parameters.C;
            var e = 1 - Mathf.Exp(-g * t / vt);

            var x = s0.x + (v0.x * vt / g) * e;
            var z = s0.z + (v0.z * vt / g) * e;
            var y = s0.y + vt / g * (v0.y + vt) * e - vt * t;
            
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Has a single solution. The x-z coordinates of the trajectory at time t can only be solved by a single curve (there can't be multiple solutions). 
        /// If velocity for x or z was different (to make the ball slower or faster - go shallower or steeper, it will be not at the point at t anymore.
        /// This is contradictory to my initial intuition that if I fix x-z orientation and variate the y-launch angle and y-launch strength, I would get multiple 
        /// curves that all reach the point at time t.
        /// Stays here so I don't forget: 
        /// OR is there? Think about a normal trajectory. Pick point p. Isn't there a second trajectory, with an insanely fast initial v0, almost going for in a straight
        /// line. NO, of course not. It will go through the point. But it will reached the point way quicker... 
        /// </summary>
        /// <returns>Velocity to be a applied at the pointFrom to reach pointTo at timeAt.</returns>
        public Vector3 InitialVelocityForPointAtTimeFromTo(Vector3 pointFrom, Vector3 pointTo, float timeAt)
        {
            var g = -_parameters.BallGravity.y;
            var vt = _parameters.Mass * g / _parameters.C;
            var t = timeAt;
            var e = 1 - Mathf.Exp(-g * t / vt);
            
            // velocity is non-constant because of e
            // -from : from is 0 point
            var v0x = (pointTo.x - pointFrom.x) * g / vt / e;
            var v0z = (pointTo.z - pointFrom.z) * g / vt / e;
            var v0y = (pointTo.y - pointFrom.y + vt * t) * g / vt / e - vt;

            var recommendedVelocity = new Vector3(v0x, v0y, v0z);

            return recommendedVelocity;
        }

    }

    [System.Serializable]
    public class TrajectoryParameters
    {
        [Header("Dynamic Properties")]
        public Vector3 StartPosition;
        public Vector3 Velocity;

        [Header("Static Properties")]
        public Vector3 BallGravity;
        public float GroundHeight = 0;
        public float Mass = 1;
        public float C = 1.2f;
    }
}
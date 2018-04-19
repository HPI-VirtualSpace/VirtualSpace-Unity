using UnityEngine;

namespace VirtualSpace.Badminton
{
    public class BadmintonTrajectory : MonoBehaviour
    {
        public TrajectoryParameters Parameters;

        [Header("Calculation Properties")]
        public int NumCalculationPoints = 100;

        [Header("Script References")]
        public TrajectoryRenderer TrajectoryRenderer;
        public TrajectoryPhysics TrajectoryPhysics;

        [Header("State")]
        public bool ShouldShowDisplayedTrajectory;

        public void SetDisplayTrajectory(bool show)
        {
            if (show && !ShouldShowDisplayedTrajectory)
                UpdateDisplayedTrajectory();

            ShowDisplayedTrajectory(show);
        }

        private void Start()
        {
            TrajectoryPhysics = new WithSimplifiedAirResistanceTrajectory();
            TrajectoryPhysics.SetParameters(Parameters);
        }

        void Update()
        {
            UpdateDisplayedTrajectory();
        }

        private void UpdateDisplayedTrajectory()
        {
            if (!ShouldShowDisplayedTrajectory) return;

            float timeOfHittingGround = 
                TrajectoryPhysics.TimeToHeightOnDownwardsSlope(Parameters.GroundHeight);

            var pointArray = new Vector3[NumCalculationPoints];

            for (var i = 0; i < NumCalculationPoints; i++)
            {
                var timeAtPoint = (float)i / (NumCalculationPoints - 1) * timeOfHittingGround;

                pointArray[i] = PositionAtTime(timeAtPoint);
            }

            TrajectoryRenderer.SetPoints(pointArray);
        }

        public void ShowDisplayedTrajectory(bool show)
        {
            ShouldShowDisplayedTrajectory = show;
            TrajectoryRenderer.gameObject.SetActive(show);
        }

        public float TimeToDownwardsHeight(float height = 0)
        {
            return TrajectoryPhysics.TimeToHeightOnDownwardsSlope(height);
        }

        public Vector3 PositionAtTime(float timeAtPoint)
        {
            return TrajectoryPhysics.PositionAtTime(timeAtPoint);
        }

        public Vector3 VelocityForPointAtTimeFrom(Vector3 pointFrom, Vector3 pointTo, float timeAt)
        {
            return TrajectoryPhysics.InitialVelocityForPointAtTimeFromTo(pointFrom, pointTo, timeAt);
        }

        public Vector3 PositionAtZ(float z, Vector3 startPosition, Vector3 appliedVelocity)
        {
            var time = TrajectoryPhysics.TimeToZValue(z, startPosition, appliedVelocity);
            return TrajectoryPhysics.PositionAtTime(time);
        }
    }
}
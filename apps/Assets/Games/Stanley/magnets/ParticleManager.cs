using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleManager : MonoBehaviour
{
    public float CalculationInterval = .01f;
    private List<ChargedParticle> _particles = new List<ChargedParticle>();
    private List<FreelyMovingChargedParticle> _movingParticles = new List<FreelyMovingChargedParticle>();
    public float ForceMultiplier = 1000f;
    public float ForceDistanceEpsilon = 0.01f;

    private float _lastCalculation = -1f;

    // only if within certain distance
    private void ApplyForce(FreelyMovingChargedParticle particle)
    {
        Vector3 particleForce = Vector3.zero;

        foreach (var otherParticle in _particles)
        {
            if (particle == otherParticle) continue;

            var vectorDifference = 
                particle.transform.position - otherParticle.transform.position;

            var distance = vectorDifference.magnitude;

            if (distance < ForceDistanceEpsilon || distance > otherParticle.InfluenceLength) continue;

            // coloumb's law
            var force = ForceMultiplier
                * particle.Charge * otherParticle.Charge 
                / Mathf.Pow(distance, 2);

            particleForce += vectorDifference.normalized * force * CalculationInterval;
        }

        particle.Rigidbody.AddForce(particleForce);
    }

    private void FixedUpdate()
    {
        // what if time.delta is lower than calculation interval?
        // calculation at the same time
        //Debug.Log("Calc diff: " + (Time.fixedTime - _lastCalculation));

        if (Time.fixedTime - _lastCalculation > CalculationInterval)
        {
            //Debug.Log("Calculating forces");
            // perhaps only update partially
            foreach (var particle in _movingParticles)
            {
                ApplyForce(particle);
            }

            _lastCalculation = Time.fixedTime;
        }
    }

    // todo check if added twice or other foo
    public void RegisterParticle(ChargedParticle particle)
    {
        _particles.Add(particle);
        var movingParticle = particle as FreelyMovingChargedParticle;
        if (movingParticle != null)
        {
            _movingParticles.Add(movingParticle);
        }
    }

    public void UnregisterParticle(ChargedParticle particle)
    {
        _particles.Remove(particle);
        var movingParticle = particle as FreelyMovingChargedParticle;
        if (movingParticle != null)
        {
            _movingParticles.Remove(movingParticle);
        }
    }
}

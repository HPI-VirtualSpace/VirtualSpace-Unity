using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChargedParticle : MonoBehaviour
{
    public float Charge = 1f;
    private ParticleManager _particleManager;
    public float InfluenceLength = .2f;

    void OnEnable()
    {
        if (_particleManager == null) _particleManager = FindObjectOfType<ParticleManager>();
        _particleManager.RegisterParticle(this);
    }

    void OnDisable()
    {
        _particleManager.UnregisterParticle(this);
    }
}

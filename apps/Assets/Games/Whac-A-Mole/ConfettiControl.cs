using System.Collections;
using System.Collections.Generic;
using EasyButtons;
using UnityEngine;

public class ConfettiControl : MonoBehaviour
{
    public ParticleEmitter[] Emitter;
    public float DeactivateAfter = 2f;

    [Button("EmitTest")]
    void Emit()
    {
        foreach (var emitter in Emitter)
        {
            emitter.emit = true;
        }
        StartCoroutine(StopEmission());
    }

    IEnumerator StopEmission()
    {
        yield return new WaitForSeconds(DeactivateAfter);
        foreach (var emitter in Emitter)
        {
            emitter.emit = false;
        }
    }

	void Start () {
		
	}
	
	void Update () {
		
	}
}

using System;
using UnityEngine;

public class DetectIfEntered : MonoBehaviour
{
    public string ObjectName;
    public Action OnTriggerEnterAction;
    public Action<Collider> OnTriggerEnterColliderAction;
    
    [InspectorButton("WasEntered")]
    public bool SimulateEntered;

    private void WasEntered(Collider other)
    {
        if (OnTriggerEnterAction != null)
            OnTriggerEnterAction.Invoke();
        if (OnTriggerEnterColliderAction != null)
            OnTriggerEnterColliderAction.Invoke(other);
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.name.Equals(ObjectName))
        {
            WasEntered(other);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomControl : MonoBehaviour
{
    protected RoomManager _roomManager;
    // debug
    [InspectorButton("InitializeRoom")]
    public bool Initialize;
    [InspectorButton("UserEntered")]
    public bool Entered;
    [InspectorButton("UserAboutToFinish")]
    public bool Almost;
    [InspectorButton("UserFinished")]
    public bool Finished;
    [InspectorButton("ResetRoom")]
    public bool Reset;

    public void Start()
    {
        _roomManager = FindObjectOfType<RoomManager>();
    }

    public virtual void InitializeRoom()
    {
        
    }

    public virtual void ResetRoom()
    {
        
    }

    protected virtual void UserEntered()
    {
        _roomManager.Entered(this);
    }

    protected void UserAboutToFinish()
    {
        _roomManager.AboutToFinish(this);
    }

    protected void UserFinished()
    {
        _roomManager.UserFinished(this);
    }
    
    // also the enter function
}

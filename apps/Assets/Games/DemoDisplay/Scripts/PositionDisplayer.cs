using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;

public class PositionDisplayer : MonoBehaviour
{
    public Users Users;
    public GameObject CircleTemplate;

    private Dictionary<User, GameObject> _usersToCircles = new Dictionary<User, GameObject>();

	void Update () {
	    foreach (var user in Users.All)
	    {
	        if (user.IsActive && user.IsInitialized)
	        {
	            if (!_usersToCircles.ContainsKey(user))
	            {
	                var userCircle = Instantiate(CircleTemplate);
	                userCircle.name = "User" + user.Id + "Circle";
	                userCircle.transform.parent = transform;
                    var userRenderer = userCircle.GetComponent<Renderer>();
                    
	                userRenderer.material = user.AreaMaterial;

                    _usersToCircles.Add(user, userCircle);
	            }

                _usersToCircles[user].transform.position = 
                    transform.TransformPoint(user.Position);
	        }
	        else
	        {
	            if (_usersToCircles.ContainsKey(user))
	            {
                    Destroy(_usersToCircles[user]);
                    _usersToCircles.Remove(user);
	            }
	        }
        }
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceInvadersMovementHints : MonoBehaviour
{
    public Vector3 firstPosition;
    public Vector3 secondPosition;
    public GameObject arrow;

    public int numArrows = 3;
    private List<GameObject> _instantiatedArrows;

    void Start()
    {
        Debug.Log("Instantiating arrows");
        _instantiatedArrows = new List<GameObject>(numArrows);
        for (int i = 0; i < numArrows; i++)
        {
            _instantiatedArrows.Add(Instantiate(arrow));
            _instantiatedArrows[i].transform.parent = transform;
            _instantiatedArrows[i].SetActive(false);
        }
    }

	// Update is called once per frame
	void Update () {
        Debug.DrawLine(firstPosition, secondPosition);

	    if (firstPosition == null || secondPosition == null || firstPosition == Vector3.zero || secondPosition == Vector3.zero)
	    {
	        HideArrows();
            return;
	    }


        if (Vector3.Distance(firstPosition, secondPosition) >= .2f)
	    {
            firstPosition.y += .1f;
            secondPosition.y += .1f;

            for (int i = 0; i < numArrows; i++)
	        {
                Debug.Log("Arrow size: " + _instantiatedArrows.Count);
	            Vector3 arrowPosition = firstPosition + (secondPosition - firstPosition) * i / numArrows;
                
                _instantiatedArrows[i].transform.position = arrowPosition;
                
                _instantiatedArrows[i].transform.LookAt(secondPosition);

                _instantiatedArrows[i].transform.rotation = Quaternion.Euler(0, _instantiatedArrows[i].transform.rotation.eulerAngles.y, 0);

                _instantiatedArrows[i].SetActive(true);
	        }
	    }
	    else
	    {
	        HideArrows();
	    }
	}

    void HideArrows()
    {
        _instantiatedArrows.ForEach(arrow => arrow.SetActive(false));
    }
}

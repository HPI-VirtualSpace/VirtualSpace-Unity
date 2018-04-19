using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnablerDisabler : MonoBehaviour {

    public GameObject Reference;

    private void OnEnabled()
    {
        Reference.SetActive(true);
    }

    private void OnDisabled()
    {
        Reference.SetActive(false);
    }
}

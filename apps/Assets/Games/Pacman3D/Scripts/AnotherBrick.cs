using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnotherBrick : MonoBehaviour {

    public Material ErrorMaterial;
    public Collider PacmanCollider;

    private Material _normalMaterial;
    private Renderer _renderer;
    private bool _collidedWithPacman;

    void Start () {
        _renderer = GetComponent<Renderer>();
        _normalMaterial = _renderer.material;
	}
	
	public void ChangeMaterial(bool errorMat)
    {
        _renderer.material = errorMat && _collidedWithPacman ? ErrorMaterial : _normalMaterial;
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider == PacmanCollider)
            _collidedWithPacman = true;
    }

    private void OnTriggerExit(Collider collider)
    {
        if(collider == PacmanCollider)
            _collidedWithPacman = false;
    }
}

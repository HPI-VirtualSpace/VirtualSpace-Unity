using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PacTile : MonoBehaviour {

    public List<PacTile> Neighbors;
    public Material ActiveMaterial;
    private Material _passiveMaterial;
    private Renderer _renderer;

	// Use this for initialization
	void Start () {
        _renderer = GetComponentInChildren<Renderer>();
        _passiveMaterial = _renderer.material;
	}

    public Collider GetCollider()
    {
        return GetComponentsInChildren<Collider>().Last();
    }

    public void SetActiveTile(bool act)
    {
        _renderer.material = act ? ActiveMaterial : _passiveMaterial;
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class UnityPolygon : MonoBehaviour
{
    private Triangulator _triangulator;

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;

    public Material Material
    {
        set { _meshRenderer.material = value; }
    }

    public Vector2[] Points =
    {
        new Vector2(0, 0),
        new Vector2(1, 1),
        new Vector3(1, 2)
    };

    void Awake()
    {
        _triangulator = new Triangulator();

        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();
    }
	
	void Update () {
	    var uvs = FindUVs();
	    var triangulation = _triangulator.TriangulatePolygon(Points);

	    if (_meshFilter.mesh == null)
	    {
	        _meshFilter.mesh = new Mesh();
	        _meshFilter.mesh.name = "PolygonMesh";
        }
        _meshFilter.mesh.Clear();
	    _meshFilter.mesh.SetVertices(
            Points.Select(point => new Vector3(point.x, point.y, 0)).ToList()
        );
        _meshFilter.mesh.SetTriangles(triangulation, 0);
	    _meshFilter.mesh.SetUVs(0, uvs.ToList());

        _meshFilter.mesh.RecalculateNormals();
	    _meshFilter.mesh.RecalculateBounds();
    }

    private Vector2[] FindUVs()
    {
        var uvs = new Vector2[Points.Length];

        for (var x = 0; x < Points.Length; x++)
        {
            if ((x % 2) == 0)
            {
                uvs[x] = new Vector2(0, 0);
            }
            else
            {
                uvs[x] = new Vector2(1, 1);
            }
        }
        return uvs;
    }
}

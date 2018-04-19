using System.Collections.Generic;
using System.Linq;
using AutoTiling;
using UnityEngine;
using UnityEngine.PostProcessing;
using UnityEngine.UI;
using VirtualSpace.Shared;
using VirtualSpace.Shared;

namespace VirtualSpaceVisuals
{
    public class VirtualSpacePlayerArea : MonoBehaviour
    {

        //[Header("change this:")]
        [Header("maybe change this:")]
        public VirtualSpaceAnotherArrow Arrow;
        public float WallYEnd = 3f;
        public float WallYStart = -1f;
        public PostProcessingProfile Profile;
        [Header("don't change this:")] 

        public VirtualSpaceBreachVisuals BreachVisuals;
        public Text BreachText;
        public RawImage ArrowImage;
        public VirtualSpaceHandler Handler;
        public Transform Reference;
        public MeshFilter GroundMeshFilter;
        public Material WallMaterial;
        public Material SimpleWallMaterial;
        public string Layer = "VirtualSpace";
        public List<Vector3> MeshPoints;
        public List<Vector3> WallPoints;
        [HideInInspector] public Vector3 CenterOfArea;
        [HideInInspector] public List<Vector3> WithoutOffset;
        [HideInInspector] public List<Vector3> WithOffset;

        private Vector3 _meshCenter;
        private MeshFilter _wallMeshFilter;
        private float Alpha
        {
            set
            {
                var alpha = value < 0.001f ? 0f : value;
                if (Handler.Settings.ShowGround)
                {
                    var col = Handler.Settings.OverallColor;
                    col.a = alpha;
                    _renderer.material.color = col;
                }
            }
            get
            {
                return _renderer.material.color.a;
            }
        }
        private float WallAlphaDist
        {
            set
            {
                if (Handler.Settings.ShowWall)
                {
                    var alpha = value < 0.001f ? 0f : value;
                    alpha *= 0.6f;
                    _wallRenderer.materials[1].SetFloat("_Transparency", 1f - alpha);
                    //Debug.Log("transparency " + (1f - alpha ) + " " + _wallRenderer.materials[1].GetFloat("_Transparency"));
                    //Debug.Log("color " + (1f - alpha) + " " + _wallRenderer.materials[1].GetColor("_Diffusecolor"));
                }
            }
        }
        private float Factor
        {
            get
            {
                return _factor;
            }
            set
            {
                if (Mathf.Abs(_factor - value) < 0.001f)
                    return;
                _factor = Mathf.Clamp01(value);
                if (_factor < 0.001f)
                {
                    if (_ppbehavior != null)
                        _ppbehavior.enabled = false;
                }
                else
                {
                
                    if (_ppbehavior != null)
                    {
                        _ppbehavior.enabled = true;
                        var settings = _ppbehavior.profile.colorGrading.settings;
                        settings.basic.temperature = _factor * _temparature;
                        settings.basic.tint = _factor * _tint;
                        settings.basic.hueShift = _factor * _hueShift;
                        settings.basic.saturation = _factor * _saturation + (1f - _factor);
                        settings.basic.contrast = _factor * _contrast + (1f - _factor);
                        _ppbehavior.profile.colorGrading.settings = settings;
                    }
                }
            }
        }
        private PostProcessingBehaviour _ppbehavior;
        [HideInInspector]
        public bool InWrongZone = false;

        private Renderer _wallRenderer;
        private Renderer _renderer;
        //private Color _standardColor;
        private Transform _wallObject;
        //private GameObject _wallFather;
        private DynamicTextureTiling _dtt;
        private float _factor;
        private float _temparature;
        private float _tint;
        private float _hueShift;
        private float _saturation;
        private float _contrast;
        private bool _init;
        void Init()
        {
            CenterOfArea = Vector3.zero;
            if (Handler.Settings.ShowGrey)
            {
                _ppbehavior = Camera.main.gameObject.AddComponent<PostProcessingBehaviour>();
                _ppbehavior.profile = Profile;
            }

            _renderer = GetComponent<Renderer>();
            _renderer.material.color = Handler.Settings.OverallColor;
            BreachVisuals.PlayerMaterial.color = Handler.Settings.EnemyBreachColor;
            //_standardColor = _renderer.material.color;
            ArrowImage.color = Handler.Settings.OverallColor;
            BreachText.color = Handler.Settings.OverallColor;

            if (Handler.Settings.ShowWall)
            {
               // _wallFather = new GameObject();
               // _wallFather.transform.parent = transform;
              //  _wallFather.transform.localPosition = Vector3.zero;
                var wall = new GameObject();
                _wallObject = wall.transform;
                wall.transform.parent = transform;// _wallFather.transform;
                wall.transform.localPosition = Vector3.zero;
                wall.transform.localRotation = Quaternion.identity;
                wall.name = "wall";
                wall.layer = LayerMask.NameToLayer(Layer);
                _wallRenderer = wall.AddComponent<MeshRenderer>();
                _wallRenderer.materials = (new List<Material> { WallMaterial, SimpleWallMaterial }).ToArray();//UseSimpleWall ? SimpleWallMaterial : WallMaterial;
                _wallMeshFilter = wall.AddComponent<MeshFilter>();
                //var mat = _wallRenderer.material;
                //if (!UseSimpleWall)
                //{
                    for (var i = 0; i < Handler.Settings.WallMeshTextures.Count; i++)
                    {
                        var factor = 1f / (Handler.Settings.WallMeshTextures.Count + 1);
                    _wallRenderer.materials[0].SetFloat("_MaxDistance" + (i + 1), Handler.Settings.Safety * (factor + (1f - factor) * (i + 1) / Handler.Settings.WallMeshTextures.Count));
                    _wallRenderer.materials[0].SetFloat("_MinDistance" + (i + 1), Handler.Settings.Safety * (factor + (1f - factor) * i / Handler.Settings.WallMeshTextures.Count));
                        var color = Handler.Settings.WallColors[i];
                        var colorNoAlpha = color;
                        colorNoAlpha.a = 0f;
                    _wallRenderer.materials[0].SetColor("_MinColor" + (i + 1), color);
                    _wallRenderer.materials[0].SetColor("_MaxColor" + (i + 1), colorNoAlpha);
                    _wallRenderer.materials[0].SetTexture("_Tex" + (i + 1), Handler.Settings.WallMeshTextures[i]);
                    }
                    _dtt = _wallMeshFilter.gameObject.AddComponent<DynamicTextureTiling>();
                //} else
                //{
                _wallRenderer.materials[1].color = Handler.Settings.SimpleWallColor;
                _wallRenderer.materials[1].SetColor("_Diffusecolor", Handler.Settings.SimpleWallColor);
                _wallRenderer.materials[1].SetColor("_Speccolor", Handler.Settings.SimpleWallColor);
                _wallRenderer.materials[1].SetTexture("_MainTex", Handler.Settings.SimpleWallTexture);
                //}
                
            }
        
            SetNewPositionsWithOffset(MeshPoints);
            if (Handler.Settings.ShowWall)// && !UseSimpleWall)
            {
                _dtt.unwrapMethod = UnwrapType.CubeProjection;
                _dtt.useUnifiedOffset = true;
                _dtt.useUnifiedScaling = true;
                _dtt.topScale = Vector2.one * Handler.Settings.Tiling;
                _dtt.CreateMeshAndUVs();
                _dtt.enabled = false;
            }
            if (_ppbehavior != null)
            {
                _temparature = _ppbehavior.profile.colorGrading.settings.basic.temperature;
                _tint = _ppbehavior.profile.colorGrading.settings.basic.tint;
                _hueShift = _ppbehavior.profile.colorGrading.settings.basic.hueShift;
                _saturation = _ppbehavior.profile.colorGrading.settings.basic.saturation;
                _contrast = _ppbehavior.profile.colorGrading.settings.basic.contrast;
                Factor = 1f;
            }
            
            Time.timeScale = 1f;
            AudioListener.volume = 1f;
            _init = true;
        }

        private void SetNewPositionsWithOffset(List<Vector3> positions)
        {
            var vecs = positions.Select(p => new Vector(p.x, p.z)).ToList();
            var poly = new Polygon(vecs);
            SetNewPositionsWithOffset(poly, true);
        }

        public void SetNewPositionsWithOffset(Polygon poly, bool beforeInit)
        {
            if (!_init && !beforeInit)
                return;
            var tmp = ClipperUtility.OffsetPolygonForSafety(poly, -Handler.Settings.Safety);
            var polyOffset = tmp.FirstOrDefault() ?? poly;
            var positionsWith = polyOffset.Points.Select(v => v.ToVector3()).ToList();
            var positionsWithout = poly.Points.Select(v => v.ToVector3()).ToList();
            //Debug.Log("Polygon passed to VSArea: poly vertex count: " + poly.Points.Count + " offset poly vertex count: " + polyOffset.Points.Count);
            positionsWith.Reverse();
            positionsWithout.Reverse();
            WithoutOffset = positionsWithout;
            WithOffset = positionsWith;

            MeshPoints = positionsWith;
        
            WallPoints = new List<Vector3>();
            for(var i = 0; i < positionsWithout.Count; i++)
            {
                WallPoints.Add(positionsWithout[i] + Vector3.up * WallYStart);
                WallPoints.Add(positionsWithout[i] + Vector3.up * WallYEnd);
            }

            if (Handler.Settings.ShowWall)
            {
                _meshCenter = Vector3.zero;
                foreach (var wp in WallPoints)
                {
                    _meshCenter += wp;
                }
                _meshCenter /= WallPoints.Count;
                //_wallFather.transform.localPosition = -_meshCenter;
                ProcessMesh(WallPoints, _wallMeshFilter, false);
                //if (!UseSimpleWall)
                    _dtt.CreateMeshAndUVs();
            }
            if (Handler.Settings.ShowGround)
            {
                ProcessMesh(positionsWithout, GroundMeshFilter, true);// MeshPoints, GroundMeshFilter, true);
                
                //adapt breaches
                if (Handler.Settings.ShowBreaches)
                {
                    BreachVisuals.Visualize = Handler.Settings.ShowBreaches;
                    BreachVisuals.SafetyPoly = 0f;//TODO Safety;
                    BreachVisuals.YourArea = Handler._TranslateIntoUnityCoordinates(new List<Vector3>(positionsWithout));
                }
            }


            var center = Vector3.zero;
            foreach (var entry in Handler._TranslateIntoUnityCoordinates(poly))
            {
                center += entry;
            }
            center /= positionsWithout.Count;
            Arrow.CenterPoint = center;//transform.position + center;
            CenterOfArea = center;
        }

        private static Triangulator _triangulator = new Triangulator();
        private static void ProcessMesh(List<Vector3> positions, MeshFilter filter, bool ground)
        {
            if (positions.Count >= 3)
            {            
                //UVs
                var uvs = new Vector2[positions.Count];

                for (var x = 0; x < positions.Count; x++)
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
            
                var tris = new int[0];
                if (ground)
                {
                    tris = _triangulator
                        .TriangulatePolygon(
                            positions.Select(position => new Vector2(position.x, position.z)).ToArray());
                }
                else
                {
                    tris = new int[3 * positions.Count]; 

                    var baseIndex = 0;
                    for (var x = 0; x < tris.Length; x += 3)
                    {
                        if (x % 2 == 0)
                        {
                            tris[x] = baseIndex % positions.Count;
                            tris[x + 1] = (baseIndex + 1) % positions.Count;
                            tris[x + 2] = (baseIndex + 2) % positions.Count;
                        } else
                        {
                            tris[x +2 ] = baseIndex % positions.Count;
                            tris[x + 1] = (baseIndex + 1) % positions.Count;
                            tris[x] = (baseIndex + 2) % positions.Count;
                        }
                        baseIndex++;
                    }
                }

                //if (num == 0)
                //{
                //    C1 = 0;
                //    C2 = 1;
                //    C3 = 2;

                //    for (var x = 0; x < tris.Length; x += 3)
                //    {
                //        tris[x] = C1;
                //        tris[x + 1] = C2;
                //        tris[x + 2] = C3;

                //        C2++;
                //        C3++;
                //    }
                //}
                //else
                //{
                //    C1 = 0;
                //    C2 = positions.Count - 1;
                //    C3 = positions.Count - 2;

                //    for (var x = 0; x < positions.Count; x += 3)
                //    {
                //        tris[x] = C1;
                //        tris[x + 1] = C2;
                //        tris[x + 2] = C3;

                //        C2--;
                //        C3--;
                //    }
                //}

                if (filter.mesh == null)
                    filter.mesh = new Mesh();
                filter.mesh.Clear();
                filter.mesh.SetVertices(positions);
                filter.mesh.SetUVs(0, uvs.ToList());
                filter.mesh.SetTriangles(tris, 0);
                filter.mesh.name = "MyMesh";
                filter.mesh.RecalculateNormals();
                filter.mesh.RecalculateBounds();

//#if UNITY_EDITOR
//            Unwrapping.GenerateSecondaryUVSet(filter.mesh);
//            EditorUtility.SetDirty(filter);
//            EditorUtility.SetDirty(filter.gameObject);
//#endif
            }
            else
            {
                //Debug.Log("Clearing");
                filter.mesh.Clear();
            }
        }

        private static float DistanceFromLine(Vector2 p, Vector2 l1, Vector2 l2){
            float xDelta = l2.x - l1.x;
            float yDelta = l2.y - l1.y;

            //	final double u = ((p3.getX() - p1.getX()) * xDelta + (p3.getY() - p1.getY()) * yDelta) / (xDelta * xDelta + yDelta * yDelta);
            float u = ((p.x - l1.x) * xDelta + (p.y - l1.y) * yDelta) / (xDelta * xDelta + yDelta * yDelta);

            Vector2 closestPointOnLine;
            if (u< 0) {
                closestPointOnLine = l1;
            } else if (u > 1) {
                closestPointOnLine = l2;
            } else {
                closestPointOnLine = new Vector2(l1.x + u* xDelta, l1.y + u* yDelta);
            }
    
   
            var d = p - closestPointOnLine;
            return Mathf.Sqrt(d.x* d.x + d.y* d.y); // distance
        }
    
        public float DistanceFromPoly(Vector3 pp,  bool insideIsZero, List<Vector3> points)
        {
            if (points.Count < 3 || !_init)
                return 0f;
            var inside = PointInPolygon(pp, points);
            if (insideIsZero && inside)
                return 0f;
            var p = new Vector2(pp.x, pp.z);
            var poly = points.Select(mp => new Vector2(mp.x, mp.z)).ToList();
            float result = 10000;

            // check each line
            for (int i = 0; i < poly.Count; i++)
            {
                int previousIndex = i - 1;
                if (previousIndex < 0)
                {
                    previousIndex = poly.Count - 1;
                }

                Vector2 currentPoint = poly[i];
                Vector2 previousPoint = poly[previousIndex];

                float segmentDistance = DistanceFromLine(new Vector2(p.x, p.y), previousPoint, currentPoint);

                if (segmentDistance < result)
                {
                    result = segmentDistance;
                }
            }
            if (inside)
                result *= -1;

            return result;
        }
    
        private static bool PointInPolygon(Vector3 point, List<Vector3> polygon)
        {
            var rev = new List<Vector3>(polygon);
            point.y = 0f;
            // Get the angle between the point and the
            // first and last vertices.
            var maxPoint = rev.Count - 1;
            var totalAngle = Vector3.Angle(rev[maxPoint] - point, rev[0] - point);

            // Add the angles from the point
            // to each other pair of vertices.
            for (var i = 0; i < maxPoint; i++)
            {
                totalAngle += Vector3.Angle(rev[i] - point, rev[i + 1] - point);
            }
            // The total angle should be 2 * PI or -2 * PI if
            // the point is in the polygon and close to zero
            // if the point is outside the polygon.
            totalAngle %= 360f;
            if (totalAngle > 359)
                totalAngle -= 360f;
            return (Mathf.Abs(totalAngle) < 0.001f);
        }

        private float _timeWrongZone = 0f;
        private float _wallSizeFactor;
        public float InitAfter = 0.5f;
        public bool AlwaysShowArea = false;
        //public bool UseSimpleWall = true;
        void Update()
        {
            if (!_init)
            {
                InitAfter -= Time.unscaledDeltaTime;
                if (InitAfter < 0f)
                {
                    Init();
                }
                return;
            }
            var camRelative = Reference.InverseTransformPoint(Camera.main.transform.position);
            var polyDist = DistanceFromPoly(camRelative, false, WithoutOffset);
            //var dist = polyDist< 0f ? 0f : polyDist;
            var distFactor = Mathf.Clamp01((polyDist + Handler.Settings.Safety) / Handler.Settings.Safety); 
            var factor = Mathf.Pow(distFactor, 0.33f);
            InWrongZone = polyDist > 0f;
            Factor = InWrongZone ? 1f: 0f;
            Alpha = InWrongZone || AlwaysShowArea ? 1f : factor;
            WallAlphaDist = distFactor;
            Arrow.Size = Handler.Settings.ShowArrow ? (InWrongZone ? 1f : 0f) : 0f;
            if (InWrongZone)
                _timeWrongZone += Time.unscaledDeltaTime;
            else
                _timeWrongZone = 0f;
            var timeOut = _timeWrongZone > 0.5f;
            if (Handler.Settings.ChangeTimeScale)
                Time.timeScale = timeOut ? 0f : 1f;
            if (Handler.Settings.ChangeAudioVolume)
                AudioListener.volume = timeOut ? 0f : 1f;

            //if (Handler.Settings.ShowWall)
            //{
            //    _wallSizeFactor -= Time.unscaledDeltaTime;
            //    if (_wallSizeFactor < 1f)
            //        _wallSizeFactor = dist > Handler.Settings.Safety ? 1.1f : 1.1f;
            //    _wallFather.transform.localPosition = (1f- _wallSizeFactor) *_meshCenter;
            //    var wallObjectLocalScale = _wallSizeFactor * Vector3.one;
            //    _wallObject.localScale = wallObjectLocalScale;
            //}
        }
        
        void OnApplicationQuit()
        {
            if(_ppbehavior != null)
            {
                var settings = _ppbehavior.profile.colorGrading.settings;
                settings.basic.temperature = _temparature;
                settings.basic.tint = _tint;
                settings.basic.hueShift = _hueShift;
                settings.basic.saturation = _saturation;
                settings.basic.contrast = _contrast;
                _ppbehavior.profile.colorGrading.settings = settings;
            }
        }
    }
}


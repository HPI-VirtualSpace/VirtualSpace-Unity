using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PacmanGhost : MonoBehaviour {

    public AudioSource GhostSourceAttack;
    public AudioSource GhostSourceFlee;
    public Collider PacmanCollider;
    //public PacmanFollowsCamera Pacman;
    //public CanvasStuff UI;
    public Material ScaredGhost;
    public Renderer Renderer;
    public Transform GhostRayCastOrigin;
    public float NormalSpeed = 1f;
    public float MaxSpeed = 1f;
    public Vector3[] StartVertices;
    public GameObject Visual;
    public float IdleRotationPerSecond;
    public float YGone = -0.5f;
    public float YShould = .411f;
    
    private float _volMax;
    private float _endGhost;
    private float _startTime;
    private float _eta;
    private Vector3 _startPos;
    private List<Vector3> _path;
    private List<float> _pathTime;
    private Material _normalGhost;
    private bool _moving;
    private bool _isFleeing;
    //private bool _end;
    //private bool _teleport;

    // Use this for initialization
    void Start ()
    {
        _volMax = GhostSourceAttack.volume;
        _normalGhost = Renderer.material;
        GhostSourceAttack.Play();
        //Pacman.MadeNormal.AddListener(StartNormal);
        // Pacman.MadeSpecial.AddListener(StartFlee);
        // InitPosition();
    }
	
	// Update is called once per frame
	void Update () {
        if(_isFleeing && Time.time > _endGhost)
        {
            StartNormal();
        }

	    if (AnimUpDown())
	    {
	        transform.LookAt(Camera.main.transform);
	        var rot = transform.rotation.eulerAngles;
	        rot.z = 0f;
	        rot.x = 0f;
	        transform.rotation = Quaternion.Euler(rot);
        }

	    if (!_moving)
	    {
            var rotation = transform.rotation.eulerAngles;
            rotation.y = rotation.y + Time.deltaTime * IdleRotationPerSecond;
            transform.rotation = Quaternion.Euler(rotation);
            return;
	    }
        
        if (_moving)
        {
            var newPos = transform.position;
            var timeDelta = Time.time - _startTime;
            var factor = 0f;
            var index = _path.Count - 1;
            for (var i = _pathTime.Count - 1; i >= 0; i--)
            {
                if (timeDelta - _pathTime[i] < 0f)
                {
                    index = i;
                    factor = timeDelta / _pathTime[i];
                    break;
                }
                timeDelta -= _pathTime[i];
            }
            if (index != _path.Count - 1)
            {
                newPos = Vector3.Lerp(_path[index + 1], _path[index], factor);
            }
            _moving = Time.time < _eta;
            if (!_moving)
                newPos = _path[0];

            if (_moving)
            {
                newPos.y = YShould;
                transform.LookAt(newPos);
                var rot = transform.rotation.eulerAngles;
                rot.z = 0f;
                rot.x = 0f;
                transform.rotation = Quaternion.Euler(rot);
            }
            transform.position = newPos;

        } else
        {
            //Debug.Log("teleported"  + TargetHash);
            //_teleport = false;
            //_moving = false;
            //newPos = _path.Last();
        }
        
    }

    private float _teleFinished;

    public bool IsOccupied()
    {
        return _tele || _moving || Time.time - _teleFinished < 1f;
    }

    public bool IsFleeing()
    {
        return _isFleeing;
    }

    public bool IsMoving()
    {
        return _moving;
    }

    public void StopMovement(bool invisible, bool stopSound)
    {
        _moving = false;
        if (invisible)
            Visual.SetActive(false);
        if (stopSound)
        {
            GhostSourceFlee.Stop();
            GhostSourceAttack.Stop();
        }
    }

    public void InitPosition(Vector3 avoid)
    {
        Visual.SetActive(true);
        var pacPos = avoid; // Pacman.transform.position;
        var index = 0;
        var dist = Vector3.Distance(pacPos, StartVertices[0]);
        for(var i = 1; i < StartVertices.Length; i++)
        {
            var tmpDist = Vector3.Distance(pacPos, StartVertices[i]);
            if (tmpDist < dist)
                continue;
            dist = tmpDist;
            index = i;
        }
        var newPos = StartVertices[index];
        newPos.y = transform.position.y;
        transform.position = newPos;
        GhostSourceAttack.Play();
    }

    public void StartNormal()
    {
        if (!_isFleeing)
            return;
        _isFleeing = false;
        GhostSourceFlee.Stop();
        GhostSourceAttack.Play();
        Renderer.material = _normalGhost;
    }

    public void StartFlee(float ghostTime)
    {
        _endGhost = Time.time + ghostTime;
        _isFleeing = true;
        GhostSourceFlee.Play();
        GhostSourceAttack.Stop();
        Renderer.material = ScaredGhost;
    }

    public Collider GetTilePosition()
    {
        RaycastHit info;
        if (!Physics.Raycast(GhostRayCastOrigin.position, Vector3.down, out info, 10))
            return null;
        return info.collider;
    }

    private int TargetHash;

    public int GetHash()
    {
        //if (_moving)
            return TargetHash;
        //return -1;
    }

    public float Animup;
    public float Animdown;
    private float _animUp;
    private float _animDown;
    private bool _tele;
    private bool AnimUpDown()
    {
        if (_animDown > 0f)
        {
            _animDown -= Time.deltaTime;
            var factor = _animDown / Animdown;
            GhostSourceAttack.volume = factor * _volMax;
            GhostSourceFlee.volume = factor * _volMax;
            if (_animDown <= 0f)
            {
                transform.position = _path.First();
            }
            var newPos = transform.position;
            newPos.y = factor * YShould + (1f - factor) * YGone;
            transform.position = newPos;
            return true;
        } else if (_animUp > 0f)
        {
            _animUp -= Time.deltaTime;
            var factor = 1f-_animUp / Animdown;
            GhostSourceAttack.volume = factor * _volMax;
            GhostSourceFlee.volume = factor * _volMax;
            var newPos = transform.position;
            newPos.y = factor * YShould + (1f - factor) * YGone;
            transform.position = newPos;
            if (_animDown <= 0f)
            {
                _teleFinished = Time.time;
                _tele = false;
                return false;
            }
            return true;
        }
        _tele = false;
        return false;
    }

    public void GoTo(List<Vector3> tiles, float time, bool justTeleport, int targetHash, float maxTileDist, Vector3 currentTile, bool startfromtile)
    {
        if (tiles.Count == 0 || targetHash == TargetHash && Time.time + time >= _eta)
            return;
        tiles.Reverse();
        TargetHash = targetHash;
        _moving = true;
        //_teleport = justTeleport;
        //if (!_teleport && time > 0f)
        //{
        //    var distance = 0f;
        //    for (var i = 0; i < tiles.Count; i++)
        //        distance +=  Vector3.Distance(i == 0 ? transform.position : tiles[i - 1], tiles[i]);
        //    Debug.Log((NormalSpeed * time) + " " + time + " " + distance);
        //    var tooFast = NormalSpeed * time < distance;
        //    _teleport = tooFast;
        //}
        _path = tiles.Select(t => new Vector3(t.x, transform.position.y, t.z)).ToList();
        var realTime = time <= 0f ? tiles.Count / NormalSpeed : time;
        var speedPerTile = maxTileDist / (realTime / tiles.Count);
        justTeleport = !_isFleeing && (justTeleport || speedPerTile > MaxSpeed && realTime > Animdown + Animup);
        if (justTeleport)
        {
            //Debug.DrawLine(transform.position, _path.First(), Color.black, 1f);
            _tele = true;
            //_teleport = false;
            _moving = false;
            _animDown = Animdown;
            _animUp = Animup;
            _eta = Animdown + Animup;
            //if(Vector3.Distance(transform.position, _path.First()) > 0.1f)
            //    transform.LookAt(_path.First());
            //TODO anim up / fade in + sound?
            return;
        }
        else
        {
            //if (Vector3.Distance(transform.position, tiles.Last()) > maxTileDist)
            //{
            //    transform.position = tiles.Last();
            //    tiles.RemoveAt(tiles.Count - 1);
            //}

            var positionCurrent = transform.position;
            positionCurrent.y = YShould;
            _path.Add(startfromtile ? currentTile : positionCurrent);
            _startTime = Time.time;
            _eta = Time.time + realTime;
            var dists = new List<float>();
            for (var i = 1; i < _path.Count; i++)
            {
                dists.Add(Vector3.Distance(_path[i], _path[i - 1]));
            }
            var distSum = dists.Sum();
            var speed = distSum / realTime;
            _pathTime = new List<float>();
            for (var i = 0; i < dists.Count; i++)
            {
                _pathTime.Add(dists[i] / speed);
            }
            //for(var i = 0; i < _path.Count-1; i++)
            //    Debug.DrawLine(_path[i], _path[i+1], i == _path.Count - 2 ? Color.magenta : Color.red, 2f);
        }
    }

    public bool AreYouMoving()
    {
        return _moving;
    }
}

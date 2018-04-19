using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VirtualSpaceVisuals;

namespace Assets.Games.SpaceInvaders.Scripts
{
    public class SpaceInvadersFancyFloor : MonoBehaviour
    {
        public SpaceInvadersPlayerAvatar Avatar;
        public SpaceInvadersFancyFloorTile Tile;
        public int Width, Depth;
        public Transform ShootOrigin;
        public float Distance = 5f;
        public float Resolve, NeighborsTell;
        public float Interval;
        public VirtualSpacePlayerArea Area;
        private float _lastTrigger;

        private SpaceInvadersFancyFloorTile _last;
        private SpaceInvadersFancyFloorTile[,] _tiles;
       // private static bool _init;

        void Start () {
            //if (!_init)
            //{
            //    _init = true;
            var children = GetComponentsInChildren<Transform>().Select(t => t.gameObject).ToList();
            foreach (var child in children)
            {
                if(child != gameObject)
                    Destroy(child);
            }
            _tiles = new SpaceInvadersFancyFloorTile[2 * Width + 1, 2 * Depth + 1];
                for (var i = -Width; i <= Width; i++)
                {
                    for (var j = -Depth; j <= Depth; j++)
                    {
                        var tile = Instantiate(Tile);
                        tile.transform.parent = transform;
                        _tiles[Width + i, Depth + j] = tile;
                        tile.transform.position = new Vector3(
                            tile.transform.localScale.x * 10f * i, 0f,
                            tile.transform.localScale.z * 10f * j);
                        tile.name = "tile_" + i + "_" + j;
                        tile.Resolve = Resolve;
                        tile.NeighborsTell = NeighborsTell;
                        tile.Area = Area;
                    }
                }
                for (var i = 0; i < _tiles.GetLength(0); i++)
                {
                    for (var j = 0; j < _tiles.GetLength(1); j++)
                    {
                        _tiles[i, j].Neighbors = new List<SpaceInvadersFancyFloorTile>();
                        if (i - 1 >= 0)
                            _tiles[i, j].Neighbors.Add(_tiles[i - 1, j]);
                        if (j + 1 <= 2 * Depth)
                            _tiles[i, j].Neighbors.Add(_tiles[i, j + 1]);
                        if (i + 1 <= 2 * Width)
                            _tiles[i, j].Neighbors.Add(_tiles[i + 1, j]);
                        if (j - 1 >= 0)
                            _tiles[i, j].Neighbors.Add(_tiles[i, j - 1]);
                    }
                }
            //}
        }

        void OnDestroy()
        {
            
        }
    
        void Update ()
        {
            if (Time.time - _lastTrigger < Interval)
                return;
            var info = Physics.RaycastAll(new Ray(ShootOrigin.position + Vector3.up, Vector3.down), Distance+1);
            foreach (var i in info)
            {
                var go = i.transform.gameObject;
                var fancy = go.GetComponent<SpaceInvadersFancyFloorTile>();
                if (fancy != null)// && (_last == null || _last != fancy))
                {
                    _last = fancy;
                    _last.Trigger();
                    _lastTrigger = Time.time;
                    //Avatar.NoGo = fancy.Nogo;
                    break;
                }
            }
        }

    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using System.Linq;
using System.Runtime.InteropServices;

public class Bidding : MonoBehaviour
{
    private struct Player
    {
        public float Credits;
        public int TimesOwned;
        public Material Material;
    }

    private class Tile
    {
        public int OwnerIndex;
    }

    public int[] Values;
    public Material[] Materials;
    public Material SystemMaterial;
    public int X, Z, T;

    private Player[] _players;
    private Tile[,,] _tiles;
    private Renderer[,,] _renderers;

    private int _movingT;

    private float _creditsLost;

    // Use this for initialization
    void Start()
    {
        _players = new Player[Values.Length];
        for (var i = 0; i < _players.Length; i++)
            _players[i] = new Player {Material = Materials[i]};
        _tiles = new Tile[X, Z, T];
        _renderers = new Renderer[X,Z,T];

        var sum = 0f;
        for (var x = 0; x < X; x++)
        {
            for (var z = 0; z < Z; z++)
            {

                sum += (x == 0 && z == 0) ? 1f*Log(1f) : 0f*Log(0f);
                for (var t = 0; t < T; t++)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    _renderers[x,z,t] = go.GetComponent<Renderer>();
                    go.transform.parent = transform;
                    go.transform.position = new Vector3(x * 1.1f,t*2f, z * 1.1f);
                    _tiles[x, z, t] = new Tile {OwnerIndex = -1};
                }
            }
        }

        var prop = 1f/(X*Z);
        var entropyNormalizedNoClue = 1+1f/Log(X*Z)*         X*Z*(prop*Log(prop));
        var entropyNormalizedAllClear = 1+1f / Log(X * Z) *  sum;

    }
    private float Log(float input)
    {
        return Math.Abs(input) < 0.0000000001f ? 0f : Mathf.Log(input, 10f);
    }

    public float ZeroSaveIt;
    public float TimeInterval;
    private float _lastBidding;

    void Update()
    {
        if (Time.time <= _lastBidding + TimeInterval)
            return;

        _lastBidding = Time.time;
        Bid();
    }

    // Update is called once per frame
    private void Bid()
    {
        Debug.Log("lost " + _creditsLost);
        _creditsLost = 0f;
        //give player credits
        for (var i = 0; i < _players.Length; i++)
        {
            Debug.Log(_players[i].Credits);
            _players[i].Credits += Values[i];
        }

        //add new tile to t queue
        for (var x = 0; x < X; x++)
        {
            for (var z = 0; z < Z; z++)
            {
                for (var t = 0; t < T; t++)
                {
                    //get real t (use moving index)
                    var tReal = (_movingT + t)%T;
                    var tile = _tiles[x, z, tReal];

                    //assign tile to player
                    if (t == T-1)
                    {
                        if (tile.OwnerIndex >= 0)
                            _players[tile.OwnerIndex].TimesOwned++;
                        tile.OwnerIndex = -1;
                    }

                    //sell all tiles again
                    var playerValue = _players.Select(p => p.Credits / (X * Z * T)).ToArray();
                    playerValue[0] *= ZeroSaveIt;
                    if(tile.OwnerIndex >= 0 || t == T-1)
                        TrySell(tile, playerValue);
                    
                    _renderers[x,z,t].material = tile.OwnerIndex >= 0 ? _players[tile.OwnerIndex].Material : SystemMaterial;
                }
            }
        }
        if (++_movingT == T)
            _movingT = 0;

        var sum = _players.Select(p => p.TimesOwned).Sum();
        var ratio = _players.Select(p => ((float)p.TimesOwned / sum).ToString(CultureInfo.InvariantCulture)).ToList();
        var dbgString = ratio.Aggregate((a, b) => a + " " + b);
        Debug.Log(dbgString);
        for (var i = 0; i< _players.Length; i++)
        {
            _players[i].TimesOwned = 0;
        }
    }

    private void TrySell(Tile tile, float[] playerValue)
    {
        var maxI = 0;
        var secMaxI = -1;
        for (var i = 0; i < _players.Length; i++)
        {
            if (i == 0)
                continue;
            if (playerValue[i] >= playerValue[maxI])
            {
                secMaxI = maxI;
                maxI = i;
            }
            else if (secMaxI < 0 || playerValue[i] >= playerValue[secMaxI])
            {
                secMaxI = i;
            }
        }
        var value = playerValue[secMaxI];
        _players[maxI].Credits -= value;
        if (tile.OwnerIndex >= 0)
        {
            _players[tile.OwnerIndex].Credits += value;
        }
        else _creditsLost += value;
        tile.OwnerIndex = maxI;
    }
}

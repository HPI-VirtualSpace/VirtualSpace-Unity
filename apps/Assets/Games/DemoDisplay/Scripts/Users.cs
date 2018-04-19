using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using VirtualSpace.Shared;

[Serializable]
public class User
{
    public Material PositionMaterial;
    public Material AreaMaterial;
    public Vector3 Position;
    public float LastUpdated;
    public float InactiveAfter = 5f;
    public bool UserHeartbeat = false;
    public int Id;

    public bool IsActive
    {
        get { return Time.unscaledTime - LastUpdated < InactiveAfter; }
    }

    public bool IsInitialized
    {
        get { return AreaMaterial != null && PositionMaterial != null; }
    }

    public void UserAlive()
    {
        UserHeartbeat = true;
    }
}

public class Users : MonoBehaviour
{
    private Dictionary<int, User> _users;

    public IEnumerable<User> All
    {
        get { return _users.Values; }
    }

    public Material DefaultMaterial;
    public Material GrayMaterial;
    public Material RedMaterial;
    public Material BlueMaterial;
    public Material YellowMaterial;
    public Material GreenMaterial;
    public Material OrangeMaterial;

    [Range(0,1)]
    public float PositionTransparency = .9f;
    [Range(0,1)]
    public float AreaTransparency = .7f;

    private List<Material> _allMaterials;
    private System.Random _random;

    void Awake()
    {
        _users = new Dictionary<int, User>();

        _random = new System.Random(DateTime.Now.Millisecond);

        _allMaterials = new List<Material>
        {
            //DefaultMaterial,
            RedMaterial,
            BlueMaterial,
            YellowMaterial,
            GreenMaterial
        };
    }

    void Update()
    {
        foreach (var user in All)
        {
            if (user.UserHeartbeat)
            {
                user.LastUpdated = Time.unscaledTime;
                user.UserHeartbeat = false;
            }
        }

        if (_callOnMain.Count > 0)
        {
            lock (_callOnMain)
            {
                foreach (var action in _callOnMain)
                {
                    action();
                }

                _callOnMain.Clear();
            }
        }
    }

    public bool HasUser(int userId)
    {
        return _users.ContainsKey(userId);
    }

    public User GetUser(int userId)
    {
        if (!_users.ContainsKey(userId))
        {
            var user = new User();
            user.Id = userId;
            _users[userId] = user;
        }

        return _users[userId];
    }

    public void SetUserPosition(int userId, Vector3 position)
    {
        //Debug.Log("Position updated");
        var user = GetUser(userId);
        user.UserAlive();

        user.Position = position;
    }

    private List<Material> GetUnassignedMaterials()
    {
        return _allMaterials
            .Except(_users.Select(user => user.Value.AreaMaterial)).ToList();
    }

    private List<Action> _callOnMain = new List<Action>();
    public void SetUserColor(int userId, ColorPref colorPref)
    {
        var user = GetUser(userId);
        user.UserAlive();

        var material = DefaultMaterial;
        switch (colorPref)
        {
            case ColorPref.Gray:
                material = GrayMaterial;
                break;
            case ColorPref.Red:
                material = RedMaterial;
                break;
            case ColorPref.Blue:
                material = BlueMaterial;
                break;
            case ColorPref.Yellow:
                material = YellowMaterial;
                break;
            case ColorPref.Green:
                material = GreenMaterial;
                break;
            case ColorPref.Orange:
                material = OrangeMaterial;
                break;
            default:
                throw new ArgumentOutOfRangeException("colorPref", colorPref, null);
        }

        var unassignedMaterials = GetUnassignedMaterials();

        if (!unassignedMaterials.Contains(material))
        {
            var randomIndex = _random.Next(0, unassignedMaterials.Count);
            material = unassignedMaterials[randomIndex];
        }

        var areaMaterial = material;
        var positionMaterial = material;
        user.AreaMaterial = areaMaterial;
        user.PositionMaterial = positionMaterial;

        //lock (_callOnMain)
        //    _callOnMain.Add(delegate ()
        //    {
        //        var areaColor = areaMaterial.color;
        //        areaColor.a = AreaTransparency;
        //        var positionColor = positionMaterial.color;
        //        positionColor.a = PositionTransparency;

        //        areaMaterial.color = areaColor;
        //        positionMaterial.color = positionColor;
        //    });
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using VirtualSpace.Shared;

namespace VirtualSpaceVisuals
{
    public class VirtualSpaceSettings : MonoBehaviour {

        [Header("---------------------------------------")]
        [Header("NEVER REVERT PREFAB!!! - SET ALL THESE:")]
        public Transform PriorityPoints;
        public Transform WalkablePoints;
        public float ViveTrackingHeight;
        public bool ChangeTimeScale;
        public bool ChangeAudioVolume;
        public bool ShowArrow;
        public bool ShowGrey;
        public bool ShowWall;
        public bool ShowGround;
        public bool ShowBreaches;
        public float Safety = 1f;
        public List<Texture> WallMeshTextures;
        public Texture SimpleWallTexture;
        public List<Color> WallColors;
        public Color SimpleWallColor = new Color(0f, 0f, 1f, 0.7f);
        public float Tiling = 0.2f;
        public string BreachMessage = "";
        public bool CheckHandheldBreach = false;
        public string BreachHandheld = "HANDS CLOSER TO BODY";
        public string BreachOther = "TAKE CARE";
        public Color OverallColor = Color.blue;
        public Color EnemyBreachColor = Color.red;
        public ColorPref BackendColor;
    }
}

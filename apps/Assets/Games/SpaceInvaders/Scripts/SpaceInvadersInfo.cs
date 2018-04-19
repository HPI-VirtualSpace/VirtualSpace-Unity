using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Games.SpaceInvaders.Scripts;
using UnityEngine;
using UnityEngine.UI;
using VirtualSpaceVisuals;

public class SpaceInvadersInfo : MonoBehaviour {

    public List<SpaceInvadersEnemyRow> Rows;
    public SpaceInvadersGotHit UFO;
    public SpaceInvadersGotHit Player;
    public string NoGoArea = "NoGoArea";
    public Text Text;
    public int UFOPoints = 1500;
    public Color OkayColor = Color.yellow;
    public Color LeaveColor = Color.red;
    public float RestartTime = 5f;
    public GameObject NewEnvironment;

    private GameObject _new;
    public VirtualSpacePlayerArea NoGo;
    private bool _over;

    void Start ()
    {
        Rows = Rows.Where(r => r.gameObject.activeSelf).ToList();
        _new = Instantiate(NewEnvironment, new Vector3(), new Quaternion(), null);
        _new.SetActive(false);
        SetNew();
	}

    private void Update()
    {
        SetNew();

        if (!_over)
            _over = UFO.IsDefeated() || Player.IsDefeated() || UFO == null ||Player == null;
        else
        {
            RestartTime -= Time.deltaTime;
            if(RestartTime < 0f)
            {

                Destroy(transform.parent.gameObject);
                _new.SetActive(true);
            }

        }
    }

    public void SetNew()
    {
        var number = 0;
        var done = true;
        foreach (var row in Rows)
        {
            number += row.Points;
            done = done && row.Done;
        }
        done = done && UFO.IsDefeated();
        if (UFO.IsDefeated())
            number += UFOPoints;
        //number -= _noGo.LostPoints;
        var text = "";
        Text.color = OkayColor;
        if (Player.IsDefeated())
        {
            text = "GAME OVER!";
        }
        else if (done)
        {
            text = "YOU WIN!";
        }
        else if (NoGo.InWrongZone)
        {
            text = "GO TO GREEN ZONE!";
            Text.color = LeaveColor;
        }
        text += System.Environment.NewLine + number + " POINTS";
        Text.text = text;
    }
}

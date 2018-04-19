using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CanvasStuff : MonoBehaviour {

    public Transform Player;
    public Text Message;
    public Text Text;

    private bool _delete;
    private string _lastPoints;
    private string _lastMsg;
    private bool _lastMsgError;
    private bool _isError;
    private bool _updated;
    //public int MaxPoints = 50;

    //private int _pointsGained;
    //private int _points;
    // private bool _over;

    private void Start()
    {
        Message.text = "";
        Text.text = "";
    }

	// Update is called once per frame
	void Update () {
        var playRot = Player.rotation.eulerAngles;
        var rot = transform.rotation.eulerAngles;
        rot.y = playRot.y;
        transform.rotation = Quaternion.Euler(rot);
    }

    //public void Restart()
    //{
    //  //  _points = 0;
    //   // _pointsGained = 0;
    //    //ChangePoints(0, "GAME STARTS!", false, true);
    //}

    public void Display(int points, string message, bool isError, bool validForSaving)
    {
        Message.text = message;
        Text.text = points >= 0 ? points.ToString() : "";
        _isError = isError;
        Message.color = isError ? Color.red : Color.yellow;
        Text.color = isError ? Color.red : Color.yellow;
        if(validForSaving)
            _updated = true;
    }

    public void SaveLastDisplay()
    {
        _lastMsg = Message.text;
        _lastPoints = Text.text;
        _lastMsgError = _isError;
        _updated = false;
    }

    public void RedoLastDisplayIfNoUpdate()
    {
        if (_updated)
            return;
        Message.text = _lastMsg;
        Text.text = _lastPoints;
        Message.color = _lastMsgError ? Color.red : Color.yellow;
        Text.color = _lastMsgError ? Color.red : Color.yellow;
    }

    //public void GameOver()
    //{
    //    _over = true;
    //    Message.text = "GAME OVER!";
    //    Message.color = Color.red;
    //    Text.text = _points.ToString();
    //    Text.color = Color.red;
    //}
}

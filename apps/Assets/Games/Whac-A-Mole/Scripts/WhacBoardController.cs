using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WhacBoardController : MonoBehaviour
{
    public Text Instructions;
    public Text Score;

    public string GameInstructions = "Whac A Mole,\nwill ya:";

    private int Hits = 0;
    private int Misses = 0;

    public int TotalScore = 0;

    public void RecordHit(int points)
    {
        TotalScore += points;
        UpdateText();
    }

    public void RecordMiss()
    {
        Misses++;
        UpdateText();
    }

    public void UpdateText()
    {
        Instructions.text = GameInstructions;
        var total = (Hits + Misses);
        //Score.text = Hits + "/" + total + " " + (total == 1 ? "Mole" : "Moles");
        Score.text = "" + TotalScore;
    }

    public void GetText(out string score, out string instruction)
    {
        score = Score.text;
        instruction = Instructions.text;
    }
}

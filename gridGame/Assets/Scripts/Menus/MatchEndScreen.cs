using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MatchEndScreen : MonoBehaviour
{
    public TMP_Text victor, loser, turnCount;

    public void Start()
    {
        victor.text = "Player " + MatchData.winnerTeams[0];
        loser.text = "Player " + MatchData.loserTeams[0];
        turnCount.text = "Turn: " + MatchData.numTurns;

    }


    public void BackButton()
    {
        SceneManager.LoadScene("mainMenu");
    }
}

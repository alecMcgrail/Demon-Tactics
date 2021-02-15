using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchData : MonoBehaviour
{
    public static MatchData instance;

    public static int numTurns;
    public static List<int> winnerTeams, loserTeams;

    // Start is called before the first frame update
    void Start()
    {
        if(instance != null)
        {
            Destroy(this.gameObject);
            return;
        }

        instance = this;
        GameObject.DontDestroyOnLoad(this.gameObject);
    }
}

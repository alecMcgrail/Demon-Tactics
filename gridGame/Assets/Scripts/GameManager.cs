using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cinemachine;

public class GameManager : MonoBehaviour
{
    //Raycast for the update for unitHover info
    private Ray ray;
    private RaycastHit2D hit;

    /// The number of teams is hard coded as 2, if there are changes in the future a few of the
    /// functions in this class need to be altered as well to update this change.
    public int numberOfTeams = 2;
    public int currentTeam;
    public int currentTurn;
    public int maxAP = 1;
    public int currentAP;
    public int usedAP;

    public GameObject unitsOnBoard;
    public GameObject team1;
    public GameObject team2;
    public GameObject team3;
    public GameObject team4;

    //Library of all the different unit types
    public Unit[] unitTypes;

    public GameObject unitBeingDisplayed;
    public GameObject tileBeingDisplayed;
    public Tile displayedTileInfo;

    public MapManager mapMan;
    public UIManager uiMan;
    public BattleManager batMan;

    //Cursor Info for tileMapScript
    public int cursorX;
    public int cursorY;
    //current Tile Being Moused Over
    public int selectedXTile;
    public int selectedYTile;

    public bool showingAttackRange;
    public bool unitInMovement;

    private Vector2 desiredCursorPos = new Vector2(0, 0);

    void Start()
    {
        mapMan = GetComponent<MapManager>();
        uiMan = GetComponent<UIManager>();
        batMan = GetComponent<BattleManager>();

        spawnInitialUnits();
        currentTeam = 0;
        currentTurn = 1;
        ResetAP();

        selectedXTile = mapMan.mapSizeX;
        selectedYTile = mapMan.mapSizeY;

        uiMan.SetCurrentTeamUI(currentTeam);
        uiMan.SetCurrentTurnText(currentTurn);
    }

    void Update()
    {
        //Always trying to see where the mouse is pointing
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        hit = Physics2D.GetRayIntersection(Camera.main.ScreenPointToRay(Input.mousePosition));
        unitInMovement = mapMan.selectedUnit != null && mapMan.selectedUnit.GetComponent<Unit>().movementQueue.Count != 0;

        if (hit && !uiMan.freezeCursor)
        {
            //Update cursor location and unit appearing in UI panels
            cursorUIUpdate();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            //spawnUnitOnSelectedTile();
            //spawnUnitOnTile(cursorX, cursorY);
            //endTurn();
            mapMan.refreshOption();
        }
    }

    public void cursorUIUpdate()
    {
        //If we are mousing over a tile, highlight it
        if (hit.transform.CompareTag("Tile"))
        {
            selectedXTile = hit.transform.gameObject.GetComponent<ClickableTileScript>().tileX;
            selectedYTile = hit.transform.gameObject.GetComponent<ClickableTileScript>().tileY;
            cursorX = selectedXTile;
            cursorY = selectedYTile;

            desiredCursorPos = hit.transform.position;

            uiMan.SetDesiredCursorPosition(desiredCursorPos);

            tileBeingDisplayed = hit.transform.gameObject;
            displayedTileInfo = mapMan.getTileAt(cursorX, cursorY);
            unitBeingDisplayed = tileBeingDisplayed.GetComponent<ClickableTileScript>().unitOnTile;

            uiMan.UpdateUIPanels();
        }
    }

    //Desc: ends the turn and plays the animation
    public void endTurn()
    {
        if (mapMan.selectedUnit == null)
        {
            switchCurrentPlayer();
            if (currentTeam == 1)
            {
                //playerPhaseAnim.SetTrigger("slideLeftTrigger");
                //playerPhaseText.SetText("Player 2 Phase");
            }
            else if (currentTeam == 0)
            {
                //playerPhaseAnim.SetTrigger("slideRightTrigger");
                //playerPhaseText.SetText("Player 1 Phase");
            }

            uiMan.SetCurrentTeamUI(currentTeam);
            uiMan.SetCurrentTurnText(currentTurn);

            uiMan.HideMenu(uiMan.gameMenu);

            uiMan.BlinkTurnPhasePanel();
        }
    }

    //Desc: increments the current team
    public void switchCurrentPlayer()
    {
        resetUnitsMovements(returnTeam(currentTeam));
        currentTeam++;
        maxAP++;
        ResetAP();

        if (currentTeam >= numberOfTeams)
        {
            currentTurn += 1;
            currentTeam = 0;
        }
        defortifyUnits(returnTeam(currentTeam));
    }

    //Desc: re-enables movement for all units on the team
    public void resetUnitsMovements(GameObject teamToReset)
    {
        foreach (Transform unit in teamToReset.transform)
        {
            unit.GetComponent<Unit>().moveAgain();
        }
    }

    public void defortifyUnits(GameObject teamToEdit)
    {
        foreach (Transform unit in teamToEdit.transform)
        {
            unit.GetComponent<Unit>().isFortified = false;
        }
    }

    //Desc: return the gameObject of the requested team
    public GameObject returnTeam(int i)
    {
        GameObject teamToReturn = null;

        switch (i)
        {
            case 0:
                teamToReturn = team1;
                break;
            case 1:
                teamToReturn = team2;
                break;
            case 2:
                teamToReturn = team3;
                break;
            case 3:
                teamToReturn = team4;
                break;
            default:
                Debug.Log("returnTeam(): No team at index " + i + "!");
                break;
        }
        return teamToReturn;
    }

    public void spawnUnitOnTile(Unit unitPrefab, int posx, int posy)
    {
        Debug.Log("Spawning " + unitPrefab.name + " at [" + posx + "," + posy + "]");
        GameObject targetTile = mapMan.tilesOnMap[posx, posy];

        //Check if there's a unit already
        if (targetTile.GetComponent<ClickableTileScript>().unitOnTile != null)
        {
            Debug.Log("Tile already occupied!");
            return;
        }

        //Make new unit at selected tile
        Unit newUnit = Instantiate(unitPrefab, new Vector2(posx, -posy), Quaternion.identity);
        //Set team to current team
        newUnit.teamNum = currentTeam;
        newUnit.transform.SetParent(returnTeam(currentTeam).transform);
        //flip horizontally if on team 2
        if (currentTeam == 1)
        {
            Vector3 scaler = newUnit.transform.localScale;
            scaler.x *= -1;
            newUnit.transform.localScale = scaler;
        }
        //Set Unit's tileBeingOccupied to current tile
        newUnit.x = posx;
        newUnit.y = posy;
        //Set tileUnitOccupies
        newUnit.tileBeingOccupied = targetTile;

        //Set tile's UnitOnTile to new unit
        targetTile.GetComponent<ClickableTileScript>().unitOnTile = newUnit.unitVisualPrefab;

        newUnit.mapMan = mapMan;
        newUnit.floatingText = uiMan.floatingText;
    }

    private void spawnInitialUnits()
    {
        for(int i = 0; i < numberOfTeams; i++)
        {
            currentTeam = i;

            if (i == 0)
            {
                spawnUnitOnTile(unitTypes[1], 4, 2);
                spawnUnitOnTile(unitTypes[3], 4, 4);
                spawnUnitOnTile(unitTypes[4], 4, 6);
                spawnUnitOnTile(unitTypes[1], 4, 8);
                spawnUnitOnTile(unitTypes[0], 5, 5);
                spawnUnitOnTile(unitTypes[2], 4, 5);
            }
            else if(i == 1)
            {
                spawnUnitOnTile(unitTypes[1], 12, 2);
                spawnUnitOnTile(unitTypes[4], 12, 4);
                spawnUnitOnTile(unitTypes[3], 12, 6);
                spawnUnitOnTile(unitTypes[1], 12, 8);
                spawnUnitOnTile(unitTypes[0], 11, 5);
                spawnUnitOnTile(unitTypes[2], 12, 5);
            }
        }

        mapMan.refreshAllUnitEffects();
    }

    public void checkIfUnitsRemain(GameObject unit, GameObject enemy)
    {
        //  Debug.Log(team1.transform.childCount);
        //  Debug.Log(team2.transform.childCount);
        StartCoroutine(checkIfUnitsRemainCoroutine(unit, enemy));
    }

    //Desc: waits until all the animations and stuff are finished before calling the game
    private IEnumerator checkIfUnitsRemainCoroutine(GameObject unit, GameObject enemy)
    {
        while (unit.GetComponent<Unit>().combatQueue.Count != 0)
        {
            yield return new WaitForEndOfFrame();
        }

        while (enemy.GetComponent<Unit>().combatQueue.Count != 0)
        {
            yield return new WaitForEndOfFrame();
        }
        if (team1.transform.childCount == 0)
        {
            declareWinner(2);
        }
        else if (team2.transform.childCount == 0)
        {
            declareWinner(1);
        }
    }

    public void declareWinner(int winningTeam)
    {
        StartCoroutine(declareWinnerCoroutine(winningTeam));
    }

    private IEnumerator declareWinnerCoroutine(int winningTeam)
    {
        int losingTeam;
        switch (winningTeam)
        {
            case 1:
                losingTeam = 2;
                break;
            case 2:
                losingTeam = 1;
                break;
            default:
                losingTeam = 1;
                break;
        }

        List<int> winners = new List<int>();
        List<int> losers = new List<int>();
        winners.Add(winningTeam);
        losers.Add(losingTeam);

        MatchData.numTurns = currentTurn;
        MatchData.winnerTeams = winners;
        MatchData.loserTeams = losers;

        yield return new WaitForSeconds(2.0f);

        SceneManager.LoadScene("endScreen");
    }

    private void ResetAP()
    {
        currentAP = maxAP;
        usedAP = 0;
    }
    public void DecrementUsedAP()
    {
        currentAP = Mathf.Clamp(currentAP - usedAP, 0, 999);
        usedAP = 0;
    }
    public bool CanAffordAPCost(int cost)
    {
        if (cost > (currentAP - usedAP))
        {
            return false;
        }
        return true;
    }

}

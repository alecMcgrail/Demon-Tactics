using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class MapManager : MonoBehaviour
{
    [Header("Manager Scripts")]
    public GameManager gameMan;
    public BattleManager batMan;
    public UIManager uiMan;

    [Header("Tiles")]
    //Library of the different tile types
    public Tile[] tileTypes;
    public int[,] tiles;

    //This is used when the game starts and there are pre-existing units
    //It uses this variable to check if there are any units and then maps them to the proper tiles
    [Header("Units on the board")]
    public GameObject unitsOnBoard;

    //This 2d array is the list of tile gameObjects on the board
    public GameObject[,] tilesOnMap;

    //Set in the inspector, might change this otherwise.
    //This is the map size (please put positive numbers it probably wont work well with negative numbers)
    [Header("Board Size")]
    public int mapSizeX;
    public int mapSizeY;

    [Header("Camera Stuff")]
    public CinemachineVirtualCamera vCam1;
    public GameObject cameraConfiner;

    //containers (parent gameObjects) for the UI tiles
    [Header("Containers")]
    public GameObject tileContainer;
    public GameObject boardEdgeContainer;

    //Nodes along the path of shortest path from the pathfinding
    public List<Node> currentPath = null;

    //Node graph for pathfinding purposes
    public Node[,] graph;

    //In the update() function mouse down raycast sets this unit
    [Header("Selected Unit Info")]
    public GameObject selectedUnit;
    public bool unitSelected = false;
    public bool hasUnitMoved = false;
    //These two are set in the highlightUnitRange() function
    //They are used for other things as well, mainly to check for movement, or finalize function
    public HashSet<Node> selectedUnitTotalRange;
    public HashSet<Node> selectedUnitMoveRange;

    public int unitSelectedPreviousX;
    public int unitSelectedPreviousY;

    public GameObject previousOccupiedTile;

    //Used when randomly generating map
    private int indexOfBoardEdge = -1;

    private List<UIManager.UnitMenuOption> activeUnitMenuButtons = new List<UIManager.UnitMenuOption>();

    void Start()
    {

        //indexOfBoardEdge = 1;

        // 0 = Sea; 1 = Badland; 2 = Boneyard; 3 = Spire; 5 = Red Base; 6 = Blue Base; 7 = Trail; 8 = Coast
        int[,] mapp2 = new int[,] { { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
                                    { 1, 1, 1, 1, 8, 0, 8, 1, 1, 1, 8, 0, 8, 1, 1, 1, 1 },
                                    { 1, 1, 1, 1, 8, 2, 0, 0, 8, 0, 0, 7, 0, 1, 1, 1, 1 },
                                    { 1, 1, 1, 1, 0, 0, 0, 0, 3, 0, 2, 0, 0, 1, 1, 1, 1 },
                                    { 1, 1, 1, 0, 0, 0, 2, 1, 1, 1, 0, 0, 0, 0, 1, 1, 1 },
                                    { 1, 1, 1, 0, 5, 7, 7, 1, 1, 1, 7, 7, 6, 0, 1, 1, 1 },
                                    { 1, 1, 1, 0, 0, 0, 0, 1, 1, 1, 2, 0, 0, 0, 1, 1, 1 },
                                    { 1, 1, 1, 1, 0, 0, 0, 7, 3, 0, 0, 0, 0, 1, 1, 1, 1 },
                                    { 1, 1, 1, 1, 8, 8, 1, 0, 0, 8, 1, 8, 8, 1, 1, 1, 1 },
                                    { 1, 1, 1, 1, 1, 1, 1, 1, 8, 1, 1, 1, 1, 1, 1, 1, 1 },
                                    { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 } };


        //Generate the 2D array of tile types as ints
        generateMapInfo(mapp2);

        //Generate pathfinding graph
        generatePathFindingGraph();

        //With the generated info this function will read the info and produce the map
        generateMapVisuals();

        //Check if there are any pre-existing units on the board
        //setIfTileIsOccupied();
    }

    private void generateMapInfo()
    {
        tiles = new int[mapSizeX, mapSizeY];
        for (int x = 0; x < mapSizeX; x++)
        {
            for (int y = 0; y < mapSizeY; y++)
            {
                tiles[x, y] = (int)UnityEngine.Random.Range(0, tileTypes.Length);
            }
        }
    }

    private void generateMapInfo(int[,] template)
    {
        //change map size to that shown by the template
        mapSizeX = template.GetLength(1);
        mapSizeY = template.GetLength(0);

        tiles = new int[mapSizeX, mapSizeY];


        //for each row
        for (int x = 0; x < mapSizeY; x++)
        {
            //for each column
            for (int y = 0; y < mapSizeX; y++)
            {
                tiles[y, x] = template[x,y];
            }
        }
    }

    //Creates the graph for the pathfinding, it sets up the neighbours
    public void generatePathFindingGraph()
    {
        graph = new Node[mapSizeX, mapSizeY];

        //initialize graph 
        for (int x = 0; x < mapSizeX; x++)
        {
            for (int y = 0; y < mapSizeY; y++)
            {
                graph[x, y] = new Node();
                graph[x, y].x = x;
                graph[x, y].y = y;
            }
        }
        //calculate neighbours
        for (int x = 0; x < mapSizeX; x++)
        {
            for (int y = 0; y < mapSizeY; y++)
            {
                //X is not 0, then we can add left (x - 1)
                if (x > 0)
                {
                    graph[x, y].neighbours.Add(graph[x - 1, y]);
                }
                //X is not mapSizeX - 1, then we can add right (x + 1)
                if (x < mapSizeX - 1)
                {
                    graph[x, y].neighbours.Add(graph[x + 1, y]);
                }
                //Y is not 0, then we can add downwards (y - 1 ) 
                if (y > 0)
                {
                    graph[x, y].neighbours.Add(graph[x, y - 1]);
                }
                //Y is not mapSizeY -1, then we can add upwards (y + 1)
                if (y < mapSizeY - 1)
                {
                    graph[x, y].neighbours.Add(graph[x, y + 1]);
                }
            }
        }
    }

    private void generateMapVisuals()
    {
        //set up the bounds for the camera
        BoxCollider2D bc = cameraConfiner.GetComponent<BoxCollider2D>();
        bc.size = new Vector2(mapSizeX + 2, mapSizeY + 2);
        bc.offset = new Vector2(mapSizeX / 2, -mapSizeY / 2 );
        vCam1.GetComponent<CinemachineConfiner>().InvalidatePathCache();

        //generate list of actual tileGameObjects
        tilesOnMap = new GameObject[mapSizeX, mapSizeY];

        //Used to see which tile type is most prominent
        int[] tileTypeCount = new int[tileTypes.Length];

        int index;
        for (int x = 0; x < mapSizeX; x++)
        {
            for (int y = 0; y < mapSizeY; y++)
            {
                index = tiles[x, y];
                GameObject newTile = Instantiate(tileTypes[index].tileVisualPrefab, new Vector3(x, -y, 0), Quaternion.identity);
                newTile.GetComponent<ClickableTileScript>().tileX = x;
                newTile.GetComponent<ClickableTileScript>().tileY = y;
                newTile.GetComponent<ClickableTileScript>().map = this;
                newTile.transform.SetParent(tileContainer.transform);

                tilesOnMap[x, y] = newTile;

                if(index == 5)
                {
                    newTile.GetComponent<ClickableTileScript>().belongsToTeam = gameMan.team1;
                }
                else if(index == 6)
                {
                    newTile.GetComponent<ClickableTileScript>().belongsToTeam = gameMan.team2;
                }

                tileTypeCount[index] += 1;
            }
        }

        //generate edges of map
        if (indexOfBoardEdge < 0)
        {
            indexOfBoardEdge = 0;
            for (int i = 0; i < tileTypes.Length; i++)
            {
                if (tileTypeCount[i] >= tileTypeCount[indexOfBoardEdge])
                {
                    indexOfBoardEdge = i;
                }
            }
        }
        for (int x = 0; x < mapSizeX + 2; x++)
        {
            GameObject newEdge = Instantiate(tileTypes[indexOfBoardEdge].tileVisualPrefab, new Vector3(x - 1, 1, 0), Quaternion.identity);
            newEdge.transform.SetParent(boardEdgeContainer.transform);
            newEdge.tag = "Untagged";

            GameObject newEdge2 = Instantiate(tileTypes[indexOfBoardEdge].tileVisualPrefab, new Vector3(x - 1, -mapSizeY, 0), Quaternion.identity);
            newEdge2.transform.SetParent(boardEdgeContainer.transform);
            newEdge2.tag = "Untagged";
        }
        for (int x = 0; x < (mapSizeX + 2) + 2; x++)
        {
            GameObject newEdge = Instantiate(tileTypes[indexOfBoardEdge].tileVisualPrefab, new Vector3(x - 2, 2, 0), Quaternion.identity);
            newEdge.transform.SetParent(boardEdgeContainer.transform);
            newEdge.tag = "Untagged";

            GameObject newEdge2 = Instantiate(tileTypes[indexOfBoardEdge].tileVisualPrefab, new Vector3(x - 2, -mapSizeY - 1, 0), Quaternion.identity);
            newEdge2.transform.SetParent(boardEdgeContainer.transform);
            newEdge2.tag = "Untagged";
        }
        for (int y = 0; y < mapSizeY; y++)
        {
            GameObject newEdge = Instantiate(tileTypes[indexOfBoardEdge].tileVisualPrefab, new Vector3(-1, -y, 0), Quaternion.identity);
            newEdge.transform.SetParent(boardEdgeContainer.transform);
            newEdge.tag = "Untagged";

            GameObject newEdge2 = Instantiate(tileTypes[indexOfBoardEdge].tileVisualPrefab, new Vector3(mapSizeX, -y, 0), Quaternion.identity);
            newEdge2.transform.SetParent(boardEdgeContainer.transform);
            newEdge2.tag = "Untagged";
        }
        for (int y = 0; y < mapSizeY + 2; y++)
        {
            GameObject newEdge = Instantiate(tileTypes[indexOfBoardEdge].tileVisualPrefab, new Vector3(-2, -y + 1, 0), Quaternion.identity);
            newEdge.transform.SetParent(boardEdgeContainer.transform);
            newEdge.tag = "Untagged";

            GameObject newEdge2 = Instantiate(tileTypes[indexOfBoardEdge].tileVisualPrefab, new Vector3(mapSizeX + 1, -y + 1, 0), Quaternion.identity);
            newEdge2.transform.SetParent(boardEdgeContainer.transform);
            newEdge2.tag = "Untagged";
        }

        //Does tile need a sprite change
        for (int x = 0; x < mapSizeX; x++)
        {
            for (int y = 0; y < mapSizeY; y++)
            {
                index = tiles[x, y];
                if (index == 7 || index == 8 || index == 1)
                {
                    setTileSpriteBasedOnNeighbours(tilesOnMap[x, y]);
                }
            }
        }
    }

    private void setTileSpriteBasedOnNeighbours(GameObject tileToSet)
    {
        ClickableTileScript cTile = tileToSet.GetComponent<ClickableTileScript>();
        int indexOfTile = tiles[cTile.tileX, cTile.tileY];
        int nwIndex, nIndex, neIndex, wIndex, eIndex, swIndex, sIndex, seIndex;

        //get terrain indices of neighbouring cells
        if(cTile.tileX == 0 || cTile.tileY == 0)
        {
             nwIndex = indexOfBoardEdge;
        }
        else
        {
            nwIndex = tiles[cTile.tileX - 1, cTile.tileY - 1];
        }
        if (cTile.tileX == mapSizeX - 1 || cTile.tileY == 0)
        {
            neIndex = indexOfBoardEdge;
        }
        else
        {
            neIndex = tiles[cTile.tileX + 1, cTile.tileY - 1];
        }
        if (cTile.tileX == 0 || cTile.tileY == mapSizeY - 1)
        {
            swIndex = indexOfBoardEdge;
        }
        else
        {
            swIndex = tiles[cTile.tileX - 1, cTile.tileY + 1];
        }
        if (cTile.tileX == mapSizeX - 1 || cTile.tileY == mapSizeY - 1)
        {
            seIndex = indexOfBoardEdge;
        }
        else
        {
            seIndex = tiles[cTile.tileX + 1, cTile.tileY + 1];
        }
        if (cTile.tileY == 0)
        {
            nIndex = indexOfBoardEdge;
        }
        else
        {
            nIndex = tiles[cTile.tileX, cTile.tileY - 1];
        }
        if (cTile.tileX == 0)
        {
            wIndex = indexOfBoardEdge;
        }
        else
        {
            wIndex = tiles[cTile.tileX - 1, cTile.tileY];
        }
        if (cTile.tileX == mapSizeX-1)
        {
            eIndex = indexOfBoardEdge;
        }
        else
        {
            eIndex = tiles[cTile.tileX + 1, cTile.tileY];
        }
        if (cTile.tileY == mapSizeY -1)
        {
            sIndex = indexOfBoardEdge;
        }
        else
        {
            sIndex = tiles[cTile.tileX, cTile.tileY + 1];
        }

        //print("Neighbours of (" + cTile.tileX + "," + cTile.tileY + "): (N " + nIndex + ") (W " + wIndex + ") (E " + eIndex + ") (S " + sIndex + ")");

        //Is it a Trail tile?
        if(indexOfTile == 7)
        {
            if(nIndex != 7 && wIndex != 7 && eIndex != 7 && sIndex != 7)
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[0];
                return;
            }
            if(nIndex != 7 && wIndex != 7 && eIndex == 7 && sIndex != 7)
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[1];
                return;
            }
            if (nIndex != 7 && wIndex == 7 && eIndex == 7 && sIndex != 7)
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[2];
                return;
            }
            if (nIndex != 7 && wIndex == 7 && eIndex != 7 && sIndex != 7)
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[3];
                return;
            }

            if (nIndex != 7 && wIndex != 7 && eIndex != 7 && sIndex == 7)
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[4];
                return;
            }
            if (nIndex == 7 && wIndex != 7 && eIndex != 7 && sIndex == 7)
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[5];
                return;
            }
            if (nIndex == 7 && wIndex != 7 && eIndex != 7 && sIndex != 7)
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[6];
                return;
            }

            if (nIndex != 7 && wIndex != 7 && eIndex == 7 && sIndex == 7)
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[7];
                return;
            }
            if (nIndex != 7 && wIndex == 7 && eIndex == 7 && sIndex == 7)
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[8];
                return;
            }
            if (nIndex != 7 && wIndex == 7 && eIndex != 7 && sIndex == 7)
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[9];
                return;
            }
            if (nIndex == 7 && wIndex != 7 && eIndex == 7 && sIndex == 7)
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[10];
                return;
            }
            if (nIndex == 7 && wIndex == 7 && eIndex == 7 && sIndex == 7)
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[11];
                return;
            }
            if (nIndex == 7 && wIndex == 7 && eIndex != 7 && sIndex == 7)
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[12];
                return;
            }
            if (nIndex == 7 && wIndex != 7 && eIndex == 7 && sIndex != 7)
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[13];
                return;
            }
            if (nIndex == 7 && wIndex == 7 && eIndex == 7 && sIndex != 7)
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[14];
                return;
            }
            if (nIndex == 7 && wIndex == 7 && eIndex != 7 && sIndex != 7)
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[15];
                return;
            }
        }
        else
        //Is it a Coast tile?
        if(indexOfTile == 8)
        {
            if (nIndex == 1 && !IsWaterTile(wIndex) && !IsWaterTile(eIndex) && !IsWaterTile(sIndex))
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[10];
            }
            else if (!IsWaterTile(nIndex) && !IsWaterTile(wIndex) && !IsWaterTile(eIndex) && sIndex == 1)
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[11];
            }
            else if (!IsWaterTile(nIndex) && !IsWaterTile(wIndex) && eIndex == 1 && !IsWaterTile(sIndex))
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[24];
            }
            else if (!IsWaterTile(nIndex) && wIndex == 1 && !IsWaterTile(eIndex) && !IsWaterTile(sIndex))
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[25];
            }

            else if (nIndex == 1 && !IsWaterTile(wIndex) && eIndex == 8 && !IsWaterTile(sIndex))
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[14];
            }
            else if (!IsWaterTile(nIndex) && !IsWaterTile(wIndex) && eIndex == 8 && sIndex == 1)
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[15];
            }
            else if (nIndex == 1 && wIndex == 8 && !IsWaterTile(eIndex) && !IsWaterTile(sIndex))
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[18];
            }
            else if (!IsWaterTile(nIndex) && wIndex == 8 && !IsWaterTile(eIndex) && sIndex == 1)
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[19];
            }

            else if (!IsWaterTile(nIndex) && wIndex == 1 && !IsWaterTile(eIndex) && sIndex == 8)
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[22];
            }
            else if (nIndex == 8 && wIndex == 1 && !IsWaterTile(eIndex) && !IsWaterTile(sIndex))
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[23];
            }
            else if (!IsWaterTile(nIndex) && !IsWaterTile(wIndex) && eIndex == 1 && sIndex == 8)
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[26];
            }
            else if (nIndex == 8 && !IsWaterTile(wIndex) && eIndex == 8 && !IsWaterTile(sIndex))
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[27];
            }

            else if (nIndex != 8 && wIndex != 8 && !IsWaterTile(eIndex) && !IsWaterTile(sIndex))
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[28];
            }
            else if (!IsWaterTile(nIndex) && wIndex != 8 && !IsWaterTile(eIndex) && sIndex != 8)
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[29];
            }
            else if (nIndex != 8 && !IsWaterTile(wIndex) && eIndex != 8 && !IsWaterTile(sIndex))
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[32];
            }
            else if (!IsWaterTile(nIndex) && !IsWaterTile(wIndex) && eIndex != 8 && sIndex != 8)
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[33];
            }

            else if (nIndex == 8 && wIndex == 8 && !IsWaterTile(eIndex) && !IsWaterTile(sIndex))
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[35];
            }
            else if (!IsWaterTile(nIndex) && wIndex == 8 && !IsWaterTile(eIndex) && sIndex == 8)
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[34];
            }
            else if (nIndex == 8 && !IsWaterTile(wIndex) && eIndex == 8 && !IsWaterTile(sIndex))
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[31];
            }
            else if (!IsWaterTile(nIndex) && !IsWaterTile(wIndex) && eIndex == 8 && sIndex == 8)
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[30];
            }

            else if (nIndex != 8 && IsWaterTile(wIndex) && !IsWaterTile(eIndex) && sIndex == 8)
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[0];
            }
            else if(nIndex == 8 && IsWaterTile(wIndex) && !IsWaterTile(eIndex) && sIndex == 8)
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[1];
            }
            else if (nIndex == 8 && IsWaterTile(wIndex) && !IsWaterTile(eIndex) && sIndex != 8)
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[2];
            }
            else if (nIndex != 8 && IsWaterTile(wIndex) && !IsWaterTile(eIndex) && sIndex != 8)
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[3];
            }

            else if (nIndex != 8 && !IsWaterTile(wIndex) && IsWaterTile(eIndex) && sIndex == 8)
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[4];
            }
            else if (nIndex == 8 && !IsWaterTile(wIndex) && IsWaterTile(eIndex) && sIndex == 8)
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[5];
            }
            else if (nIndex == 8 && !IsWaterTile(wIndex) && IsWaterTile(eIndex) && sIndex != 8)
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[6];
            }
            else if (nIndex != 8 && !IsWaterTile(wIndex) && IsWaterTile(eIndex) && sIndex != 8)
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[7];
            }

            else if(IsWaterTile(nIndex) && wIndex != 8 && eIndex == 8 && !IsWaterTile(sIndex))
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[8];
            }
            else if (IsWaterTile(nIndex) && wIndex == 8 && eIndex == 8 && !IsWaterTile(sIndex))
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[12];
            }
            else if (IsWaterTile(nIndex) && wIndex == 8 && eIndex != 8 && !IsWaterTile(sIndex))
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[16];
            }
            else if (IsWaterTile(nIndex) && wIndex != 8 && eIndex != 8 && !IsWaterTile(sIndex))
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[20];
            }

            else if (!IsWaterTile(nIndex) && wIndex != 8 && eIndex == 8 && IsWaterTile(sIndex))
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[9];
            }
            else if (!IsWaterTile(nIndex) && wIndex == 8 && eIndex == 8 && IsWaterTile(sIndex))
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[13];
            }
            else if (!IsWaterTile(nIndex) && wIndex == 8 && eIndex != 8 && IsWaterTile(sIndex))
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[17];
            }
            else if (!IsWaterTile(nIndex) && wIndex != 8 && eIndex != 8 && IsWaterTile(sIndex))
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[21];
            }
        }else
        //Is it a Sea tile?
        if(indexOfTile == 1)
        {
            // Isolated water
            if( !IsWaterTile(nIndex) && !IsWaterTile(wIndex) && !IsWaterTile(eIndex) && !IsWaterTile(sIndex))
            {
                cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[0];
                return;
            }
            // SW Corner
            if (IsWaterTile(nIndex) && !IsWaterTile(wIndex) && IsWaterTile(eIndex) && !IsWaterTile(sIndex))
            {
                if (!IsWaterTile(neIndex))
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[19];
                }
                else
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[1];
                }
                return;
            }
            // South wall
            if (IsWaterTile(nIndex) && IsWaterTile(wIndex) && IsWaterTile(eIndex) && !IsWaterTile(sIndex))
            {
                if (!IsWaterTile(nwIndex) && !IsWaterTile(neIndex))
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[39];
                }
                else if(!IsWaterTile(nwIndex))
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[43];
                }
                else if (!IsWaterTile(neIndex))
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[35];
                }
                else
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[2];
                }
                return;
            }
            // SE Corner
            if (IsWaterTile(nIndex) && IsWaterTile(wIndex) && !IsWaterTile(eIndex) && !IsWaterTile(sIndex))
            {
                if (!IsWaterTile(nwIndex))
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[23];
                }
                else
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[3];
                }
                return;
            }
            // West Wall
            if (IsWaterTile(nIndex) && !IsWaterTile(wIndex) && IsWaterTile(eIndex) && IsWaterTile(sIndex))
            {
                if (!IsWaterTile(neIndex) && !IsWaterTile(seIndex))
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[25];
                }
                else if (!IsWaterTile(neIndex))
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[26];
                }
                else if (!IsWaterTile(seIndex))
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[24];
                }
                else
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[4];
                }
                return;
            }
            // Open Water
            if (IsWaterTile(nIndex) && IsWaterTile(wIndex) && IsWaterTile(eIndex) && IsWaterTile(sIndex))
            {
                if (IsWaterTile(nwIndex) && IsWaterTile(neIndex) && IsWaterTile(swIndex) && IsWaterTile(seIndex))
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[5];
                }
                else
                if (!IsWaterTile(nwIndex) && IsWaterTile(neIndex) && IsWaterTile(swIndex) && IsWaterTile(seIndex))
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[21];
                }
                else
                if (IsWaterTile(nwIndex) && !IsWaterTile(neIndex) && IsWaterTile(swIndex) && IsWaterTile(seIndex))
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[17];
                }
                else
                if (IsWaterTile(nwIndex) && IsWaterTile(neIndex) && !IsWaterTile(swIndex) && IsWaterTile(seIndex))
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[20];
                }
                else
                if (IsWaterTile(nwIndex) && IsWaterTile(neIndex) && IsWaterTile(swIndex) && !IsWaterTile(seIndex))
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[16];
                }
                else
                if (!IsWaterTile(nwIndex) && IsWaterTile(neIndex) && IsWaterTile(swIndex) && !IsWaterTile(seIndex))
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[27];
                }
                else
                if (IsWaterTile(nwIndex) && !IsWaterTile(neIndex) && !IsWaterTile(swIndex) && IsWaterTile(seIndex))
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[31];
                }
                else
                if (!IsWaterTile(nwIndex) && !IsWaterTile(neIndex) && IsWaterTile(swIndex) && IsWaterTile(seIndex))
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[32];
                }
                else
                if (!IsWaterTile(nwIndex) && IsWaterTile(neIndex) && !IsWaterTile(swIndex) && IsWaterTile(seIndex))
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[33];
                }
                else
                if (IsWaterTile(nwIndex) && !IsWaterTile(neIndex) && IsWaterTile(swIndex) && !IsWaterTile(seIndex))
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[36];
                }
                else
                if (IsWaterTile(nwIndex) && IsWaterTile(neIndex) && !IsWaterTile(swIndex) && !IsWaterTile(seIndex))
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[37];
                }
                else
                // 3 Corners
                if (IsWaterTile(nwIndex) && !IsWaterTile(neIndex) && !IsWaterTile(swIndex) && !IsWaterTile(seIndex))
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[40];
                }
                else
                if (!IsWaterTile(nwIndex) && IsWaterTile(neIndex) && !IsWaterTile(swIndex) && !IsWaterTile(seIndex))
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[44];
                }
                else
                if (!IsWaterTile(nwIndex) && !IsWaterTile(neIndex) && IsWaterTile(swIndex) && !IsWaterTile(seIndex))
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[41];
                }
                else
                if (!IsWaterTile(nwIndex) && !IsWaterTile(neIndex) && !IsWaterTile(swIndex) && IsWaterTile(seIndex))
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[45];
                }
                else
                // 4 Corners
                if (!IsWaterTile(nwIndex) && !IsWaterTile(neIndex) && !IsWaterTile(swIndex) && !IsWaterTile(seIndex))
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[46];
                }
                return;
            }
            // East Wall
            if (IsWaterTile(nIndex) && IsWaterTile(wIndex) && !IsWaterTile(eIndex) && IsWaterTile(sIndex))
            {
                if (!IsWaterTile(nwIndex) && !IsWaterTile(swIndex))
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[29];
                }
                else if (!IsWaterTile(nwIndex))
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[30];
                }
                else if (!IsWaterTile(swIndex))
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[28];
                }
                else
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[6];
                }
                return;
            }
            // NW Corner
            if (!IsWaterTile(nIndex) && !IsWaterTile(wIndex) && IsWaterTile(eIndex) && IsWaterTile(sIndex))
            {
                if (!IsWaterTile(seIndex))
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[18];
                }
                else
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[7];
                }
                return;
            }
            // North wall
            if (!IsWaterTile(nIndex) && IsWaterTile(wIndex) && IsWaterTile(eIndex) && IsWaterTile(sIndex))
            {
                if (!IsWaterTile(swIndex) && !IsWaterTile(seIndex))
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[38];
                }
                else if (!IsWaterTile(swIndex))
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[42];
                }
                else if (!IsWaterTile(seIndex))
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[34];
                }
                else
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[8];
                }
                return;
            }
            // NE Corner
            if (!IsWaterTile(nIndex) && IsWaterTile(wIndex) && !IsWaterTile(eIndex) && IsWaterTile(sIndex))
            {
                if (!IsWaterTile(swIndex))
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[22];
                }
                else
                {
                    cTile.GetComponent<SpriteRenderer>().sprite = cTile.sprites[9];
                }
                return;
            }
        }

    }

    //Is tile a "water tile" for purposes of assembling the map?
    private bool IsWaterTile(int indexOfTile)
    {
        return (indexOfTile == 1 || indexOfTile == 8);
    }

    //Desc: sets the tile as occupied, if a unit is on the tile
    public void setIfTileIsOccupied()
    {
        foreach (Transform team in unitsOnBoard.transform)
        {
            foreach (Transform unitOnTeam in team)
            {
                int unitX = unitOnTeam.GetComponent<Unit>().x;
                int unitY = unitOnTeam.GetComponent<Unit>().y;
                unitOnTeam.GetComponent<Unit>().tileBeingOccupied = tilesOnMap[unitX, unitY];
                tilesOnMap[unitX, unitY].GetComponent<ClickableTileScript>().unitOnTile = unitOnTeam.gameObject;
            }
        }
    }

    private void Update()
    {

        if (Input.GetMouseButton(2) && !gameMan.showingAttackRange && selectedUnit == null && gameMan.tileBeingDisplayed.GetComponent<ClickableTileScript>().unitOnTile != null)
        {
            highlightAttackableTiles();
            gameMan.showingAttackRange = true;
            return;
        }
        else if (Input.GetMouseButtonUp(2) && gameMan.showingAttackRange && selectedUnit == null)
        {
            disableHighlightUnitRange();
            gameMan.showingAttackRange = false;
        }

        //If input is left mouse down then select the unit
        if (Input.GetMouseButtonDown(0))
        {
            if (selectedUnit == null && !gameMan.showingAttackRange)
            {
                if(mouseClickToSelectUnitV2())
                {
                    gameMan.showingAttackRange = false;
                }
                else
                {

                    if (gameMan.unitBeingDisplayed != null && gameMan.unitBeingDisplayed.GetComponent<Unit>().unitMoveState == Unit.MovementStates.Wait)
                    {
                        uiMan.displayingInfoPanels = false;
                        adjustUnitMenuPrep();
                        uiMan.ShowMenu(uiMan.unitMenu);
                    }
                    else if (!uiMan.freezeCursor)
                    {
                        uiMan.ShowMenu(uiMan.gameMenu);
                    }
                }
            }

            //After a unit has been selected then if we get a mouse click, we need to check if the unit has entered the selection state (1) 'Selected'
            //Move the unit
            else if (selectedUnit.GetComponent<Unit>().unitMoveState == Unit.MovementStates.Selected && selectedUnit.GetComponent<Unit>().movementQueue.Count == 0)
            {
                if (selectTileToMoveTo())
                {
                    //selectedSound.Play();
                    //Debug.Log("movement path has been located");
                    unitSelectedPreviousX = selectedUnit.GetComponent<Unit>().x;
                    unitSelectedPreviousY = selectedUnit.GetComponent<Unit>().y;
                    previousOccupiedTile = selectedUnit.GetComponent<Unit>().tileBeingOccupied;
                    moveUnit();

                    StartCoroutine(moveUnitAndFinalize());
                    //The moveUnit function calls a function on the unitScript when the movement is completed the finalization is called from that script.
                }
            }
            //Finalize the movement
            else if (selectedUnit.GetComponent<Unit>().unitMoveState == Unit.MovementStates.Moved && uiMan.IsMenuHidden(uiMan.unitMenu))
            {
                finalizeOption();
            }
        }

        //Unselect unit with the right click
        if (Input.GetMouseButtonDown(1))
        {
            if (!uiMan.IsMenuHidden(uiMan.gameMenu))
            {
                uiMan.HideMenu(uiMan.gameMenu);
            }
            else if (selectedUnit != null)
            {
                if (selectedUnit.GetComponent<Unit>().movementQueue.Count == 0 && selectedUnit.GetComponent<Unit>().combatQueue.Count == 0)
                {
                    if (selectedUnit.GetComponent<Unit>().unitMoveState != Unit.MovementStates.Wait)
                    {
                        //unselectedSound.Play();
                        //selectedUnit.GetComponent<Unit>().setIdleAnimation();
                        deselectUnit();
                    }
                }
            }else if(gameMan.unitBeingDisplayed != null)
            {
                deselectUnit();
            }
        }        
    }

    //Moves the unit
    public void moveUnit()
    {
        if (selectedUnit != null)
        {
            selectedUnit.GetComponent<Unit>().MoveNextTile();
        }
    }

    //Desc: returns a vector 3 of the tile in world space, 
    public Vector3 tileCoordToWorldCoord(int x, int y)
    {
        return new Vector3(x, -y, 0);
    }

    //Desc: finalizes the movement, sets the tile the unit moved to as occupied, etc
    public void finalizeMovementPosition()
    {
        hasUnitMoved = !(tilesOnMap[selectedUnit.GetComponent<Unit>().x, selectedUnit.GetComponent<Unit>().y].GetComponent<ClickableTileScript>().unitOnTile == selectedUnit);
        if (hasUnitMoved)
        {
            gameMan.usedAP += 1;
        }
        tilesOnMap[selectedUnit.GetComponent<Unit>().x, selectedUnit.GetComponent<Unit>().y].GetComponent<ClickableTileScript>().unitOnTile = selectedUnit;
        
        //After a unit has been moved we will set the unitMoveState to (2) the 'Moved' state
        selectedUnit.GetComponent<Unit>().unitMoveState = Unit.MovementStates.Moved;

        adjustUnitMenuPrep();
        uiMan.ShowMenu(uiMan.unitMenu);
        highlightTileUnitIsOccupying();

        //Unit moved, so might need to adjust stats due to buffs/debuffs of adjacent units
        selectedUnit.GetComponent<Unit>().critAvoid = selectedUnit.GetComponent<Unit>().baseCritAvoid;
        selectedUnit.GetComponent<Unit>().chanceToCrit = selectedUnit.GetComponent<Unit>().baseChanceToCrit;

        cleansePreviouslyOccupiedTile();
        affectTilesAfterMoving();
        checkTilesAfterMoving();
        //gameMan.displayingInfoPanels = true;
    }

    //Desc: make any changes to neighbouring units after the unit moved
    public void affectTilesAfterMoving()
    {
        if (selectedUnit.GetComponent<Unit>().unitClass == Unit.UnitClasses.Sneaker)
        {
            foreach (Node n in graph[selectedUnit.GetComponent<Unit>().x, selectedUnit.GetComponent<Unit>().y].neighbours)
            {
                if (tilesOnMap[n.x, n.y].GetComponent<ClickableTileScript>().unitOnTile != null)
                {
                    GameObject unitOnTile = tilesOnMap[n.x, n.y].GetComponent<ClickableTileScript>().unitOnTile;
                    if (unitOnTile.GetComponent<Unit>().teamNum != selectedUnit.GetComponent<Unit>().teamNum)
                    {
                        print("Debuffing enemy unit!");
                        unitOnTile.GetComponent<Unit>().critAvoid -= 5.0f;
                    }
                }
            }
        }else if (selectedUnit.GetComponent<Unit>().unitClass == Unit.UnitClasses.Supporter)
        {
            foreach (Node n in graph[selectedUnit.GetComponent<Unit>().x, selectedUnit.GetComponent<Unit>().y].neighbours)
            {
                if (tilesOnMap[n.x, n.y].GetComponent<ClickableTileScript>().unitOnTile != null)
                {
                    GameObject unitOnTile = tilesOnMap[n.x, n.y].GetComponent<ClickableTileScript>().unitOnTile;
                    if (unitOnTile.GetComponent<Unit>().teamNum == selectedUnit.GetComponent<Unit>().teamNum)
                    {
                        print("Buffing friendly unit!");
                        unitOnTile.GetComponent<Unit>().chanceToCrit += 5.0f;
                    }
                }
            }
        }
    }

    //Desc: See if unit has moved next to any units that will change their stats
    public void checkTilesAfterMoving()
    {
        foreach (Node n in graph[selectedUnit.GetComponent<Unit>().x, selectedUnit.GetComponent<Unit>().y].neighbours)
        {
            if (tilesOnMap[n.x, n.y].GetComponent<ClickableTileScript>().unitOnTile != null)
            {
                GameObject unitOnTile = tilesOnMap[n.x, n.y].GetComponent<ClickableTileScript>().unitOnTile;
                if (unitOnTile.GetComponent<Unit>().teamNum != selectedUnit.GetComponent<Unit>().teamNum && unitOnTile.GetComponent<Unit>().unitClass == Unit.UnitClasses.Sneaker)
                {
                    print("Moved next to a sneaker!");
                    selectedUnit.GetComponent<Unit>().critAvoid -= 5.0f;
                } else if (unitOnTile.GetComponent<Unit>().teamNum == selectedUnit.GetComponent<Unit>().teamNum && unitOnTile.GetComponent<Unit>().unitClass == Unit.UnitClasses.Supporter)
                {
                    print("Moved next to a supporter!");
                    selectedUnit.GetComponent<Unit>().chanceToCrit += 5.0f;
                }
            }
        }
    }

    //Desc: Cleanse the tiles around the tile the unit previously occupied (after moving)
    public void cleansePreviouslyOccupiedTile()
    {
        if (selectedUnit.GetComponent<Unit>().unitClass == Unit.UnitClasses.Sneaker)
        {
            foreach (Node n in graph[previousOccupiedTile.GetComponent<ClickableTileScript>().tileX, previousOccupiedTile.GetComponent<ClickableTileScript>().tileY].neighbours)
            {
                if (tilesOnMap[n.x, n.y].GetComponent<ClickableTileScript>().unitOnTile != null)
                {
                    GameObject unitOnTile = tilesOnMap[n.x, n.y].GetComponent<ClickableTileScript>().unitOnTile;
                    if (unitOnTile.GetComponent<Unit>().teamNum != selectedUnit.GetComponent<Unit>().teamNum)
                    {
                        print("Removing debuff from a unit!");
                        unitOnTile.GetComponent<Unit>().critAvoid += 5.0f;
                    }
                }
            }
        } else if (selectedUnit.GetComponent<Unit>().unitClass == Unit.UnitClasses.Supporter)
        {
            foreach (Node n in graph[previousOccupiedTile.GetComponent<ClickableTileScript>().tileX, previousOccupiedTile.GetComponent<ClickableTileScript>().tileY].neighbours)
            {
                if (tilesOnMap[n.x, n.y].GetComponent<ClickableTileScript>().unitOnTile != null)
                {
                    GameObject unitOnTile = tilesOnMap[n.x, n.y].GetComponent<ClickableTileScript>().unitOnTile;
                    if (unitOnTile.GetComponent<Unit>().teamNum == selectedUnit.GetComponent<Unit>().teamNum)
                    {
                        print("Removing buff from a unit!");
                        unitOnTile.GetComponent<Unit>().chanceToCrit -= 5.0f;
                    }
                }
            }
        }
    }

    //Desc: Cleanse neighbouring tiles around [tile]. Called if a unit dies
    public void cleanseNeighbouringTiles(GameObject tileToCleanse)
    {
        ClickableTileScript tempTile = tileToCleanse.GetComponent<ClickableTileScript>();

        if (tempTile.unitOnTile.GetComponent<Unit>().unitClass == Unit.UnitClasses.Sneaker)
        {
            foreach (Node n in graph[tempTile.tileX, tempTile.tileY].neighbours)
            {
                if (tilesOnMap[n.x, n.y].GetComponent<ClickableTileScript>().unitOnTile != null)
                {
                    GameObject unitOnTile = tilesOnMap[n.x, n.y].GetComponent<ClickableTileScript>().unitOnTile;
                    if (unitOnTile.GetComponent<Unit>().teamNum != tempTile.unitOnTile.GetComponent<Unit>().teamNum)
                    {
                        print("Removing debuff from a unit!");
                        unitOnTile.GetComponent<Unit>().critAvoid += 5.0f;
                    }
                }
            }
        } else if (tempTile.unitOnTile.GetComponent<Unit>().unitClass == Unit.UnitClasses.Supporter)
        {
            foreach (Node n in graph[tempTile.tileX, tempTile.tileY].neighbours)
            {
                if (tilesOnMap[n.x, n.y].GetComponent<ClickableTileScript>().unitOnTile != null)
                {
                    GameObject unitOnTile = tilesOnMap[n.x, n.y].GetComponent<ClickableTileScript>().unitOnTile;
                    if (unitOnTile.GetComponent<Unit>().teamNum == tempTile.unitOnTile.GetComponent<Unit>().teamNum)
                    {
                        print("Removing buff from a unit!");
                        unitOnTile.GetComponent<Unit>().chanceToCrit -= 5.0f;
                    }
                }
            }
        }
    }

    public void refreshAllUnitEffects()
    {
        foreach (Transform team in unitsOnBoard.transform)
        {
            foreach (Transform unitOnTeam in team)
            {
                if (unitOnTeam.GetComponent<Unit>().unitClass == Unit.UnitClasses.Sneaker)
                {
                    foreach (Node n in graph[unitOnTeam.GetComponent<Unit>().x, unitOnTeam.GetComponent<Unit>().y].neighbours)
                    {
                        if (tilesOnMap[n.x, n.y].GetComponent<ClickableTileScript>().unitOnTile != null)
                        {
                            GameObject unitOnTile = tilesOnMap[n.x, n.y].GetComponent<ClickableTileScript>().unitOnTile;
                            if (unitOnTile.GetComponent<Unit>().teamNum != unitOnTeam.GetComponent<Unit>().teamNum)
                            {
                                print("Debuffing enemy unit!");
                                unitOnTile.GetComponent<Unit>().critAvoid -= 5.0f;
                            }
                        }
                    }
                }
                else if (unitOnTeam.GetComponent<Unit>().unitClass == Unit.UnitClasses.Supporter)
                {
                    foreach (Node n in graph[unitOnTeam.GetComponent<Unit>().x, unitOnTeam.GetComponent<Unit>().y].neighbours)
                    {
                        if (tilesOnMap[n.x, n.y].GetComponent<ClickableTileScript>().unitOnTile != null)
                        {
                            GameObject unitOnTile = tilesOnMap[n.x, n.y].GetComponent<ClickableTileScript>().unitOnTile;
                            if (unitOnTile.GetComponent<Unit>().teamNum == unitOnTeam.GetComponent<Unit>().teamNum)
                            {
                                print("Buffing friendly unit!");
                                unitOnTile.GetComponent<Unit>().chanceToCrit += 5.0f;
                            }
                        }
                    }
                }
            }
        }
    }

    //Desc: Make sure the unit dropdown menu has the appropriate actions in it
    private void adjustUnitMenuPrep()
    {
        print("Adjusting Unit Menu...");

        List<UIManager.UnitMenuOption> availableOptions = new List<UIManager.UnitMenuOption>();
        List<UIManager.UnitMenuOption> disabledOptions = new List<UIManager.UnitMenuOption>();

        Unit tempSelectedUnit;
        if (selectedUnit != null)
        {
           tempSelectedUnit = selectedUnit.GetComponent<Unit>();
        }
        else
        {
            tempSelectedUnit = gameMan.unitBeingDisplayed.GetComponent<Unit>();
        }

        if (tempSelectedUnit.unitMoveState == Unit.MovementStates.Wait)
        {
            if (gameMan.CanAffordAPCost(1))
            {
                availableOptions.Add(new UIManager.UnitMenuOption(UIManager.UnitMenuButtons.REFRESH, 1));
            }
            else
            {
                disabledOptions.Add(new UIManager.UnitMenuOption(UIManager.UnitMenuButtons.REFRESH, 1));
            }
            activeUnitMenuButtons = availableOptions;
            uiMan.AdjustUnitMenu(availableOptions, disabledOptions);
            return;
        }

        //all units gain Wait
        availableOptions.Add(new UIManager.UnitMenuOption(UIManager.UnitMenuButtons.WAIT, 0));

        //Is the Attack option getting added
        //check if there are attackable enemy units-- if not, disable Attack option
        if (getAttackableEnemiesFromPosition().Count > 0) 
        {
            int cost = 1;
            if (tempSelectedUnit.unitClass == Unit.UnitClasses.Attacker && hasUnitMoved)
            {
                cost = 0;
            }
            if (gameMan.CanAffordAPCost(cost))
            {
                availableOptions.Add(new UIManager.UnitMenuOption(UIManager.UnitMenuButtons.ATTACK, cost));
            }
            else
            {
                disabledOptions.Add(new UIManager.UnitMenuOption(UIManager.UnitMenuButtons.ATTACK, cost));
            }
        }

        //Is the heal option getting added
        if (getHealableUnitsFromPosition().Count > 0)
        {
            if (gameMan.CanAffordAPCost(1))
            {
                availableOptions.Add(new UIManager.UnitMenuOption(UIManager.UnitMenuButtons.HEAL, 1));
            }
            else
            {
                disabledOptions.Add(new UIManager.UnitMenuOption(UIManager.UnitMenuButtons.HEAL, 1));
            }
        }

        //Unit is on enemy base, so gains Sunder
        if (tempSelectedUnit.tileBeingOccupied.GetComponent<ClickableTileScript>().belongsToTeam != null
            && tempSelectedUnit.tileBeingOccupied.GetComponent<ClickableTileScript>().belongsToTeam != gameMan.returnTeam(tempSelectedUnit.teamNum)
            && tempSelectedUnit.attackDamage > 0)
        {
            if (gameMan.CanAffordAPCost(1))
            {
                availableOptions.Add(new UIManager.UnitMenuOption(UIManager.UnitMenuButtons.SUNDER, 1));
            }
            else
            {
                disabledOptions.Add(new UIManager.UnitMenuOption(UIManager.UnitMenuButtons.SUNDER, 1));
            }
        }

        //Unit is a Defender, so gains Fortify 
        if (tempSelectedUnit.unitClass == Unit.UnitClasses.Defender)
        {
            if (gameMan.CanAffordAPCost(1))
            {
                availableOptions.Add(new UIManager.UnitMenuOption(UIManager.UnitMenuButtons.FORTIFY, 1));
            }
            else
            {
                disabledOptions.Add(new UIManager.UnitMenuOption(UIManager.UnitMenuButtons.FORTIFY, 1));
            }
        }

        activeUnitMenuButtons = availableOptions;

        uiMan.AdjustUnitMenu(availableOptions, disabledOptions);
    }

    //Desc: selects a unit based on the cursor from the other script
    // Returns true if selection was successful, false otherwise
    public bool mouseClickToSelectUnitV2()
    {
        if (unitSelected == false && gameMan.tileBeingDisplayed != null)
        {
            if (gameMan.tileBeingDisplayed.GetComponent<ClickableTileScript>().unitOnTile != null)
            {
                GameObject tempSelectedUnit = gameMan.tileBeingDisplayed.GetComponent<ClickableTileScript>().unitOnTile;
                if (tempSelectedUnit.GetComponent<Unit>().unitMoveState == Unit.MovementStates.Unselected
                               && tempSelectedUnit.GetComponent<Unit>().teamNum == gameMan.currentTeam)
                {
                    disableHighlightUnitRange();
                    //selectedSound.Play();
                    selectedUnit = tempSelectedUnit;
                    selectedUnit.GetComponent<Unit>().mapMan = this;
                    selectedUnit.GetComponent<Unit>().unitMoveState = Unit.MovementStates.Selected;
                    //selectedUnit.GetComponent<Unit>().setSelectedAnimation();
                    unitSelected = true;
                    hasUnitMoved = false;
                    highlightUnitRange();

                    uiMan.displayingInfoPanels = false;
                    return true;
                }
            }
        }
        return false;
    }

    public void highlightUnitRange()
    {
        HashSet<Node> finalUnoccupiedMoves = new HashSet<Node>();
        HashSet<Node> finalMovementHighlight = new HashSet<Node>();

        HashSet<Node> totalAttackableTiles = new HashSet<Node>();
        HashSet<Node> finalEnemyUnitsInMovementRange = new HashSet<Node>();

        int attRangeMin = (int)selectedUnit.GetComponent<Unit>().attackRange.x;
        int attRangeMax = (int)selectedUnit.GetComponent<Unit>().attackRange.y;

        int moveSpeed = selectedUnit.GetComponent<Unit>().moveSpeed;

        Node unitInitialNode = graph[selectedUnit.GetComponent<Unit>().x, selectedUnit.GetComponent<Unit>().y];
        finalUnoccupiedMoves = RemoveOccupiedTiles(getUnitMovementOptions());
        finalUnoccupiedMoves.Add(unitInitialNode);

        finalMovementHighlight = getUnitMovementOptions();

        if (attRangeMin == attRangeMax)
        {
            totalAttackableTiles = getUnitTotalAttackableTiles(finalUnoccupiedMoves, selectedUnit.GetComponent<Unit>().attackRange, unitInitialNode);
        }
        if ( selectedUnit.GetComponent<Unit>().attackDamage <= 0)
        {
            totalAttackableTiles = new HashSet<Node>();
        }

        foreach (Node n in totalAttackableTiles)
        {
            if (tilesOnMap[n.x, n.y].GetComponent<ClickableTileScript>().unitOnTile != null)
            {
                GameObject unitOnCurrentlySelectedTile = tilesOnMap[n.x, n.y].GetComponent<ClickableTileScript>().unitOnTile;
                if (unitOnCurrentlySelectedTile.GetComponent<Unit>().teamNum != selectedUnit.GetComponent<Unit>().teamNum)
                {
                    finalEnemyUnitsInMovementRange.Add(n);
                }
            }
        }

        highlightTileset(finalMovementHighlight, ClickableTileScript.FlashStates.Move);

        highlightEnemiesInRange(totalAttackableTiles);

        selectedUnitMoveRange = finalUnoccupiedMoves;
    }

    //Desc: returns the hashSet of nodes that the unit can reach from its position
    public HashSet<Node> getUnitMovementOptions()
    {
        float[,] cost = new float[mapSizeX, mapSizeY];
        HashSet<Node> UIHighlight = new HashSet<Node>();
        HashSet<Node> tempUIHighlight = new HashSet<Node>();
        HashSet<Node> finalMovementHighlight = new HashSet<Node>();
        int moveSpeed = selectedUnit.GetComponent<Unit>().moveSpeed;
        Node unitInitialNode = graph[selectedUnit.GetComponent<Unit>().x, selectedUnit.GetComponent<Unit>().y];

        ///Set-up the initial costs for the neighbouring nodes
        finalMovementHighlight.Add(unitInitialNode);
        if(gameMan.currentAP <= 0)
        {
            return finalMovementHighlight; 
        }

        foreach (Node n in unitInitialNode.neighbours)
        {
            cost[n.x, n.y] = costToEnterTile(n.x, n.y);
            if (moveSpeed - cost[n.x, n.y] >= 0)
            {
                UIHighlight.Add(n);
            }
        }

        finalMovementHighlight.UnionWith(UIHighlight);

        while (UIHighlight.Count != 0)
        {
            foreach (Node n in UIHighlight)
            {
                foreach (Node neighbour in n.neighbours)
                {
                    if (!finalMovementHighlight.Contains(neighbour))
                    {
                        cost[neighbour.x, neighbour.y] = costToEnterTile(neighbour.x, neighbour.y) + cost[n.x, n.y];
                        if (moveSpeed - cost[neighbour.x, neighbour.y] >= 0)
                        {
                            tempUIHighlight.Add(neighbour);
                        }
                    }
                }
            }

            UIHighlight = tempUIHighlight;
            finalMovementHighlight.UnionWith(UIHighlight);
            tempUIHighlight = new HashSet<Node>();

        }
        //Debug.Log("The total amount of movable spaces for this unit is: " + finalMovementHighlight.Count);
        //Debug.Log("We have used the function to calculate it this time");
        return finalMovementHighlight;
    }

    //Removes nodes with a unit on them from the input set
    public HashSet<Node> RemoveOccupiedTiles(HashSet<Node> set)
    {
        HashSet<Node> purgedSet = new HashSet<Node>();

        foreach(Node n in set)
        {
            if(tilesOnMap[n.x, n.y].GetComponent<ClickableTileScript>().unitOnTile == null)
            {
                purgedSet.Add(n);
            }
        }
        return purgedSet;
    }

    //Desc: checks the cost of the tile for a unit to enter
    public float costToEnterTile(int x, int y)
    {
        if (!unitCanEnterTile(selectedUnit.GetComponent<Unit>(), x, y))
        {
            return Mathf.Infinity;
        }

        if (selectedUnit.GetComponent<Unit>().moveOptions.Contains(Unit.MovementTypes.Fly))
        {
            return 1.0f;
        }

        Tile t = tileTypes[tiles[x, y]];
        float dist = t.movementCost;
        return dist;
    }

    public int defenseOfTile(int x, int y)
    {
        Tile t = tileTypes[tiles[x, y]];
        int def = t.defenseModifier;
        return def;
    }

    public Tile getTileAt(int x, int y)
    {
        return tileTypes[tiles[x, y]];
    }

    //Desc: if the tile is not occupied by another team's unit, then you can walk through and if the tile is walkable 
    public bool unitCanEnterTile(Unit u, int x, int y)
    {
        if (tilesOnMap[x, y].GetComponent<ClickableTileScript>().unitOnTile != null)
        {
            if (tilesOnMap[x, y].GetComponent<ClickableTileScript>().unitOnTile.GetComponent<Unit>().teamNum != selectedUnit.GetComponent<Unit>().teamNum)
            {
                return false;
            }
        }

        foreach (Unit.MovementTypes unitMoves in u.moveOptions)
        {
            if (getTileAt(x, y).movesAllowed.Contains(unitMoves))
            {
                return true;
            }
        }
        return false;
    }

    //Desc: highlights the selected unit's tile
    public void highlightTileUnitIsOccupying()
    {
        if (selectedUnit != null)
        {
            highlightTileset(getTileSelectedUnitIsOccupying(), ClickableTileScript.FlashStates.Move);
        }
    }

    //Desc: returns a set of nodes of the tile that the unit is occupying
    public HashSet<Node> getTileSelectedUnitIsOccupying()
    {
        int x = selectedUnit.GetComponent<Unit>().x;
        int y = selectedUnit.GetComponent<Unit>().y;
        HashSet<Node> singleTile = new HashSet<Node>();
        singleTile.Add(graph[x, y]);
        return singleTile;
    }

    //Desc: This function highlights the tileset with the type of flashing that is passed in
    public void highlightTileset(HashSet<Node> toHighlight, ClickableTileScript.FlashStates flashType)
    {
        foreach (Node n in toHighlight)
        {
            tilesOnMap[n.x, n.y].GetComponent<ClickableTileScript>().ToggleFlash(flashType);
        }
    }

    //Desc: This function highlights the enemies in range once they have been added to a hashSet
    public void highlightEnemiesInRange(HashSet<Node> enemiesToHighlight)
    {
        foreach (Node n in enemiesToHighlight)
        {
            if (!tilesOnMap[n.x, n.y].GetComponent<ClickableTileScript>().isFlashing)
            {
                tilesOnMap[n.x, n.y].GetComponent<ClickableTileScript>().StartFlash(ClickableTileScript.FlashStates.Attack);
            }
        }
    }

    //Desc: highlights the selected unit's attackOptions from its position
    public void highlightUnitAttackOptionsFromPosition()
    {
        if (selectedUnit != null)
        {
            highlightEnemiesInRange(getUnitAttackOptionsFromPosition());
        }
    }

    //Desc: highlights the unit's attack range, taking their movement into account
    public void highlightAttackableTiles()
    {
        if (gameMan.tileBeingDisplayed.GetComponent<ClickableTileScript>().unitOnTile != null)
        {
            selectedUnit = gameMan.tileBeingDisplayed.GetComponent<ClickableTileScript>().unitOnTile;

            if (selectedUnit.GetComponent<Unit>().attackDamage <= 0)
            {
                selectedUnit = null;
                return;
            }

            HashSet<Node> finalMovementHighlight = new HashSet<Node>();
            HashSet<Node> totalAttackableTiles = new HashSet<Node>();

            Node unitInitialNode = graph[selectedUnit.GetComponent<Unit>().x, selectedUnit.GetComponent<Unit>().y];
            finalMovementHighlight = getUnitMovementOptions();
            totalAttackableTiles = getUnitTotalAttackableTiles(finalMovementHighlight, selectedUnit.GetComponent<Unit>().attackRange, unitInitialNode);
            totalAttackableTiles.Add(unitInitialNode);
            
            highlightEnemiesInRange(totalAttackableTiles);

            selectedUnit = null;
        }
    }

    //Desc: disables the quads that are being used to highlight position
    public void disableHighlightUnitRange()
    {
        foreach (GameObject tile in tilesOnMap)
        {
            if (tile.GetComponent<ClickableTileScript>().isFlashing)
            {
                tile.GetComponent<ClickableTileScript>().ToggleFlash();
            }
        }
    }

    //Desc: moves the unit then finalizes the movement
    public IEnumerator moveUnitAndFinalize()
    {
        disableHighlightUnitRange();
        //disableUnitUIRoute();
        while (selectedUnit.GetComponent<Unit>().movementQueue.Count != 0)
        {
            yield return new WaitForEndOfFrame();
        }
        finalizeMovementPosition();
        //selectedUnit.GetComponent<Unit>().setSelectedAnimation();
    }

    //Desc: finalizes the player's option
    public void finalizeOption()
    {
        RaycastHit2D hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        hit = Physics2D.GetRayIntersection(Camera.main.ScreenPointToRay(Input.mousePosition));
        HashSet<Node> attackableTiles = getUnitAttackOptionsFromPosition();
        HashSet<Node> healableTiles = getHealableUnitsFromPosition(); ;

        if (hit)
        {
            //This portion is to ensure that the tile has been clicked
            //If the tile has been clicked then we need to check if there is a unit on it
            if (hit.transform.gameObject.CompareTag("Tile"))
            {
                if (hit.transform.GetComponent<ClickableTileScript>().isFlashing)
                {
                    GameObject unitOnTile = hit.transform.GetComponent<ClickableTileScript>().unitOnTile;
                    int unitX = unitOnTile.GetComponent<Unit>().x;
                    int unitY = unitOnTile.GetComponent<Unit>().y;

                    if (unitOnTile.GetComponent<Unit>().teamNum != selectedUnit.GetComponent<Unit>().teamNum && attackableTiles.Contains(graph[unitX, unitY]))
                    {
                        if (unitOnTile.GetComponent<Unit>().currentHealthPoints > 0)
                        {
                            Debug.Log("We clicked the tile of an enemy that should be attacked");
                            //Debug.Log(selectedUnit.GetComponent<Unit>().currentHealthPoints);
                            
                            StartCoroutine(batMan.attack(selectedUnit, unitOnTile));

                            StartCoroutine(deselectAfterMovements(selectedUnit, unitOnTile));
                        }
                    } else if (unitOnTile.GetComponent<Unit>().teamNum == selectedUnit.GetComponent<Unit>().teamNum && healableTiles.Contains(graph[unitX, unitY]))
                    {
                        if (unitOnTile.GetComponent<Unit>().currentHealthPoints > 0)
                        {
                            Debug.Log("We clicked the tile of an ally that should be supported");
                            //Debug.Log(selectedUnit.GetComponent<Unit>().currentHealthPoints);

                            StartCoroutine(batMan.attack(selectedUnit, unitOnTile));

                            StartCoroutine(deselectAfterMovements(selectedUnit, unitOnTile));
                        }
                    }
                }
            }
            else if (hit.transform.parent != null && hit.transform.parent.gameObject.CompareTag("Unit"))
            {
                GameObject unitClicked = hit.transform.parent.gameObject;
                int unitX = unitClicked.GetComponent<Unit>().x;
                int unitY = unitClicked.GetComponent<Unit>().y;

                if (unitClicked.GetComponent<Unit>().tileBeingOccupied.GetComponent<ClickableTileScript>().isFlashing)
                {
                    if (unitClicked.GetComponent<Unit>().teamNum != selectedUnit.GetComponent<Unit>().teamNum && attackableTiles.Contains(graph[unitX, unitY]))
                    {
                        if (unitClicked.GetComponent<Unit>().currentHealthPoints > 0)
                        {

                            Debug.Log("We clicked an enemy that should be attacked");
                            //selectedUnit.GetComponent<UnitScript>().setAttackAnimation();

                            StartCoroutine(batMan.attack(selectedUnit, unitClicked));

                            selectedUnit.GetComponent<Unit>().wait();

                            //Check if soemone has won
                            //gameMan.checkIfUnitsRemain();
                            StartCoroutine(deselectAfterMovements(selectedUnit, unitClicked));
                        }
                    }
                    else if (unitClicked.GetComponent<Unit>().teamNum == selectedUnit.GetComponent<Unit>().teamNum && healableTiles.Contains(graph[unitX, unitY]))
                    {
                        if (unitClicked.GetComponent<Unit>().currentHealthPoints > 0)
                        {
                            Debug.Log("We clicked the tile of an ally that should be supported");
                            //Debug.Log(selectedUnit.GetComponent<Unit>().currentHealthPoints);

                            StartCoroutine(batMan.attack(selectedUnit, unitClicked));

                            StartCoroutine(deselectAfterMovements(selectedUnit, unitClicked));
                        }
                    }
                }
            }
        }
    }

    public void waitOption()
    {
        disableHighlightUnitRange();

        selectedUnit.GetComponent<Unit>().wait();
        //selectedUnit.GetComponent<Unit>().setWaitIdleAnimation();
        selectedUnit.GetComponent<Unit>().unitMoveState = Unit.MovementStates.Wait;
        gameMan.DecrementUsedAP();
        deselectUnit();
    }

    public void attackOption()
    {
        gameMan.usedAP += GetCostOfUnitMenuOption(UIManager.UnitMenuButtons.ATTACK);
        uiMan.displayingInfoPanels = true;
        uiMan.HideMenu(uiMan.unitMenu);
        highlightTileset(getAttackableEnemiesFromPosition(), ClickableTileScript.FlashStates.Attack);
    }
    
    public void sunderOption()
    {
        gameMan.usedAP += GetCostOfUnitMenuOption(UIManager.UnitMenuButtons.SUNDER);

        Unit tempSelectedUnit = selectedUnit.GetComponent<Unit>();
        getTileAt(tempSelectedUnit.x, tempSelectedUnit.y).tileHP -= tempSelectedUnit.attackDamage;

        //spawn damage number
        GameObject dmgNum = Instantiate(uiMan.floatingText, tempSelectedUnit.transform.position + (Vector3.up * 0.4f), Quaternion.identity);
        dmgNum.transform.GetChild(0).GetComponent<TMPro.TextMeshPro>().text = tempSelectedUnit.attackDamage.ToString();
        dmgNum.transform.GetChild(0).GetComponent<TMPro.TextMeshPro>().color = Color.black;

        //shake camera
        CinemachineShake.instance.ShakeCamera(tempSelectedUnit.attackDamage, 0.2f);

        if (getTileAt(tempSelectedUnit.x, tempSelectedUnit.y).tileHP <= 0)
        {
            gameMan.declareWinner(tempSelectedUnit.teamNum);
        }
        waitOption();
    }

    public void fortifyOption()
    {
        gameMan.usedAP += GetCostOfUnitMenuOption(UIManager.UnitMenuButtons.FORTIFY);

        selectedUnit.GetComponent<Unit>().isFortified = true;
        waitOption();
    }

    public void healOption()
    {
        gameMan.usedAP += GetCostOfUnitMenuOption(UIManager.UnitMenuButtons.HEAL);

        uiMan.displayingInfoPanels = true;
        uiMan.HideMenu(uiMan.unitMenu);
        highlightTileset(getHealableUnitsFromPosition(), ClickableTileScript.FlashStates.Support);
    }

    public void refreshOption()
    {
        if (gameMan.tileBeingDisplayed.GetComponent<ClickableTileScript>().unitOnTile != null)
        {
            selectedUnit = gameMan.tileBeingDisplayed.GetComponent<ClickableTileScript>().unitOnTile;

            selectedUnit.GetComponent<Unit>().moveAgain();

            gameMan.usedAP += GetCostOfUnitMenuOption(UIManager.UnitMenuButtons.REFRESH);
            gameMan.DecrementUsedAP();

            selectedUnit = null;
            deselectUnit();
        }
    }

    //Desc: de-selects the unit
    public void deselectUnit()
    {
        uiMan.displayingInfoPanels = true;
        uiMan.HideMenu(uiMan.unitMenu);

        if (selectedUnit != null)
        {
            gameMan.usedAP = 0;
            if (selectedUnit.GetComponent<Unit>().unitMoveState == Unit.MovementStates.Selected)
            {
                disableHighlightUnitRange();
                //disableUnitUIRoute();
                selectedUnit.GetComponent<Unit>().unitMoveState = Unit.MovementStates.Unselected;

                selectedUnit = null;
                unitSelected = false;
            }
            else if (selectedUnit.GetComponent<Unit>().unitMoveState == Unit.MovementStates.Wait)
            {
                disableHighlightUnitRange();
                //disableUnitUIRoute();
                unitSelected = false;
                selectedUnit = null;
            }
            else
            {
                disableHighlightUnitRange();
                //disableUnitUIRoute();
                tilesOnMap[selectedUnit.GetComponent<Unit>().x, selectedUnit.GetComponent<Unit>().y].GetComponent<ClickableTileScript>().unitOnTile = null;
                tilesOnMap[unitSelectedPreviousX, unitSelectedPreviousY].GetComponent<ClickableTileScript>().unitOnTile = selectedUnit;

                selectedUnit.GetComponent<Unit>().x = unitSelectedPreviousX;
                selectedUnit.GetComponent<Unit>().y = unitSelectedPreviousY;
                selectedUnit.GetComponent<Unit>().tileBeingOccupied = previousOccupiedTile;
                selectedUnit.transform.position = tileCoordToWorldCoord(unitSelectedPreviousX, unitSelectedPreviousY);
                selectedUnit.GetComponent<Unit>().unitMoveState = Unit.MovementStates.Unselected;
                selectedUnit = null;
                unitSelected = false;
            }
        }
    }

    //Desc: deselects the selected unit after the action has been taken
    public IEnumerator deselectAfterMovements(GameObject unit, GameObject enemy)
    {
        gameMan.DecrementUsedAP();
        selectedUnit.GetComponent<Unit>().unitMoveState = Unit.MovementStates.Wait;
        selectedUnit.GetComponent<Unit>().SetColor(Color.gray);

        disableHighlightUnitRange();
        //disableUnitUIRoute();

        //If i dont have this wait for seconds the while loops get passed as the coroutine has not started from the other script
        //Adding a delay here to ensure that it all works smoothly. (probably not the best idea)
        yield return new WaitForSeconds(.25f);
        while (unit != null && unit.GetComponent<Unit>().combatQueue.Count > 0)
        {
            yield return new WaitForEndOfFrame();
        }
        if (enemy != null)
        {
            while (enemy.GetComponent<Unit>().combatQueue.Count > 0)
            {
                yield return new WaitForEndOfFrame();
            }
        }
        Debug.Log("All animations done playing");
        deselectUnit();
    }

    //Out: true if there is a tile that was clicked that the unit can move to, false otherwise 
    //Desc: checks if the tile that was clicked is move-able for the selected unit
    public bool selectTileToMoveTo()
    {
        RaycastHit2D hit;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        hit = Physics2D.GetRayIntersection(Camera.main.ScreenPointToRay(Input.mousePosition));

        if (hit)
        {

            if (hit.transform.gameObject.CompareTag("Tile"))
            {
                int clickedTileX = hit.transform.GetComponent<ClickableTileScript>().tileX;
                int clickedTileY = hit.transform.GetComponent<ClickableTileScript>().tileY;
                Node nodeToCheck = graph[clickedTileX, clickedTileY];

                if (selectedUnitMoveRange.Contains(nodeToCheck))
                {
                    if ((hit.transform.gameObject.GetComponent<ClickableTileScript>().unitOnTile == null || hit.transform.gameObject.GetComponent<ClickableTileScript>().unitOnTile == selectedUnit) && (selectedUnitMoveRange.Contains(nodeToCheck)))
                    {
                        generatePathTo(clickedTileX, clickedTileY);
                        return true;
                    }
                }
            }
            else if (hit.transform.gameObject.CompareTag("Unit"))
            {

                if (hit.transform.parent.GetComponent<Unit>().teamNum != selectedUnit.GetComponent<Unit>().teamNum)
                {
                    Debug.Log("Clicked an Enemy");
                }
                else if (hit.transform.parent.GetComponent<Unit>().teamNum == selectedUnit.GetComponent<Unit>().teamNum)
                {
                    Debug.Log("Clicked a Friend");
                }
                else if (hit.transform.parent.gameObject == selectedUnit)
                {

                    generatePathTo(selectedUnit.GetComponent<Unit>().x, selectedUnit.GetComponent<Unit>().y);
                    return true;
                }
            }
        }
        return false;
    }

    //Desc: generates the path for the selected unit
    //Think this one is also partially from Quill18Create's tutorial
    public void generatePathTo(int x, int y)
    {
        if (selectedUnit.GetComponent<Unit>().x == x && selectedUnit.GetComponent<Unit>().y == y)
        {
            Debug.Log("clicked the same tile that the unit is standing on");
            currentPath = new List<Node>();
            selectedUnit.GetComponent<Unit>().path = currentPath;
            return;
        }
        if (!unitCanEnterTile(selectedUnit.GetComponent<Unit>(), x, y))
        {
            //cant move into something so we can probably just return
            //cant set this endpoint as our goal
            return;
        }

        selectedUnit.GetComponent<Unit>().path = null;
        currentPath = null;
        //from wiki dijkstra's
        Dictionary<Node, float> dist = new Dictionary<Node, float>();
        Dictionary<Node, Node> prev = new Dictionary<Node, Node>();
        Node source = graph[selectedUnit.GetComponent<Unit>().x, selectedUnit.GetComponent<Unit>().y];
        Node target = graph[x, y];
        dist[source] = 0;
        prev[source] = null;
        //Unchecked nodes
        List<Node> unvisited = new List<Node>();

        //Initialize
        foreach (Node n in graph)
        {
            //Initialize to infite distance as we don't know the answer
            //Also some places are infinity
            if (n != source)
            {
                dist[n] = Mathf.Infinity;
                prev[n] = null;
            }
            unvisited.Add(n);
        }
        //if there is a node in the unvisited list lets check it
        while (unvisited.Count > 0)
        {
            //u will be the unvisited node with the shortest distance
            Node u = null;
            foreach (Node possibleU in unvisited)
            {
                if (u == null || dist[possibleU] < dist[u])
                {
                    u = possibleU;
                }
            }


            if (u == target)
            {
                break;
            }

            unvisited.Remove(u);

            foreach (Node n in u.neighbours)
            {

                //float alt = dist[u] + u.DistanceTo(n);
                float alt = dist[u] + costToEnterTile(n.x, n.y);
                if (alt < dist[n])
                {
                    dist[n] = alt;
                    prev[n] = u;
                }
            }
        }
        //if were here we found shortest path, or no path exists
        if (prev[target] == null)
        {
            //No route;
            return;
        }
        currentPath = new List<Node>();
        Node curr = target;
        //Step through the current path and add it to the chain
        while (curr != null)
        {
            currentPath.Add(curr);
            curr = prev[curr];
        }
        //Now currPath is from target to our source, we need to reverse it from source to target.
        currentPath.Reverse();

        selectedUnit.GetComponent<Unit>().path = currentPath;
    }

    //In:  finalMovement highlight, the attack range of the unit, and the initial node that the unit was standing on
    //Out: hashSet Node of the total attackable tiles for the unit
    //Desc: returns a set of nodes that represent the unit's total attackable tiles
    public HashSet<Node> getUnitTotalAttackableTiles(HashSet<Node> finalMovementHighlight, Vector2 attRange, Node unitInitialNode)
    {
        HashSet<Node> tempNeighbourHash = new HashSet<Node>();
        HashSet<Node> neighbourHash = new HashSet<Node>();
        HashSet<Node> seenNodes = new HashSet<Node>();
        HashSet<Node> totalAttackableTiles = new HashSet<Node>();

        if (attRange.x == 1 && attRange.x == attRange.y)
        {
            foreach (Node n in finalMovementHighlight)
            {
                neighbourHash = new HashSet<Node>();
                neighbourHash.Add(n);
                for (int i = 0; i < attRange.x; i++)
                {
                    foreach (Node t in neighbourHash)
                    {
                        foreach (Node tn in t.neighbours)
                        {
                            tempNeighbourHash.Add(tn);
                        }
                    }

                    neighbourHash = tempNeighbourHash;
                    tempNeighbourHash = new HashSet<Node>();
                    if (i < attRange.x - 1)
                    {
                        seenNodes.UnionWith(neighbourHash);
                    }
                }
                neighbourHash.ExceptWith(seenNodes);
                seenNodes = new HashSet<Node>();
                totalAttackableTiles.UnionWith(neighbourHash);
            }
        }
        else
        {
            neighbourHash.Add(unitInitialNode);
            seenNodes.Add(unitInitialNode);

            for (int i = 0; i < attRange.y; i++)
            {
                foreach (Node t in neighbourHash)
                {
                    foreach (Node tn in t.neighbours)
                    {
                        if (!seenNodes.Contains(tn))
                        {
                            tempNeighbourHash.Add(tn);
                        }
                    }
                }

                neighbourHash = tempNeighbourHash;
                tempNeighbourHash = new HashSet<Node>();
                if (i < attRange.x - 1)
                {
                    seenNodes.UnionWith(neighbourHash);
                }
                else
                {
                    totalAttackableTiles.UnionWith(neighbourHash);
                }
            }
            //neighbourHash.ExceptWith(seenNodes);
            //seenNodes = new HashSet<Node>();
            //totalAttackableTiles.UnionWith(neighbourHash);
        }

        totalAttackableTiles.Remove(unitInitialNode);
        return (totalAttackableTiles);
    }

    //Desc: returns a set of nodes that are all the attackable tiles from the units current position
    public HashSet<Node> getUnitAttackOptionsFromPosition()
    {
        HashSet<Node> tempNeighbourHash = new HashSet<Node>();
        HashSet<Node> neighbourHash = new HashSet<Node>();
        HashSet<Node> seenNodes = new HashSet<Node>();
        HashSet<Node> totalAttackableTiles = new HashSet<Node>();

        Node initialNode = graph[selectedUnit.GetComponent<Unit>().x, selectedUnit.GetComponent<Unit>().y];

        int attRangeMin = (int)selectedUnit.GetComponent<Unit>().attackRange.x;
        int attRangeMax = (int)selectedUnit.GetComponent<Unit>().attackRange.y;

        if (attRangeMin == attRangeMax)
        {
            neighbourHash.Add(initialNode);
            for (int i = 0; i < attRangeMin; i++)
            {
                foreach (Node t in neighbourHash)
                {
                    foreach (Node tn in t.neighbours)
                    {
                        tempNeighbourHash.Add(tn);
                    }
                }
                neighbourHash = tempNeighbourHash;
                tempNeighbourHash = new HashSet<Node>();
                if (i < attRangeMin - 1)
                {
                    seenNodes.UnionWith(neighbourHash);
                }
            }
            neighbourHash.ExceptWith(seenNodes);
            neighbourHash.Remove(initialNode);
            return neighbourHash;
        }
        else
        {
            neighbourHash.Add(initialNode);
            seenNodes.Add(initialNode);

            for (int i = 0; i < attRangeMax; i++)
            {
                foreach (Node t in neighbourHash)
                {
                    foreach (Node tn in t.neighbours)
                    {
                        if (!seenNodes.Contains(tn))
                        {
                            tempNeighbourHash.Add(tn);
                        }
                    }
                }

                neighbourHash = tempNeighbourHash;
                tempNeighbourHash = new HashSet<Node>();
                if (i < attRangeMin - 1)
                {
                    seenNodes.UnionWith(neighbourHash);
                }
                else
                {
                    totalAttackableTiles.UnionWith(neighbourHash);
                }
            }
        }
        totalAttackableTiles.Remove(initialNode);
        return (totalAttackableTiles);
    }

    public HashSet<Node> getAttackableEnemiesFromPosition()
    {
        HashSet<Node> tempNeighbourHash = new HashSet<Node>();
        HashSet<Node> neighbourHash = new HashSet<Node>();
        HashSet<Node> seenNodes = new HashSet<Node>();
        HashSet<Node> totalAttackableTiles = new HashSet<Node>();
        HashSet<Node> enemyPositions = new HashSet<Node>();

        Node initialNode = graph[selectedUnit.GetComponent<Unit>().x, selectedUnit.GetComponent<Unit>().y];

        if (selectedUnit.GetComponent<Unit>().attackDamage <= 0)
        {
            return enemyPositions;
        }

        int attRangeMin = (int)selectedUnit.GetComponent<Unit>().attackRange.x;
        int attRangeMax = (int)selectedUnit.GetComponent<Unit>().attackRange.y;

        if (attRangeMin == attRangeMax)
        {
            neighbourHash.Add(initialNode);
            for (int i = 0; i < attRangeMin; i++)
            {
                foreach (Node t in neighbourHash)
                {
                    foreach (Node tn in t.neighbours)
                    {
                        tempNeighbourHash.Add(tn);
                    }
                }
                neighbourHash = tempNeighbourHash;
                tempNeighbourHash = new HashSet<Node>();
                if (i < attRangeMin - 1)
                {
                    seenNodes.UnionWith(neighbourHash);
                }
            }
            neighbourHash.ExceptWith(seenNodes);
            totalAttackableTiles = neighbourHash;
        }
        else
        {
            neighbourHash.Add(initialNode);
            seenNodes.Add(initialNode);

            for (int i = 0; i < attRangeMax; i++)
            {
                foreach (Node t in neighbourHash)
                {
                    foreach (Node tn in t.neighbours)
                    {
                        if (!seenNodes.Contains(tn))
                        {
                            tempNeighbourHash.Add(tn);
                        }
                    }
                }

                neighbourHash = tempNeighbourHash;
                tempNeighbourHash = new HashSet<Node>();
                if (i < attRangeMin - 1)
                {
                    seenNodes.UnionWith(neighbourHash);
                }
                else
                {
                    totalAttackableTiles.UnionWith(neighbourHash);
                }
            }
        }
        totalAttackableTiles.Remove(initialNode);
        foreach (Node n in totalAttackableTiles)
        {
            if (tilesOnMap[n.x, n.y].GetComponent<ClickableTileScript>().unitOnTile != null)
            {
                if (tilesOnMap[n.x, n.y].GetComponent<ClickableTileScript>().unitOnTile.GetComponent<Unit>().teamNum != selectedUnit.GetComponent<Unit>().teamNum)
                {
                    enemyPositions.Add(n);
                }
            }
        }
        return (enemyPositions);
    }

    public HashSet<Node> getHealableUnitsFromPosition()
    {
        HashSet<Node> tempNeighbourHash = new HashSet<Node>();
        HashSet<Node> neighbourHash = new HashSet<Node>();
        HashSet<Node> seenNodes = new HashSet<Node>();
        HashSet<Node> totalHealableTiles = new HashSet<Node>();
        HashSet<Node> friendlyPositions = new HashSet<Node>();

        Node initialNode = graph[selectedUnit.GetComponent<Unit>().x, selectedUnit.GetComponent<Unit>().y];

        int healRangeMin = (int)selectedUnit.GetComponent<Unit>().attackRange.x;
        int healRangeMax = (int)selectedUnit.GetComponent<Unit>().attackRange.y;

        if (selectedUnit.GetComponent<Unit>().powerAmount <= 0)
        {
            return friendlyPositions;
        }

        neighbourHash.Add(initialNode);
        for (int i = 0; i < 1; i++)
        {
            foreach (Node t in neighbourHash)
            {
                foreach (Node tn in t.neighbours)
                {
                    tempNeighbourHash.Add(tn);
                }
            }
            neighbourHash = tempNeighbourHash;
            tempNeighbourHash = new HashSet<Node>();
            if (i < 1 - 1)
            {
                seenNodes.UnionWith(neighbourHash);
            }
        }
        neighbourHash.ExceptWith(seenNodes);
        totalHealableTiles = neighbourHash;

        /*
        if (healRangeMin == healRangeMax)
        {
            neighbourHash.Add(initialNode);
            for (int i = 0; i < healRangeMin; i++)
            {
                foreach (Node t in neighbourHash)
                {
                    foreach (Node tn in t.neighbours)
                    {
                        tempNeighbourHash.Add(tn);
                    }
                }
                neighbourHash = tempNeighbourHash;
                tempNeighbourHash = new HashSet<Node>();
                if (i < healRangeMin - 1)
                {
                    seenNodes.UnionWith(neighbourHash);
                }
            }
            neighbourHash.ExceptWith(seenNodes);
            totalHealableTiles = neighbourHash;
        }
        else
        {
            neighbourHash.Add(initialNode);
            seenNodes.Add(initialNode);

            for (int i = 0; i < healRangeMax; i++)
            {
                foreach (Node t in neighbourHash)
                {
                    foreach (Node tn in t.neighbours)
                    {
                        if (!seenNodes.Contains(tn))
                        {
                            tempNeighbourHash.Add(tn);
                        }
                    }
                }

                neighbourHash = tempNeighbourHash;
                tempNeighbourHash = new HashSet<Node>();
                if (i < healRangeMin - 1)
                {
                    seenNodes.UnionWith(neighbourHash);
                }
                else
                {
                    totalHealableTiles.UnionWith(neighbourHash);
                }
            }
        }
        */
        totalHealableTiles.Remove(initialNode);
        foreach (Node n in totalHealableTiles)
        {
            if (tilesOnMap[n.x, n.y].GetComponent<ClickableTileScript>().unitOnTile != null)
            {
                if (tilesOnMap[n.x, n.y].GetComponent<ClickableTileScript>().unitOnTile.GetComponent<Unit>().teamNum == selectedUnit.GetComponent<Unit>().teamNum)
                {
                    friendlyPositions.Add(n);
                }
            }
        }
        return (friendlyPositions);
    }

    private int GetCostOfUnitMenuOption(UIManager.UnitMenuButtons opt)
    {
        foreach(UIManager.UnitMenuOption btn in activeUnitMenuButtons)
        {
            if((int)opt == (int)btn.option)
            {
                return btn.cost;
            }
        }
        Debug.Log("No active button of type " + opt.ToString());
        return 0;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]

public class Unit : MonoBehaviour
{
    public enum UnitClasses
    {
        Attacker,
        Defender,
        Supporter,
        Sneaker,
        Classless
    }
    public enum MovementTypes
    {
        Walk,
        Crawl,
        Fly,
        Swim
    }
    public enum MovementStates
    {
        Unselected,
        Selected,
        Moved,
        Wait
    }

    public GameObject unitVisualPrefab;

    public int teamNum;
    public int x;
    public int y;

    public Queue<int> movementQueue;
    public Queue<int> combatQueue;
    //This global variable is used to increase the units movementSpeed when travelling on the board
    //TO DO: make this a Map Manager variable, since this is the same for all units
    public static float visualMovementSpeed = 0.08f;

    //Animator
    public Animator animator;

    //Other script objects
    public MapManager mapMan;

    //UnitStats
    [Header("Unit Stats")]
    public string unitName;
    public UnitClasses unitClass;
    public int moveSpeed;
    public List<MovementTypes> moveOptions;
    public Vector2 attackRange; // (min, max)
    public int attackDamage = 1;
    public float crackbackModifier = 1.0f; //affects damage of retaliatory strikes
    public int maxHealthPoints;
    public int currentHealthPoints;
    public float baseChanceToCrit;
    public float chanceToCrit;
    public float baseCritAvoid;
    public float critAvoid;
    public Sprite unitSprite;

    public bool isFortified = false;
    public int fortifyAmount;

    public int powerAmount;

    [Header("Pathfinding")]

    public GameObject tileBeingOccupied;

    //Location for positional update
    public Transform startPoint;
    public Transform endPoint;
    //public static float moveSpeedTime;

    //3D Model or 2D Sprite variables to check which version to use
    //Make sure only one of them are enabled in the inspector
    //public GameObject holder3D;
    public GameObject holder2D;
    // Total distance between the markers.
    private float journeyLength;

    //Boolean to startTravelling
    public bool unitInMovement;
    public MovementStates unitMoveState;

    //Pathfinding

    public List<Node> path = null;

    //Path for moving unit's transform
    public List<Node> pathForMovement = null;
    public bool completedMovement = false;

    [Header("Other Stuff")]
    public GameObject floatingText;

    private void Awake()
    {

        animator = holder2D.GetComponent<Animator>();
        //moveOptions = new List<MovementTypes>();
        movementQueue = new Queue<int>();
        combatQueue = new Queue<int>();

        x = (int)transform.position.x;
        y = (int)transform.position.z;
        unitMoveState = MovementStates.Unselected;

        currentHealthPoints = maxHealthPoints;
        chanceToCrit = baseChanceToCrit;
        critAvoid = baseCritAvoid;
    }

    private void Start()
    {
        SetColor(GetTeamColor());
    }

    public void Update()
    {
        switch (unitMoveState)
        {
            case MovementStates.Selected:
            case MovementStates.Moved: 
                animator.SetTrigger("Running");
                break;
            case MovementStates.Unselected:
                animator.SetTrigger("Idling");
                break;
            default:
                animator.SetTrigger("Idling");
                break;
        }
    }

    public void MoveNextTile()
    {
        if (path.Count == 0)
        {
            return;
        }
        else
        {
            StartCoroutine(moveOverSeconds(transform.gameObject, path[path.Count - 1]));
        }
    }

    public void moveAgain()
    {
        path = null;
        unitMoveState = MovementStates.Unselected;
        completedMovement = false;
        SetColor(GetTeamColor());

        isFortified = false;
    }

    public void takeDamage(int amt, bool isCrit)
    {
        //reduce incoming damage by tile defense modifier
        //Unless the unit is flying
        if (!moveOptions.Contains(Unit.MovementTypes.Fly))
        {
            amt -= mapMan.defenseOfTile(x, y);
        }
        //Also reduce the damage if the unit is fortified
        if (isFortified)
        {
            amt -= fortifyAmount;
        }
        amt = (int)Mathf.Clamp(amt, 0, Mathf.Infinity);

        //reduce hp by damage amount
        currentHealthPoints = currentHealthPoints - amt;

        //spawn damage numbers
        GameObject dmgNum = Instantiate(floatingText, transform.position + (Vector3.up * 0.4f), Quaternion.identity);
        dmgNum.transform.GetChild(0).GetComponent<TMPro.TextMeshPro>().text = amt.ToString();
        if (isCrit)
        {
            dmgNum.transform.GetChild(0).GetComponent<TMPro.TextMeshPro>().color = Color.red;
        }
        //shake screen
        CinemachineShake.instance.ShakeCamera(amt, 0.2f);
    }

    public void heal(int amt, bool isCrit)
    {
        amt = (int)Mathf.Clamp(amt, 0, Mathf.Infinity);
        if (currentHealthPoints + amt > maxHealthPoints)
        {
            amt = maxHealthPoints - currentHealthPoints;
        }
        currentHealthPoints = (int)Mathf.Clamp(currentHealthPoints + amt, 0, maxHealthPoints);
        GameObject dmgNum = Instantiate(floatingText, transform.position + (Vector3.up * 0.4f), Quaternion.identity);
        dmgNum.transform.GetChild(0).GetComponent<TMPro.TextMeshPro>().text = amt.ToString();
        dmgNum.transform.GetChild(0).GetComponent<TMPro.TextMeshPro>().color = new Color(0,100,0);
    }

    public IEnumerator moveOverSeconds(GameObject objectToMove, Node endNode)
    {
        movementQueue.Enqueue(1);

        //remove first thing on path because, its the tile we are standing on
        path.RemoveAt(0);

        while (path.Count != 0)
        {
            Vector3 endPos = mapMan.tileCoordToWorldCoord(path[0].x, path[0].y);
            objectToMove.transform.position = Vector3.Lerp(transform.position, endPos, visualMovementSpeed);
            if ((transform.position - endPos).sqrMagnitude < 0.001)
            {
                path.RemoveAt(0);
            }
            yield return new WaitForEndOfFrame();
        }
        //visualMovementSpeed = 0.15f;
        transform.position = mapMan.tileCoordToWorldCoord(endNode.x, endNode.y);

        x = endNode.x;
        y = endNode.y;
        tileBeingOccupied.GetComponent<ClickableTileScript>().unitOnTile = null;
        tileBeingOccupied = mapMan.tilesOnMap[x, y];
        movementQueue.Dequeue();
    }

    public void wait()
    {
        unitMoveState = MovementStates.Moved;
        gameObject.GetComponentInChildren<SpriteRenderer>().color = Color.gray;
    }

    public void unitDie()
    {
        if (holder2D.activeSelf)
        {
            Instantiate(UIManager.explosionEffectStatic, transform.position, Quaternion.identity);

            mapMan.deselectUnit();
            mapMan.cleanseNeighbouringTiles(tileBeingOccupied);
            StartCoroutine(checkIfRoutinesRunning());
        }
    }

    public IEnumerator checkIfRoutinesRunning()
    {
        while (combatQueue.Count > 0)
        {
            yield return new WaitForEndOfFrame();
        }
        Destroy(gameObject);
    }

    public bool checkIfDead()
    {
        return currentHealthPoints <= 0;
    }

    public Color GetTeamColor()
    {
        Color c = Color.white;

        if(teamNum == 0)
        {
            c = Color.red;
        }else if(teamNum == 1)
        {
            c = Color.blue;
        }
        return c;
    }
    public void SetColor(Color c)
    {
        gameObject.GetComponentInChildren<SpriteRenderer>().color = c;
    }
}

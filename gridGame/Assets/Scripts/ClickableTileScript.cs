using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickableTileScript : MonoBehaviour    
{
    public Sprite[] sprites;

    [Header("Tile Flashing")]

    public Material material;
    public Color tintColor;
    private static Color moveColor, attackColor, supportColor;
    private float flashSpeed = 1f;
    private static float flashMinimum = 0.2f;
    private static float flashMaximum = 0.5f;
    public bool isFlashing = false;
    public float currentGoal;

    public enum FlashStates
    {
        None,
        Move,
        Attack,
        Support
    }
    public FlashStates currentFlashState;

    [Header("Game Stuff")]

    //The x and y co-ordinate of the tile
    public int tileX;
    public int tileY;
    //The unit on the tile
    public GameObject unitOnTile;
    //Team the tile belongs to, if any
    public GameObject belongsToTeam;

    public MapManager map;

    private void Awake()
    {
        SetMaterial(GetComponent<SpriteRenderer>().material);
        moveColor = new Color(0.1f, 0.1f, 0.4f, 0);
        attackColor = new Color(0.6f, 0.0f, 0.05f, 0);
        supportColor = new Color(0.0f, 0.45f, 0.0f, 0);

        SetColor(moveColor);
        currentFlashState = FlashStates.None;

        if (flashMaximum < flashMinimum)
        {
            flashMaximum = flashMinimum;
        }
        if (flashMinimum > flashMaximum)
        {
            flashMinimum = flashMaximum;
        }
    }

    public void Update()
    {
        if (isFlashing)
        {
            tintColor.a = Mathf.MoveTowards(tintColor.a, currentGoal, Time.deltaTime * flashSpeed);
            SetColor(tintColor);

            if (Mathf.Approximately(tintColor.a, currentGoal))
            {
                if (currentGoal == flashMaximum)
                {
                    currentGoal = flashMinimum;
                }
                else
                {
                    currentGoal = flashMaximum;
                }
            }
        }
    }

    private void OnMouseOver()
    {
        //Debug.Log(tileX + ", " + tileY);
    }

    public void ToggleFlash(){
        isFlashing = !isFlashing;
        if (isFlashing)
        {
            tintColor.a = flashMinimum;
            currentGoal = flashMaximum;
        }
        else
        {
            tintColor.a = 0;
            SetColor(tintColor);
        }
    }

    public void ToggleFlash(FlashStates fs)
    {
        switch (fs)
        {
            case FlashStates.Move:
                tintColor = moveColor;
                break;
            case FlashStates.Attack:
                tintColor = attackColor;
                break;
            case FlashStates.Support:
                tintColor = supportColor;
                break;
            default:
                tintColor = moveColor;
                break;
        }
        isFlashing = !isFlashing;
        if (isFlashing)
        {
            tintColor.a = flashMinimum;
            currentGoal = flashMaximum;
            currentFlashState = fs;
        }
        else
        {
            tintColor.a = 0;
            SetColor(tintColor);
            currentFlashState = FlashStates.None;
        }
    }

    public void StartFlash(FlashStates fs)
    {
        switch (fs)
        {
            case FlashStates.Move:
                tintColor = moveColor;
                break;
            case FlashStates.Attack:
                tintColor = attackColor;
                break;
            case FlashStates.Support:
                tintColor = supportColor;
                break;
            default:
                tintColor = moveColor;
                break;
        }
        isFlashing = true;
        currentFlashState = fs;

        tintColor.a = flashMinimum;
        currentGoal = flashMaximum;
    }

    public void SetMaterial(Material mat)
    {
        this.material = mat;
    }

    public void SetColor(Color col)
    {
        tintColor = col;
        material.SetColor("_Tint", tintColor);
    }
    
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Tile
{
    public string name;
    public GameObject tileVisualPrefab;
    public float movementCost = 1;
    public int defenseModifier = 0;
    public int tileHP = 10;
    public List<Unit.MovementTypes> movesAllowed;

    public Sprite mapSprite;

}
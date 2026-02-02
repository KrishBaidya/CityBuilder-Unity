using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "City/Building")]
public class BuildingData : ScriptableObject
{
    public string id;
    public TileBase tile;
    public int populationChange;
    public int powerChange;
    public int moneyChange;
}
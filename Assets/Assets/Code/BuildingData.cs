using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "City/Building")]
public class BuildingData : ScriptableObject
{
    [Header("Building Info")]
    public string id;
    public TileBase tile;
    
    [Header("Costs")]
    public int cost = 100;
    
    [Header("Effects")]
    public int populationChange = 0;
    public int powerChange = 0;
    public int moneyChange = 0;
    public int incomeChange = 0;
}
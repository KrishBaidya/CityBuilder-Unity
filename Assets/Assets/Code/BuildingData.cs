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

    [Header("Road Requirements")]
    public bool requiresRoadAccess = false; // Does this building need a road nearby?
    public int roadBonusIncome = 0; // Bonus income if connected to road
    public int roadBonusPopulation = 0; // Bonus population if connected to road
}
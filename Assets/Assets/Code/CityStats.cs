using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CityStats : MonoBehaviour
{
    public int population = 0;
    public int power = 0;
    public int money = 0;

    public void AddBuilding(BuildingData buildingData)
    {
        population += buildingData.populationChange;
        power += buildingData.powerChange;
        money += buildingData.moneyChange;

        Debug.Log($"City Stats Updated - Population: {population}, Power: {power}, Money: {money}");
    }
    
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CityStats : MonoBehaviour
{
    [Header("City Resources")]
    public int population = 0;
    public int power = 0;
    public int money = 1000; // Start with some money
    public int income = 0;

    [Header("Building Counts")]
    public int houseCount = 0;
    public int roadCount = 0;
    public int powerPlantCount = 0;
    public int economicCount = 0;

    public void AddBuilding(BuildingData buildingData)
    {
        population += buildingData.populationChange;
        power += buildingData.powerChange;
        money += buildingData.moneyChange;
        income += buildingData.incomeChange;

        UpdateBuildingCount(buildingData.id, 1);

        Debug.Log($"✅ Added {buildingData.id} - Pop: {population}, Power: {power}, Money: ${money}, Income: ${income}/turn");
    }

    public void RemoveBuilding(BuildingData buildingData)
    {
        population -= buildingData.populationChange;
        power -= buildingData.powerChange;
        money -= buildingData.moneyChange;
        income -= buildingData.incomeChange;

        UpdateBuildingCount(buildingData.id, -1);

        Debug.Log($"❌ Removed {buildingData.id} - Pop: {population}, Power: {power}, Money: ${money}, Income: ${income}/turn");
    }

    void UpdateBuildingCount(string id, int change)
    {
        switch (id)
        {
            case "House":
                houseCount += change;
                break;
            case "Road":
                roadCount += change;
                break;
            case "PowerPlant":
                powerPlantCount += change;
                break;
            case "Economic":
                economicCount += change;
                break;
        }
    }

    public void ProcessTurn()
    {
        money += income;
        Debug.Log($"⏱️ Turn processed! Money: ${money} (+${income})");
    }

    public bool HasEnoughMoney(int amount)
    {
        return money >= amount;
    }

    public bool HasPowerDeficit()
    {
        return power < 0;
    }
}
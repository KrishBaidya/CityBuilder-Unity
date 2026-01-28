using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BuildingPlacer : MonoBehaviour
{
    public Tilemap groundMap;
    public Tilemap buildingMap;
    
    public BuildingData buildingData; // Current selected building
    public List<BuildingData> availableBuildings; // All buildings
    public CityStats cityStats;

    void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cell = groundMap.WorldToCell(mouseWorld);
        cell.z = 0;

        PlaceBuildingAt(cell, buildingData);
    }

    public void PlaceBuildingAt(Vector3Int cell, BuildingData building)
    {
        if (!groundMap.HasTile(cell))
        {
            Debug.LogWarning("No ground tile at position");
            return;
        }

        if (buildingMap.HasTile(cell))
        {
            Debug.LogWarning("Building already exists at position");
            return;
        }

        buildingMap.SetTile(cell, building.tile);
        cityStats.AddBuilding(building);
        
        Debug.Log($"Placed {building.id} at {cell}");
    }
    
    public BuildingData GetBuildingById(string id)
    {
        foreach (var building in availableBuildings)
        {
            if (building.id == id)
                return building;
        }
        return null;
    }
}
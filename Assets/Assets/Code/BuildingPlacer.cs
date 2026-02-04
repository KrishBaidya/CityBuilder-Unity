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
        // Left click - place building
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int cell = groundMap.WorldToCell(mouseWorld);
            cell.z = 0;

            PlaceBuildingAt(cell, buildingData);
            return;
        }

        // Right click - remove building
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int cell = groundMap.WorldToCell(mouseWorld);
            cell.z = 0;

            RemoveBuildingAt(cell);
            return;
        }
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
        
        // Check if player has enough money
        if (cityStats.money < building.cost)
        {
            Debug.LogWarning($"Not enough money! Need ${building.cost}, have ${cityStats.money}");
            return;
        }

        buildingMap.SetTile(cell, building.tile);
        cityStats.AddBuilding(building);
        cityStats.money -= building.cost; // Deduct cost

        Debug.Log($"✅ Placed {building.id} at {cell} for ${building.cost}");
    }

    public void RemoveBuildingAt(Vector3Int cell)
    {
        if (!groundMap.HasTile(cell))
        {
            Debug.LogWarning("No ground tile at position");
            return;
        }

        if (!buildingMap.HasTile(cell))
        {
            Debug.LogWarning("No building exists at position");
            return;
        }

        // Get the building data before removing
        var tile = buildingMap.GetTile(cell);
        BuildingData building = null;

        foreach (var b in availableBuildings)
        {
            if (b.tile == tile)
            {
                building = b;
                break;
            }
        }

        // Remove the tile
        buildingMap.SetTile(cell, null);

        // Update stats if we found the building data
        if (building != null)
        {
            cityStats.RemoveBuilding(building);
            
            // Refund 50% of cost
            int refund = building.cost / 2;
            cityStats.money += refund;
            
            Debug.Log($"💥 Removed {building.id} at {cell}, refunded ${refund}");
        }
        else
        {
            Debug.Log($"💥 Removed unknown building at {cell}");
        }
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
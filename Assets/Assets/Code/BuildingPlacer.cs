using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class BuildingPlacer : MonoBehaviour
{
    public Tilemap groundMap;
    public Tilemap buildingMap;

    public BuildingData buildingData;
    public List<BuildingData> availableBuildings;
    public CityStats cityStats;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int cell = groundMap.WorldToCell(mouseWorld);
            cell.z = 0;

            PlaceBuildingAt(cell, buildingData);
            return;
        }

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

        if (cityStats.money < building.cost)
        {
            Debug.LogWarning($"Not enough money! Need ${building.cost}, have ${cityStats.money}");
            return;
        }

        // Check road requirement
        if (building.requiresRoadAccess && !HasRoadAccess(cell))
        {
            Debug.LogWarning($"{building.id} requires road access! Build a road nearby first.");
            return;
        }

        // Place building
        buildingMap.SetTile(cell, building.tile);
        cityStats.money -= building.cost;
        
        // Add base stats
        cityStats.AddBuilding(building);
        
        // Apply road bonuses if connected
        if (building.id != "road" && HasRoadAccess(cell))
        {
            ApplyRoadBonus(building, cell);
        }

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

        buildingMap.SetTile(cell, null);

        if (building != null)
        {
            cityStats.RemoveBuilding(building);
            
            // Remove road bonus if it had one
            if (HasRoadAccess(cell))
            {
                RemoveRoadBonus(building, cell);
            }
            
            int refund = building.cost / 2;
            cityStats.money += refund;
            
            Debug.Log($"💥 Removed {building.id} at {cell}, refunded ${refund}");
        }
    }

    // Check if a position has road access (road in adjacent tiles)
    public bool HasRoadAccess(Vector3Int position)
    {
        // Check all 4 adjacent tiles (up, down, left, right)
        Vector3Int[] adjacentTiles = new Vector3Int[]
        {
            position + Vector3Int.up,
            position + Vector3Int.down,
            position + Vector3Int.left,
            position + Vector3Int.right
        };

        foreach (var adjacentPos in adjacentTiles)
        {
            if (buildingMap.HasTile(adjacentPos))
            {
                var tile = buildingMap.GetTile(adjacentPos);
                
                // Check if this tile is a road
                foreach (var b in availableBuildings)
                {
                    if (b.tile == tile && b.id == "road")
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    void ApplyRoadBonus(BuildingData building, Vector3Int position)
    {
        if (building.roadBonusIncome > 0)
        {
            cityStats.income += building.roadBonusIncome;
            Debug.Log($"🛣️ Road bonus! +${building.roadBonusIncome} income for {building.id}");
        }

        if (building.roadBonusPopulation > 0)
        {
            cityStats.population += building.roadBonusPopulation;
            Debug.Log($"🛣️ Road bonus! +{building.roadBonusPopulation} population for {building.id}");
        }
    }

    void RemoveRoadBonus(BuildingData building, Vector3Int position)
    {
        if (building.roadBonusIncome > 0)
        {
            cityStats.income -= building.roadBonusIncome;
        }

        if (building.roadBonusPopulation > 0)
        {
            cityStats.population -= building.roadBonusPopulation;
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
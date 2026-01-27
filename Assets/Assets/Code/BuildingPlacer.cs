using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BuildingPlacer : MonoBehaviour
{
    public Tilemap groundMap;
    public Tilemap buildingMap;
    public TileBase buildingTile;

    void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cell = groundMap.WorldToCell(mouseWorld);

        cell.z = 0; // Ensure building is placed above other tiles

        Debug.Log("Placing building at: " + cell);

        // Only place if ground exists and no building is there
        if (!groundMap.HasTile(cell))
            return;

        if (buildingMap.HasTile(cell))
            return;

        buildingMap.SetTile(cell, buildingTile);
    }
}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class InputManager : MonoBehaviour
{
    public Tilemap groundMap;
    public Tilemap selectionMap;
    public TileBase highlightTile;

    Vector3Int lastCell;

    void Update()
    {
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cell = groundMap.WorldToCell(mouseWorld);

        if (cell == lastCell)
            return;

        selectionMap.ClearAllTiles();
        selectionMap.SetTile(cell, highlightTile);

        lastCell = cell;
    }
}

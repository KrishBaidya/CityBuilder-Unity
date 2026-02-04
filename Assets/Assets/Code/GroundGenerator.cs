using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GroundGenerator : MonoBehaviour
{
    [Header("References")]
    public Tilemap groundMap;
    public TileBase grassTile;
    
    [Header("Map Settings")]
    public int mapWidth = 50;
    public int mapHeight = 50;
    public Vector2Int mapCenter = new Vector2Int(25, 25);
    
    [Header("Generation")]
    public bool generateOnStart = true;
    
    void Start()
    {
        if (generateOnStart)
        {
            GenerateGround();
        }
    }

    [ContextMenu("Generate Ground")]
    public void GenerateGround()
    {
        if (groundMap == null || grassTile == null)
        {
            Debug.LogError("GroundMap or GrassTile not assigned!");
            return;
        }

        // Clear existing tiles
        groundMap.ClearAllTiles();

        // Generate centered grid
        int startX = mapCenter.x - (mapWidth / 2);
        int startY = mapCenter.y - (mapHeight / 2);

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                Vector3Int tilePos = new Vector3Int(startX + x, startY + y, 0);
                groundMap.SetTile(tilePos, grassTile);
            }
        }

        Debug.Log($"✅ Generated {mapWidth}x{mapHeight} ground tiles centered at ({mapCenter.x}, {mapCenter.y})");
        Debug.Log($"Map bounds: X({startX} to {startX + mapWidth}) Y({startY} to {startY + mapHeight})");
    }

    // Get map info for AI
    public MapInfo GetMapInfo()
    {
        int startX = mapCenter.x - (mapWidth / 2);
        int startY = mapCenter.y - (mapHeight / 2);
        
        return new MapInfo
        {
            width = mapWidth,
            height = mapHeight,
            minX = startX,
            maxX = startX + mapWidth - 1,
            minY = startY,
            maxY = startY + mapHeight - 1,
            centerX = mapCenter.x,
            centerY = mapCenter.y
        };
    }

    public bool IsValidPosition(int x, int y)
    {
        int startX = mapCenter.x - (mapWidth / 2);
        int startY = mapCenter.y - (mapHeight / 2);
        
        return x >= startX && x < startX + mapWidth &&
               y >= startY && y < startY + mapHeight;
    }
}

[System.Serializable]
public class MapInfo
{
    public int width;
    public int height;
    public int minX;
    public int maxX;
    public int minY;
    public int maxY;
    public int centerX;
    public int centerY;
}
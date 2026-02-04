using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Collections.Generic;

public class AutomationServer : MonoBehaviour
{
    public BuildingPlacer buildingPlacer;
    public CityStats cityStats;
    public GroundGenerator groundGenerator;
    public CameraController cameraController;

    TcpListener listener;
    Thread serverThread;
    
    // Single command processing at a time
    GameCommand currentCommand = null;
    string currentResponse = null;
    readonly object commandLock = new();
    bool commandReady = false;
    bool responseReady = false;
    bool isRunning = false;

    void Start()
    {
        // Validate references
        Debug.Log("🔍 Validating references...");
        if (buildingPlacer == null) Debug.LogError("❌ BuildingPlacer not assigned!");
        if (cityStats == null) Debug.LogError("❌ CityStats not assigned!");
        if (groundGenerator == null) Debug.LogWarning("⚠️ GroundGenerator not assigned");
        
        isRunning = true;
        serverThread = new Thread(ServerLoop);
        serverThread.IsBackground = true;
        serverThread.Start();
        
        Debug.Log("🎮 AutomationServer started!");
    }

    void Update()
    {
        // Process one command at a time on main thread
        lock (commandLock)
        {
            if (commandReady && currentCommand != null)
            {
                Debug.Log($"⚙️ Processing: {currentCommand.action}");
                
                try
                {
                    currentResponse = ExecuteCommand(currentCommand);
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ Error executing command: {e.Message}\n{e.StackTrace}");
                    currentResponse = $"{{\"status\":\"error\",\"message\":\"Exception: {e.Message}\"}}";
                }
                
                commandReady = false;
                responseReady = true;
                
                Debug.Log($"✅ Response ready: {currentResponse.Substring(0, Math.Min(100, currentResponse.Length))}");
            }
        }
    }

    void ServerLoop()
    {
        try
        {
            listener = new TcpListener(IPAddress.Any, 5050);
            listener.Start();
            Debug.Log("🌐 Server listening on 0.0.0.0:5050");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to start server: {e.Message}");
            return;
        }

        while (isRunning)
        {
            TcpClient client = null;
            NetworkStream stream = null;
            
            try
            {
                // Accept connection
                client = listener.AcceptTcpClient();
                stream = client.GetStream();
                Debug.Log("🔌 Client connected");

                // Read request
                byte[] buffer = new byte[4096];
                int length = stream.Read(buffer, 0, buffer.Length);
                string message = Encoding.UTF8.GetString(buffer, 0, length);
                Debug.Log($"📨 Received: {message}");

                // Parse command
                GameCommand command;
                try
                {
                    command = JsonUtility.FromJson<GameCommand>(message);
                    Debug.Log($"✅ Parsed: {command.action}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ JSON parse error: {e.Message}");
                    SendResponse(stream, "{\"status\":\"error\",\"message\":\"Invalid JSON\"}");
                    continue;
                }

                // Send command to Unity main thread
                lock (commandLock)
                {
                    currentCommand = command;
                    currentResponse = null;
                    commandReady = true;
                    responseReady = false;
                }

                // Wait for Unity to process (max 5 seconds)
                int waited = 0;
                while (waited < 5000) // 5 seconds
                {
                    lock (commandLock)
                    {
                        if (responseReady && currentResponse != null)
                        {
                            Debug.Log($"📤 Sending response");
                            SendResponse(stream, currentResponse);
                            
                            // Reset
                            currentCommand = null;
                            currentResponse = null;
                            responseReady = false;
                            break;
                        }
                    }
                    
                    Thread.Sleep(10);
                    waited++;
                }

                // Timeout
                if (waited >= 5000)
                {
                    Debug.LogError("⏱️ TIMEOUT waiting for Unity!");
                    SendResponse(stream, "{\"status\":\"error\",\"message\":\"Unity timeout\"}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Server error: {e.Message}");
            }
            finally
            {
                stream?.Close();
                client?.Close();
                Debug.Log("✅ Connection closed");
            }
        }
    }

    void SendResponse(NetworkStream stream, string response)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(response);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
    }

    string ExecuteCommand(GameCommand cmd)
    {
        switch (cmd.action)
        {
            case "place_building":
                return PlaceBuilding(cmd);
            case "demolish":
                return DemolishBuilding(cmd);
            case "upgrade":
                return UpgradeBuilding(cmd);
            case "get_stats":
                return GetStats();
            case "get_map":
                return GetMapInfo();
            case "get_buildings_data":
                return GetBuildingsData();
            case "focus_position":
                return FocusPosition(cmd);
            default:
                return $"{{\"status\":\"error\",\"message\":\"Unknown: {cmd.action}\"}}";
        }
    }

    string PlaceBuilding(GameCommand cmd)
    {
        if (buildingPlacer == null)
            return "{\"status\":\"error\",\"message\":\"BuildingPlacer null\"}";

        if (groundGenerator != null && !groundGenerator.IsValidPosition(cmd.x, cmd.y))
        {
            MapInfo info = groundGenerator.GetMapInfo();
            return $"{{\"status\":\"error\",\"message\":\"Out of bounds. Valid X({info.minX} to {info.maxX}) Y({info.minY} to {info.maxY})\"}}";
        }

        Vector3Int cell = new(cmd.x, cmd.y, 0);

        if (!buildingPlacer.groundMap.HasTile(cell))
            return $"{{\"status\":\"error\",\"message\":\"No ground at ({cmd.x},{cmd.y})\"}}";

        if (buildingPlacer.buildingMap.HasTile(cell))
            return $"{{\"status\":\"error\",\"message\":\"Building exists\"}}";

        BuildingData building = buildingPlacer.GetBuildingById(cmd.buildingType);
        if (building == null)
            return $"{{\"status\":\"error\",\"message\":\"Building '{cmd.buildingType}' not found\"}}";

        if (cityStats.money < building.cost)
            return $"{{\"status\":\"error\",\"message\":\"Not enough money. Need ${building.cost}\"}}";

        buildingPlacer.buildingMap.SetTile(cell, building.tile);
        cityStats.AddBuilding(building);
        cityStats.money -= building.cost;

        return $"{{\"status\":\"success\",\"message\":\"Placed {cmd.buildingType} at ({cmd.x},{cmd.y})\",\"cost\":{building.cost}}}";
    }

    string DemolishBuilding(GameCommand cmd)
    {
        Vector3Int cell = new(cmd.x, cmd.y, 0);

        if (!buildingPlacer.buildingMap.HasTile(cell))
            return $"{{\"status\":\"error\",\"message\":\"No building\"}}";

        var tile = buildingPlacer.buildingMap.GetTile(cell);
        BuildingData building = null;

        foreach (var b in buildingPlacer.availableBuildings)
        {
            if (b.tile == tile)
            {
                building = b;
                break;
            }
        }

        buildingPlacer.buildingMap.SetTile(cell, null);

        if (building != null)
        {
            cityStats.RemoveBuilding(building);
            int refund = building.cost / 2;
            cityStats.money += refund;
            return $"{{\"status\":\"success\",\"message\":\"Demolished\",\"refund\":{refund}}}";
        }

        return "{\"status\":\"success\",\"message\":\"Demolished\"}";
    }

    string UpgradeBuilding(GameCommand cmd)
    {
        Vector3Int cell = new(cmd.x, cmd.y, 0);

        if (!buildingPlacer.buildingMap.HasTile(cell))
            return "{\"status\":\"error\",\"message\":\"No building\"}";

        int cost = 100 * cmd.Upgrade;

        if (cityStats.money < cost)
            return "{\"status\":\"error\",\"message\":\"Not enough money\"}";

        cityStats.money -= cost;
        cityStats.power += 10 * cmd.Upgrade;
        cityStats.income += 5 * cmd.Upgrade;

        return $"{{\"status\":\"success\",\"message\":\"Upgraded\",\"cost\":{cost}}}";
    }

    string GetStats()
    {
        if (cityStats == null)
            return "{\"status\":\"error\",\"message\":\"CityStats null\"}";

        return $"{{\"status\":\"success\",\"population\":{cityStats.population},\"power\":{cityStats.power},\"money\":{cityStats.money},\"income\":{cityStats.income}}}";
    }

    string GetMapInfo()
    {
        if (groundGenerator == null)
            return "{\"status\":\"error\",\"message\":\"GroundGenerator null\"}";

        MapInfo info = groundGenerator.GetMapInfo();
        return $"{{\"status\":\"success\",\"width\":{info.width},\"height\":{info.height},\"minX\":{info.minX},\"maxX\":{info.maxX},\"minY\":{info.minY},\"maxY\":{info.maxY},\"centerX\":{info.centerX},\"centerY\":{info.centerY}}}";
    }

    string GetBuildingsData()
    {
        List<string> list = new();
        var bounds = buildingPlacer.buildingMap.cellBounds;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int pos = new(x, y, 0);
                var tile = buildingPlacer.buildingMap.GetTile(pos);

                if (tile != null)
                {
                    foreach (var b in buildingPlacer.availableBuildings)
                    {
                        if (b.tile == tile)
                        {
                            list.Add($"{{\"buildingType\":\"{b.id}\",\"x\":{x},\"y\":{y}}}");
                            break;
                        }
                    }
                }
            }
        }

        return $"{{\"status\":\"success\",\"buildings\":[{string.Join(",", list)}]}}";
    }

    string FocusPosition(GameCommand cmd)
    {
        if (cameraController == null)
            return "{\"status\":\"error\",\"message\":\"CameraController null\"}";

        Vector3 pos = new(cmd.x, cmd.y, cameraController.transform.position.z);
        float zoom = cmd.Upgrade > 0 ? cmd.Upgrade : 5f;
        cameraController.ZoomToPosition(pos, zoom);

        return $"{{\"status\":\"success\",\"message\":\"Focused\"}}";
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        listener?.Stop();
    }
}

[Serializable]
public class GameCommand
{
    public string action;
    public int x;
    public int y;
    public string buildingType;
    public int Upgrade;
    public string LLMReasoning;
}
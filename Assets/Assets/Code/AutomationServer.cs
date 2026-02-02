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

    TcpListener listener;
    Thread serverThread;
    Queue<GameCommand> commandQueue = new Queue<GameCommand>();
    object queueLock = new object();

    void Start()
    {
        serverThread = new Thread(ServerLoop);
        serverThread.Start();
    }

    void Update()
    {
        // Process commands on main thread (Unity requires this)
        lock (queueLock)
        {
            while (commandQueue.Count > 0)
            {
                var command = commandQueue.Dequeue();
                ExecuteCommand(command);
            }
        }
    }

    void ServerLoop()
    {
        listener = new TcpListener(IPAddress.Loopback, 5050);
        listener.Start();
        Debug.Log("Automation server started on port 5050");

        while (true)
        {
            try
            {
                var client = listener.AcceptTcpClient();
                var stream = client.GetStream();

                byte[] buffer = new byte[1024];
                int length = stream.Read(buffer, 0, buffer.Length);
                string message = Encoding.UTF8.GetString(buffer, 0, length);

                Debug.Log("Received: " + message);

                // Parse and queue command
                var command = JsonUtility.FromJson<GameCommand>(message);
                lock (queueLock)
                {
                    commandQueue.Enqueue(command);
                }

                // Send response
                string response = "{\"status\":\"queued\"}";
                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                stream.Write(responseBytes, 0, responseBytes.Length);

                client.Close();
            }
            catch (Exception e)
            {
                Debug.LogError("Server error: " + e.Message);
            }
        }
    }

    void ExecuteCommand(GameCommand command)
    {
        Debug.Log($"Executing command: {command.action}");

        switch (command.action)
        {
            case "place_building":
                PlaceBuilding(command.x, command.y, command.buildingId);
                break;
            case "get_stats":
                GetStats();
                break;
            case "get_map":
                GetMapState();
                break;
            default:
                Debug.LogWarning("Unknown command: " + command.action);
                break;
        }
    }

    void PlaceBuilding(int x, int y, string buildingId)
    {
        Vector3Int cell = new Vector3Int(x, y, 0);

        if (!buildingPlacer.groundMap.HasTile(cell))
        {
            Debug.LogWarning($"No ground at {x},{y}");
            return;
        }

        if (buildingPlacer.buildingMap.HasTile(cell))
        {
            Debug.LogWarning($"Building already exists at {x},{y}");
            return;
        }

        // Find building data by ID
        BuildingData building = FindBuildingData(buildingId);
        if (building == null)
        {
            Debug.LogError($"Building '{buildingId}' not found");
            return;
        }

        buildingPlacer.buildingMap.SetTile(cell, building.tile);
        cityStats.AddBuilding(building);

        Debug.Log($"Placed {buildingId} at ({x},{y})");
    }

    void GetStats()
    {
        Debug.Log($"Current Stats - Population: {cityStats.population}, Power: {cityStats.power}");
    }

    void GetMapState()
    {
        // You can expand this to return actual map data
        Debug.Log("Map state requested");
    }

    BuildingData FindBuildingData(string id)
    {
        // You'll need to maintain a registry of buildings
        // For now, this returns the current building from BuildingPlacer
        return buildingPlacer.GetBuildingById(id);
    }

    void OnApplicationQuit()
    {
        listener?.Stop();
        serverThread?.Abort();
    }
}

[Serializable]
public class GameCommand
{
    public string action;
    public int x;
    public int y;
    public string buildingId;
}
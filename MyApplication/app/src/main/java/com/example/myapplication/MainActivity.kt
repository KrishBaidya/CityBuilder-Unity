package com.example.myapplication

import CityBuilderClient
import GameCommand
import android.Manifest
import android.net.Network
import android.net.Uri.fromParts
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.annotation.RequiresPermission
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.myapplication.ui.theme.MyApplicationTheme
import com.google.genai.types.GenerateContentConfig
import com.google.genai.types.ThinkingConfig
import com.google.gson.Gson
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch
import com.google.genai.types.*;
import com.google.genai.types.Content.fromParts


lateinit var network : Network;

class MainActivity : ComponentActivity() {
    @RequiresPermission(Manifest.permission.ACCESS_NETWORK_STATE)
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            MyApplicationTheme {
                Scaffold(modifier = Modifier.fillMaxSize()) { _ ->
                    CityControllerApp()
                }
            }
        }
    }
}
@Composable
fun CityControllerApp() {
    // STATE
    val logs = remember { mutableStateListOf<String>("System Ready.") }
    var isRunning by remember { mutableStateOf(false) }
    var money by remember { mutableStateOf(0) }
    val scope = rememberCoroutineScope()

    val client = remember { CityBuilderClient() }

    val config: GenerateContentConfig? =
        GenerateContentConfig
            .builder()
            .thinkingConfig(
                ThinkingConfig
                    .builder()
                    .includeThoughts(true)
                    .thinkingLevel("HIGH")
                    .build()
            )
            .responseMimeType("application/json")
            .responseSchema(
                Schema.fromJson(
                    """
        {
          "type": "object",
          "properties": {
            "action": {
              "type": "string",
              "enum": [
                "place_building",
                "upgrade",
                "get_stats",
                "get_map",
                "get_buildings_data",
                "focus_position"
              ]
            },
            "buildingType": {
              "type": "string",
              "enum": [
                "House",
                "PowerPlant",
                "Road",
                "Economic"
              ]
            },
            "x": {
              "type": "integer"
            },
            "y": {
              "type": "integer"
            },
            "Upgrade": {
              "type": "integer"
            }
          },
          "required": [
            "action"
          ],
          "propertyOrdering": [
            "action",
            "buildingType",
            "x",
            "y",
            "Upgrade"
          ]
        }
      
      """.trimIndent()
                )
            )
            .systemInstruction(
                Content
                    .fromParts(
                        Part.fromText("You are playing a city builder game.\nCurrent Money: ${money}\nMap Size: 30x30 (Range -15 to 14).\nValid buildings: \"House\" ($100), \"Road\" ($50), \"PowerPlant\" ($200), \"Economic\" ($150).\nOutput ONLY a JSON object for the next move. Example:\n{\"action\": \"place_building\", \"buildingType\": \"House\", \"x\": 0, \"y\": 0, \"LLMReasoning\": \"Starting House\"}")
                    ))
            .build()


    // GEMINI SETUP
    val gemini = remember {
        GenerativeModel(
            modelName = "gemini-flash-latest",
            apiKey = "", // <--- PASTE KEY HERE
        )
    }

    // THE AI LOOP
    fun runAILoop() {
        scope.launch {
            logs.add("🚀 Starting AI Loop...")

            while (isRunning) {
                // 1. GET STATS
                logs.add("📊 Fetching stats...")
                val stats = client.sendCommand(GameCommand("get_stats"))

                if (stats?.status == "success") {
                    money = stats.money

                    // 2. ASK GEMINI
                    logs.add("🧠 Thinking...")
                    val prompt = """
                        You are playing a city builder game.
                        Current Money: ${stats.money}
                        Map Size: 30x30 (Range -15 to 14).
                        
                        Valid buildings: "House" ($100), "Road" ($50), "PowerPlant" ($200), "Economic" ($150).
                        
                        Output ONLY a JSON object for the next move. Example:
                        {"action": "place_building", "buildingType": "House", "x": 0, "y": 0, "LLMReasoning": "Starting House"}
                        
                        Do not output markdown. Just JSON.
                    """.trimIndent()

                    try {
                        val response = gemini.generateContent(prompt)
                        val aiText = response.text?.replace("```json", "")?.replace("```", "")?.trim()

                        if (aiText != null) {
                            // Parse Gemini's JSON to Command
                            val command = Gson().fromJson(aiText, GameCommand::class.java)

                            logs.add("🤖 AI: ${command.LLMReasoning}")

                            // 3. EXECUTE COMMAND
                            val result = client.sendCommand(command)
                            if (result?.status == "error") {
                                logs.add("❌ Server: ${result.message}")
                            }
                        }
                    } catch (e: Exception) {
                        logs.add("⚠️ AI Error: ${e.message}")
                    }
                } else {
                    logs.add("❌ Connection failed. Is Game running?")
                    isRunning = false
                }

                // Wait 2 seconds before next move
                delay(2000)
            }
        }
    }

    // UI LAYOUT
    Column(modifier = Modifier
        .fillMaxSize()
        .padding(16.dp)) {
        Text("🏙️ CityAI Controller", fontSize = 24.sp, color = MaterialTheme.colorScheme.primary)

        Spacer(modifier = Modifier.height(10.dp))

        Card(modifier = Modifier
            .fillMaxWidth()
            .padding(bottom = 10.dp)) {
            Column(modifier = Modifier.padding(16.dp)) {
                Text("Money: $$money", fontSize = 20.sp, style = MaterialTheme.typography.titleMedium)
                Text("Status: ${if (isRunning) "🟢 AI Running" else "🔴 Stopped"}")
            }
        }

        Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceEvenly) {
            Button(onClick = {
                if (!isRunning) {
                    isRunning = true
                    runAILoop()
                }
            }) { Text("START AI") }

            Button(onClick = { isRunning = false }, colors = ButtonDefaults.buttonColors(containerColor = Color.Red)) {
                Text("STOP")
            }
        }

        Spacer(modifier = Modifier.height(10.dp))
        Text("Logs:", fontSize = 16.sp)

        // LOG CONSOLE
        LazyColumn(
            modifier = Modifier
                .fillMaxSize()
                .background(Color.Black.copy(alpha = 0.05f))
                .padding(8.dp)
        ) {
            items(logs.reversed()) { log ->
                Text(text = log, fontSize = 12.sp, lineHeight = 14.sp)
                HorizontalDivider(color = Color.Gray.copy(alpha = 0.2f))
            }
        }
    }
}

@Preview(showBackground = true)
@Composable
fun GreetingPreview() {
    MyApplicationTheme {
        CityControllerApp()
    }
}
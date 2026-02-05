import android.util.Log
import com.google.gson.Gson
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.io.BufferedReader
import java.io.InputStreamReader
import java.io.PrintWriter
import java.net.Socket

data class GameCommand(
    val action: String,
    val buildingType: String? = null,
    val x: Int = 0,
    val y: Int = 0,
    val Upgrade: Int = 0,
    val LLMReasoning: String? = ""
)

data class GameResponse(
    val status: String,
    val message: String? = null,
    val width: Int = 0,
    val height: Int = 0,
    val money: Int = 0,
    // Add other fields as needed based on your Unity JSON
)

class CityBuilderClient {
    private val gson = Gson()
    private val host = "127.0.0.1" // Localhost on Android refers to the device itself
    private val port = 5050

    suspend fun sendCommand(command: GameCommand): GameResponse? {
        return withContext(Dispatchers.IO) {
            try {
                // 1. Open Connection
                val socket = Socket(host, port)
                socket.soTimeout = 5000 // 5 sec timeout

                // 2. Send Data
                val writer = PrintWriter(socket.getOutputStream(), true)
                val json = gson.toJson(command)
                Log.d("CityClient", "📤 Sending: $json")
                writer.println(json)

                // 3. Read Response
                val reader = BufferedReader(InputStreamReader(socket.getInputStream()))
                val responseJson = reader.readLine()

                // 4. Close (Crucial!)
                socket.close()

                if (responseJson != null) {
                    Log.d("CityClient", "📥 Received: $responseJson")
                    return@withContext gson.fromJson(responseJson, GameResponse::class.java)
                }
                null
            } catch (e: Exception) {
                Log.e("CityClient", "❌ Error: ${e.message}")
                GameResponse("error", e.message)
            }
        }
    }
}
import socket
import json
import time
import random


class CityBuilderClient:
    """Complete Python client for Unity City Builder"""
    
    def __init__(self, host='localhost', port=5050):
        self.host = host
        self.port = port
        self.map_info = None
        
    def send_command(self, command):
        """Send a command to Unity and get response (With Retry)"""
        max_retries = 3
        for attempt in range(max_retries):
            try:
                client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
                client.settimeout(5)
                client.connect((self.host, self.port))
                
                message = json.dumps(command)
                # print(f"📤 Sending: {message}") # Uncomment for debug
                client.send(message.encode('utf-8'))
                
                response = client.recv(4096).decode('utf-8')
                client.close()
                
                result = json.loads(response)
                # print(f"📥 Response: {result}") # Uncomment for debug
                return result

            except (ConnectionRefusedError, socket.timeout) as e:
                # If Unity is busy processing the previous frame, wait a tiny bit and retry
                if attempt < max_retries - 1:
                    time.sleep(0.2)
                    continue
                else:
                    print(f"❌ Connection failed after {max_retries} attempts: {e}")
                    return {"status": "error", "message": "connection failed"}
            except Exception as e:
                print(f"❌ Error: {e}")
                return {"status": "error", "message": str(e)}
    
    # ==================== BUILDING COMMANDS ====================
    
    def place_building(self, building_type, x, y, reasoning=""):
        """Place a building at coordinates
        
        Args:
            building_type: "House", "Road", "PowerPlant", or "Economic"
            x: X coordinate
            y: Y coordinate
            reasoning: Optional LLM reasoning text
        """
        response = self.send_command({
            "action": "place_building",
            "buildingType": building_type,
            "x": x,
            "y": y,
            "LLMReasoning": reasoning
        })
        
        if response.get("status") == "success":
            print(f"✅ {response.get('message')}")
        else:
            print(f"❌ {response.get('message')}")
        
        return response
    
    def demolish(self, x, y):
        """Demolish a building at coordinates"""
        response = self.send_command({
            "action": "demolish",
            "x": x,
            "y": y
        })
        
        if response.get("status") == "success":
            refund = response.get("refund", 0)
            print(f"💥 Demolished building, refunded ${refund}")
        else:
            print(f"❌ {response.get('message')}")
        
        return response
    
    def upgrade(self, x, y, level):
        """Upgrade a building
        
        Args:
            x: X coordinate
            y: Y coordinate  
            level: Upgrade level (1, 2, 3, etc.)
        """
        response = self.send_command({
            "action": "upgrade",
            "x": x,
            "y": y,
            "Upgrade": level
        })
        
        if response.get("status") == "success":
            cost = response.get("cost", 0)
            print(f"⬆️ Upgraded to level {level}, cost ${cost}")
        else:
            print(f"❌ {response.get('message')}")
        
        return response
    
    # ==================== QUERY COMMANDS ====================
    
    def get_stats(self):
        """Get current city statistics"""
        response = self.send_command({
            "action": "get_stats"
        })
        
        if response.get("status") == "success":
            print(f"📊 Population: {response['population']} | "
                  f"Power: {response['power']} | "
                  f"Money: ${response['money']} | "
                  f"Income: ${response['income']}/turn")
        
        return response
    
    def get_map_info(self):
        """Get map dimensions and bounds"""
        response = self.send_command({
            "action": "get_map"
        })
        
        if response.get("status") == "success":
            self.map_info = response
            print(f"🗺️  Map: {response['width']}x{response['height']}")
            print(f"   X range: {response['minX']} to {response['maxX']}")
            print(f"   Y range: {response['minY']} to {response['maxY']}")
            print(f"   Center: ({response['centerX']}, {response['centerY']})")
        
        return response
    
    def get_buildings(self):
        """Get all buildings on the map"""
        response = self.send_command({
            "action": "get_buildings_data"
        })
        
        if response.get("status") == "success":
            buildings = response.get("buildings", [])
            print(f"🏢 Buildings on map: {len(buildings)}")
            for b in buildings:
                print(f"   - {b['buildingType']} at ({b['x']}, {b['y']})")
        
        return response
    
    # ==================== CAMERA COMMANDS ====================
    
    def focus_camera(self, x, y, zoom=5):
        """Move camera to position
        
        Args:
            x: X coordinate
            y: Y coordinate
            zoom: Camera zoom level (lower = closer)
        """
        response = self.send_command({
            "action": "focus_position",
            "x": x,
            "y": y,
            "Upgrade": zoom  # Reusing Upgrade field for zoom
        })
        
        if response.get("status") == "success":
            print(f"📷 Camera focused on ({x}, {y})")
        
        return response
    
    # ==================== HELPER METHODS ====================
    
    def place_building_safe(self, building_type, x, y, reasoning=""):
        """Place building with automatic bounds checking"""
        if self.map_info is None:
            print("⚠️  Map info not loaded, fetching...")
            self.get_map_info()
        
        if self.map_info:
            if x < self.map_info['minX'] or x > self.map_info['maxX']:
                print(f"❌ X={x} out of bounds ({self.map_info['minX']}-{self.map_info['maxX']})")
                return {"status": "error", "message": "X out of bounds"}
            if y < self.map_info['minY'] or y > self.map_info['maxY']:
                print(f"❌ Y={y} out of bounds ({self.map_info['minY']}-{self.map_info['maxY']})")
                return {"status": "error", "message": "Y out of bounds"}
        
        return self.place_building(building_type, x, y, reasoning)
    
    def get_random_position(self):
        """Get a random valid position on the map"""
        if self.map_info is None:
            self.get_map_info()
        
        if self.map_info:
            x = random.randint(self.map_info['minX'], self.map_info['maxX'])
            y = random.randint(self.map_info['minY'], self.map_info['maxY'])
            return (x, y)
        
        return (0, 0)


# ==================== DEMO FUNCTIONS ====================

def test_connection():
    """Test basic connection to Unity"""
    print("=" * 60)
    print("🔌 CONNECTION TEST")
    print("=" * 60)
    
    city = CityBuilderClient()
    
    print("\n1. Testing connection...")
    result = city.get_stats()
    
    if result.get("status") == "success":
        print("✅ Connection successful!")
        return True
    else:
        print("❌ Connection failed! Make sure Unity is running.")
        return False


def demo_basic_commands():
    """Demo all basic commands with VALID coordinates"""
    print("\n" + "=" * 60)
    print("🎮 BASIC COMMANDS DEMO (FIXED)")
    print("=" * 60)
    
    city = CityBuilderClient()
    
    print("\n📍 Step 1: Get map info")
    map_info = city.get_map_info()
    
    if map_info.get("status") != "success":
        print("❌ Failed to get map info. Is Unity running?")
        return

    # USE DYNAMIC COORDINATES (Center of the map)
    cx = map_info['centerX']
    cy = map_info['centerY']
    print(f"🎯 Target Coordinates: ({cx}, {cy})")
    
    print("\n📊 Step 2: Get initial stats")
    city.get_stats()
    time.sleep(0.5)
    
    print("\n🏗️  Step 3: Place some buildings")
    # Using cx, cy ensures we are always inside the generated map
    city.place_building("PowerPlant", cx, cy, "Central power hub")
    time.sleep(0.5) # Increased sleep slightly to prevent socket exhaustion
    
    city.place_building("House", cx + 1, cy, "Residential area")
    time.sleep(0.5)
    
    city.place_building("Road", cx + 2, cy, "Connect buildings")
    time.sleep(0.5)
    
    city.place_building("Economic", cx + 3, cy, "Generate income")
    time.sleep(1.0) # Wait for Unity to update physics/logic
    
    print("\n📊 Step 4: Check stats after building")
    city.get_stats()
    time.sleep(0.5)
    
    print("\n🗺️  Step 5: List all buildings")
    city.get_buildings()
    time.sleep(0.5)
    
    print("\n⬆️  Step 6: Upgrade a building")
    city.upgrade(cx, cy, 2) # Upgrade the power plant we placed at center
    time.sleep(0.5)
    
    print("\n💥 Step 7: Demolish a building")
    city.demolish(cx + 2, cy) # Demolish the road
    time.sleep(0.5)
    
    print("\n📷 Step 8: Focus camera")
    city.focus_camera(cx, cy, 3)
    
    print("\n✅ Demo complete!")

def demo_city_planning():
    """Demo strategic city planning"""
    print("\n" + "=" * 60)
    print("🏙️  STRATEGIC CITY PLANNING DEMO")
    print("=" * 60)
    
    city = CityBuilderClient()
    
    # Get map info
    city.get_map_info()
    center_x = city.map_info['centerX']
    center_y = city.map_info['centerY']
    
    print("\n⚡ Phase 1: Power Infrastructure")
    city.focus_camera(center_x, center_y, 8)
    time.sleep(0.5)
    
    city.place_building("PowerPlant", center_x, center_y, "Main power plant")
    time.sleep(0.2)
    city.place_building("PowerPlant", center_x + 10, center_y, "Backup power")
    time.sleep(0.5)
    
    print("\n🛣️  Phase 2: Road Network")
    for i in range(5):
        city.place_building("road", center_x + i, center_y + 3, f"Road segment {i+1}")
        time.sleep(0.1)
    
    for i in range(5):
        city.place_building("road", center_x + 2, center_y + i, f"Road segment {i+6}")
        time.sleep(0.1)
    
    city.get_stats()
    time.sleep(0.5)
    
    print("\n🏠 Phase 3: Residential Zone")
    positions = [
        (center_x + 1, center_y + 1),
        (center_x + 3, center_y + 1),
        (center_x + 1, center_y + 5),
        (center_x + 3, center_y + 5),
    ]
    
    for i, (x, y) in enumerate(positions):
        city.place_building("House", x, y, f"Residential building {i+1}")
        time.sleep(0.2)
    
    city.get_stats()
    time.sleep(0.5)
    
    print("\n💼 Phase 4: Economic Zone")
    positions = [
        (center_x + 5, center_y + 1),
        (center_x + 5, center_y + 2),
    ]
    
    for i, (x, y) in enumerate(positions):
        city.place_building("economic", x, y, f"Economic building {i+1}")
        time.sleep(0.2)
    
    print("\n📊 Final Stats:")
    city.get_stats()
    
    print("\n🗺️  Final Layout:")
    city.get_buildings()
    
    print("\n✅ City planning complete!")


def demo_random_city(num_buildings=20):
    """Build a random city"""
    print("\n" + "=" * 60)
    print(f"🎲 RANDOM CITY BUILDER ({num_buildings} buildings)")
    print("=" * 60)
    
    city = CityBuilderClient()
    city.get_map_info()
    
    building_types = ["House", "road", "PowerPlant", "economic"]
    weights = [40, 30, 15, 15]  # Probability weights
    
    placed = 0
    attempts = 0
    max_attempts = num_buildings * 3
    
    while placed < num_buildings and attempts < max_attempts:
        attempts += 1
        
        # Random position and building type
        x, y = city.get_random_position()
        building_type = random.choices(building_types, weights=weights)[0]
        
        result = city.place_building(building_type, x, y, f"Random placement #{placed+1}")
        
        if result.get("status") == "success":
            placed += 1
            print(f"Progress: {placed}/{num_buildings}")
        
        time.sleep(0.1)
    
    print(f"\n✅ Placed {placed} buildings in {attempts} attempts")
    city.get_stats()
    city.get_buildings()


def demo_with_llm_reasoning():
    """Demo with LLM-style reasoning"""
    print("\n" + "=" * 60)
    print("🤖 LLM REASONING DEMO")
    print("=" * 60)
    
    city = CityBuilderClient()
    city.get_map_info()
    
    # LLM-style actions with detailed reasoning
    actions = [
        {
            "type": "PowerPlant",
            "x": 25,
            "y": 25,
            "reasoning": "Establishing central power infrastructure as foundation for city growth. Strategic placement allows equal distribution to all quadrants."
        },
        {
            "type": "road",
            "x": 26,
            "y": 25,
            "reasoning": "Creating main arterial road east from power plant to enable future development zones."
        },
        {
            "type": "road",
            "x": 25,
            "y": 26,
            "reasoning": "Extending road network north to create grid pattern for efficient city layout."
        },
        {
            "type": "House",
            "x": 26,
            "y": 26,
            "reasoning": "Placing residential unit near power and roads to maximize infrastructure efficiency and minimize power loss."
        },
        {
            "type": "economic",
            "x": 27,
            "y": 26,
            "reasoning": "Building economic center adjacent to residential area to generate income while utilizing existing power grid."
        },
        {
            "type": "House",
            "x": 26,
            "y": 27,
            "reasoning": "Expanding residential capacity to increase population and city growth potential."
        },
        {
            "type": "PowerPlant",
            "x": 30,
            "y": 30,
            "reasoning": "Adding secondary power plant in eastern quadrant to support future expansion and provide power redundancy."
        },
    ]
    
    for i, action in enumerate(actions, 1):
        print(f"\n🤖 LLM Decision {i}/{len(actions)}:")
        print(f"   Building: {action['type']}")
        print(f"   Position: ({action['x']}, {action['y']})")
        print(f"   Reasoning: {action['reasoning']}")
        
        city.place_building(
            action['type'],
            action['x'],
            action['y'],
            action['reasoning']
        )
        time.sleep(0.5)
        
        if i % 3 == 0:
            print("\n📊 Progress check:")
            city.get_stats()
    
    print("\n📊 Final city state:")
    city.get_stats()
    city.get_buildings()


def interactive_mode():
    """Interactive command-line interface"""
    print("\n" + "=" * 60)
    print("🎮 INTERACTIVE MODE")
    print("=" * 60)
    print("\nCommands:")
    print("  place <type> <x> <y>  - Place building (House/road/PowerPlant/economic)")
    print("  demolish <x> <y>      - Demolish building")
    print("  upgrade <x> <y> <lvl> - Upgrade building")
    print("  stats                 - Show city stats")
    print("  map                   - Show map info")
    print("  buildings             - List all buildings")
    print("  focus <x> <y> [zoom]  - Focus camera")
    print("  quit                  - Exit")
    print("=" * 60)
    
    city = CityBuilderClient()
    city.get_map_info()
    
    while True:
        try:
            cmd = input("\n> ").strip().lower().split()
            
            if not cmd:
                continue
            
            if cmd[0] == "quit":
                print("👋 Goodbye!")
                break
            
            elif cmd[0] == "place" and len(cmd) >= 4:
                building_type = cmd[1]
                x, y = int(cmd[2]), int(cmd[3])
                city.place_building(building_type, x, y)
            
            elif cmd[0] == "demolish" and len(cmd) >= 3:
                x, y = int(cmd[1]), int(cmd[2])
                city.demolish(x, y)
            
            elif cmd[0] == "upgrade" and len(cmd) >= 4:
                x, y, level = int(cmd[1]), int(cmd[2]), int(cmd[3])
                city.upgrade(x, y, level)
            
            elif cmd[0] == "stats":
                city.get_stats()
            
            elif cmd[0] == "map":
                city.get_map_info()
            
            elif cmd[0] == "buildings":
                city.get_buildings()
            
            elif cmd[0] == "focus" and len(cmd) >= 3:
                x, y = int(cmd[1]), int(cmd[2])
                zoom = int(cmd[3]) if len(cmd) > 3 else 5
                city.focus_camera(x, y, zoom)
            
            else:
                print("❌ Invalid command")
        
        except ValueError:
            print("❌ Invalid coordinates (must be numbers)")
        except Exception as e:
            print(f"❌ Error: {e}")


# ==================== MAIN ====================

if __name__ == "__main__":
    print("🏙️  CITY BUILDER - Python Client")
    print("=" * 60)
    print("\nSelect demo:")
    print("1. Test Connection")
    print("2. Basic Commands Demo")
    print("3. Strategic City Planning")
    print("4. Random City Builder")
    print("5. LLM Reasoning Demo")
    print("6. Interactive Mode")
    
    choice = input("\nEnter choice (1-6): ").strip()
    
    if choice == "1":
        test_connection()
    elif choice == "2":
        demo_basic_commands()
    elif choice == "3":
        demo_city_planning()
    elif choice == "4":
        num = input("Number of buildings (default 20): ").strip()
        num = int(num) if num else 20
        demo_random_city(num)
    elif choice == "5":
        demo_with_llm_reasoning()
    elif choice == "6":
        interactive_mode()
    else:
        print("Running basic demo...")
        demo_basic_commands()
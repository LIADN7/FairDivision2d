import base64
import json
import os

import requests

# --- Step 1: Load service account credentials ---
config_path = r"C:\StudyProjects\FairDivision2dUnityKey\unity_cloud_config.json"

if not os.path.exists(config_path):
    raise FileNotFoundError(f"⚠️ Service account config file not found at: {config_path}")

with open(config_path, "r") as f:
    config = json.load(f)

client_id = config["client_id"]
client_secret = config["client_secret"]
project_id = config["project_id"]
token_uri = config["token_uri"]
environment = config.get("environment", "production")

# --- Step 2: Get Bearer Token from Unity ---
print("🔐 Requesting access token...")
token_response = requests.post(
    token_uri,
    data={
        "grant_type": "client_credentials",
        "client_id": client_id,
        "client_secret": client_secret
    }
)

if not token_response.ok:
    raise Exception(f"❌ Failed to get access token: {token_response.status_code} - {token_response.text}")

access_token = token_response.json()["access_token"]
headers = {
    "Authorization": f"Bearer {access_token}",
    "Content-Type": "application/json"
}

# --- Step 3: Fetch all player data ---
players_url = f"https://services.api.unity.com/cloud-save/v1/projects/{project_id}/environments/{environment}/players"

print("📡 Fetching player list from:", players_url)
players_response = requests.get(players_url, headers=headers)

if not players_response.ok:
    raise Exception(f"❌ Failed to get players list: {players_response.status_code} - {players_response.text}")

players = players_response.json().get("results", [])
print(f"✅ Found {len(players)} players.")

all_player_data = {}

for player in players:
    player_id = player["id"]
    print(f"🔍 Fetching data for player {player_id}...")
    data_url = f"https://services.api.unity.com/cloud-save/v1/projects/{project_id}/environments/{environment}/players/{player_id}/items"
    data_response = requests.get(data_url, headers=headers)

    if data_response.ok:
        all_player_data[player_id] = data_response.json()
    else:
        print(f"❌ Failed to fetch data for player {player_id}: {data_response.status_code} - {data_response.text}")

# --- Step 4: Save all data to JSON file ---
output_path = r"C:\StudyProjects\UnityProjects\FairDivision2d\Assets\Script\CutAndChooseClasses\DataScript_PY\all_player_data.json"
with open(output_path, "w", encoding="utf-8") as f:
    json.dump(all_player_data, f, ensure_ascii=False, indent=2)

print(f"✅ All data saved to {output_path}")
import base64
import json

import requests

# ==============================================================================
# TODO: Fill in these 3 details from your Unity Cloud Project
# ==============================================================================
PROJECT_ID = "07f90f77-1b7d-4bd2-a993-3ddcea0993b7"
SERVICE_ACCOUNT_KEY_ID = "038a4792-c08c-4f57-a132-23ea6faf97c7"
SERVICE_ACCOUNT_SECRET_KEY = "tj5bosBKwmv2-WL29C8k8bcxwoIZgdKr"
# ==============================================================================

def get_access_token(key_id, secret_key):
    """Requests a new access token from Unity's auth service."""
    print("Requesting new access token from Unity Services...")
    
    # Encode the Key ID and Secret Key for Basic Authentication
    credentials = f"{key_id}:{secret_key}"
    encoded_credentials = base64.b64encode(credentials.encode()).decode()
    
    token_url = "https://services.api.unity.com/auth/v1/token"
    headers = {
        "Authorization": f"Basic {encoded_credentials}",
        "Content-Type": "application/x-www-form-urlencoded"
    }
    # The scope defines what permissions the token will have.
    # We need to read data from cloud save.
    payload = "grant_type=client_credentials&scope=cloud_save.data.read"

    try:
        response = requests.post(token_url, headers=headers, data=payload)
        
        if response.status_code == 200:
            access_token = response.json().get("access_token")
            print("✅ Successfully obtained new access token.")
            return access_token
        else:
            print(f"❌ Failed to get access token. Status: {response.status_code}, Reason: {response.text}")
            return None
    except Exception as e:
        print(f"An error occurred during token request: {e}")
        return None

# --- Main script logic ---

# 1. Get the access token automatically
access_token = get_access_token(SERVICE_ACCOUNT_KEY_ID, SERVICE_ACCOUNT_SECRET_KEY)

# If we failed to get a token, stop the script.
if not access_token:
    print("Halting script because authentication failed.")
else:
    # The list of Player IDs you provided
    player_ids = [
        "0I1pgTVUM3Kg6rSynfhv7RvYfwJ7h", "114zMOhI1WPUFo36cVv6BGtEIG4d",
        # "7zD0sc5HHYLyLe4lHlUX0KjN9AXo", "CnGGM7aJOFwf0unJGm5npIBuaMCJ",
        # "DGKhGlpK2bgcq5lPFkepAhRMdvkt", "Dgt25xDnjAAcDaPSHgkQJ51YIEp",
        # "GDttl4WG3zK2jpPKdh3LPWldbPBQ", "MPzS35R1xf4t2t8ld8iEJ22xCSp",
        # "O2gHvawXZkbig9QB8udBkmQmnitS", "O6Xuj56X8vWXA3k1bB98GJ2neBxY",
        # "OHh1JrdD3ddN9eYqnkWks7vWL9sZ", "QSeghntQyKS7E0nlWqefvllcdAHe",
        # "UBD00b0vsaCrY2TLU2FRUTWHTgT", "VcNVfRpjaOYmV8AqrqRp0eeE7k2C",
        # "WXvBjarGOdOVJ8POvuMcUSbSqTfn", "ZbPka9nXZqD3j0H3EEkcCs5IYP06N",
        # "dFTwNOcSBC8VfD5LZjcPoY6qCeaf", "frmqWkwUV8JH1PmSn1YQbnU5g2W9",
        # "ggOmJNGw7Um6cAxt13ZesGK7JZpC", "kTFcbFUGiax6RfVqhlud2r0oGndN",
        # "ovPhaN8I2XWE1XbZEActdKLpBo2d", "p3lyZPgVc1LggJR1jE0kkZl3uTO",
        # "sT4xmToL1hGCQjm9s9Ns7hlMjAv", "wC17IjajUGhxNC6rB7yWzKUl4YEI"
    ]

    all_players_data = {}
    headers = {
        "Authorization": f"Bearer {access_token}"
    }
    base_url = f"https://cloud-save.services.api.unity.com/v1/data/projects/{PROJECT_ID}/players"

    print(f"\nStarting to process {len(player_ids)} players...")
    for player_id in player_ids:
        try:
            player_data_url = f"{base_url}/{player_id}/items"
            response = requests.get(player_data_url, headers=headers)
            
            if response.status_code == 200:
                player_data_items = response.json().get('results', [])
                processed_data = {item['key']: item['value'] for item in player_data_items}
                all_players_data[player_id] = processed_data
                print(f"✅ Successfully fetched data for player: {player_id}")
            else:
                print(f"❌ Failed for player {player_id}. Status: {response.status_code}, Reason: {response.text}")

        except Exception as e:
            print(f"An error occurred while processing player {player_id}: {e}")

    output_filename = "all_players_data.json"
    with open(output_filename, "w", encoding="utf-8") as f:
        json.dump(all_players_data, f, indent=4, ensure_ascii=False)

    print(f"\n🚀 Done! All available data has been exported to {output_filename}")
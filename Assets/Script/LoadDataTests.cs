using UnityEngine;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

public class LoadDataTests : MonoBehaviour
{
    [SerializeField]
    private string clientId = "1db21af1-427e-492d-a297-c83a72b888ec"; // החלף ב-Client ID שלך
    [SerializeField]
    private string clientSecret = "zqnjh6jVVBFuxT9zbgwIpw1oG6RFQxRZ"; // החלף בסוד שלך
    [SerializeField]
    private string projectId = "07f90f77-1b7d-4bd2-a993-3ddcea0993b7"; // החלף במזהה הפרויקט שלך


    private CloudSaveAdmin _cloudSaveAdmin;

    async void Start()
    {
        _cloudSaveAdmin = new CloudSaveAdmin(clientId, clientSecret, projectId);
        await GetAllCloudSaveData();
    }

    private async Task GetAllCloudSaveData()
    {
        var allPlayerData = await _cloudSaveAdmin.GetAllPlayerData();

        if (allPlayerData != null)
        {
            Debug.Log($"Found {allPlayerData.Count} player data items:");
            foreach (var data in allPlayerData)
            {
                Debug.Log($"Key: {data.key}, Modified: {data.modified}, Value: {data.value.ToString(Formatting.Indented)}");
                // כאן תוכל לעבד את נתוני השחקן (data.value) כ-JObject
                // JObject parsedValue = data.value;
                // if (parsedValue.ContainsKey("score"))
                // {
                //     Debug.Log($"  Score: {parsedValue["score"]}");
                // }
            }
        }
        else
        {
            Debug.LogError("Failed to retrieve player data.");
        }
    }
}

public class CloudSaveAdmin
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _projectId; // מזהה הפרויקט שלך ב-Unity Cloud

    public CloudSaveAdmin(string clientId, string clientSecret, string projectId)
    {
        _clientId = clientId;
        _clientSecret = clientSecret;
        _projectId = projectId;
    }

    private async Task<string> GetAccessToken()
    {
        using var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://identity.services.unity.com/oauth2/token");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", _clientId },
            { "client_secret", _clientSecret },
            { "scope", "com.unity.services.cloudsave.admin" }
        });

        try
        {
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenData = JsonConvert.DeserializeObject<AccessTokenResponse>(responseContent);
            return tokenData?.access_token;
        }
        catch (HttpRequestException ex)
        {
            Debug.LogError($"Error getting access token: {ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            Debug.LogError($"Error parsing access token response: {ex.Message}");
            return null;
        }
    }

    public async Task<List<CloudSaveItem>> GetAllPlayerData()
    {
        var accessToken = await GetAccessToken();
        if (accessToken == null)
        {
            return null;
        }

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            var response = await client.GetAsync($"https://cloud-save.services.unity.com/v1/projects/{_projectId}/items");
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<CloudSaveItem>>(responseContent);
        }
        catch (HttpRequestException ex)
        {
            Debug.LogError($"Error getting all player data: {ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            Debug.LogError($"Error parsing player data response: {ex.Message}");
            return null;
        }
    }

    // מחלקה פנימית לייצוג פריט Cloud Save
    public class CloudSaveItem
    {
        public string key { get; set; }
        [JsonProperty("value")]
        public JObject value { get; set; } // השתמש ב-JObject עבור Newtonsoft.Json
        public DateTimeOffset modified { get; set; }
    }

    // מחלקה פנימית לעיבוד תגובת אסימון הגישה
    private class AccessTokenResponse
    {
        public string access_token { get; set; }
    }
}
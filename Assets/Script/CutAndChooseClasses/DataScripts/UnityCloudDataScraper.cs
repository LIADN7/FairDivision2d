using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using UnityEngine;

/// <summary>
/// UnityCloudDataScraper downloads Cloud Save data for a list of player IDs from Unity Cloud Save API,
/// and exports all results to a single JSON file.
/// </summary>
public class UnityCloudDataScraper : MonoBehaviour
{
    //     [SerializeField] private string projectId = "07f90f77-1b7d-4bd2-a993-3ddcea0993b7";
    // [SerializeField] private string serviceAccountKeyId = "66e2018a-6141-4869-857c-2a36ea6d8c5f";
    // [SerializeField] private string serviceAccountSecretKey = "wWrUZ8TpU9lMz4cQVCTGw62XQaUUxTPh";
    private readonly HttpClient httpClient;
    private readonly string projectId = "07f90f77-1b7d-4bd2-a993-3ddcea0993b7";
    private readonly string accessToken;

    /// <summary>
    /// Initializes a new instance of the UnityCloudDataScraper class.
    /// </summary>
    /// <param name="projectId">Unity Project ID</param>
    /// <param name="accessToken">Bearer access token for Unity Cloud API</param>
    public UnityCloudDataScraper(string projectId, string accessToken)
    {
        this.projectId = projectId;
        this.accessToken = accessToken;
        httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
    }

    /// <summary>
    /// Exports Cloud Save data for all player IDs in the list and saves it to a JSON file.
    /// </summary>
    public async Task ExportAllData()
    {
        var allData = new Dictionary<string, object>();

        // List of player IDs to export data for
        var playerIds = new List<string>
        {
            "0I1pgTVUM3Kg6rSynfhv7RvYfwJ7h",
            "114zMOhI1WPUFo36cVv6BGtEIG4d",
            "7zD0sc5HHYLyLe4lHlUX0KjN9AXo",
            "CnGGM7aJOFwf0unJGm5npIBuaMCJ",
            "DGKhGlpK2bgcq5lPFkepAhRMdvkt",
            "Dgt25xDnjAAcDaPSHgkQJ51YIEp",
            "GDttl4WG3zK2jpPKdh3LPWldbPBQ",
            "MPzS35R1xf4t2t8ld8iEJ22xCSp",
            "O2gHvawXZkbig9QB8udBkmQmnitS",
            "O6Xuj56X8vWXA3k1bB98GJ2neBxY",
            "OHh1JrdD3ddN9eYqnkWks7vWL9sZ",
            "QSeghntQyKS7E0nlWqefvllcdAHe",
            "UBD00b0vsaCrY2TLU2FRUTWHTgT",
            "VcNVfRpjaOYmV8AqrqRp0eeE7k2C",
            "WXvBjarGOdOVJ8POvuMcUSbSqTfn",
            "ZbPka9nXZqD3j0H3EEkcCs5IYP06N",
            "dFTwNOcSBC8VfD5LZjcPoY6qCeaf",
            "frmqWkwUV8JH1PmSn1YQbnU5g2W9",
            "ggOmJNGw7Um6cAxt13ZesGK7JZpC",
            "kTFcbFUGiax6RfVqhlud2r0oGndN",
            "ovPhaN8I2XWE1XbZEActdKLpBo2d",
            "p3lyZPgVc1LggJR1jE0kkZl3uTO",
            "sT4xmToL1hGCQjm9s9Ns7hlMjAv",
            "wC17IjajUGhxNC6rB7yWzKUl4YEI"
        };

        foreach (string playerId in playerIds)
        {
            try
            {
                var playerData = await GetPlayerCloudSaveData(playerId);
                allData[playerId] = playerData;
                Console.WriteLine($"✓ Exported data for {playerId}");

                // Delay to avoid API rate limiting
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error for {playerId}: {ex.Message}");
            }
        }

        // Save all player data to a JSON file
        string json = JsonConvert.SerializeObject(allData, Formatting.Indented);
        string fileName = $"unity_cloud_save_export_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        await File.WriteAllTextAsync(fileName, json);

        Console.WriteLine($"Export completed! {allData.Count} players exported. Output: {fileName}");
    }

    /// <summary>
    /// Gets the Cloud Save data for a specific player ID.
    /// </summary>
    /// <param name="playerId">The player ID</param>
    /// <returns>The player's cloud save data as an object</returns>
    private async Task<object> GetPlayerCloudSaveData(string playerId)
    {
        string url = $"https://cloud-save-api.unity.com/v1/projects/{projectId}/players/{playerId}/data";

        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        string content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject(content);
    }
}
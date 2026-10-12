using System.Net.Http;
using System.Net.Http.Json;
using SafeHavenWPF.Models;

namespace SafeHavenWPF.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;

    public bool IsBackendReachable { get; private set; }

    public ApiService()
    {
        _httpClient = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:8000/") };
    }

    public async Task<List<Building>> GetBuildingsAsync()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<Building>>("buildings") ?? [];
            IsBackendReachable = true;
            return result;
        }
        catch
        {
            IsBackendReachable = false;
            return [];
        }
    }

    public async Task<Building?> AnalyzeBuildingAsync(int id)
    {
        try
        {
            var response = await _httpClient.PostAsync($"analyze/{id}", null);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<Building>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<Earthquake>> GetEarthquakesAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<Earthquake>>("earthquakes") ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<SafeZone>> GetSafeZonesAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<SafeZone>>("safe-zones") ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<bool> BootstrapDataAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("data/bootstrap", null);
            IsBackendReachable = response.IsSuccessStatusCode;
            return response.IsSuccessStatusCode;
        }
        catch
        {
            IsBackendReachable = false;
            return false;
        }
    }
}

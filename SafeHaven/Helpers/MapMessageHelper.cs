using System.Text.Json;
using System.Text.Json.Serialization;
using SafeHavenWPF.Models;

namespace SafeHavenWPF.Helpers;

public class MapMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "full";

    [JsonPropertyName("fitBounds")]
    public bool FitBounds { get; set; } = true;

    [JsonPropertyName("buildings")]
    public List<Building> Buildings { get; set; } = [];

    [JsonPropertyName("buildingsOnly")]
    public bool BuildingsOnly { get; set; } = true;

    [JsonPropertyName("heatmap")]
    public List<NeighborhoodRiskZone> Heatmap { get; set; } = [];
}

public static class MapMessageHelper
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize(MapMessage message) =>
        JsonSerializer.Serialize(message, Options);

    public static MapMessage Create(List<Building> buildings) => new()
    {
        Buildings = buildings,
        BuildingsOnly = true,
        Heatmap = []
    };
}

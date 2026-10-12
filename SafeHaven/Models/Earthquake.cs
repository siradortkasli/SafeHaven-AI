using System.Text.Json.Serialization;

namespace SafeHavenWPF.Models;

public class Earthquake
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("tarih")]
    public string Tarih { get; set; } = string.Empty;

    [JsonPropertyName("yer")]
    public string Yer { get; set; } = string.Empty;

    [JsonPropertyName("buyukluk")]
    public double Buyukluk { get; set; }

    [JsonPropertyName("derinlik")]
    public double Derinlik { get; set; }

    [JsonPropertyName("enlem")]
    public double Enlem { get; set; }

    [JsonPropertyName("boylam")]
    public double Boylam { get; set; }
}
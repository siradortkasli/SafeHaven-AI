using System.Text.Json.Serialization;

namespace SafeHavenWPF.Models;

public class SafeZone
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("ad")]
    public string Ad { get; set; } = string.Empty;

    [JsonPropertyName("mahalle")]
    public string Mahalle { get; set; } = string.Empty;

    [JsonPropertyName("kapasite")]
    public int Kapasite { get; set; }

    [JsonPropertyName("doluluk")]
    public int Doluluk { get; set; }

    [JsonPropertyName("ulasim_durumu")]
    public string UlasimDurumu { get; set; } = string.Empty;

    [JsonPropertyName("guvenlik_skoru")]
    public double GuvenlikSkoru { get; set; }

    [JsonPropertyName("ai_aciklama")]
    public string AiAciklama { get; set; } = string.Empty;

    [JsonPropertyName("ai_doluluk")]
    public string AiDoluluk { get; set; } = string.Empty;

    [JsonPropertyName("yonlendirme_gerekce")]
    public string YonlendirmeGerekce { get; set; } = string.Empty;

    [JsonPropertyName("enlem")]
    public double Enlem { get; set; }

    [JsonPropertyName("boylam")]
    public double Boylam { get; set; }

    [JsonIgnore]
    public int DolulukYuzdesi => Kapasite > 0
        ? (int)Math.Min(100, Math.Round((double)Doluluk / Kapasite * 100))
        : Math.Min(100, Doluluk);

    [JsonIgnore]
    public string DolulukGosterim => $"%{DolulukYuzdesi}";
}
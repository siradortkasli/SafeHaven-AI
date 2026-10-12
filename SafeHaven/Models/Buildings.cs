using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.Json.Serialization;

namespace SafeHavenWPF.Models;

public class Building
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("mahalle")]
    public string Mahalle { get; set; } = string.Empty;

    [JsonPropertyName("adres")]
    public string Adres { get; set; } = string.Empty;

    [JsonPropertyName("bina_yasi")]
    public int BinaYasi { get; set; }

    [JsonPropertyName("kat_sayisi")]
    public int KatSayisi { get; set; }

    [JsonPropertyName("zemin_riski")]
    public int ZeminRiski { get; set; }

    [JsonPropertyName("hasar_durumu")]
    public int HasarDurumu { get; set; }

    [JsonPropertyName("yasli_sayisi")]
    public int YasliSayisi { get; set; }

    [JsonPropertyName("engelli_sayisi")]
    public int EngelliSayisi { get; set; }

    [JsonPropertyName("cocuk_sayisi")]
    public int CocukSayisi { get; set; }

    [JsonPropertyName("risk_skoru")]
    public double RiskSkoru { get; set; }

    [JsonPropertyName("risk_seviyesi")]
    public string RiskSeviyesi { get; set; } = string.Empty;

    [JsonPropertyName("oneri")]
    public string Oneri { get; set; } = string.Empty;

    [JsonPropertyName("ai_aciklama")]
    public string AiAciklama { get; set; } = string.Empty;

    [JsonPropertyName("yapisal_analiz")]
    public string YapisalAnaliz { get; set; } = string.Empty;

    [JsonPropertyName("karar_faktorleri")]
    public List<string> KararFaktorleri { get; set; } = [];

    [JsonPropertyName("tahliye_zorlugu")]
    public string TahliyeZorlugu { get; set; } = string.Empty;

    [JsonPropertyName("enlem")]
    public double Enlem { get; set; }

    [JsonPropertyName("boylam")]
    public double Boylam { get; set; }
}
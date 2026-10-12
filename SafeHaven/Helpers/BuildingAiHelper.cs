using SafeHavenWPF.Models;

namespace SafeHavenWPF.Helpers;

/// <summary>
/// API'den gelen AI alanlarını gösterir; eksikse minimal yedek üretir.
/// </summary>
public static class BuildingAiHelper
{
    public static string GetAiSummary(Building b)
    {
        if (!string.IsNullOrWhiteSpace(b.AiAciklama))
            return b.AiAciklama;

        return
            $"[Bina #{b.Id}] {b.Mahalle}, {b.Adres}: Ön değerlendirme '{b.RiskSeviyesi}' " +
            $"(skor {b.RiskSkoru:0.0}). Veri yükleniyor.";
    }

    public static string GetStructuralAnalysis(Building b)
    {
        if (!string.IsNullOrWhiteSpace(b.YapisalAnaliz))
            return b.YapisalAnaliz;

        return "Yapısal analiz verisi henüz yüklenmedi. Bina seçimini yenileyin.";
    }

    public static List<string> GetDecisionFactors(Building b)
    {
        if (b.KararFaktorleri is { Count: > 0 })
            return b.KararFaktorleri;

        return ["Risk verileri yükleniyor…"];
    }

    public static double GetConfidence(Building b)
    {
        double c = 55 + b.ZeminRiski * 6 + b.HasarDurumu * 5 + b.RiskSkoru * 0.25;
        return Math.Min(Math.Max(c, 52), 98);
    }

    /// <summary>Harita risk sınıfına göre tahliye zorluğu etiketi ve renk kodu.</summary>
    public static (string Label, string ColorHex) GetEvacuationDifficulty(Building b)
    {
        var label = !string.IsNullOrWhiteSpace(b.TahliyeZorlugu)
            ? b.TahliyeZorlugu
            : b.RiskSeviyesi switch
            {
                "Acil Müdahale" or "Yüksek Risk" => "Yüksek",
                "Orta Risk" => "Orta",
                _ => "Düşük",
            };

        var color = label switch
        {
            "Yüksek" => "#EF4444",
            "Orta" => "#FACC15",
            _ => "#22C55E",
        };

        return (label, color);
    }
}

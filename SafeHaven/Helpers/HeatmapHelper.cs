using SafeHavenWPF.Models;

namespace SafeHavenWPF.Helpers;

public class NeighborhoodRiskZone
{
    public string Mahalle { get; set; } = string.Empty;
    public double OrtalamaRisk { get; set; }
    public double MaxRisk { get; set; }
    public double Enlem { get; set; }
    public double Boylam { get; set; }
    public int BinaSayisi { get; set; }
    public string RiskSeviyesi { get; set; } = string.Empty;
}

public static class HeatmapHelper
{
    // Backend ile uyumlu: 25 / 50 / 75
    public static string GetLevel(double risk) => risk switch
    {
        >= 75 => "Acil Müdahale",
        >= 50 => "Yüksek Risk",
        >= 25 => "Orta Risk",
        _ => "Düşük Risk"
    };

    public static string GetHeatmapColor(double risk) => risk switch
    {
        >= 75 => "#DC2626",
        >= 50 => "#F97316",
        >= 25 => "#FACC15",
        _ => "#22C55E"
    };

    public static List<NeighborhoodRiskZone> BuildFromBuildings(List<Building> buildings)
    {
        if (buildings.Count == 0)
            return [];

        return buildings
            .GroupBy(b => string.IsNullOrWhiteSpace(b.Mahalle) ? "Diğer" : b.Mahalle)
            .Select(g =>
            {
                double avg = g.Average(b => b.RiskSkoru);
                double max = g.Max(b => b.RiskSkoru);
                double colorRisk = max;

                return new NeighborhoodRiskZone
                {
                    Mahalle = g.Key,
                    OrtalamaRisk = Math.Round(avg, 1),
                    MaxRisk = Math.Round(max, 1),
                    Enlem = g.Average(b => b.Enlem),
                    Boylam = g.Average(b => b.Boylam),
                    BinaSayisi = g.Count(),
                    RiskSeviyesi = GetLevel(colorRisk)
                };
            })
            .OrderByDescending(z => z.MaxRisk)
            .ToList();
    }
}

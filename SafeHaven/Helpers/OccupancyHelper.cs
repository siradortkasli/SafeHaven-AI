using System.Windows.Media;
using SafeHavenWPF.Models;

namespace SafeHavenWPF.Helpers;

public static class OccupancyHelper
{
    public static int GetOccupancyPercent(SafeZone zone)
    {
        if (zone.Kapasite > 0)
            return (int)Math.Min(100, Math.Round((double)zone.Doluluk / zone.Kapasite * 100));

        return Math.Min(100, zone.Doluluk);
    }

    public static string GetOccupancyLevel(int percent) => percent switch
    {
        >= 80 => "Kritik",
        >= 50 => "Yoğun",
        _ => "Uygun"
    };

    public static Brush GetOccupancyBrush(int percent)
    {
        var color = percent switch
        {
            >= 80 => "#EF4444",
            >= 50 => "#FACC15",
            _ => "#22C55E"
        };

        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    }

    public static string GetOccupancyAiMessage(int percent, string zoneName)
    {
        if (percent >= 80)
            return $"{zoneName} kapasite sınırına yaklaşmaktadır (%{percent}). Alternatif yönlendirme önerilir.";

        if (percent >= 50)
            return $"{zoneName} doluluk oranı artmaktadır (%{percent}). Yakın alternatif güvenli alanlar değerlendirilmelidir.";

        return $"{zoneName} şu an yeterli kapasiteye sahiptir (%{percent}). Bu alan tahliye yönlendirmesi için uygundur.";
    }
}

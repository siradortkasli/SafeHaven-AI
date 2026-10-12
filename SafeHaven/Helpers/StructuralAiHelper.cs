using SafeHavenWPF.Models;

namespace SafeHavenWPF.Helpers;

public static class StructuralAiHelper
{
    public static string GetStructuralAnalysis(Building building) =>
        BuildingAiHelper.GetStructuralAnalysis(building);
}

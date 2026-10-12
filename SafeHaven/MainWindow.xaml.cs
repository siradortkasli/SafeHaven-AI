using Microsoft.Web.WebView2.Core;
using SafeHavenWPF.Helpers;
using SafeHavenWPF.Models;
using SafeHavenWPF.Services;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SafeHavenWPF;

public partial class MainWindow : Window
{
    private readonly ApiService _apiService = new();

    private List<Building> _buildings = new();
    private List<SafeZone> _safeZones = new();
    private List<Earthquake> _earthquakes = new();
    private List<SafeZone>? _pendingSafeZones;
    private bool _mapPageReady;
    private bool _pendingMapRefresh;
    private bool _safeZoneMapPageReady;

    public MainWindow()
    {
        InitializeComponent();
        WindowState = WindowState.Maximized;
        Loaded += MainWindow_Loaded;
        ContentRendered += (_, _) => _ = RefreshMapsLayoutAsync();
        SizeChanged += MainWindow_SizeChanged;
        MainTabControl.SelectionChanged += MainTabControl_SelectionChanged;
        BuildingsGrid.SelectionChanged += BuildingsGrid_SelectionChanged;
        SafeZonesGrid.SelectionChanged += SafeZonesGrid_SelectionChanged;
    }

    private void SafeZonesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MainTabControl.SelectedIndex != 2)
            return;
        if (SafeZonesGrid.SelectedItem is not SafeZone selected)
            return;

        FocusSafeZoneOnMap(selected);
        ShowSafeZoneAnalysis(selected);
    }

    private async void BuildingsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MainTabControl.SelectedIndex != 1)
            return;
        if (BuildingsGrid.SelectedItem is not Building selected)
            return;

        FocusBuildingOnMap(selected);
        await ShowAnalysisAsync(selected);
    }

    private async void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        if (MainTabControl.SelectedIndex == 1)
        {
            await Task.Delay(120);
            await SendMapAsync();
        }
        else if (MainTabControl.SelectedIndex == 2)
        {
            await Task.Delay(120);
            SendSafeZonesToMap(_safeZones);
        }
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateSyncTime();
        EnsureAllTabsLoaded();

        await SetupMapAsync();
        await SetupSafeZoneMapAsync();

        await _apiService.BootstrapDataAsync();

        await LoadEarthquakesAsync();
        await LoadBuildingsAsync();
        await LoadSafeZonesAsync();

        BindAllTabGrids();
        await RefreshMapsLayoutAsync();
    }

    /// <summary>
    /// WPF TabControl lazy-load: tüm sekmeleri açılışta oluşturur.
    /// </summary>
    private void EnsureAllTabsLoaded()
    {
        if (MainTabControl.Items.Count == 0)
            return;

        var originalIndex = MainTabControl.SelectedIndex;
        for (var i = 0; i < MainTabControl.Items.Count; i++)
        {
            MainTabControl.SelectedIndex = i;
            MainTabControl.UpdateLayout();
        }
        MainTabControl.SelectedIndex = originalIndex >= 0 ? originalIndex : 0;
    }

    private void BindAllTabGrids()
    {
        EarthquakesGrid.ItemsSource = null;
        EarthquakesGrid.ItemsSource = _earthquakes;

        BuildingsGrid.ItemsSource = null;
        BuildingsGrid.ItemsSource = _buildings;

        SafeZonesGrid.ItemsSource = null;
        SafeZonesGrid.ItemsSource = _safeZones;

        RefreshSafeZoneOccupancyUi();
    }

    private async void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        await RefreshMapsLayoutAsync();
    }

    private async Task RefreshMapsLayoutAsync()
    {
        await Task.Delay(150);
        await SendMapAsync();
        SendSafeZonesToMap(_safeZones);
    }

    private void UpdateSyncTime()
    {
        LastSyncText.Text = _apiService.IsBackendReachable
            ? $"Son Senkronizasyon: {DateTime.Now:HH:mm}"
            : "Backend bağlantısı bekleniyor";
    }

    private async Task SetupMapAsync()
    {
        await MapView.EnsureCoreWebView2Async();

        string mapPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "map.html");

        if (!File.Exists(mapPath))
        {
            MessageBox.Show($"map.html bulunamadı:\n{mapPath}");
            return;
        }

        var mapLoaded = new TaskCompletionSource<bool>();

        MapView.NavigationCompleted += async (_, args) =>
        {
            if (!args.IsSuccess)
                return;

            _mapPageReady = true;
            mapLoaded.TrySetResult(true);

            if (_pendingMapRefresh || _buildings.Count > 0)
            {
                _pendingMapRefresh = false;
                await SendMapAsync();
            }
        };

        MapView.WebMessageReceived += async (sender, e) =>
        {
            try
            {
                var json = e.WebMessageAsJson;
                using var doc = JsonDocument.Parse(json);
                var type = doc.RootElement.GetProperty("type").GetString();

                if (type == "mapReady")
                {
                    _mapPageReady = true;
                    if (_buildings.Count > 0)
                        await SendMapAsync();
                    return;
                }

                if (type == "buildingClicked")
                {
                    int id = doc.RootElement.GetProperty("id").GetInt32();

                    var selected = _buildings.FirstOrDefault(b => b.Id == id);

                    if (selected != null)
                    {
                        BuildingsGrid.SelectedItem = selected;
                        BuildingsGrid.ScrollIntoView(selected);
                        FocusBuildingOnMap(selected);
                        await ShowAnalysisAsync(selected);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Harita tıklama hatası: {ex.Message}");
            }
        };

        MapView.Source = new Uri(mapPath);
        await mapLoaded.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }

    private async Task SetupSafeZoneMapAsync()
    {
        await SafeZoneMapView.EnsureCoreWebView2Async();

        string mapPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "safezones.html");

        if (!File.Exists(mapPath))
        {
            MessageBox.Show($"safezones.html bulunamadı:\n{mapPath}");
            return;
        }

        SafeZoneMapView.Source = new Uri(mapPath);

        SafeZoneMapView.NavigationCompleted += (_, _) =>
        {
            _safeZoneMapPageReady = true;
            if (_pendingSafeZones != null)
                SendSafeZonesToMap(_pendingSafeZones);
            else if (_safeZones.Count > 0)
                SendSafeZonesToMap(_safeZones);
        };

        SafeZoneMapView.WebMessageReceived += (sender, e) =>
        {
            try
            {
                var json = e.WebMessageAsJson;
                using var doc = JsonDocument.Parse(json);
                var type = doc.RootElement.GetProperty("type").GetString();

                if (type == "safeZoneMapReady")
                {
                    _safeZoneMapPageReady = true;
                    SendSafeZonesToMap(_safeZones);
                    return;
                }

                if (type == "safeZoneClicked")
                {
                    int id = doc.RootElement.GetProperty("id").GetInt32();

                    var selected = _safeZones.FirstOrDefault(z => z.Id == id);

                    if (selected != null)
                    {
                        SafeZonesGrid.SelectedItem = selected;
                        SafeZonesGrid.ScrollIntoView(selected);
                        FocusSafeZoneOnMap(selected);
                        ShowSafeZoneAnalysis(selected);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Güvenli alan harita tıklama hatası: {ex.Message}");
            }
        };
    }

    private async Task LoadEarthquakesAsync()
    {
        try
        {
            _earthquakes = await _apiService.GetEarthquakesAsync();

            EarthquakesGrid.ItemsSource = _earthquakes;

            UpdateEarthquakeStats(_earthquakes);
            CheckEmergencyMode(_earthquakes);
            UpdateEarthquakeAiSummary(_earthquakes);
            UpdateSyncTime();
        }
        catch
        {
            ApplyEmptyEarthquakesState();
        }
    }

    private void ApplyEmptyEarthquakesState()
    {
        _earthquakes = [];
        EarthquakesGrid.ItemsSource = _earthquakes;
        UpdateEarthquakeStats(_earthquakes);
        ResetEmergencyUi();
        UpdateEarthquakeAiSummary(_earthquakes);
        UpdateSyncTime();
    }

    private void UpdateEarthquakeStats(List<Earthquake> earthquakes)
    {
        TotalEarthquakeText.Text = earthquakes.Count.ToString();

        MaxMagnitudeText.Text = earthquakes.Any()
            ? earthquakes.Max(e => e.Buyukluk).ToString("0.0")
            : "0.0";

        AverageDepthText.Text = earthquakes.Any()
            ? $"{earthquakes.Average(e => e.Derinlik):0.0} km"
            : "0 km";

        LastUpdateText.Text = DateTime.Now.ToString("HH:mm");
    }

    private void UpdateEarthquakeAiSummary(List<Earthquake> earthquakes)
    {
        if (earthquakes == null || earthquakes.Count == 0)
        {
            EarthquakeAiSummaryText.Text =
                "Henüz deprem verisi alınamadı. Sistem veri akışını bekliyor.";
            return;
        }

        var maxEarthquake = earthquakes.OrderByDescending(e => e.Buyukluk).First();
        var countAboveFour = earthquakes.Count(e => e.Buyukluk >= 4.0);

        if (maxEarthquake.Buyukluk >= 4.5)
        {
            EarthquakeAiSummaryText.Text =
                $"Son deprem akışında {maxEarthquake.Yer} bölgesinde {maxEarthquake.Buyukluk:0.0} büyüklüğünde kritik bir deprem algılandı. " +
                "Sistem, riskli bina analizi ve güvenli alan yönlendirmesinin aktif olarak takip edilmesini önerir.";
        }
        else if (countAboveFour > 0)
        {
            EarthquakeAiSummaryText.Text =
                $"Son verilerde 4.0 üzeri {countAboveFour} deprem tespit edildi. " +
                "Şu an için acil müdahale modu tetiklenmedi ancak riskli bölgeler izlenmelidir.";
        }
        else
        {
            EarthquakeAiSummaryText.Text =
                "Son deprem verilerine göre kritik büyüklükte bir deprem algılanmadı. Sistem normal izleme modunda çalışmaktadır.";
        }
    }

    private void CheckEmergencyMode(List<Earthquake> earthquakes)
    {
        if (earthquakes == null || earthquakes.Count == 0)
        {
            ResetEmergencyUi();
            return;
        }

        var latest = earthquakes.OrderByDescending(e => e.Buyukluk).First();

        if (latest.Buyukluk >= 4.5)
        {
            EmergencyBanner.Visibility = Visibility.Visible;

            EmergencyDescriptionText.Text =
                $"{latest.Yer} bölgesinde {latest.Buyukluk:0.0} büyüklüğünde deprem algılandı. " +
                "Tahliye ve risk analizi önerilmektedir.";

            Background = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#1F1111"));
        }
        else
        {
            ResetEmergencyUi();
        }
    }

    private void ResetEmergencyUi()
    {
        EmergencyBanner.Visibility = Visibility.Collapsed;
        Background = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString("#111827"));
    }

    private async Task LoadBuildingsAsync()
    {
        try
        {
            _buildings = await _apiService.GetBuildingsAsync();

            BuildingsGrid.ItemsSource = _buildings;

            UpdateDashboardStats(_buildings);
            await SendMapAsync();

            if (_buildings.Count > 0)
            {
                BuildingsGrid.SelectedIndex = 0;
                await ShowAnalysisAsync(_buildings[0]);
            }

            UpdateSyncTime();
        }
        catch
        {
            _buildings = [];
            BuildingsGrid.ItemsSource = _buildings;
            UpdateDashboardStats(_buildings);
            await SendMapAsync();
            UpdateSyncTime();
        }
    }

    private Task SendMapAsync()
    {
        if (MapView.CoreWebView2 == null)
            return Task.CompletedTask;

        if (!_mapPageReady)
        {
            _pendingMapRefresh = true;
            return Task.CompletedTask;
        }

        var message = MapMessageHelper.Create(_buildings);
        MapView.CoreWebView2.PostWebMessageAsJson(MapMessageHelper.Serialize(message));

        if (BuildingsGrid.SelectedItem is Building selected)
            FocusBuildingOnMap(selected);

        return Task.CompletedTask;
    }

    private void FocusBuildingOnMap(Building building)
    {
        if (MapView.CoreWebView2 == null || !_mapPageReady)
            return;
        if (building.Enlem == 0 && building.Boylam == 0)
            return;

        var payload = JsonSerializer.Serialize(new
        {
            type = "focusBuilding",
            id = building.Id,
            enlem = building.Enlem,
            boylam = building.Boylam
        });
        MapView.CoreWebView2.PostWebMessageAsJson(payload);
    }

    private void UpdateDashboardStats(List<Building> buildings)
    {
        TotalBuildingText.Text = buildings.Count.ToString();

        EmergencyBuildingText.Text = buildings
            .Count(b => b.RiskSeviyesi == "Acil Müdahale")
            .ToString();

        HighRiskBuildingText.Text = buildings
            .Count(b => b.RiskSeviyesi == "Yüksek Risk")
            .ToString();

        AverageRiskText.Text = buildings.Any()
            ? buildings.Average(b => b.RiskSkoru).ToString("0.0")
            : "0";
    }

    private async void BuildingsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (BuildingsGrid.SelectedItem is not Building selected)
            return;

        await ShowAnalysisAsync(selected);
    }

    private async Task ShowAnalysisAsync(Building selected)
    {
        try
        {
            Building analyzed = selected;

            var fromApi = await _apiService.AnalyzeBuildingAsync(selected.Id);
            if (fromApi != null)
            {
                analyzed = fromApi;
                var idx = _buildings.FindIndex(b => b.Id == analyzed.Id);
                if (idx >= 0)
                    _buildings[idx] = analyzed;
            }

            SelectedBuildingText.Text = string.IsNullOrWhiteSpace(analyzed.Adres)
                ? $"{analyzed.Mahalle} (Bina #{analyzed.Id})"
                : analyzed.Adres;

            RiskScoreText.Text = analyzed.RiskSkoru.ToString("0.0");
            RiskScoreText.Foreground = analyzed.RiskSeviyesi switch
            {
                "Acil Müdahale" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
                "Yüksek Risk" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F97316")),
                "Orta Risk" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FACC15")),
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E"))
            };

            ActionText.Text = string.IsNullOrWhiteSpace(analyzed.Oneri)
                ? "Belediye saha ekibi yapı güvenliği kontrolü planlamalıdır."
                : analyzed.Oneri;

            AiExplanationText.Text = BuildingAiHelper.GetAiSummary(analyzed);
            StructuralAiText.Text = BuildingAiHelper.GetStructuralAnalysis(analyzed);

            UpdateAdvancedAiAnalysis(analyzed);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Analiz hatası: {ex.Message}");
        }
    }

    private void UpdateAdvancedAiAnalysis(Building building)
    {
        double confidence = BuildingAiHelper.GetConfidence(building);

        AiConfidenceBar.Value = confidence;
        AiConfidenceText.Text = $"%{confidence:0} Güvenilirlik";

        DecisionFactorsText.Text = string.Join(
            "\n",
            BuildingAiHelper.GetDecisionFactors(building).Select(f => f.StartsWith("✓") ? f : $"✓ {f}"));

        CriticalPopulationText.Text =
            "⚠ Bu binada:\n" +
            $"- {building.YasliSayisi} yaşlı\n" +
            $"- {building.EngelliSayisi} engelli\n" +
            $"- {building.CocukSayisi} çocuk bulunuyor.";

        var (evacLabel, evacColor) = BuildingAiHelper.GetEvacuationDifficulty(building);
        EvacuationDifficultyText.Text = evacLabel;
        EvacuationDifficultyText.Foreground =
            new SolidColorBrush((Color)ColorConverter.ConvertFromString(evacColor));
    }

    private async Task LoadSafeZonesAsync()
    {
        try
        {
            _safeZones = await _apiService.GetSafeZonesAsync();

            SafeZonesGrid.ItemsSource = _safeZones;

            TotalSafeZoneText.Text = _safeZones.Count.ToString();
            TotalCapacityText.Text = _safeZones.Sum(z => z.Kapasite).ToString();

            AverageSafeScoreText.Text = _safeZones.Any()
                ? _safeZones.Average(z => z.GuvenlikSkoru).ToString("0.0")
                : "0";

            RefreshSafeZoneOccupancyUi();
            SendSafeZonesToMap(_safeZones);

            if (_safeZones.Count > 0)
            {
                SafeZonesGrid.SelectedIndex = 0;
                FocusSafeZoneOnMap(_safeZones[0]);
                ShowSafeZoneAnalysis(_safeZones[0]);
            }

        }
        catch
        {
            ApplyEmptySafeZonesState();
        }
    }

    private void ApplyEmptySafeZonesState()
    {
        _safeZones = [];
        SafeZonesGrid.ItemsSource = _safeZones;
        TotalSafeZoneText.Text = "0";
        TotalCapacityText.Text = "0";
        AverageSafeScoreText.Text = "0";
        RefreshSafeZoneOccupancyUi();
        SendSafeZonesToMap(_safeZones);
        UpdateSyncTime();
    }

    private void SendSafeZonesToMap(List<SafeZone> safeZones)
    {
        if (SafeZoneMapView.CoreWebView2 == null)
        {
            _pendingSafeZones = safeZones;
            return;
        }

        if (!_safeZoneMapPageReady)
        {
            _pendingSafeZones = safeZones;
            return;
        }

        _pendingSafeZones = null;
        var json = JsonSerializer.Serialize(safeZones);
        SafeZoneMapView.CoreWebView2.PostWebMessageAsJson(json);

        if (SafeZonesGrid.SelectedItem is SafeZone selected)
            FocusSafeZoneOnMap(selected);
    }

    private void FocusSafeZoneOnMap(SafeZone zone)
    {
        if (SafeZoneMapView.CoreWebView2 == null || !_safeZoneMapPageReady)
            return;
        if (zone.Enlem == 0 && zone.Boylam == 0)
            return;

        var payload = JsonSerializer.Serialize(new
        {
            type = "focusSafeZone",
            id = zone.Id,
            enlem = zone.Enlem,
            boylam = zone.Boylam
        });
        SafeZoneMapView.CoreWebView2.PostWebMessageAsJson(payload);
    }

    private void ShowSafeZoneAnalysis(SafeZone selected)
    {
        int percent = OccupancyHelper.GetOccupancyPercent(selected);

        SelectedSafeZoneText.Text = $"{selected.Ad} - {selected.Mahalle}";
        SafeZoneScoreText.Text = selected.GuvenlikSkoru.ToString("0.0");
        SafeZoneAiExplanationText.Text = string.IsNullOrWhiteSpace(selected.AiAciklama)
            ? $"{selected.Ad}: Alan profili yüklenemedi."
            : selected.AiAciklama;

        SafeZoneOccupancyLabel.Text = $"{selected.Ad} Doluluk";
        SafeZoneOccupancyPercentText.Text = $"%{percent}";
        SafeZoneOccupancyPercentText.Foreground = OccupancyHelper.GetOccupancyBrush(percent);
        SafeZoneOccupancyBar.Value = percent;
        SafeZoneOccupancyBar.Foreground = OccupancyHelper.GetOccupancyBrush(percent);
        SafeZoneOccupancyLevelText.Text =
            $"Seviye: {OccupancyHelper.GetOccupancyLevel(percent)} • {selected.Doluluk}/{selected.Kapasite} kişi";

        SafeZoneOccupancyAiText.Text = string.IsNullOrWhiteSpace(selected.AiDoluluk)
            ? OccupancyHelper.GetOccupancyAiMessage(percent, selected.Ad)
            : selected.AiDoluluk;

        double confidence = Math.Min(98, 55 + selected.GuvenlikSkoru * 0.35 + (100 - percent) * 0.15);
        SafeZoneConfidenceBar.Value = confidence;
        SafeZoneConfidenceText.Text = $"%{confidence:0} Güvenilirlik";

        var factors = new List<string>();
        if (percent >= 80)
            factors.Add("✓ Kritik doluluk seviyesi");
        else if (percent >= 50)
            factors.Add("✓ Artan doluluk trendi");
        else
            factors.Add("✓ Uygun kapasite durumu");

        if (selected.GuvenlikSkoru >= 80)
            factors.Add("✓ Yüksek güvenlik skoru");
        if (selected.UlasimDurumu.Contains("Açık", StringComparison.OrdinalIgnoreCase) ||
            selected.UlasimDurumu.Contains("Uygun", StringComparison.OrdinalIgnoreCase))
            factors.Add("✓ Ulaşım yolu açık");

        SafeZoneFactorsText.Text = string.Join("\n", factors);

        if (percent >= 80)
        {
            SafeZonePriorityText.Text = "Alternatif Alan Öner";
            SafeZonePriorityText.Foreground =
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
        }
        else if (percent >= 50)
        {
            SafeZonePriorityText.Text = "Kontrollü Yönlendirme";
            SafeZonePriorityText.Foreground =
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FACC15"));
        }
        else
        {
            SafeZonePriorityText.Text = "Öncelikli Yönlendirme";
            SafeZonePriorityText.Foreground =
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E"));
        }

        SafeZoneReasonText.Text = string.IsNullOrWhiteSpace(selected.YonlendirmeGerekce)
            ? $"{selected.Ad}: Güvenlik skoru {selected.GuvenlikSkoru:0.0}, doluluk %{percent}."
            : selected.YonlendirmeGerekce;
    }

    private void RefreshSafeZoneOccupancyUi()
    {
        if (_safeZones.Count == 0)
        {
            AverageOccupancyText.Text = "%0";
            OccupancyOverviewPanel.Children.Clear();
            return;
        }

        int average = (int)_safeZones.Average(z => OccupancyHelper.GetOccupancyPercent(z));
        AverageOccupancyText.Text = $"%{average}";
        AverageOccupancyText.Foreground = OccupancyHelper.GetOccupancyBrush(average);

        OccupancyOverviewPanel.Children.Clear();

        SafeZonesGrid.Items.Refresh();

        foreach (var zone in _safeZones.OrderByDescending(z => OccupancyHelper.GetOccupancyPercent(z)).Take(5))
        {
            int percent = OccupancyHelper.GetOccupancyPercent(zone);
            var row = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });

            var nameText = new TextBlock
            {
                Text = zone.Ad,
                Foreground = Brushes.White,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(nameText, 0);
            row.Children.Add(nameText);

            var percentText = new TextBlock
            {
                Text = $"%{percent}",
                Foreground = OccupancyHelper.GetOccupancyBrush(percent),
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(percentText, 1);
            row.Children.Add(percentText);

            var bar = new ProgressBar
            {
                Value = percent,
                Maximum = 100,
                Height = 10,
                Margin = new Thickness(0, 4, 0, 0),
                Foreground = OccupancyHelper.GetOccupancyBrush(percent)
            };
            Grid.SetColumnSpan(bar, 2);

            var container = new StackPanel();
            container.Children.Add(row);
            container.Children.Add(bar);

            OccupancyOverviewPanel.Children.Add(container);
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadBuildingsAsync();
    }

    private void SafeZonesGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (SafeZonesGrid.SelectedItem is not SafeZone selected)
            return;

        ShowSafeZoneAnalysis(selected);
    }

    private async void RefreshSafeZonesButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadSafeZonesAsync();
    }

}
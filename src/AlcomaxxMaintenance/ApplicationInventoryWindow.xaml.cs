using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using AlcomaxxMaintenance.Models;
using AlcomaxxMaintenance.Services;

namespace AlcomaxxMaintenance;

public partial class ApplicationInventoryWindow : Window
{
    private readonly JobState _job;
    private readonly UsbWorkspace _workspace;
    private readonly ApplicationInventoryService _inventory = new();
    public ObservableCollection<InstalledApplication> Applications { get; } = [];
    public ICollectionView ApplicationsView { get; }

    public ApplicationInventoryWindow(JobState job, UsbWorkspace workspace)
    {
        _job = job; _workspace = workspace;
        ApplicationsView = CollectionViewSource.GetDefaultView(Applications);
        ApplicationsView.Filter = Filter;
        InitializeComponent(); DataContext = this;
    }

    private async void Scan_Click(object sender, RoutedEventArgs e)
    {
        ScanButton.IsEnabled = false; StatusText.Text = "Analizando…"; Applications.Clear();
        try
        {
            foreach (var app in await _inventory.ScanAsync()) { app.IsSelected = _job.SelectedApplicationIds.Contains(app.Id); Applications.Add(app); }
            StatusText.Text = $"{Applications.Count} aplicaciones encontradas. Ninguna se selecciona automáticamente.";
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "No se pudo completar el análisis", MessageBoxButton.OK, MessageBoxImage.Error); }
        finally { ScanButton.IsEnabled = true; SaveButton.IsEnabled = Applications.Count > 0; UpdateCount(); }
    }

    private bool Filter(object value) => value is InstalledApplication app && (string.IsNullOrWhiteSpace(SearchBox?.Text) || $"{app.Name} {app.Publisher} {app.Version}".Contains(SearchBox.Text, StringComparison.CurrentCultureIgnoreCase));
    private void Search_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e) => ApplicationsView.Refresh();
    private void Selection_Changed(object sender, RoutedEventArgs e) => UpdateCount();
    private void UpdateCount() => SelectedText.Text = $"{Applications.Count(x => x.IsSelected)} seleccionadas";

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var selected = Applications.Where(x => x.IsSelected).ToArray();
        var summary = selected.Length == 0 ? "No se ha seleccionado ninguna aplicación." : string.Join("\n", selected.Take(10).Select(x => $"• {x.Name}")) + (selected.Length > 10 ? $"\n… y {selected.Length - 10} más" : "");
        if (MessageBox.Show($"Revise la selección antes de guardarla:\n\n{summary}\n\n¿Desea continuar?", "Confirmar selección", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        _job.SelectedApplicationIds = selected.Select(x => x.Id).ToList();
        _job.SelectedApplications = selected.Select(x => new SelectedApplication { Id = x.Id, Name = x.Name, Publisher = x.Publisher, Version = x.Version, Source = x.Source, Category = x.Category }).ToList();
        _job.CurrentStep = 2; var path = _workspace.SaveJob(_job);
        MessageBox.Show($"Selección guardada en el USB.\n{path}\n\nNo se ha desinstalado nada.", "ALCOMAXX", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}

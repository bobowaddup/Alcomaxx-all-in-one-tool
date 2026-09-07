using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using AlcomaxxMaintenance.Models;
using AlcomaxxMaintenance.Services;
using Microsoft.Win32;

namespace AlcomaxxMaintenance;

public partial class MainWindow : Window
{
    private readonly UsbWorkspace _workspace;
    public ObservableCollection<WorkflowStep> Steps { get; } = new();
    public ObservableCollection<string> Technicians { get; } = new();
    public string ComputerName => Environment.MachineName;
    public string OsDescription => RuntimeInformation.OSDescription;

    public MainWindow()
    {
        InitializeComponent();
        _workspace = new UsbWorkspace(AppContext.BaseDirectory);
        try
        {
            _workspace.Initialize();
            foreach (var name in _workspace.LoadTechnicians()) Technicians.Add(name);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo preparar la unidad USB:\n{ex.Message}", "ALCOMAXX", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        AddSteps();
        DataContext = this;
        if (Technicians.Count > 0) Technician.SelectedIndex = 0;
    }

    private void AddSteps()
    {
        Steps.Add(new("1", "Inicio", "Datos del servicio"));
        Steps.Add(new("2", "Aplicaciones", "Análisis y selección"));
        Steps.Add(new("3", "Comprobación del disco", "CHKDSK y reinicio"));
        Steps.Add(new("4", "Limpieza inicial", "Archivos temporales"));
        Steps.Add(new("5", "BleachBit", "Limpieza confirmada"));
        Steps.Add(new("6", "ZHP Cleaner", "Análisis interactivo"));
        Steps.Add(new("7", "CCleaner", "Registro e inicio"));
        Steps.Add(new("8", "Reparación de Windows", "DISM y SFC"));
        Steps.Add(new("9", "Limpieza final", "Sistema y restauración"));
        Steps.Add(new("10", "Informe", "Resumen del trabajo"));
    }

    private void CreateJob_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SatNumber.Text) || string.IsNullOrWhiteSpace(ClientName.Text) || Technician.SelectedItem is null)
        {
            System.Media.SystemSounds.Exclamation.Play();
            MessageBox.Show("Complete el número de SAT, el cliente y el técnico.", "Faltan datos", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var job = new JobState
        {
            SatNumber = SatNumber.Text.Trim(),
            ClientName = ClientName.Text.Trim(),
            Technician = Technician.SelectedItem.ToString()!,
            StartedAt = DateTime.UtcNow,
            CurrentStep = 1
        };
        _workspace.SaveJob(job);
        var inventory = new ApplicationInventoryWindow(job, _workspace) { Owner = this };
        inventory.ShowDialog();
    }

    private void Resume_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { InitialDirectory = _workspace.JobsDirectory, Filter = "Trabajos ALCOMAXX (*.json)|*.json", Title = "Reanudar trabajo" };
        if (dialog.ShowDialog(this) == true)
            MessageBox.Show($"Trabajo seleccionado:\n{dialog.FileName}\n\nLa reanudación completa se conectará al motor del flujo en el siguiente módulo.", "Reanudar trabajo", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}

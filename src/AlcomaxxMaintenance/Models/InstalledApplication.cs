using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AlcomaxxMaintenance.Models;

public sealed class InstalledApplication : INotifyPropertyChanged
{
    private bool _isSelected;
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string Publisher { get; init; } = "";
    public string Version { get; init; } = "";
    public string UninstallCommand { get; init; } = "";
    public string Source { get; init; } = "Escritorio";
    public required string Category { get; init; }
    public bool IsProtected { get; init; }
    public bool CanSelect => !IsProtected && !string.IsNullOrWhiteSpace(UninstallCommand);
    public bool IsSelected { get => _isSelected; set { var next = CanSelect && value; if (_isSelected == next) return; _isSelected = next; PropertyChanged?.Invoke(this, new(nameof(IsSelected))); } }
    public event PropertyChangedEventHandler? PropertyChanged;
}

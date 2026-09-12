namespace AlcomaxxMaintenance.Models;

public sealed class JobState
{
    public int SchemaVersion { get; set; } = 1;
    public string SatNumber { get; set; } = "";
    public string ClientName { get; set; } = "";
    public string Technician { get; set; } = "";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "En curso";
    public int CurrentStep { get; set; }
    public List<string> SelectedApplicationIds { get; set; } = [];
    public List<SelectedApplication> SelectedApplications { get; set; } = [];
}

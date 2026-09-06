namespace AlcomaxxMaintenance.Models;

public sealed class JobState
{
    public string SatNumber { get; set; } = "";
    public string ClientName { get; set; } = "";
    public string Technician { get; set; } = "";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public int CurrentStep { get; set; }
}

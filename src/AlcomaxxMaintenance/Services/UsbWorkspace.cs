using System.Text.Json;
using AlcomaxxMaintenance.Models;

namespace AlcomaxxMaintenance.Services;

public sealed class UsbWorkspace
{
    private readonly string _root;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public UsbWorkspace(string executableDirectory)
    {
        _root = executableDirectory;
    }

    public string ConfigDirectory => Path.Combine(_root, "Config");
    public string JobsDirectory => Path.Combine(_root, "Jobs");
    public string ReportsDirectory => Path.Combine(_root, "Reports");
    public string ToolsDirectory => Path.Combine(_root, "Tools");

    public void Initialize()
    {
        Directory.CreateDirectory(ConfigDirectory);
        Directory.CreateDirectory(JobsDirectory);
        Directory.CreateDirectory(ReportsDirectory);
        Directory.CreateDirectory(ToolsDirectory);

        CopyTemplateWhenMissing("technicians.txt");
        var technicians = Path.Combine(ConfigDirectory, "technicians.txt");
        if (!File.Exists(technicians))
            File.WriteAllText(technicians, "Técnico 1" + Environment.NewLine);

        var usbId = Path.Combine(ConfigDirectory, "usb-id.txt");
        if (!File.Exists(usbId))
            File.WriteAllText(usbId, Guid.NewGuid().ToString("D"));

        var bleachBitConfig = Path.Combine(ConfigDirectory, "bleachbit.ini");
        var bleachBitTemplate = Path.Combine(_root, "Templates", "bleachbit.ini");
        if (!File.Exists(bleachBitConfig) && File.Exists(bleachBitTemplate))
            File.Copy(bleachBitTemplate, bleachBitConfig);

        CopyTemplateWhenMissing("tools.json");
    }

    private void CopyTemplateWhenMissing(string fileName)
    {
        var destination = Path.Combine(ConfigDirectory, fileName);
        var template = Path.Combine(_root, "Templates", fileName);
        if (!File.Exists(destination) && File.Exists(template))
            File.Copy(template, destination);
    }

    public IReadOnlyList<string> LoadTechnicians() =>
        File.ReadAllLines(Path.Combine(ConfigDirectory, "technicians.txt"))
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

    public string SaveJob(JobState job)
    {
        var safeId = string.Concat(job.SatNumber.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_'));
        if (string.IsNullOrWhiteSpace(safeId)) safeId = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var path = Path.Combine(JobsDirectory, $"{safeId}.json");
        job.LastUpdatedAt = DateTime.UtcNow;
        var temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(job, JsonOptions));
        File.Move(temporaryPath, path, true);
        return path;
    }

    public JobState LoadJob(string path)
    {
        var jobsRoot = Path.GetFullPath(JobsDirectory) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(path);
        if (!fullPath.StartsWith(jobsRoot, StringComparison.OrdinalIgnoreCase) || !fullPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("El archivo seleccionado no pertenece a la carpeta Jobs de esta unidad.");
        var job = JsonSerializer.Deserialize<JobState>(File.ReadAllText(fullPath))
            ?? throw new InvalidDataException("El trabajo está vacío o dañado.");
        if (job.SchemaVersion != 1 || string.IsNullOrWhiteSpace(job.SatNumber))
            throw new InvalidDataException("El formato del trabajo no es compatible.");
        return job;
    }
}

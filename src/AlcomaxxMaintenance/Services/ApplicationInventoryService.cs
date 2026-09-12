using AlcomaxxMaintenance.Models;
using Microsoft.Win32;
using System.Diagnostics;
using System.Text.Json;

namespace AlcomaxxMaintenance.Services;

public sealed class ApplicationInventoryService
{
    private static readonly string[] Security = ["avast", "avg", "avira", "bitdefender", "mcafee", "norton", "kaspersky", "eset", "panda", "malwarebytes", "trend micro", "sophos", "webroot", "totalav"];
    private static readonly string[] Removable = ["news", "weather", "xbox", "clipchamp", "solitaire", "feedback hub", "get help", "mixed reality"];
    private static readonly string[] Review = ["office", "onedrive", "teams", "outlook", "copilot", "phone link", "your phone"];
    private static readonly string[] Protected = ["visual c++", "webview2", "windows app runtime", ".net", "windows security", "windows desktop runtime", "windows sdk", "driver"];

    public async Task<IReadOnlyList<InstalledApplication>> ScanAsync()
    {
        var results = await Task.Run(() =>
        {
            var desktop = new List<InstalledApplication>();
            var views = Environment.Is64BitOperatingSystem ? new[] { RegistryView.Registry64, RegistryView.Registry32 } : [RegistryView.Registry32];
            foreach (var hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
            foreach (var view in views) ReadRegistry(hive, view, desktop);
            return desktop;
        });
        results.AddRange(await ReadStoreAppsAsync());
        return results.GroupBy(x => $"{x.Name}|{x.Publisher}|{x.Version}", StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First()).OrderBy(x => x.Category).ThenBy(x => x.Name).ToArray();
    }

    private static async Task<IReadOnlyList<InstalledApplication>> ReadStoreAppsAsync()
    {
        const string query = "Get-AppxPackage | Select-Object Name,PackageFullName,Publisher,Version,NonRemovable | ConvertTo-Json -Compress";
        using var process = new Process { StartInfo = new() { FileName = "powershell.exe", Arguments = $"-NoProfile -NonInteractive -Command \"{query}\"", UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true } };
        try
        {
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output)) return [];
            using var json = JsonDocument.Parse(output);
            var entries = json.RootElement.ValueKind == JsonValueKind.Array ? json.RootElement.EnumerateArray().ToArray() : [json.RootElement];
            return entries.Select(CreateStoreApp).Where(x => x is not null).Cast<InstalledApplication>().ToArray();
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or JsonException) { return []; }
    }

    private static InstalledApplication? CreateStoreApp(JsonElement entry)
    {
        var name = entry.TryGetProperty("Name", out var p) ? p.GetString() : null;
        var package = entry.TryGetProperty("PackageFullName", out p) ? p.GetString() : null;
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(package)) return null;
        var publisher = entry.TryGetProperty("Publisher", out p) ? p.GetString() ?? "" : "";
        var nonRemovable = entry.TryGetProperty("NonRemovable", out p) && p.ValueKind == JsonValueKind.True;
        var classification = nonRemovable ? ("Componentes protegidos", true) : Classify(name, publisher);
        return new() { Id = $"appx:{package}", Name = name, Publisher = publisher, Version = entry.TryGetProperty("Version", out p) ? p.ToString() : "", Source = "Microsoft Store", UninstallCommand = classification.Item2 ? "" : $"Remove-AppxPackage -Package '{package.Replace("'", "''")}'", Category = classification.Item1, IsProtected = classification.Item2 };
    }

    private static void ReadRegistry(RegistryHive hive, RegistryView view, List<InstalledApplication> results)
    {
        try
        {
            using var root = RegistryKey.OpenBaseKey(hive, view);
            using var uninstall = root.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            if (uninstall is null) return;
            foreach (var subkeyName in uninstall.GetSubKeyNames())
            {
                using var key = uninstall.OpenSubKey(subkeyName);
                var name = key?.GetValue("DisplayName") as string;
                if (string.IsNullOrWhiteSpace(name) || Convert.ToInt32(key?.GetValue("SystemComponent", 0)) == 1) continue;
                var publisher = key!.GetValue("Publisher") as string ?? "";
                var command = key.GetValue("QuietUninstallString") as string ?? key.GetValue("UninstallString") as string ?? "";
                var (category, isProtected) = Classify(name, publisher);
                results.Add(new() { Id = $"{hive}:{view}:{subkeyName}", Name = name.Trim(), Publisher = publisher, Version = key.GetValue("DisplayVersion") as string ?? "", UninstallCommand = isProtected ? "" : command, Category = category, IsProtected = isProtected });
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException) { }
    }

    private static (string Category, bool Protected) Classify(string name, string publisher)
    {
        var value = $"{name} {publisher}".ToLowerInvariant();
        if (Protected.Any(value.Contains)) return ("Componentes protegidos", true);
        if (Security.Any(value.Contains)) return ("Antivirus y seguridad", false);
        if (Removable.Any(value.Contains)) return ("Microsoft · normalmente eliminable", false);
        if (Review.Any(value.Contains)) return ("Microsoft · revisar", false);
        return ("Otras aplicaciones", false);
    }
}

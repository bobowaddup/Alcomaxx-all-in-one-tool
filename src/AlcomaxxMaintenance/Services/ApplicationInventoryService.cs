using AlcomaxxMaintenance.Models;
using Microsoft.Win32;

namespace AlcomaxxMaintenance.Services;

public sealed class ApplicationInventoryService
{
    private static readonly string[] Security = ["avast", "avg", "avira", "bitdefender", "mcafee", "norton", "kaspersky", "eset", "panda", "malwarebytes", "trend micro", "sophos", "webroot", "totalav"];
    private static readonly string[] Removable = ["news", "weather", "xbox", "clipchamp", "solitaire", "feedback hub", "get help", "mixed reality"];
    private static readonly string[] Review = ["office", "onedrive", "teams", "outlook", "copilot", "phone link", "your phone"];
    private static readonly string[] Protected = ["visual c++", "webview2", "windows app runtime", ".net", "windows security", "windows desktop runtime", "windows sdk", "driver"];

    public Task<IReadOnlyList<InstalledApplication>> ScanAsync() => Task.Run<IReadOnlyList<InstalledApplication>>(() =>
    {
        var results = new List<InstalledApplication>();
        var views = Environment.Is64BitOperatingSystem ? new[] { RegistryView.Registry64, RegistryView.Registry32 } : [RegistryView.Registry32];
        foreach (var hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
        foreach (var view in views)
            ReadRegistry(hive, view, results);
        return results.GroupBy(x => $"{x.Name}|{x.Publisher}|{x.Version}", StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First()).OrderBy(x => x.Category).ThenBy(x => x.Name).ToArray();
    });

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

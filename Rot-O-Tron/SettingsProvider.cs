using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Text.Json;

internal static class SettingsProvider
{
    public static Settings GetSettings(string[] args)
    {
        string? projectPath = null;
        bool checkMethodLength = false;
        bool checkMagicNumbers = false;

        // Argumente parsen
        foreach (var arg in args)
        {
            if (arg.StartsWith("--path:"))
                projectPath = arg.Substring("--path:".Length);
            else if (arg == "--checkMethodLength")
                checkMethodLength = true;
            else if (arg == "--checkMagicNumbers")
                checkMagicNumbers = true;
        }

        // Settings aus Datei laden, falls nötig
        if (string.IsNullOrEmpty(projectPath) && File.Exists("appsettings.json"))
        {
            try
            {
                var json = File.ReadAllText("appsettings.json");
                var settings = JsonSerializer.Deserialize<Settings>(json);
                if (settings != null)
                {
                    projectPath = settings.ProjectPath ?? projectPath;
                    checkMethodLength = settings.CheckMethodLength || checkMethodLength;
                    checkMagicNumbers = settings.CheckMagicNumbers || checkMagicNumbers;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Laden der Settings: {ex.Message}");
            }
        }

        return new Settings
        {
            ProjectPath = projectPath,
            CheckMethodLength = checkMethodLength,
            CheckMagicNumbers = checkMagicNumbers
        };
    }
}
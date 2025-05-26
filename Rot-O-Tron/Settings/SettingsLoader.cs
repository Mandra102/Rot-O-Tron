using System.Text.Json;

namespace Rot_O_Tron.Settings
{
    internal class SettingsLoader
    {
        public static Settings LoadAndMerge(Settings cliSettings)
        {
            Settings settings = cliSettings;

            if (File.Exists("appsettings.json"))
            {
                try
                {
                    var json = File.ReadAllText("appsettings.json");
                    var appSettings = JsonSerializer.Deserialize<Settings>(json);
                    if (appSettings != null)
                    {
                        settings = MergeSettings(cliSettings, appSettings);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Fehler beim Laden der Settings aus den Appsettings: {ex.Message}");
                }
            }

            return settings;
        }

        public static bool Validate(Settings settings)
        {
            if (string.IsNullOrEmpty(settings.ProjectPath) || !File.Exists(settings.ProjectPath))
            {
                Console.WriteLine("Gültigen Pfad mit --ProjectPath angeben oder in appsettings.json setzen!");
                return false;
            }
            return true;
        }

        private static Settings MergeSettings(Settings primary, Settings fallback)
        {
            var result = new Settings();
            var props = typeof(Settings).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            foreach (var prop in props)
            {
                var primaryValue = prop.GetValue(primary);
                var fallbackValue = prop.GetValue(fallback);

                if (primaryValue != null)
                    prop.SetValue(result, primaryValue);
                else
                    prop.SetValue(result, fallbackValue);
            }
            return result;
        }
    }
}

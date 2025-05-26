using System.CommandLine;

namespace Rot_O_Tron.Settings
{
    internal class ArgumentParser
    {
        public static Settings Parse(string[] args)
        {
            var pathOption = new Option<string>(
                name: "--ProjectPath",
                description: "Pfad zur .csproj Datei"
            );
            var checkMethodLengthOption = new Option<bool?>(
                name: "--checkMethodLength",
                description: "Prüft, ob Methoden zu lang sind"
            );
            var checkMethodLengthCountOption = new Option<int?>(
                name: "--checkMethodLengthDefault",
                description: "Standardwert für checkMethodLength",
                getDefaultValue: () => 40
            );
            var checkMagicNumbersOption = new Option<bool?>(
                name: "--checkMagicNumbers",
                description: "Prüft auf magische Zahlen"
            );
            var countLinesOption = new Option<bool?>(
                name: "--countLines",
                description: "Zählt die Zeilen aller .cs Dateien"
            );
            var checkNugetOption = new Option<bool?>(
                name: "--checkNuget",
                description: "Prüft auf veraltete NuGet-Pakete"
            );
            var checkUnusedUsingsOption = new Option<bool?>(
                name: "--checkUnusedUsings",
                description: "Prüft auf unbenutzte usings"
            );

            var rootCommand = new RootCommand("Rot-O-Tron Code Analyzer");
            rootCommand.AddOption(pathOption);
            rootCommand.AddOption(checkMethodLengthOption);
            rootCommand.AddOption(checkMagicNumbersOption);
            rootCommand.AddOption(checkMethodLengthCountOption);
            rootCommand.AddOption(countLinesOption);
            rootCommand.AddOption(checkNugetOption);
            rootCommand.AddOption(checkUnusedUsingsOption);

            var parseResult = rootCommand.Parse(args);

            return new Settings
            {
                ProjectPath = parseResult.GetValueForOption(pathOption),
                CheckMethodLength = parseResult.GetValueForOption(checkMethodLengthOption),
                CheckMethodLengthDefault = parseResult.GetValueForOption(checkMethodLengthCountOption) ?? 40,
                CheckMagicNumbers = parseResult.GetValueForOption(checkMagicNumbersOption),
                CountLines = parseResult.GetValueForOption(countLinesOption),
                CheckNuget = parseResult.GetValueForOption(checkNugetOption),
                CheckUnusedUsings = parseResult.GetValueForOption(checkUnusedUsingsOption)
            };
        }
    }
}

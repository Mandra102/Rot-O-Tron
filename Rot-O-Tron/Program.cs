using System;
using System.Linq;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.CommandLine;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var pathOption = new Option<string>(
            name: "--ProjectPath",
            description: "Pfad zur .csproj Datei"
        );
        var checkMethodLengthOption = new Option<bool?>(
            name: "--checkMethodLength",
            description: "Prüft, ob Methoden zu lang sind"
        );
        var checkMethodLengthCountOption = new Option<int>(
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

        var rootCommand = new RootCommand("Rot-O-Tron Code Analyzer");

        rootCommand.AddOption(pathOption);
        rootCommand.AddOption(checkMethodLengthOption);
        rootCommand.AddOption(checkMagicNumbersOption);
        rootCommand.AddOption(checkMethodLengthCountOption);
        rootCommand.AddOption(countLinesOption);
        rootCommand.AddOption(checkNugetOption);

        // Argumente parsen
        var parseResult = rootCommand.Parse(args);

        string? projectPath = parseResult.GetValueForOption(pathOption);
        bool? checkMethodLength = parseResult.GetValueForOption(checkMethodLengthOption);
        bool? checkMagicNumbers = parseResult.GetValueForOption(checkMagicNumbersOption);
        int? MethodLengthDefault = parseResult.GetValueForOption(checkMethodLengthCountOption);
        bool? countLines = parseResult.GetValueForOption(countLinesOption);
        bool? checkNuget = parseResult.GetValueForOption(checkNugetOption);

        await LoadAndValidateSettings(projectPath, checkMethodLength, checkMagicNumbers, countLines, checkNuget);

        return 0;
    }

    private static async Task LoadAndValidateSettings(string? projectPath, bool? checkMethodLength, bool? checkMagicNumbers, bool? countLines, bool? checkNuget)
    {
        if ((string.IsNullOrEmpty(projectPath) || checkMethodLength == null || checkMagicNumbers == null) && File.Exists("appsettings.json"))
        {
            try
            {
                var json = File.ReadAllText("appsettings.json");
                var settings = JsonSerializer.Deserialize<Settings>(json);
                if (settings != null)
                {
                    projectPath ??= settings.ProjectPath;
                    checkMethodLength ??= settings.CheckMethodLength;
                    checkMagicNumbers ??= settings.CheckMagicNumbers;
                    countLines ??= settings.CountLines;
                    checkNuget ??= settings.CheckNuget;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Laden der Settings: {ex.Message}");
            }
        }

        if (string.IsNullOrEmpty(projectPath) || !File.Exists(projectPath))
        {
            Console.WriteLine("Gültigen Pfad mit --ProjectPath angeben oder in appsettings.json setzen!");
            return;
        }

        await AnalyzeProject(projectPath, checkMethodLength!.Value, checkMagicNumbers!.Value, countLines!.Value, checkNuget!.Value);
    }

    private static async Task AnalyzeProject(string projectPath, bool checkMethodLength, bool checkMagicNumbers, bool countLines, bool checkNuget)
    {
        List<VisualStudioInstance> instances = MSBuildLocator.QueryVisualStudioInstances().ToList();
        if (!instances.Any())
        {
            Console.WriteLine("Keine MSBuild Instanzen gefunden!");
            return;
        }
        var instance = instances.First();
        MSBuildLocator.RegisterInstance(instance);

        using var workspace = MSBuildWorkspace.Create();
        int totalLines = 0;

        try
        {
            Project project = await workspace.OpenProjectAsync(projectPath);
            Console.WriteLine($"Projekt geladen: {project.Name}");
            Console.WriteLine($"Anzahl Dokumente: {project.DocumentIds.Count}");

            foreach (var document in project.Documents)
            {
                if (document.Name.EndsWith(".Designer.cs")) continue;

                Console.WriteLine($"Datei: {document.Name}");

                if (countLines)
                {
                    var text = await document.GetTextAsync();
                    totalLines += text.Lines.Count;
                }

                SyntaxTree? syntaxTree = await document.GetSyntaxTreeAsync();
                if (syntaxTree == null) continue;

                SyntaxNode root = await syntaxTree.GetRootAsync();
                IEnumerable<MethodDeclarationSyntax> methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

                foreach (var method in methods)
                {
                    AnalyzeMethod(method, checkMethodLength, checkMagicNumbers);
                }
            }

            if (countLines)
            {
                Console.WriteLine($"Gesamtanzahl Codezeilen im Projekt: {totalLines}");
            }

            if (checkNuget)
            {
                await CheckNugetPackages(projectPath);
            }

            Console.ReadKey();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fehler beim Laden des Projekts: {ex.Message}");
        }
    }

    private static void AnalyzeMethod(MethodDeclarationSyntax method, bool checkMethodLength, bool checkMagicNumbers)
    {
        FileLinePositionSpan linesSpan = method.GetLocation().GetLineSpan();
        int length = linesSpan.EndLinePosition.Line - linesSpan.StartLinePosition.Line + 1;

        if (checkMethodLength && length > 40)
        {
            Console.WriteLine($"Methode {method.Identifier.Text} ist {length} Zeilen lang.");
        }

        if (checkMagicNumbers)
        {
            const string ZeroLiteral = "0";
            const string OneLiteral = "1";

            bool IsMagicNumber(SyntaxToken t) =>
                t.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.NumericLiteralToken)
                && t.ValueText != ZeroLiteral
                && t.ValueText != OneLiteral;

            var magicNumbers = method.DescendantTokens()
                .Where(IsMagicNumber)
                .Select(t => t.ValueText)
                .Distinct();

            foreach (var number in magicNumbers)
            {
                Console.WriteLine($"Magische Zahl {number} in Methode {method.Identifier.Text}");
            }
        }
    }

    private static async Task CheckNugetPackages(string projectPath)
    {
        var doc = new System.Xml.XmlDocument();
        doc.Load(projectPath);

        var nsMgr = new System.Xml.XmlNamespaceManager(doc.NameTable);
        nsMgr.AddNamespace("msb", "http://schemas.microsoft.com/developer/msbuild/2003");

        var packageRefs = doc.GetElementsByTagName("PackageReference");
        foreach (System.Xml.XmlNode node in packageRefs)
        {
            var name = node.Attributes["Include"]?.Value;
            var version = node.Attributes["Version"]?.Value;
            if (name == null || version == null) continue;

            // NuGet API: https://api.nuget.org/v3-flatcontainer/{package-lowercase}/index.json
            var url = $"https://api.nuget.org/v3-flatcontainer/{name.ToLowerInvariant()}/index.json";
            using var http = new System.Net.Http.HttpClient();
            try
            {
                var json = await http.GetStringAsync(url);
                var versions = System.Text.Json.JsonDocument.Parse(json).RootElement.GetProperty("versions");
                var latest = versions.EnumerateArray().Last().GetString();
                if (latest != null && latest != version)
                {
                    Console.WriteLine($"Paket {name}: installiert {version}, aktuell {latest} -> Update verfügbar!");
                }
                else
                {
                    Console.WriteLine($"Paket {name}: installiert {version}, aktuell.");
                }
            }
            catch
            {
                Console.WriteLine($"Paket {name}: Version konnte nicht geprüft werden.");
            }
        }
    }
}
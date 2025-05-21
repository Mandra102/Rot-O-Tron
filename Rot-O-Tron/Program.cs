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
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Bitte Befehl eingeben (z.B. checkProject --path:C:\\test\\test.csproj --checkMethodLength --checkMagicNumbers):");
            var input = Console.ReadLine() ?? "";
            args = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }

        var settings = SettingsProvider.GetSettings(args);

        if (string.IsNullOrEmpty(settings.ProjectPath) || !File.Exists(settings.ProjectPath))
        {
            Console.WriteLine("Gültigen Pfad mit --path:<PfadZurCsproj> angeben oder in appsettings.json setzen!");
            return;
        }

        List<VisualStudioInstance> instances = MSBuildLocator.QueryVisualStudioInstances().ToList();
        if (!instances.Any())
        {
            Console.WriteLine("Keine MSBuild Instanzen gefunden!");
            return;
        }
        foreach (VisualStudioInstance vsi in instances)
        {
            Console.WriteLine($"Gefunden: {vsi.Name} - {vsi.MSBuildPath}");
        }
        var instance = instances.First();
        MSBuildLocator.RegisterInstance(instance);
        Console.WriteLine($"MSBuild registriert: {instance.MSBuildPath}");

        using var workspace = MSBuildWorkspace.Create();

        try
        {
            Project project = await workspace.OpenProjectAsync(settings.ProjectPath);
            Console.WriteLine($"Projekt geladen: {project.Name}");
            Console.WriteLine($"Anzahl Dokumente: {project.DocumentIds.Count}");

            foreach (var document in project.Documents)
            {
                if (document.Name.EndsWith(".Designer.cs")) continue;

                Console.WriteLine($"Datei: {document.Name}");

                SyntaxTree? syntaxTree = await document.GetSyntaxTreeAsync();
                if (syntaxTree == null) continue;

                SyntaxNode root = await syntaxTree.GetRootAsync();
                IEnumerable<MethodDeclarationSyntax> methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

                foreach (var method in methods)
                {
                    AnalyzeMethod(method, settings.CheckMethodLength, settings.CheckMagicNumbers);
                }
            }
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
}
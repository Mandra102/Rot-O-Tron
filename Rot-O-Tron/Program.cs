using System;
using System.Linq;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Bitte Pfad zu einer .csproj Datei angeben");
            return;
        }

        var projectPath = args[0];

        var instances = MSBuildLocator.QueryVisualStudioInstances().ToList();
        if (!instances.Any())
        {
            Console.WriteLine("Keine MSBuild Instanzen gefunden!");
            return;
        }
        foreach (var i in instances)
        {
            Console.WriteLine($"Gefunden: {i.Name} - {i.MSBuildPath}");
        }
        var instance = instances.First();
        MSBuildLocator.RegisterInstance(instance);
        Console.WriteLine($"MSBuild registriert: {instance.MSBuildPath}");

        using var workspace = MSBuildWorkspace.Create();

        try
        {
            var project = await workspace.OpenProjectAsync(projectPath);
            Console.WriteLine($"Projekt geladen: {project.Name}");
            Console.WriteLine($"Anzahl Dokumente: {project.DocumentIds.Count}");

            foreach (var document in project.Documents)
            {
                if (document.Name.EndsWith(".Designer.cs")) continue;

                Console.WriteLine($"Datei: {document.Name}");

                var syntaxTree = await document.GetSyntaxTreeAsync();
                if (syntaxTree == null) continue;

                var root = await syntaxTree.GetRootAsync();
                var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

                foreach (var method in methods)
                {
                    AnalyzeMethod(method);
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fehler beim Laden des Projekts: {ex.Message}");
        }
    }

    private static void AnalyzeMethod(MethodDeclarationSyntax method)
    {
        var linesSpan = method.GetLocation().GetLineSpan();
        var length = linesSpan.EndLinePosition.Line - linesSpan.StartLinePosition.Line;

        if (length > 40)
        {
            Console.WriteLine($"Methode {method.Identifier.Text} ist {length} Zeilen lang.");
        }

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


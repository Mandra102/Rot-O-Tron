using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Rot_O_Tron
{
    internal class CheckMethodLength : IProjectCheck
    {
        public async Task RunAsync(Microsoft.CodeAnalysis.Project project, Settings settings)
        {
            if (!(settings.CheckMethodLength ?? false))
                return;

            foreach (var document in project.Documents)
            {
                if (document.Name.EndsWith(".Designer.cs")) continue;
                var syntaxTree = await document.GetSyntaxTreeAsync();
                if (syntaxTree == null) continue;
                var root = await syntaxTree.GetRootAsync();
                var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
                foreach (var method in methods)
                {
                    var linesSpan = method.GetLocation().GetLineSpan();
                    int length = linesSpan.EndLinePosition.Line - linesSpan.StartLinePosition.Line + 1;
                    if (length > settings.CheckMethodLengthDefault)
                    {
                        Console.WriteLine($"Methode {method.Identifier.Text} ist {length} Zeilen lang.");
                    }
                }
            }
        }
    }
}
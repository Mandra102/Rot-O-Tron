using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Rot_O_Tron.Settings
{
    internal class CheckMethodLength : IProjectCheck
    {
        public async Task RunOnDocumentAsync(Document document, Settings settings)
        {
            if (!(settings.CheckMethodLength ?? false))
                return;

            var syntaxTree = await document.GetSyntaxTreeAsync();
            if (syntaxTree == null)
                return;

            var root = await syntaxTree.GetRootAsync();
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
            foreach (var method in methods)
            {
                var linesSpan = method.GetLocation().GetLineSpan();
                int length = linesSpan.EndLinePosition.Line - linesSpan.StartLinePosition.Line + 1;
                if (length > settings.CheckMethodLengthDefault)
                {
                    Console.WriteLine($"Methode {method.Identifier.Text} ist {length} Zeilen lang in Datei {document.Name}.");
                }
            }
        }
    }
}
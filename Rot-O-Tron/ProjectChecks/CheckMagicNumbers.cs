using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Rot_O_Tron.Settings
{
    internal class CheckMagicNumbers : IProjectCheck
    {
        public async Task RunOnDocumentAsync(Document document, Settings settings)
        {
            if (!(settings.CheckMagicNumbers ?? false))
                return;

            if (document.Name.EndsWith(".Designer.cs"))
                return;

            var syntaxTree = await document.GetSyntaxTreeAsync();
            if (syntaxTree == null)
                return;

            var root = await syntaxTree.GetRootAsync();

            var literals = root.DescendantNodes()
                .OfType<LiteralExpressionSyntax>()
                .Where(l => l.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.NumericLiteralExpression))
                .Where(l => l.Token.ValueText != "0" && l.Token.ValueText != "1");

            foreach (var literal in literals)
            {
                Console.WriteLine($"Magische Zahl gefunden: {literal.Token.ValueText} in Datei {document.Name}");
            }
        }
    }
}
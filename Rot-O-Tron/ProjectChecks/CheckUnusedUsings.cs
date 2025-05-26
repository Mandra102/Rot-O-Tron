using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Rot_O_Tron.Settings
{
    internal class CheckUnusedUsings : IProjectCheck
    {
        public async Task RunOnDocumentAsync(Document document, Settings settings)
        {
            if (settings == null || !(settings.CheckUnusedUsings ?? false))
                return;

            if (document.Name.EndsWith(".Designer.cs"))
                return;

            var syntaxTree = await document.GetSyntaxTreeAsync();
            if (syntaxTree == null)
                return;

            var root = await syntaxTree.GetRootAsync();
            var compilation = await document.Project.GetCompilationAsync();
            if (compilation == null)
                return;

            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            var usings = root.DescendantNodes().OfType<UsingDirectiveSyntax>().ToList();

            foreach (var usingDirective in usings)
            {
                var nsSymbol = semanticModel.GetSymbolInfo(usingDirective.Name).Symbol as INamespaceSymbol;
                if (nsSymbol == null)
                    continue;

                bool isUsed = root.DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Select(id => semanticModel.GetSymbolInfo(id).Symbol)
                    .Where(s => s != null)
                    .Any(s => SymbolBelongsToNamespace(s, nsSymbol));

                if (!isUsed)
                {
                    Console.WriteLine($"Unbenutztes using: {usingDirective.Name} in Datei {document.Name}");
                }
            }
        }

        private static bool SymbolBelongsToNamespace(ISymbol symbol, INamespaceSymbol ns)
        {
            var containing = symbol.ContainingNamespace;
            while (containing != null && !containing.IsGlobalNamespace)
            {
                if (SymbolEqualityComparer.Default.Equals(containing, ns))
                    return true;
                containing = containing.ContainingNamespace;
            }
            return false;
        }
    }
}
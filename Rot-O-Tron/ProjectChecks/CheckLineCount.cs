using Microsoft.CodeAnalysis;

namespace Rot_O_Tron.Settings
{
    internal class CheckLineCount : IProjectCheck
    {
        public async Task RunOnDocumentAsync(Document document, Settings settings)
        {
            if (!(settings.CountLines ?? false))
                return;

            if (document.Name.EndsWith(".Designer.cs"))
                return;

            var text = await document.GetTextAsync();
            Console.WriteLine($"Datei {document.Name}: {text.Lines.Count} Zeilen");
        }
    }
}
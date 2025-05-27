using Microsoft.CodeAnalysis;
using Spectre.Console;

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
            //Console.WriteLine($"Datei {document.Name}: {text.Lines.Count} Zeilen");
            AnsiConsole.MarkupLine($"[blue]Datei {document.Name}:[/] [green]{text.Lines.Count} Zeilen[/]");
        }
    }
}
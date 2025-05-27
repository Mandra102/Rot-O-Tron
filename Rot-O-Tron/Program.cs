using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis;
using Rot_O_Tron.Settings;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var cliSettings = ArgumentParser.Parse(args);
        var settings = SettingsLoader.LoadAndMerge(cliSettings);

        if (!SettingsLoader.Validate(settings))
            return 1;

        await AnalyzeProject(settings);
        return 0;
    }

    private static async Task AnalyzeProject(Settings settings)
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

        // Automatically find and instantiate all checks via reflection
        var checkTypes = typeof(Program).Assembly
            .GetTypes()
            .Where(t => typeof(IProjectCheck).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        var checks = checkTypes
            .Select(t => Activator.CreateInstance(t) as IProjectCheck)
            .Where(c => c != null)
            .ToList();

        try
        {
            Project project = await workspace.OpenProjectAsync(settings.ProjectPath!);
            Console.WriteLine($"Projekt geladen: {project.Name}");
            Console.WriteLine($"Anzahl Dokumente: {project.DocumentIds.Count}");

            foreach (var document in project.Documents)
            {
                if (document.Name.EndsWith(".Designer.cs"))
                    continue;

                foreach (var check in checks)
                {
                    await check!.RunOnDocumentAsync(document, settings);
                }
            }

            if (settings.CheckNuget ?? false)
                await CheckNugetPackages(settings.ProjectPath!);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fehler beim Laden des Projekts: {ex.Message}");
            return;
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
            if (node.Attributes == null) 
                continue;

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
                    Console.WriteLine($"Paket {name}: installiert {version}, aktuell {latest} -> Update verfügbar!");
                else
                    Console.WriteLine($"Paket {name}: installiert {version}, aktuell.");
            }
            catch
            {
                Console.WriteLine($"Paket {name}: Version konnte nicht geprüft werden.");
            }
        }
    }

}
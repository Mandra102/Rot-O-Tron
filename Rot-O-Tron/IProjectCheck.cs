using Microsoft.CodeAnalysis;

public interface IProjectCheck
{
    Task RunAsync(Project project, Settings settings);
}
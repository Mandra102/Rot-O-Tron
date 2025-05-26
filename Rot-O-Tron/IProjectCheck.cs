using Microsoft.CodeAnalysis;

namespace Rot_O_Tron.Settings
{
    internal interface IProjectCheck
    {
        Task RunOnDocumentAsync(Document document, Settings settings);
    }
}
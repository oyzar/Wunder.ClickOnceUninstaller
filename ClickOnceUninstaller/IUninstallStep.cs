using Microsoft.Extensions.Logging;

namespace ClickOnceUninstaller;

public interface IUninstallStep : IDisposable
{
    void Prepare(List<string> componentsToRemove);

    void PrintDebugInformation(ILogger logger);

    void Execute(ILogger logger);
}
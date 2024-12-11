using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ClickOnceUninstaller;

public class StopProcesses(string mainProcessName) : IUninstallStep
{
    private List<Process> _processes;

    public void Prepare(
        List<string> componentsToRemove
    )
    {
        _processes = Process.GetProcessesByName(mainProcessName).Where(
            p => p.MainModule?.FileName != null && componentsToRemove.Any(component => p.MainModule.FileName.Contains(component))
        ).ToList();
    }

    public void PrintDebugInformation(
        ILogger logger
    )
    {
        foreach (var process in _processes)
        {
            logger.LogDebug("Going to kill {Process}", process.MainModule!.FileName);
        }
    }

    public void Execute(
        ILogger logger
    )
    {
        foreach (var process in _processes)
        {
            try
            {
                process.Kill(true);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to kill clickonce process");
            }
        }
    }
    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
        _processes = null;
    }
}
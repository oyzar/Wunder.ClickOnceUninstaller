using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace ClickOnceUninstaller;

[SupportedOSPlatform("windows")]
public class RemoveUninstallEntry(UninstallInfo uninstallInfo) : IUninstallStep
{
    private RegistryKey _uninstall;

    public void Prepare(List<string> componentsToRemove)
    {
        _uninstall = Registry.CurrentUser.OpenSubKey(UninstallInfo.UninstallRegistryPath, true);
    }

    public void PrintDebugInformation(
        ILogger logger
    )
    {
        if (_uninstall == null)
        {
            throw new InvalidOperationException("Call Prepare() first.");
        }

        logger.LogDebug("Remove uninstall info from {Key}", uninstallInfo.Key);
    }

    public void Execute(ILogger logger)
    {
        if (_uninstall == null)
        {
            throw new InvalidOperationException("Call Prepare() first.");
        }

        _uninstall.DeleteSubKey(uninstallInfo.Key);
    }

    public void Dispose()
    {
        if (_uninstall != null)
        {
            _uninstall.Close();
            _uninstall = null;
        }
    }
}
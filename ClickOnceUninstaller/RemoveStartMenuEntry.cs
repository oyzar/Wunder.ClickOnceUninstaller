using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace ClickOnceUninstaller;

[SupportedOSPlatform("windows")]
public class RemoveStartMenuEntry(UninstallInfo uninstallInfo) : IUninstallStep
{
    private List<string> _foldersToRemove;
    private List<string> _filesToRemove;

    public void Prepare(List<string> componentsToRemove)
    {
        var programsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
        var folder = Path.Combine(programsFolder, uninstallInfo.ShortcutFolderName);
        var shortcut = Path.Combine(folder, uninstallInfo.ShortcutFileName + ".appref-ms");
        var supportShortcut = Path.Combine(folder, uninstallInfo.SupportShortcutFileName + ".url");

        var desktopFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var desktopShortcut = Path.Combine(desktopFolder, uninstallInfo.ShortcutFileName + ".appref-ms");

        _filesToRemove = [];
        if (File.Exists(shortcut))
        {
            _filesToRemove.Add(shortcut);
        }

        if (File.Exists(supportShortcut))
        {
            _filesToRemove.Add(supportShortcut);
        }

        if (File.Exists(desktopShortcut))
        {
            _filesToRemove.Add(desktopShortcut);
        }

        _foldersToRemove = [];
        if (Directory.Exists(folder) && Directory.GetFiles(folder).All(d => _filesToRemove.Contains(d)))
        {
            _foldersToRemove.Add(folder);
        }
    }

    public void PrintDebugInformation(
        ILogger logger
    )
    {
        if (_foldersToRemove == null)
        {
            throw new InvalidOperationException("Call Prepare() first.");
        }

        var programsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Programs);

        logger.LogDebug("Remove start menu entries from {Folder}", programsFolder);

        foreach (var file in _filesToRemove)
        {
            logger.LogDebug("Delete file {File}", file);
        }

        foreach (var folder in _foldersToRemove)
        {
            logger.LogDebug("Delete folder {Folder}", folder);
        }
    }

    public void Execute(ILogger logger)
    {
        if (_foldersToRemove == null)
        {
            throw new InvalidOperationException("Call Prepare() first.");
        }

        foreach (var file in _filesToRemove)
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failure while trying to remove start menu entries {File}", file);
            }
        }

        foreach (var folder in _foldersToRemove)
        {
            try
            {
                Directory.Delete(folder, false);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failure while trying to remove start menu entries {Folder}", folder);
            }
        }
    }

    public void Dispose()
    {
    }
}
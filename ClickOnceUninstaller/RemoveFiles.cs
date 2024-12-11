using Microsoft.Extensions.Logging;

namespace ClickOnceUninstaller;

public class RemoveFiles : IUninstallStep
{
    private List<string> _clickOnceFolders;
    private List<string> _foldersToRemove;
    private List<string> _filesToRemove;

    public void Prepare(List<string> componentsToRemove)
    {
        _clickOnceFolders = FindClickOnceFolders().ToList();

        _foldersToRemove = [];
        _filesToRemove = [];
        foreach (var clickOnceFolder in _clickOnceFolders)
        {
            foreach (var directory in Directory.GetDirectories(clickOnceFolder))
            {
                if (componentsToRemove.Contains(Path.GetFileName(directory)))
                {
                    _foldersToRemove.Add(directory);
                }
            }

            var manifests = Path.Combine(clickOnceFolder, "manifests");
            if (Directory.Exists(manifests))
            {
                foreach (var file in Directory.GetFiles(manifests))
                {
                    if (componentsToRemove.Contains(Path.GetFileNameWithoutExtension(file)))
                    {
                        _filesToRemove.Add(file);
                    }
                }
            }
        }
    }

    public void PrintDebugInformation(
        ILogger logger
    )
    {
        foreach (var folder in _foldersToRemove)
        {
            logger.LogDebug("Delete folder {Folder}", folder);
        }

        foreach (var file in _filesToRemove)
        {
            logger.LogDebug("Delete file {File}", file);
        }
    }

    public void Execute(ILogger logger)
    {
        foreach (var file in _filesToRemove)
        {
            try
            {
                RetryHelpers.WithRetries(() => File.Delete(file), logger);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Unable to delete file {File}", file);
            }
        }

        foreach (var folder in _foldersToRemove)
        {
            try
            {
                RetryHelpers.WithRetries(() => Directory.Delete(folder, true), logger);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Unable to delete folder {Folder}", folder);
            }
        }
    }

    private static IEnumerable<string> FindClickOnceFolders()
    {
        var apps20Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Apps\2.0");
        if (!Directory.Exists(apps20Folder))
        {
            throw new ArgumentException("Could not find ClickOnce folder");
        }

        foreach (var subFolder in Directory.GetDirectories(apps20Folder))
        {
            //clickonce creates random folders that are 12 characters long with another folder inside that is also randomly 12 characters long
            if (Path.GetFileName(subFolder).Length == 12)
            {
                foreach (var subSubFolder in Directory.GetDirectories(subFolder))
                {
                    if (Path.GetFileName(subSubFolder).Length == 12)
                    {
                        yield return subSubFolder;
                    }
                }
            }
        }
    }

    public void Dispose()
    {
    }
}
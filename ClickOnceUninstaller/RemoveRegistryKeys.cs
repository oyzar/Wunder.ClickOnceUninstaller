using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace ClickOnceUninstaller;

[SupportedOSPlatform("windows")]
public class RemoveRegistryKeys(ClickOnceRegistry registry, UninstallInfo uninstallInfo, string firstFour, string lastFour) : IUninstallStep
{
    private const string PackageMetadataRegistryPath = @"Software\Classes\Software\Microsoft\Windows\CurrentVersion\Deployment\SideBySide\2.0\PackageMetadata";
    private const string ApplicationsRegistryPath = @"Software\Classes\Software\Microsoft\Windows\CurrentVersion\Deployment\SideBySide\2.0\StateManager\Applications";
    private const string FamiliesRegistryPath = @"Software\Classes\Software\Microsoft\Windows\CurrentVersion\Deployment\SideBySide\2.0\StateManager\Families";
    private const string VisibilityRegistryPath = @"Software\Classes\Software\Microsoft\Windows\CurrentVersion\Deployment\SideBySide\2.0\Visibility";

    private readonly List<IDisposable> _disposables = [];
    private List<RegistryMarker> _keysToRemove;
    private List<RegistryMarker> _valuesToRemove;

    public void Prepare(List<string> componentsToRemove)
    {
        _keysToRemove = [];
        _valuesToRemove = [];

        var componentsKey = Registry.CurrentUser.OpenSubKey(ClickOnceRegistry.ComponentsRegistryPath, true);
        if (componentsKey != null)
        {
            _disposables.Add(componentsKey);
            foreach (var component in registry.Components)
            {
                if (componentsToRemove.Contains(component.Key))
                {
                    _keysToRemove.Add(new RegistryMarker(componentsKey, component.Key));
                }
            }
        }

        var marksKey = Registry.CurrentUser.OpenSubKey(ClickOnceRegistry.MarksRegistryPath, true);
        if (marksKey != null)
        {
            _disposables.Add(marksKey);
            foreach (var mark in registry.Marks)
            {
                if (componentsToRemove.Contains(mark.Key))
                {
                    _keysToRemove.Add(new RegistryMarker(marksKey, mark.Key));
                }
                else
                {
                    var implications = mark.Implications.Where(
                        implication => componentsToRemove
                            .Any(component => component == implication.Name)
                    ).ToList();
                    if (implications.Any())
                    {
                        var markKey = marksKey.OpenSubKey(mark.Key, true);
                        _disposables.Add(markKey);

                        foreach (var implication in implications)
                        {
                            _valuesToRemove.Add(new RegistryMarker(markKey, implication.Key));
                        }
                    }
                }
            }
        }

        var token = uninstallInfo.GetPublicKeyToken();

        var packageMetadata = Registry.CurrentUser.OpenSubKey(PackageMetadataRegistryPath);
        if (packageMetadata != null)
        {
            _disposables.Add(packageMetadata);
            foreach (var keyName in packageMetadata.GetSubKeyNames())
            {
                FindMatchingSubKeysToDelete(PackageMetadataRegistryPath + "\\" + keyName, token);
            }
        }

        FindMatchingSubKeysToDelete(ApplicationsRegistryPath, token);
        FindMatchingSubKeysToDelete(FamiliesRegistryPath, token);
        FindMatchingSubKeysToDelete(VisibilityRegistryPath, token);
    }

    private void FindMatchingSubKeysToDelete(
        string registryPath,
        string token
    )
    {
        var key = Registry.CurrentUser.OpenSubKey(registryPath, true);
        if (key == null)
        {
            return;
        }
        _disposables.Add(key);
        foreach (var subKeyName in key.GetSubKeyNames())
        {
            if (subKeyName.Contains(token) && subKeyName.Contains(firstFour) && subKeyName.Contains(lastFour))
            {
                _keysToRemove.Add(new RegistryMarker(key, subKeyName));
            }
        }
    }

    public void PrintDebugInformation(
        ILogger logger
    )
    {
        if (_keysToRemove == null)
        {
            throw new InvalidOperationException("Call Prepare() first.");
        }

        foreach (var key in _keysToRemove)
        {
            logger.LogDebug("Delete key {Parent} in {ItemName}", key.Parent, key.ItemName);
        }

        foreach (var value in _valuesToRemove)
        {
            logger.LogDebug("Delete value {Parent} in {ItemName}", value.Parent, value.ItemName);
        }
    }

    public void Execute(ILogger logger)
    {
        if (_keysToRemove == null)
        {
            throw new InvalidOperationException("Call Prepare() first.");
        }

        foreach (var key in _keysToRemove)
        {
            key.Parent.DeleteSubKeyTree(key.ItemName, false);
        }

        foreach (var value in _valuesToRemove)
        {
            value.Parent.DeleteValue(value.ItemName, false);
        }
    }

    public void Dispose()
    {
        _disposables.ForEach(d => d.Dispose());
        _disposables.Clear();

        _keysToRemove = null;
        _valuesToRemove = null;
    }

    private record RegistryMarker(RegistryKey Parent, string ItemName);
}
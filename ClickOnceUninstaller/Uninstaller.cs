using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace ClickOnceUninstaller;

[SupportedOSPlatform("windows")]
public class Uninstaller(ClickOnceRegistry registry, ILogger logger, string mainProcessName)
{
    public Uninstaller(ILogger logger, string mainProcessName)
        : this(new ClickOnceRegistry(), logger, mainProcessName)
    {
    }

    public void Uninstall(UninstallInfo uninstallInfo)
    {
        var firstFour = uninstallInfo.DisplayName[..4].ToLower();
        var lastFour = new string(uninstallInfo.DisplayName.Where(char.IsLetter).ToArray())[^4..].ToLower();
        var toRemove = FindComponentsToRemove(uninstallInfo.GetPublicKeyToken(), firstFour, lastFour);

        logger.LogDebug("Uninstall: Components to remove:");
        foreach (var component in toRemove)
        {
            logger.LogDebug("Removing: {Component}", component);
        }

        var steps = new List<IUninstallStep>
        {
            new StopProcesses(mainProcessName),
            new RemoveFiles(),
            new RemoveStartMenuEntry(uninstallInfo),
            new RemoveRegistryKeys(registry, uninstallInfo, firstFour, lastFour),
            new RemoveUninstallEntry(uninstallInfo),
        };

        try
        {
            foreach (var step in steps)
            {
                step.Prepare(toRemove);
            }
            foreach (var step in steps)
            {
                step.PrintDebugInformation(logger);
            }
            foreach (var step in steps)
            {
                step.Execute(logger);
            }
        }
        finally
        {
            foreach (var step in steps)
            {
                step.Dispose();
            }
        }
    }

    private List<string> FindComponentsToRemove(
        string token,
        string firstFour,
        string lastFour
    )
    {
        var components = registry.Components.Where(
            c => c.Key.Contains(token)
              && c.Key.Contains(firstFour)
              && c.Key.Contains(lastFour)
        ).ToList();

        var toRemove = new List<string>();
        foreach (var component in components)
        {
            toRemove.Add(component.Key);

            foreach (var dependency in component.Dependencies)
            {
                if (toRemove.Contains(dependency))
                {
                    continue; // already in the list
                }

                if (registry.Components.All(c => c.Key != dependency))
                {
                    continue; // not a public component
                }

                var mark = registry.Marks.FirstOrDefault(m => m.Key == dependency);
                if (mark != null && mark.Implications.Any(i => components.All(c => c.Key != i.Name)))
                {
                    // don't remove because other apps depend on this
                    continue;
                }

                toRemove.Add(dependency);
            }
        }

        return toRemove;
    }
}